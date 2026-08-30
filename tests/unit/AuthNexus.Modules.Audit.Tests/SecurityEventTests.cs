using AuthNexus.Domain;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Audit;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Audit.Tests;

public sealed class SecurityEventTests
{
    private static readonly SecurityEventId EventId = new(
        Guid.Parse("fb9323ce-e3c5-4f5c-ad10-38bfe0ad58aa"));

    private static readonly UserId ActorUserId = new(
        Guid.Parse("4135aef4-2175-4dc0-92b5-03bdd916c1d2"));

    private static readonly UserId TargetUserId = new(
        Guid.Parse("0e6b45d9-f539-415c-8505-af66dfe2e147"));

    private static readonly ApplicationId ApplicationId = new(
        Guid.Parse("a6401821-b604-4b37-af9a-b7d308428775"));

    private static readonly TenantId TenantId = new(
        Guid.Parse("bfdd0771-1443-40b8-aa6e-7246380abb02"));

    private static readonly SessionId SessionId = new(
        Guid.Parse("7d3b645d-0f60-4bb7-a83b-f5847141f51a"));

    private static readonly CorrelationId CorrelationId = new(
        Guid.Parse("715f549e-a718-4f39-9450-cc3ba4755a44"));

    private static readonly DateTimeOffset Timestamp = new(
        2026,
        8,
        30,
        12,
        0,
        0,
        TimeSpan.FromHours(2));

    private static readonly (SecurityEventType Type, string Code)[] ExpectedEventTypes =
    [
        (SecurityEventType.RegistrationRequested, "registration_requested"),
        (SecurityEventType.RegistrationCompleted, "registration_completed"),
        (SecurityEventType.EmailVerificationSent, "email_verification_sent"),
        (SecurityEventType.EmailVerified, "email_verified"),
        (SecurityEventType.PhoneVerificationSent, "phone_verification_sent"),
        (SecurityEventType.PhoneVerified, "phone_verified"),
        (SecurityEventType.LoginSucceeded, "login_succeeded"),
        (SecurityEventType.LoginFailed, "login_failed"),
        (SecurityEventType.LoginThrottled, "login_throttled"),
        (SecurityEventType.MfaRequired, "mfa_required"),
        (SecurityEventType.MfaSucceeded, "mfa_succeeded"),
        (SecurityEventType.MfaFailed, "mfa_failed"),
        (SecurityEventType.PasswordResetRequested, "password_reset_requested"),
        (SecurityEventType.PasswordResetCompleted, "password_reset_completed"),
        (SecurityEventType.PasswordChanged, "password_changed"),
        (SecurityEventType.PasskeyAdded, "passkey_added"),
        (SecurityEventType.PasskeyUsed, "passkey_used"),
        (SecurityEventType.PasskeyRemoved, "passkey_removed"),
        (SecurityEventType.TotpAdded, "totp_added"),
        (SecurityEventType.TotpRemoved, "totp_removed"),
        (SecurityEventType.RecoveryCodeUsed, "recovery_code_used"),
        (SecurityEventType.ExternalIdentityLinked, "external_identity_linked"),
        (SecurityEventType.ExternalIdentityUnlinked, "external_identity_unlinked"),
        (SecurityEventType.SessionCreated, "session_created"),
        (SecurityEventType.SessionRevoked, "session_revoked"),
        (SecurityEventType.LogoutAll, "logout_all"),
        (SecurityEventType.SecurityStepUpRequired, "security_step_up_required"),
        (SecurityEventType.SecurityStepUpCompleted, "security_step_up_completed"),
        (SecurityEventType.AccountTemporarilyProtected, "account_temporarily_protected"),
        (SecurityEventType.AccountSuspended, "account_suspended"),
        (SecurityEventType.AccountDeletionRequested, "account_deletion_requested"),
        (SecurityEventType.AccountDeleted, "account_deleted"),
        (SecurityEventType.AuthorizationDenied, "authorization_denied"),
        (SecurityEventType.PolicyDraftCreated, "policy_draft_created"),
        (SecurityEventType.PolicyChanged, "policy_changed"),
        (SecurityEventType.PolicyApproved, "policy_approved"),
        (SecurityEventType.ProviderUnavailable, "provider_unavailable"),
    ];

    private static readonly SecurityEventResult[] ExpectedResults =
    [
        SecurityEventResult.Succeeded,
        SecurityEventResult.Failed,
        SecurityEventResult.Denied,
        SecurityEventResult.Throttled,
        SecurityEventResult.Cancelled,
        SecurityEventResult.Informational,
    ];

    public enum OptionalContext
    {
        ActorUser,
        TargetUser,
        Application,
        Tenant,
        Session,
    }

    public static TheoryData<SecurityEventType, string> EventTypeCases
    {
        get
        {
            var cases = new TheoryData<SecurityEventType, string>();

            foreach (var (eventType, code) in ExpectedEventTypes)
            {
                cases.Add(eventType, code);
            }

            return cases;
        }
    }

    public static TheoryData<SecurityEventResult> ResultCases
    {
        get
        {
            var cases = new TheoryData<SecurityEventResult>();

            foreach (var result in ExpectedResults)
            {
                cases.Add(result);
            }

            return cases;
        }
    }

    [Fact]
    public void Create_minimal_event_is_immutable_and_canonical()
    {
        var securityEvent = CreateEvent();

        Assert.Equal(EventId, securityEvent.EventId);
        Assert.Equal(Timestamp.ToUniversalTime(), securityEvent.Timestamp);
        Assert.Equal(TimeSpan.Zero, securityEvent.Timestamp.Offset);
        Assert.Equal(SecurityEventType.LoginSucceeded, securityEvent.EventType);
        Assert.Equal("login_succeeded", securityEvent.EventTypeCode);
        Assert.Equal(SecurityEventResult.Succeeded, securityEvent.Result);
        Assert.Null(securityEvent.ActorUserId);
        Assert.Null(securityEvent.TargetUserId);
        Assert.Null(securityEvent.ApplicationId);
        Assert.Null(securityEvent.TenantId);
        Assert.Null(securityEvent.SessionId);
        Assert.Equal(CorrelationId, securityEvent.CorrelationId);
        Assert.Null(securityEvent.NetworkSummary);
        Assert.Null(securityEvent.UserAgentSummary);
        Assert.Equal(0, securityEvent.Metadata.Count);
    }

    [Fact]
    public void Create_accepts_complete_actor_target_and_request_context()
    {
        var metadata = SecurityEventMetadata.Create(
        [
            new("authentication.method", "passkey"),
            new("risk.band", "low"),
        ]);

        var securityEvent = CreateEvent(
            actorUserId: ActorUserId,
            targetUserId: TargetUserId,
            applicationId: ApplicationId,
            tenantId: TenantId,
            sessionId: SessionId,
            networkSummary: "  country=DE  ",
            userAgentSummary: "  Chrome on Windows  ",
            metadata: metadata);

        Assert.Equal(ActorUserId, securityEvent.ActorUserId);
        Assert.Equal(TargetUserId, securityEvent.TargetUserId);
        Assert.Equal(ApplicationId, securityEvent.ApplicationId);
        Assert.Equal(TenantId, securityEvent.TenantId);
        Assert.Equal(SessionId, securityEvent.SessionId);
        Assert.Equal("country=DE", securityEvent.NetworkSummary);
        Assert.Equal("Chrome on Windows", securityEvent.UserAgentSummary);
        Assert.Same(metadata, securityEvent.Metadata);
    }

    [Fact]
    public void Strong_security_event_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SecurityEventId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Strong_security_event_id_uses_canonical_guid_text()
    {
        Assert.Equal(EventId.Value.ToString("D"), EventId.ToString());
    }

    [Fact]
    public void Event_type_and_result_vocabularies_are_fixed_and_one_based()
    {
        Assert.Equal(
            ExpectedEventTypes.Select(item => item.Type),
            Enum.GetValues<SecurityEventType>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedEventTypes.Length),
            ExpectedEventTypes.Select(item => (int)item.Type));
        Assert.Equal(ExpectedResults, Enum.GetValues<SecurityEventResult>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedResults.Length),
            ExpectedResults.Select(result => (int)result));
    }

    [Theory]
    [MemberData(nameof(EventTypeCases))]
    public void Every_planned_event_type_has_the_exact_machine_code(
        SecurityEventType eventType,
        string expectedCode)
    {
        var securityEvent = CreateEvent(eventType: eventType);

        Assert.Equal(expectedCode, eventType.ToCode());
        Assert.Equal(expectedCode, securityEvent.EventTypeCode);
    }

    [Theory]
    [MemberData(nameof(ResultCases))]
    public void Every_declared_result_can_be_recorded(SecurityEventResult result)
    {
        Assert.Equal(result, CreateEvent(result: result).Result);
    }

    [Fact]
    public void Undefined_event_type_is_rejected()
    {
        const SecurityEventType undefinedType = (SecurityEventType)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateEvent(eventType: undefinedType));

        Assert.Equal("eventType", exception.ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => undefinedType.ToCode());
    }

    [Fact]
    public void Undefined_event_result_is_rejected()
    {
        const SecurityEventResult undefinedResult = (SecurityEventResult)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateEvent(result: undefinedResult));

        Assert.Equal("result", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_event_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateEvent(eventId: default(SecurityEventId)));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_correlation_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateEvent(correlationId: default(CorrelationId)));

        Assert.Equal("correlationId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_timestamp()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateEvent(timestamp: default(DateTimeOffset)));

        Assert.Equal("timestamp", exception.ParamName);
    }

    [Theory]
    [InlineData(OptionalContext.ActorUser)]
    [InlineData(OptionalContext.TargetUser)]
    [InlineData(OptionalContext.Application)]
    [InlineData(OptionalContext.Tenant)]
    [InlineData(OptionalContext.Session)]
    public void Supplied_optional_context_cannot_use_a_default_id(OptionalContext context)
    {
        var exception = context switch
        {
            OptionalContext.ActorUser => Assert.Throws<ArgumentException>(
                () => CreateEvent(actorUserId: default(UserId))),
            OptionalContext.TargetUser => Assert.Throws<ArgumentException>(
                () => CreateEvent(targetUserId: default(UserId))),
            OptionalContext.Application => Assert.Throws<ArgumentException>(
                () => CreateEvent(applicationId: default(ApplicationId))),
            OptionalContext.Tenant => Assert.Throws<ArgumentException>(
                () => CreateEvent(tenantId: default(TenantId))),
            OptionalContext.Session => Assert.Throws<ArgumentException>(
                () => CreateEvent(sessionId: default(SessionId))),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
        };

        Assert.Equal(
            context switch
            {
                OptionalContext.ActorUser => "actorUserId",
                OptionalContext.TargetUser => "targetUserId",
                OptionalContext.Application => "applicationId",
                OptionalContext.Tenant => "tenantId",
                OptionalContext.Session => "sessionId",
                _ => throw new ArgumentOutOfRangeException(nameof(context), context, null),
            },
            exception.ParamName);
    }

    [Fact]
    public void Actor_and_target_may_be_the_same_user()
    {
        var securityEvent = CreateEvent(
            actorUserId: ActorUserId,
            targetUserId: ActorUserId);

        Assert.Equal(securityEvent.ActorUserId, securityEvent.TargetUserId);
    }

    [Fact]
    public void Create_rejects_null_metadata()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => SecurityEvent.Create(
                EventId,
                Timestamp,
                SecurityEventType.LoginSucceeded,
                SecurityEventResult.Succeeded,
                actorUserId: null,
                targetUserId: null,
                applicationId: null,
                tenantId: null,
                sessionId: null,
                CorrelationId,
                networkSummary: null,
                userAgentSummary: null,
                metadata: null!));

        Assert.Equal("metadata", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Supplied_summary_cannot_be_blank(bool networkSummary)
    {
        var exception = networkSummary
            ? Assert.Throws<ArgumentException>(() => CreateEvent(networkSummary: "   "))
            : Assert.Throws<ArgumentException>(() => CreateEvent(userAgentSummary: "   "));

        Assert.Equal(networkSummary ? "networkSummary" : "userAgentSummary", exception.ParamName);
    }

    [Fact]
    public void Network_summary_is_bounded()
    {
        Assert.Equal(
            SecurityEvent.MaximumNetworkSummaryLength,
            CreateEvent(
                networkSummary: new string('n', SecurityEvent.MaximumNetworkSummaryLength))
                .NetworkSummary!.Length);

        var exception = Assert.Throws<ArgumentException>(
            () => CreateEvent(
                networkSummary: new string('n', SecurityEvent.MaximumNetworkSummaryLength + 1)));

        Assert.Equal("networkSummary", exception.ParamName);
    }

    [Fact]
    public void User_agent_summary_is_bounded()
    {
        Assert.Equal(
            SecurityEvent.MaximumUserAgentSummaryLength,
            CreateEvent(
                userAgentSummary: new string('u', SecurityEvent.MaximumUserAgentSummaryLength))
                .UserAgentSummary!.Length);

        var exception = Assert.Throws<ArgumentException>(
            () => CreateEvent(
                userAgentSummary: new string('u', SecurityEvent.MaximumUserAgentSummaryLength + 1)));

        Assert.Equal("userAgentSummary", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Supplied_summary_rejects_control_characters(bool networkSummary)
    {
        var exception = networkSummary
            ? Assert.Throws<ArgumentException>(() => CreateEvent(networkSummary: "DE\nnetwork"))
            : Assert.Throws<ArgumentException>(
                () => CreateEvent(userAgentSummary: "browser\r\nversion"));

        Assert.Equal(networkSummary ? "networkSummary" : "userAgentSummary", exception.ParamName);
    }

    [Fact]
    public void Empty_metadata_is_allowed_and_does_not_expose_values_in_display()
    {
        var metadata = SecurityEventMetadata.Create([]);

        Assert.Equal(0, metadata.Count);
        Assert.Empty(metadata.Values);
        Assert.Equal("[security-event-metadata:0]", metadata.ToString());
    }

    [Fact]
    public void Metadata_normalizes_keys_and_values()
    {
        var metadata = SecurityEventMetadata.Create(
        [
            new("  Risk.Band  ", "  low  "),
        ]);

        Assert.Equal("low", metadata.Values["risk.band"]);
    }

    [Fact]
    public void Metadata_defensively_copies_the_source_collection()
    {
        var source = new List<KeyValuePair<string, string>>
        {
            new("risk.band", "low"),
        };
        var metadata = SecurityEventMetadata.Create(source);

        source[0] = new KeyValuePair<string, string>("risk.band", "high");
        source.Add(new KeyValuePair<string, string>("method", "password"));

        Assert.Single(metadata.Values);
        Assert.Equal("low", metadata.Values["risk.band"]);
    }

    [Fact]
    public void Metadata_exposure_is_read_only()
    {
        var metadata = SecurityEventMetadata.Create(
        [
            new("risk.band", "low"),
        ]);
        var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(metadata.Values);

        Assert.Throws<NotSupportedException>(() => dictionary.Add("method", "passkey"));
        Assert.Equal("low", metadata.Values["risk.band"]);
    }

    [Fact]
    public void Metadata_requires_a_collection()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => SecurityEventMetadata.Create(null!));

        Assert.Equal("entries", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1invalid")]
    [InlineData("invalid key")]
    [InlineData("invalid/key")]
    public void Metadata_rejects_invalid_keys(string key)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create([new(key, "value")]));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void Metadata_rejects_an_overlong_key()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create(
            [
                new($"a{new string('b', SecurityEventMetadata.MaximumKeyLength)}", "value"),
            ]));

        Assert.Equal("entries", exception.ParamName);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("raw_password")]
    [InlineData("otp")]
    [InlineData("otp_value")]
    [InlineData("reset_token")]
    [InlineData("session_secret")]
    [InlineData("oauth_authorization_code")]
    [InlineData("authorization_code")]
    [InlineData("refresh_token")]
    [InlineData("totp_seed")]
    [InlineData("recovery_code")]
    [InlineData("request.password")]
    [InlineData("password.value")]
    [InlineData("otp.code")]
    [InlineData("otp_code")]
    [InlineData("reset.token")]
    [InlineData("token")]
    [InlineData("totp.seed")]
    [InlineData("recovery.code")]
    [InlineData("authorization.code")]
    public void Metadata_rejects_explicitly_sensitive_keys(string key)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create([new(key, "must-not-appear")]));

        Assert.Equal("entries", exception.ParamName);
        Assert.DoesNotContain("must-not-appear", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_rejects_duplicates_after_key_normalization()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create(
            [
                new("Risk.Band", "low"),
                new("risk.band", "high"),
            ]));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void Metadata_accepts_the_entry_limit_and_rejects_one_more()
    {
        var maximum = Enumerable.Range(0, SecurityEventMetadata.MaximumEntryCount)
            .Select(index => new KeyValuePair<string, string>($"key_{index}", "value"))
            .ToArray();

        Assert.Equal(
            SecurityEventMetadata.MaximumEntryCount,
            SecurityEventMetadata.Create(maximum).Count);

        var overLimit = maximum
            .Append(new KeyValuePair<string, string>("overflow", "value"))
            .ToArray();
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create(overLimit));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void Metadata_accepts_the_value_limit_and_rejects_one_more()
    {
        Assert.Equal(
            SecurityEventMetadata.MaximumValueLength,
            SecurityEventMetadata.Create(
                [new("detail", new string('v', SecurityEventMetadata.MaximumValueLength))])
                .Values["detail"].Length);

        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create(
            [
                new("detail", new string('v', SecurityEventMetadata.MaximumValueLength + 1)),
            ]));

        Assert.Equal("entries", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    [InlineData("line\u2028break")]
    [InlineData("spoof\u202Etext")]
    public void Metadata_rejects_invalid_values(string? value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => SecurityEventMetadata.Create(
            [
                new KeyValuePair<string, string>("detail", value!),
            ]));

        Assert.Equal("entries", exception.ParamName);
    }

    [Fact]
    public void Security_event_has_no_public_mutation_surface()
    {
        Assert.All(
            typeof(SecurityEvent).GetProperties(),
            property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(
            typeof(SecurityEvent).GetMethods(),
            method => method.IsPublic &&
                      !method.IsStatic &&
                      method.Name.StartsWith("Set", StringComparison.Ordinal));
    }

    private static SecurityEvent CreateEvent(
        SecurityEventId? eventId = null,
        DateTimeOffset? timestamp = null,
        SecurityEventType? eventType = null,
        SecurityEventResult? result = null,
        UserId? actorUserId = null,
        UserId? targetUserId = null,
        ApplicationId? applicationId = null,
        TenantId? tenantId = null,
        SessionId? sessionId = null,
        CorrelationId? correlationId = null,
        string? networkSummary = null,
        string? userAgentSummary = null,
        SecurityEventMetadata? metadata = null) =>
        SecurityEvent.Create(
            eventId ?? EventId,
            timestamp ?? Timestamp,
            eventType ?? SecurityEventType.LoginSucceeded,
            result ?? SecurityEventResult.Succeeded,
            actorUserId,
            targetUserId,
            applicationId,
            tenantId,
            sessionId,
            correlationId ?? CorrelationId,
            networkSummary,
            userAgentSummary,
            metadata ?? SecurityEventMetadata.Create([]));
}
