using AuthNexus.Domain;
using AuthNexus.Domain.Authentication;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Authentication;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Authentication.Tests;

public sealed class AuthenticationTransactionTests
{
    private static readonly AuthenticationTransactionId TransactionId = new(
        Guid.Parse("6cf58c48-5920-43bb-ac62-00cdcb7caee3"));

    private static readonly ApplicationId ApplicationId = new(
        Guid.Parse("f2e7fa27-494d-48af-b944-b9388ad6b172"));

    private static readonly TenantId TenantId = new(
        Guid.Parse("bd5f1e0a-bbd7-4729-aa98-cdc325c0e6aa"));

    private static readonly UserId UserId = new(
        Guid.Parse("dc987031-01f7-4bb7-857a-1d75e225ebdf"));

    private static readonly CorrelationId CorrelationId = new(
        Guid.Parse("43cc0db5-3673-4c82-ad3a-d8e79ee3e2de"));

    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        8,
        30,
        10,
        0,
        0,
        TimeSpan.FromHours(2));

    private static readonly DateTimeOffset ExpiresAt = CreatedAt.AddHours(1);

    private static readonly AuthenticationTransactionState[] LiveStates =
    [
        AuthenticationTransactionState.Initiated,
        AuthenticationTransactionState.ChallengeIssued,
        AuthenticationTransactionState.PrimaryVerified,
        AuthenticationTransactionState.StepUpRequired,
    ];

    private static readonly AuthenticationTransactionPurpose[] ExpectedPurposes =
    [
        AuthenticationTransactionPurpose.SignIn,
        AuthenticationTransactionPurpose.Register,
        AuthenticationTransactionPurpose.VerifyIdentifier,
        AuthenticationTransactionPurpose.PasswordReset,
        AuthenticationTransactionPurpose.LinkExternalIdentity,
        AuthenticationTransactionPurpose.UnlinkExternalIdentity,
        AuthenticationTransactionPurpose.EnrollPasskey,
        AuthenticationTransactionPurpose.EnrollTotp,
        AuthenticationTransactionPurpose.ChangeEmail,
        AuthenticationTransactionPurpose.ChangePhone,
        AuthenticationTransactionPurpose.ReplaceRecoveryMethod,
        AuthenticationTransactionPurpose.SensitiveActionStepUp,
        AuthenticationTransactionPurpose.AdminPolicyChange,
        AuthenticationTransactionPurpose.AccountDeletion,
    ];

    private static readonly TransitionDefinition[] TransitionDefinitions =
    [
        new(
            TransactionAction.IssueChallenge,
            AuthenticationTransactionState.ChallengeIssued,
            [AuthenticationTransactionState.Initiated]),
        new(
            TransactionAction.MarkPrimaryVerified,
            AuthenticationTransactionState.PrimaryVerified,
            [
                AuthenticationTransactionState.Initiated,
                AuthenticationTransactionState.ChallengeIssued,
            ]),
        new(
            TransactionAction.RequireStepUp,
            AuthenticationTransactionState.StepUpRequired,
            [AuthenticationTransactionState.PrimaryVerified]),
        new(
            TransactionAction.Complete,
            AuthenticationTransactionState.Completed,
            [
                AuthenticationTransactionState.PrimaryVerified,
                AuthenticationTransactionState.StepUpRequired,
            ]),
        new(
            TransactionAction.Fail,
            AuthenticationTransactionState.Failed,
            LiveStates),
        new(
            TransactionAction.Cancel,
            AuthenticationTransactionState.Cancelled,
            LiveStates),
        new(
            TransactionAction.Expire,
            AuthenticationTransactionState.Expired,
            LiveStates),
    ];

    public enum TransactionAction
    {
        IssueChallenge,
        MarkPrimaryVerified,
        RequireStepUp,
        Complete,
        Fail,
        Cancel,
        Expire,
    }

    public static TheoryData<AuthenticationTransactionPurpose> PurposeCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionPurpose>();

            foreach (var purpose in ExpectedPurposes)
            {
                cases.Add(purpose);
            }

            return cases;
        }
    }

    public static TheoryData<AuthenticationTransactionState, TransactionAction,
        AuthenticationTransactionState> LegalTransitionCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionState, TransactionAction,
                AuthenticationTransactionState>();

            foreach (var definition in TransitionDefinitions)
            {
                foreach (var sourceState in definition.LegalSources)
                {
                    cases.Add(sourceState, definition.Action, definition.RequestedState);
                }
            }

            return cases;
        }
    }

    public static TheoryData<AuthenticationTransactionState, TransactionAction,
        AuthenticationTransactionState> InvalidTransitionCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionState, TransactionAction,
                AuthenticationTransactionState>();

            foreach (var sourceState in Enum.GetValues<AuthenticationTransactionState>())
            {
                foreach (var definition in TransitionDefinitions)
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

    public static TheoryData<AuthenticationTransactionState, TransactionAction>
        DeadlineOperationCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionState, TransactionAction>();

            foreach (var definition in TransitionDefinitions.Where(
                         definition => definition.Action != TransactionAction.Expire))
            {
                foreach (var sourceState in definition.LegalSources)
                {
                    cases.Add(sourceState, definition.Action);
                }
            }

            return cases;
        }
    }

    public static TheoryData<AuthenticationTransactionState> LiveStateCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionState>();

            foreach (var state in LiveStates)
            {
                cases.Add(state);
            }

            return cases;
        }
    }

    public static TheoryData<AuthenticationTransactionState> TerminalStateCases
    {
        get
        {
            var cases = new TheoryData<AuthenticationTransactionState>();

            foreach (var state in new[]
                     {
                         AuthenticationTransactionState.Completed,
                         AuthenticationTransactionState.Failed,
                         AuthenticationTransactionState.Expired,
                         AuthenticationTransactionState.Cancelled,
                     })
            {
                cases.Add(state);
            }

            return cases;
        }
    }

    [Fact]
    public void Create_captures_the_resolved_context_and_starts_initiated()
    {
        var transaction = CreateTransaction();

        Assert.Equal(TransactionId, transaction.TransactionId);
        Assert.Equal(ApplicationId, transaction.ApplicationId);
        Assert.Equal(TenantId, transaction.TenantId);
        Assert.Equal(UserId, transaction.UserId);
        Assert.Equal(AuthenticationTransactionPurpose.SignIn, transaction.Purpose);
        Assert.Equal(CorrelationId, transaction.CorrelationId);
        Assert.Equal(AuthenticationTransactionState.Initiated, transaction.State);
        Assert.Equal(CreatedAt.ToUniversalTime(), transaction.CreatedAt);
        Assert.Equal(ExpiresAt.ToUniversalTime(), transaction.ExpiresAt);
        Assert.Equal(transaction.CreatedAt, transaction.StateChangedAt);
        Assert.Null(transaction.CompletedAt);
        Assert.Null(transaction.FailedAt);
        Assert.Equal(TimeSpan.Zero, transaction.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, transaction.ExpiresAt.Offset);
    }

    [Fact]
    public void Create_allows_a_transaction_without_tenant_or_known_user()
    {
        var transaction = AuthenticationTransaction.Create(
            TransactionId,
            ApplicationId,
            null,
            null,
            AuthenticationTransactionPurpose.Register,
            CorrelationId,
            CreatedAt,
            ExpiresAt);

        Assert.Null(transaction.TenantId);
        Assert.Null(transaction.UserId);
    }

    [Fact]
    public void Strong_transaction_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new AuthenticationTransactionId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Strong_correlation_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(() => new CorrelationId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Strong_transaction_identifiers_use_canonical_guid_text()
    {
        Assert.Equal(TransactionId.Value.ToString("D"), TransactionId.ToString());
        Assert.Equal(CorrelationId.Value.ToString("D"), CorrelationId.ToString());
    }

    [Fact]
    public void Create_rejects_a_default_transaction_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateWith(transactionId: default(AuthenticationTransactionId)));

        Assert.Equal("transactionId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_application_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateWith(applicationId: default(ApplicationId)));

        Assert.Equal("applicationId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_supplied_default_tenant_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateWith(tenantId: default(TenantId)));

        Assert.Equal("tenantId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_supplied_default_user_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateWith(userId: default(UserId)));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_correlation_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateWith(correlationId: default(CorrelationId)));

        Assert.Equal("correlationId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_an_undefined_purpose()
    {
        const AuthenticationTransactionPurpose undefinedPurpose =
            (AuthenticationTransactionPurpose)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateWith(purpose: undefinedPurpose));

        Assert.Equal("purpose", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_the_default_purpose()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateWith(purpose: default(AuthenticationTransactionPurpose)));

        Assert.Equal("purpose", exception.ParamName);
    }

    [Fact]
    public void Purpose_vocabulary_matches_the_fourteen_planned_values()
    {
        Assert.Equal(ExpectedPurposes, Enum.GetValues<AuthenticationTransactionPurpose>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedPurposes.Length),
            ExpectedPurposes.Select(purpose => (int)purpose));
    }

    [Theory]
    [MemberData(nameof(PurposeCases))]
    public void Create_accepts_each_declared_purpose(
        AuthenticationTransactionPurpose purpose)
    {
        var transaction = CreateWith(purpose: purpose);

        Assert.Equal(purpose, transaction.Purpose);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_rejects_a_default_lifetime_timestamp(bool defaultCreationTime)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateWith(
                createdAt: defaultCreationTime ? default : CreatedAt,
                expiresAt: defaultCreationTime ? ExpiresAt : default));

        Assert.Equal(defaultCreationTime ? "createdAt" : "expiresAt", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_requires_expiry_after_creation(int expiryOffsetMinutes)
    {
        var invalidExpiry = CreatedAt.AddMinutes(expiryOffsetMinutes);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateWith(expiresAt: invalidExpiry));

        Assert.Equal("expiresAt", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(LegalTransitionCases))]
    public void Every_legal_state_action_pair_reaches_its_declared_state(
        AuthenticationTransactionState sourceState,
        TransactionAction action,
        AuthenticationTransactionState requestedState)
    {
        var transaction = CreateInState(sourceState);
        var occurredAt = LegalOccurrence(transaction, action);

        ApplyAction(transaction, action, occurredAt);

        Assert.Equal(requestedState, transaction.State);
        Assert.Equal(occurredAt.ToUniversalTime(), transaction.StateChangedAt);

        if (requestedState == AuthenticationTransactionState.Completed)
        {
            Assert.Equal(transaction.StateChangedAt, transaction.CompletedAt);
            Assert.Null(transaction.FailedAt);
        }
        else if (requestedState == AuthenticationTransactionState.Failed)
        {
            Assert.Equal(transaction.StateChangedAt, transaction.FailedAt);
            Assert.Null(transaction.CompletedAt);
        }
        else
        {
            Assert.Null(transaction.CompletedAt);
            Assert.Null(transaction.FailedAt);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTransitionCases))]
    public void Every_forbidden_state_action_pair_is_rejected_without_mutation(
        AuthenticationTransactionState sourceState,
        TransactionAction action,
        AuthenticationTransactionState requestedState)
    {
        var transaction = CreateInState(sourceState);
        var occurredAt = InvalidOccurrence(transaction, action);
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<InvalidAuthenticationTransactionStateTransitionException>(
            () => ApplyAction(transaction, action, occurredAt));

        Assert.Equal(sourceState, exception.CurrentState);
        Assert.Equal(requestedState, exception.RequestedState);
        AssertMutableState(snapshot, transaction);
    }

    [Theory]
    [MemberData(nameof(DeadlineOperationCases))]
    public void Every_non_expiry_action_rejects_the_expiry_deadline_without_mutation(
        AuthenticationTransactionState sourceState,
        TransactionAction action)
    {
        var transaction = CreateInState(sourceState);
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<AuthenticationTransactionExpiredException>(
            () => ApplyAction(transaction, action, transaction.ExpiresAt));

        Assert.Equal(transaction.ExpiresAt, exception.ExpiresAt);
        Assert.Equal(transaction.ExpiresAt, exception.AttemptedAt);
        AssertMutableState(snapshot, transaction);
    }

    [Theory]
    [MemberData(nameof(LiveStateCases))]
    public void Every_live_state_rejects_early_expiry_without_mutation(
        AuthenticationTransactionState sourceState)
    {
        var transaction = CreateInState(sourceState);
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => transaction.Expire(transaction.ExpiresAt.AddTicks(-1)));

        Assert.Equal("occurredAt", exception.ParamName);
        AssertMutableState(snapshot, transaction);
    }

    [Theory]
    [MemberData(nameof(LiveStateCases))]
    public void Every_live_state_can_expire_after_its_deadline(
        AuthenticationTransactionState sourceState)
    {
        var transaction = CreateInState(sourceState);
        var expiredAt = transaction.ExpiresAt.AddTicks(1);

        transaction.Expire(expiredAt);

        Assert.Equal(AuthenticationTransactionState.Expired, transaction.State);
        Assert.Equal(expiredAt, transaction.StateChangedAt);
        Assert.Null(transaction.CompletedAt);
        Assert.Null(transaction.FailedAt);
    }

    [Theory]
    [MemberData(nameof(TerminalStateCases))]
    public void A_terminal_outcome_does_not_become_expired_after_its_deadline(
        AuthenticationTransactionState terminalState)
    {
        var transaction = CreateInState(terminalState);
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<InvalidAuthenticationTransactionStateTransitionException>(
            () => transaction.Expire(transaction.ExpiresAt.AddMinutes(1)));

        Assert.Equal(terminalState, exception.CurrentState);
        Assert.Equal(AuthenticationTransactionState.Expired, exception.RequestedState);
        AssertMutableState(snapshot, transaction);
    }

    [Fact]
    public void A_transition_rejects_a_default_timestamp_without_mutation()
    {
        var transaction = CreateTransaction();
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => transaction.IssueChallenge(default));

        Assert.Equal("occurredAt", exception.ParamName);
        AssertMutableState(snapshot, transaction);
    }

    [Fact]
    public void A_transition_cannot_precede_the_previous_state_change()
    {
        var transaction = CreateInState(AuthenticationTransactionState.PrimaryVerified);
        var snapshot = CaptureMutableState(transaction);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => transaction.RequireStepUp(transaction.StateChangedAt.AddTicks(-1)));

        Assert.Equal("occurredAt", exception.ParamName);
        AssertMutableState(snapshot, transaction);
    }

    [Fact]
    public void A_transition_normalizes_its_timestamp_to_utc()
    {
        var transaction = CreateTransaction();
        var occurredAt = new DateTimeOffset(
            2026,
            8,
            30,
            11,
            15,
            0,
            TimeSpan.FromHours(3));

        transaction.IssueChallenge(occurredAt);

        Assert.Equal(occurredAt.ToUniversalTime(), transaction.StateChangedAt);
        Assert.Equal(TimeSpan.Zero, transaction.StateChangedAt.Offset);
    }

    private static AuthenticationTransaction CreateTransaction() =>
        AuthenticationTransaction.Create(
            TransactionId,
            ApplicationId,
            TenantId,
            UserId,
            AuthenticationTransactionPurpose.SignIn,
            CorrelationId,
            CreatedAt,
            ExpiresAt);

    private static AuthenticationTransaction CreateWith(
        AuthenticationTransactionId? transactionId = null,
        ApplicationId? applicationId = null,
        TenantId? tenantId = null,
        UserId? userId = null,
        AuthenticationTransactionPurpose? purpose = null,
        CorrelationId? correlationId = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? expiresAt = null) =>
        AuthenticationTransaction.Create(
            transactionId ?? TransactionId,
            applicationId ?? ApplicationId,
            tenantId ?? TenantId,
            userId ?? UserId,
            purpose ?? AuthenticationTransactionPurpose.SignIn,
            correlationId ?? CorrelationId,
            createdAt ?? CreatedAt,
            expiresAt ?? ExpiresAt);

    private static AuthenticationTransaction CreateInState(
        AuthenticationTransactionState state)
    {
        var transaction = CreateTransaction();

        switch (state)
        {
            case AuthenticationTransactionState.Initiated:
                break;
            case AuthenticationTransactionState.ChallengeIssued:
                transaction.IssueChallenge(CreatedAt.AddMinutes(5));
                break;
            case AuthenticationTransactionState.PrimaryVerified:
                transaction.MarkPrimaryVerified(CreatedAt.AddMinutes(5));
                break;
            case AuthenticationTransactionState.StepUpRequired:
                transaction.MarkPrimaryVerified(CreatedAt.AddMinutes(5));
                transaction.RequireStepUp(CreatedAt.AddMinutes(10));
                break;
            case AuthenticationTransactionState.Completed:
                transaction.MarkPrimaryVerified(CreatedAt.AddMinutes(5));
                transaction.Complete(CreatedAt.AddMinutes(10));
                break;
            case AuthenticationTransactionState.Failed:
                transaction.Fail(CreatedAt.AddMinutes(5));
                break;
            case AuthenticationTransactionState.Expired:
                transaction.Expire(ExpiresAt);
                break;
            case AuthenticationTransactionState.Cancelled:
                transaction.Cancel(CreatedAt.AddMinutes(5));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(state),
                    state,
                    "The transaction state is not defined.");
        }

        Assert.Equal(state, transaction.State);
        return transaction;
    }

    private static DateTimeOffset LegalOccurrence(
        AuthenticationTransaction transaction,
        TransactionAction action) =>
        action == TransactionAction.Expire
            ? transaction.ExpiresAt
            : transaction.StateChangedAt.AddMinutes(1);

    private static DateTimeOffset InvalidOccurrence(
        AuthenticationTransaction transaction,
        TransactionAction action) =>
        action == TransactionAction.Expire
            ? transaction.ExpiresAt
            : transaction.StateChangedAt.AddMinutes(1);

    private static void ApplyAction(
        AuthenticationTransaction transaction,
        TransactionAction action,
        DateTimeOffset occurredAt)
    {
        switch (action)
        {
            case TransactionAction.IssueChallenge:
                transaction.IssueChallenge(occurredAt);
                return;
            case TransactionAction.MarkPrimaryVerified:
                transaction.MarkPrimaryVerified(occurredAt);
                return;
            case TransactionAction.RequireStepUp:
                transaction.RequireStepUp(occurredAt);
                return;
            case TransactionAction.Complete:
                transaction.Complete(occurredAt);
                return;
            case TransactionAction.Fail:
                transaction.Fail(occurredAt);
                return;
            case TransactionAction.Cancel:
                transaction.Cancel(occurredAt);
                return;
            case TransactionAction.Expire:
                transaction.Expire(occurredAt);
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    "The transaction action is not defined.");
        }
    }

    private static MutableStateSnapshot CaptureMutableState(
        AuthenticationTransaction transaction) =>
        new(
            transaction.State,
            transaction.StateChangedAt,
            transaction.CompletedAt,
            transaction.FailedAt);

    private static void AssertMutableState(
        MutableStateSnapshot expected,
        AuthenticationTransaction transaction)
    {
        Assert.Equal(expected.State, transaction.State);
        Assert.Equal(expected.StateChangedAt, transaction.StateChangedAt);
        Assert.Equal(expected.CompletedAt, transaction.CompletedAt);
        Assert.Equal(expected.FailedAt, transaction.FailedAt);
    }

    private sealed record TransitionDefinition(
        TransactionAction Action,
        AuthenticationTransactionState RequestedState,
        AuthenticationTransactionState[] LegalSources);

    private sealed record MutableStateSnapshot(
        AuthenticationTransactionState State,
        DateTimeOffset StateChangedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? FailedAt);
}
