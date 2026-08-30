using AuthNexus.Domain;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Notifications;
using System.Text.Json;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Notifications.Tests;

public sealed class NotificationOutboxMessageTests
{
    private static readonly NotificationOutboxMessageId MessageId = new(
        Guid.Parse("a8a16afd-8f8a-4c41-9945-0065c8f499d0"));

    private static readonly CorrelationId CorrelationId = new(
        Guid.Parse("af84a43c-7766-4870-81d8-029569998b10"));

    private static readonly UserId TargetUserId = new(
        Guid.Parse("bf994b60-3ca4-45b6-9c62-479404a6dc20"));

    private static readonly ApplicationId ApplicationId = new(
        Guid.Parse("12a4f215-67b4-4494-9626-36b85e938d3f"));

    private static readonly TenantId TenantId = new(
        Guid.Parse("b34d04e9-8e56-4b24-aabe-65095370d184"));

    private static readonly NotificationType NotificationType = new("security.password_changed");
    private static readonly NotificationDestination Destination = new("nana@example.test");
    private static readonly ProtectedNotificationPayload ProtectedPayload =
        ProtectedNotificationPayload.Create(
            Enumerable.Range(1, 32).Select(value => (byte)value).ToArray(),
            "local-key:v1",
            formatVersion: 1);

    private static readonly NotificationDeliveryFailureCode TransientFailure = new(
        "provider.temporarily_unavailable");

    private static readonly NotificationDeliveryFailureCode PermanentFailure = new(
        "destination.rejected");

    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        8,
        30,
        13,
        0,
        0,
        TimeSpan.FromHours(2));

    private static readonly DateTimeOffset AvailableAt = CreatedAt.AddMinutes(5);

    private static readonly NotificationOutboxState[] ExpectedStates =
    [
        NotificationOutboxState.Pending,
        NotificationOutboxState.RetryScheduled,
        NotificationOutboxState.Delivered,
        NotificationOutboxState.PermanentlyFailed,
    ];

    private static readonly NotificationChannel[] ExpectedChannels =
    [
        NotificationChannel.Email,
        NotificationChannel.Sms,
        NotificationChannel.WhatsApp,
    ];

    private static readonly DeliveryActionDefinition[] ActionDefinitions =
    [
        new(
            DeliveryAction.RecordDelivered,
            NotificationOutboxState.Delivered,
            [NotificationOutboxState.Pending, NotificationOutboxState.RetryScheduled]),
        new(
            DeliveryAction.ScheduleRetry,
            NotificationOutboxState.RetryScheduled,
            [NotificationOutboxState.Pending, NotificationOutboxState.RetryScheduled]),
        new(
            DeliveryAction.FailPermanently,
            NotificationOutboxState.PermanentlyFailed,
            [NotificationOutboxState.Pending, NotificationOutboxState.RetryScheduled]),
    ];

    public enum DeliveryAction
    {
        RecordDelivered,
        ScheduleRetry,
        FailPermanently,
    }

    public enum OptionalContext
    {
        TargetUser,
        Application,
        Tenant,
    }

    public static TheoryData<NotificationOutboxState, DeliveryAction, NotificationOutboxState>
        LegalActionCases
    {
        get
        {
            var cases = new TheoryData<NotificationOutboxState, DeliveryAction,
                NotificationOutboxState>();

            foreach (var definition in ActionDefinitions)
            {
                foreach (var sourceState in definition.LegalSources)
                {
                    cases.Add(sourceState, definition.Action, definition.RequestedState);
                }
            }

            return cases;
        }
    }

    public static TheoryData<NotificationOutboxState, DeliveryAction, NotificationOutboxState>
        ForbiddenActionCases
    {
        get
        {
            var cases = new TheoryData<NotificationOutboxState, DeliveryAction,
                NotificationOutboxState>();

            foreach (var sourceState in ExpectedStates)
            {
                foreach (var definition in ActionDefinitions)
                {
                    if (!definition.LegalSources.Contains(sourceState))
                    {
                        cases.Add(sourceState, definition.Action, definition.RequestedState);
                    }
                }
            }

            return cases;
        }
    }

    [Fact]
    public void Create_stores_a_protected_delivery_envelope_and_initial_schedule()
    {
        var message = CreateMessage(
            targetUserId: TargetUserId,
            applicationId: ApplicationId,
            tenantId: TenantId);

        Assert.Equal(MessageId, message.MessageId);
        Assert.Equal(CorrelationId, message.CorrelationId);
        Assert.Equal(TargetUserId, message.TargetUserId);
        Assert.Equal(ApplicationId, message.ApplicationId);
        Assert.Equal(TenantId, message.TenantId);
        Assert.Equal(NotificationType, message.NotificationType);
        Assert.Equal(NotificationChannel.Email, message.Channel);
        Assert.Equal(Destination, message.Destination);
        Assert.Same(ProtectedPayload, message.ProtectedPayload);
        Assert.Equal(NotificationOutboxState.Pending, message.State);
        Assert.Equal(CreatedAt.ToUniversalTime(), message.CreatedAt);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.AvailableAt);
        Assert.Equal(message.CreatedAt, message.StateChangedAt);
        Assert.Equal(0, message.AttemptCount);
        Assert.Null(message.LastAttemptedAt);
        Assert.Equal(message.AvailableAt, message.NextAttemptAt);
        Assert.Null(message.DeliveredAt);
        Assert.Null(message.PermanentlyFailedAt);
        Assert.Null(message.LastFailureCode);
        Assert.Equal(TimeSpan.Zero, message.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, message.AvailableAt.Offset);
    }

    [Fact]
    public void Create_allows_an_envelope_without_existing_domain_context()
    {
        var message = CreateMessage();

        Assert.Null(message.TargetUserId);
        Assert.Null(message.ApplicationId);
        Assert.Null(message.TenantId);
    }

    [Fact]
    public void Strong_message_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationOutboxMessageId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Strong_message_id_uses_canonical_guid_text()
    {
        Assert.Equal(MessageId.Value.ToString("D"), MessageId.ToString());
    }

    [Fact]
    public void State_and_channel_vocabularies_are_fixed_and_one_based()
    {
        Assert.Equal(ExpectedStates, Enum.GetValues<NotificationOutboxState>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedStates.Length),
            ExpectedStates.Select(state => (int)state));
        Assert.Equal(ExpectedChannels, Enum.GetValues<NotificationChannel>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedChannels.Length),
            ExpectedChannels.Select(channel => (int)channel));
    }

    [Theory]
    [InlineData("security.password_changed", "security.password_changed")]
    [InlineData("  SECURITY.Password_Changed  ", "security.password_changed")]
    [InlineData("verification-email", "verification-email")]
    public void Notification_type_is_canonical(string input, string expected)
    {
        var notificationType = new NotificationType(input);

        Assert.Equal(expected, notificationType.Value);
        Assert.Equal(expected, notificationType.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1invalid")]
    [InlineData("invalid type")]
    [InlineData("invalid/type")]
    public void Notification_type_rejects_invalid_values(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new NotificationType(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Notification_type_is_bounded()
    {
        Assert.Equal(
            NotificationType.MaximumLength,
            new NotificationType($"a{new string('b', NotificationType.MaximumLength - 1)}")
                .Value.Length);

        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationType($"a{new string('b', NotificationType.MaximumLength)}"));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Destination_requires_explicit_delivery_reveal_and_stays_redacted_in_diagnostics()
    {
        var destination = new NotificationDestination("  nana@example.test  ");

        Assert.Equal("nana@example.test", destination.RevealForDelivery());
        Assert.Equal("[notification-destination]", destination.ToString());
        Assert.DoesNotContain(
            "nana@example.test",
            JsonSerializer.Serialize(destination),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    [InlineData("nana @example.test")]
    [InlineData("nana\u2028@example.test")]
    [InlineData("nana\u200B@example.test")]
    [InlineData("nana\u202E@example.test")]
    [InlineData("nana\U000E0001@example.test")]
    public void Destination_rejects_invalid_values(string value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationDestination(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Destination_rejects_an_unpaired_surrogate()
    {
        var value = string.Concat("nana", '\uD800', "@example.test");

        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationDestination(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Destination_is_bounded()
    {
        Assert.Equal(
            NotificationDestination.MaximumLength,
            new NotificationDestination(new string('d', NotificationDestination.MaximumLength))
                .RevealForDelivery()
                .Length);

        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationDestination(
                new string('d', NotificationDestination.MaximumLength + 1)));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("provider.temporarily_unavailable", "provider.temporarily_unavailable")]
    [InlineData("  PROVIDER.Timeout  ", "provider.timeout")]
    public void Failure_code_is_machine_readable(string input, string expected)
    {
        var code = new NotificationDeliveryFailureCode(input);

        Assert.Equal(expected, code.Value);
        Assert.Equal(expected, code.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1invalid")]
    [InlineData("invalid code")]
    [InlineData("invalid/code")]
    public void Failure_code_rejects_invalid_values(string value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationDeliveryFailureCode(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Failure_code_is_bounded()
    {
        Assert.Equal(
            NotificationDeliveryFailureCode.MaximumLength,
            new NotificationDeliveryFailureCode(
                $"a{new string('b', NotificationDeliveryFailureCode.MaximumLength - 1)}")
                .Value.Length);

        var exception = Assert.Throws<ArgumentException>(
            () => new NotificationDeliveryFailureCode(
                $"a{new string('b', NotificationDeliveryFailureCode.MaximumLength)}"));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Protected_payload_defensively_copies_input_and_output()
    {
        var source = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        var expected = source.ToArray();
        var payload = ProtectedNotificationPayload.Create(source, "key:v1", 2);

        source[0] = 255;
        var firstCopy = payload.CopyCiphertext();
        firstCopy[1] = 255;

        Assert.Equal(expected, payload.CopyCiphertext());
        Assert.Equal("key:v1", payload.ProtectionKeyId);
        Assert.Equal(2, payload.FormatVersion);
        Assert.Equal(expected.Length, payload.CiphertextLength);
        Assert.Equal(
            $"[protected-notification-payload:{expected.Length}]",
            payload.ToString());
    }

    [Fact]
    public void Protected_payload_requires_ciphertext()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProtectedNotificationPayload.Create(Array.Empty<byte>(), "key:v1", 1));

        Assert.Equal("ciphertext", exception.ParamName);
    }

    [Fact]
    public void Protected_payload_is_bounded()
    {
        Assert.Equal(
            ProtectedNotificationPayload.MaximumCiphertextLength,
            ProtectedNotificationPayload.Create(
                new byte[ProtectedNotificationPayload.MaximumCiphertextLength],
                "key:v1",
                1).CiphertextLength);

        var exception = Assert.Throws<ArgumentException>(
            () => ProtectedNotificationPayload.Create(
                new byte[ProtectedNotificationPayload.MaximumCiphertextLength + 1],
                "key:v1",
                1));

        Assert.Equal("ciphertext", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad key")]
    [InlineData("bad/key")]
    public void Protected_payload_rejects_invalid_key_ids(string keyId)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProtectedNotificationPayload.Create([1, 2, 3], keyId, 1));

        Assert.Equal("protectionKeyId", exception.ParamName);
    }

    [Fact]
    public void Protected_payload_key_id_is_bounded()
    {
        Assert.Equal(
            ProtectedNotificationPayload.MaximumProtectionKeyIdLength,
            ProtectedNotificationPayload.Create(
                [1],
                new string('k', ProtectedNotificationPayload.MaximumProtectionKeyIdLength),
                1).ProtectionKeyId.Length);

        var exception = Assert.Throws<ArgumentException>(
            () => ProtectedNotificationPayload.Create(
                [1],
                new string('k', ProtectedNotificationPayload.MaximumProtectionKeyIdLength + 1),
                1));

        Assert.Equal("protectionKeyId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Protected_payload_requires_a_positive_format_version(int formatVersion)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ProtectedNotificationPayload.Create([1], "key:v1", formatVersion));

        Assert.Equal("formatVersion", exception.ParamName);
    }

    [Theory]
    [InlineData("messageId")]
    [InlineData("correlationId")]
    [InlineData("notificationType")]
    [InlineData("destination")]
    public void Create_rejects_a_default_required_value(string parameter)
    {
        var exception = parameter switch
        {
            "messageId" => Assert.Throws<ArgumentException>(
                () => CreateMessage(messageId: default(NotificationOutboxMessageId))),
            "correlationId" => Assert.Throws<ArgumentException>(
                () => CreateMessage(correlationId: default(CorrelationId))),
            "notificationType" => Assert.Throws<ArgumentException>(
                () => CreateMessage(notificationType: default(NotificationType))),
            "destination" => Assert.Throws<ArgumentException>(
                () => CreateMessage(destination: default(NotificationDestination))),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
        };

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Create_rejects_an_undefined_channel()
    {
        const NotificationChannel undefinedChannel = (NotificationChannel)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateMessage(channel: undefinedChannel));

        Assert.Equal("channel", exception.ParamName);
    }

    [Theory]
    [InlineData(OptionalContext.TargetUser)]
    [InlineData(OptionalContext.Application)]
    [InlineData(OptionalContext.Tenant)]
    public void Supplied_optional_context_cannot_use_a_default_id(OptionalContext context)
    {
        var exception = context switch
        {
            OptionalContext.TargetUser => Assert.Throws<ArgumentException>(
                () => CreateMessage(targetUserId: default(UserId))),
            OptionalContext.Application => Assert.Throws<ArgumentException>(
                () => CreateMessage(applicationId: default(ApplicationId))),
            OptionalContext.Tenant => Assert.Throws<ArgumentException>(
                () => CreateMessage(tenantId: default(TenantId))),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
        };

        Assert.Equal(
            context switch
            {
                OptionalContext.TargetUser => "targetUserId",
                OptionalContext.Application => "applicationId",
                OptionalContext.Tenant => "tenantId",
                _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
            },
            exception.ParamName);
    }

    [Fact]
    public void Create_rejects_null_protected_payload()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => NotificationOutboxMessage.Create(
                MessageId,
                CorrelationId,
                targetUserId: null,
                applicationId: null,
                tenantId: null,
                NotificationType,
                NotificationChannel.Email,
                Destination,
                protectedPayload: null!,
                CreatedAt,
                AvailableAt));

        Assert.Equal("protectedPayload", exception.ParamName);
    }

    [Theory]
    [InlineData("createdAt")]
    [InlineData("availableAt")]
    public void Create_rejects_a_default_timestamp(string parameter)
    {
        var exception = parameter switch
        {
            "createdAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateMessage(createdAt: default(DateTimeOffset))),
            "availableAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateMessage(availableAt: default(DateTimeOffset))),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
        };

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Availability_cannot_precede_creation()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateMessage(availableAt: CreatedAt.AddTicks(-1)));

        Assert.Equal("availableAt", exception.ParamName);
    }

    [Fact]
    public void Immediate_availability_is_allowed()
    {
        var message = CreateMessage(availableAt: CreatedAt);

        Assert.Equal(message.CreatedAt, message.AvailableAt);
        Assert.Equal(message.AvailableAt, message.NextAttemptAt);
    }

    [Fact]
    public void Due_query_is_inclusive_and_does_not_mutate_the_message()
    {
        var message = CreateMessage();
        var before = Snapshot(message);

        Assert.False(message.CanBeAttemptedAt(AvailableAt.AddTicks(-1)));
        Assert.True(message.CanBeAttemptedAt(AvailableAt));
        Assert.True(message.CanBeAttemptedAt(AvailableAt.AddTicks(1)));
        Assert.Equal(before, Snapshot(message));
    }

    [Fact]
    public void Due_query_rejects_a_default_timestamp()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateMessage().CanBeAttemptedAt(default));

        Assert.Equal("observedAt", exception.ParamName);
    }

    [Fact]
    public void Delivered_outcome_records_one_attempt_and_only_delivery_terminal_data()
    {
        var message = CreateMessage();

        message.RecordDelivered(AvailableAt);

        Assert.Equal(NotificationOutboxState.Delivered, message.State);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.LastAttemptedAt);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.DeliveredAt);
        Assert.Null(message.NextAttemptAt);
        Assert.Null(message.PermanentlyFailedAt);
        Assert.Null(message.LastFailureCode);
        Assert.False(message.CanBeAttemptedAt(AvailableAt.AddMinutes(1)));
    }

    [Fact]
    public void Retry_outcome_records_failure_and_a_strictly_later_schedule()
    {
        var message = CreateMessage();
        var nextAttemptAt = AvailableAt.AddMinutes(5);

        message.ScheduleRetry(AvailableAt, nextAttemptAt, TransientFailure);

        Assert.Equal(NotificationOutboxState.RetryScheduled, message.State);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.LastAttemptedAt);
        Assert.Equal(nextAttemptAt.ToUniversalTime(), message.NextAttemptAt);
        Assert.Equal(TransientFailure, message.LastFailureCode);
        Assert.Null(message.DeliveredAt);
        Assert.Null(message.PermanentlyFailedAt);
    }

    [Fact]
    public void Permanent_failure_records_one_attempt_and_only_failure_terminal_data()
    {
        var message = CreateMessage();

        message.FailPermanently(AvailableAt, PermanentFailure);

        Assert.Equal(NotificationOutboxState.PermanentlyFailed, message.State);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.LastAttemptedAt);
        Assert.Equal(AvailableAt.ToUniversalTime(), message.PermanentlyFailedAt);
        Assert.Equal(PermanentFailure, message.LastFailureCode);
        Assert.Null(message.NextAttemptAt);
        Assert.Null(message.DeliveredAt);
        Assert.False(message.CanBeAttemptedAt(AvailableAt.AddMinutes(1)));
    }

    [Fact]
    public void Successful_retry_clears_the_previous_failure_code()
    {
        var message = CreateMessage();
        var retryAt = AvailableAt.AddMinutes(5);
        message.ScheduleRetry(AvailableAt, retryAt, TransientFailure);

        message.RecordDelivered(retryAt);

        Assert.Equal(NotificationOutboxState.Delivered, message.State);
        Assert.Equal(2, message.AttemptCount);
        Assert.Equal(retryAt.ToUniversalTime(), message.DeliveredAt);
        Assert.Null(message.LastFailureCode);
    }

    [Fact]
    public void Multiple_retries_keep_attempts_and_schedules_monotonic()
    {
        var message = CreateMessage();
        var secondAttemptAt = AvailableAt.AddMinutes(5);
        var thirdAttemptAt = secondAttemptAt.AddMinutes(10);

        message.ScheduleRetry(AvailableAt, secondAttemptAt, TransientFailure);
        message.ScheduleRetry(secondAttemptAt, thirdAttemptAt, TransientFailure);

        Assert.Equal(2, message.AttemptCount);
        Assert.Equal(secondAttemptAt.ToUniversalTime(), message.LastAttemptedAt);
        Assert.Equal(thirdAttemptAt.ToUniversalTime(), message.NextAttemptAt);
        Assert.Equal(NotificationOutboxState.RetryScheduled, message.State);
    }

    [Fact]
    public void Attempt_one_tick_before_due_is_rejected_without_mutation()
    {
        var message = CreateMessage();
        var before = Snapshot(message);

        var exception = Assert.Throws<NotificationDeliveryNotDueException>(
            () => message.RecordDelivered(AvailableAt.AddTicks(-1)));

        Assert.Equal(AvailableAt.ToUniversalTime(), exception.NextAttemptAt);
        Assert.Equal(AvailableAt.AddTicks(-1).ToUniversalTime(), exception.AttemptedAt);
        Assert.Equal(before, Snapshot(message));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Retry_schedule_must_be_strictly_after_the_failed_attempt(long ticksAfterAttempt)
    {
        var message = CreateMessage();
        var before = Snapshot(message);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => message.ScheduleRetry(
                AvailableAt,
                AvailableAt.AddTicks(ticksAfterAttempt),
                TransientFailure));

        Assert.Equal("nextAttemptAt", exception.ParamName);
        Assert.Equal(before, Snapshot(message));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Failure_outcome_rejects_a_default_failure_code_without_mutation(bool retry)
    {
        var message = CreateMessage();
        var before = Snapshot(message);

        var exception = retry
            ? Assert.Throws<ArgumentException>(
                () => message.ScheduleRetry(
                    AvailableAt,
                    AvailableAt.AddMinutes(1),
                    default))
            : Assert.Throws<ArgumentException>(
                () => message.FailPermanently(AvailableAt, default));

        Assert.Equal("failureCode", exception.ParamName);
        Assert.Equal(before, Snapshot(message));
    }

    [Theory]
    [MemberData(nameof(LegalActionCases))]
    public void Every_declared_legal_state_action_pair_is_accepted(
        NotificationOutboxState sourceState,
        DeliveryAction action,
        NotificationOutboxState requestedState)
    {
        var message = CreateInState(sourceState);

        ApplyAction(message, action);

        Assert.Equal(requestedState, message.State);
    }

    [Theory]
    [MemberData(nameof(ForbiddenActionCases))]
    public void Every_terminal_state_action_pair_is_rejected_without_mutation(
        NotificationOutboxState sourceState,
        DeliveryAction action,
        NotificationOutboxState requestedState)
    {
        var message = CreateInState(sourceState);
        var before = Snapshot(message);

        var exception = Assert.Throws<InvalidNotificationOutboxStateTransitionException>(
            () => ApplyAction(message, action));

        Assert.Equal(sourceState, exception.CurrentState);
        Assert.Equal(requestedState, exception.RequestedState);
        Assert.Equal(before, Snapshot(message));
    }

    [Fact]
    public void Attempt_timestamp_cannot_precede_the_previous_state_change()
    {
        var message = CreateMessage();
        var retryAt = AvailableAt.AddMinutes(5);
        message.ScheduleRetry(AvailableAt, retryAt, TransientFailure);
        var before = Snapshot(message);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => message.RecordDelivered(AvailableAt.AddTicks(-1)));

        Assert.Equal("attemptedAt", exception.ParamName);
        Assert.Equal(before, Snapshot(message));
    }

    [Fact]
    public void Attempt_rejects_a_default_timestamp_without_mutation()
    {
        var message = CreateMessage();
        var before = Snapshot(message);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => message.RecordDelivered(default));

        Assert.Equal("attemptedAt", exception.ParamName);
        Assert.Equal(before, Snapshot(message));
    }

    private static NotificationOutboxMessage CreateMessage(
        NotificationOutboxMessageId? messageId = null,
        CorrelationId? correlationId = null,
        UserId? targetUserId = null,
        ApplicationId? applicationId = null,
        TenantId? tenantId = null,
        NotificationType? notificationType = null,
        NotificationChannel? channel = null,
        NotificationDestination? destination = null,
        ProtectedNotificationPayload? protectedPayload = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? availableAt = null) =>
        NotificationOutboxMessage.Create(
            messageId ?? MessageId,
            correlationId ?? CorrelationId,
            targetUserId,
            applicationId,
            tenantId,
            notificationType ?? NotificationType,
            channel ?? NotificationChannel.Email,
            destination ?? Destination,
            protectedPayload ?? ProtectedPayload,
            createdAt ?? CreatedAt,
            availableAt ?? AvailableAt);

    private static NotificationOutboxMessage CreateInState(NotificationOutboxState state)
    {
        var message = CreateMessage();

        switch (state)
        {
            case NotificationOutboxState.Pending:
                return message;
            case NotificationOutboxState.RetryScheduled:
                message.ScheduleRetry(
                    AvailableAt,
                    AvailableAt.AddMinutes(5),
                    TransientFailure);
                return message;
            case NotificationOutboxState.Delivered:
                message.RecordDelivered(AvailableAt);
                return message;
            case NotificationOutboxState.PermanentlyFailed:
                message.FailPermanently(AvailableAt, PermanentFailure);
                return message;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private static void ApplyAction(NotificationOutboxMessage message, DeliveryAction action)
    {
        var attemptedAt = message.NextAttemptAt ?? AvailableAt.AddHours(1);

        switch (action)
        {
            case DeliveryAction.RecordDelivered:
                message.RecordDelivered(attemptedAt);
                break;
            case DeliveryAction.ScheduleRetry:
                message.ScheduleRetry(
                    attemptedAt,
                    attemptedAt.AddMinutes(5),
                    TransientFailure);
                break;
            case DeliveryAction.FailPermanently:
                message.FailPermanently(attemptedAt, PermanentFailure);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private static MessageSnapshot Snapshot(NotificationOutboxMessage message) =>
        new(
            message.State,
            message.StateChangedAt,
            message.AttemptCount,
            message.LastAttemptedAt,
            message.NextAttemptAt,
            message.DeliveredAt,
            message.PermanentlyFailedAt,
            message.LastFailureCode);

    private sealed record DeliveryActionDefinition(
        DeliveryAction Action,
        NotificationOutboxState RequestedState,
        NotificationOutboxState[] LegalSources);

    private sealed record MessageSnapshot(
        NotificationOutboxState State,
        DateTimeOffset StateChangedAt,
        int AttemptCount,
        DateTimeOffset? LastAttemptedAt,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? DeliveredAt,
        DateTimeOffset? PermanentlyFailedAt,
        NotificationDeliveryFailureCode? LastFailureCode);
}
