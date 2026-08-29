using AuthNexus.Domain.Identity;
using AuthNexus.Modules.Identity;

namespace AuthNexus.Modules.Identity.Tests;

public sealed class UserAccountTests
{
    private static readonly UserId UserId = new(
        Guid.Parse("0aee242d-b4c9-43c7-aa88-a8236ddf59c6"));

    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        8,
        29,
        10,
        0,
        0,
        TimeSpan.FromHours(2));

    public enum AccountTransition
    {
        Activate,
        ProtectTemporarily,
        RestoreAfterProtection,
        Suspend,
        Reactivate,
        RequestDeletion,
        CompleteDeletion,
    }

    public static TheoryData<UserAccountState, AccountTransition, UserAccountState>
        InvalidTransitionCases
    {
        get
        {
            var cases = new TheoryData<UserAccountState, AccountTransition, UserAccountState>();
            var transitions = new[]
            {
                (AccountTransition.Activate, UserAccountState.PendingVerification, UserAccountState.Active),
                (AccountTransition.ProtectTemporarily, UserAccountState.Active, UserAccountState.TemporarilyProtected),
                (AccountTransition.RestoreAfterProtection, UserAccountState.TemporarilyProtected, UserAccountState.Active),
                (AccountTransition.Suspend, UserAccountState.Active, UserAccountState.Suspended),
                (AccountTransition.Reactivate, UserAccountState.Suspended, UserAccountState.Active),
                (AccountTransition.RequestDeletion, UserAccountState.Active, UserAccountState.DeletionPending),
                (AccountTransition.CompleteDeletion, UserAccountState.DeletionPending, UserAccountState.Deleted),
            };

            foreach (var sourceState in Enum.GetValues<UserAccountState>())
            {
                foreach (var (transition, requiredState, requestedState) in transitions)
                {
                    if (sourceState != requiredState)
                    {
                        cases.Add(sourceState, transition, requestedState);
                    }
                }
            }

            return cases;
        }
    }

    [Fact]
    public void Create_starts_a_pending_account_with_canonical_timestamps()
    {
        var account = UserAccount.Create(UserId, CreatedAt);

        Assert.Equal(UserId, account.UserId);
        Assert.Equal(UserAccountState.PendingVerification, account.State);
        Assert.Equal(CreatedAt.ToUniversalTime(), account.CreatedAt);
        Assert.Equal(account.CreatedAt, account.StateChangedAt);
        Assert.Equal(TimeSpan.Zero, account.CreatedAt.Offset);
    }

    [Fact]
    public void Strong_user_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(() => new UserId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_user_id()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => UserAccount.Create(default, CreatedAt));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_default_timestamp()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => UserAccount.Create(UserId, default));

        Assert.Equal("createdAt", exception.ParamName);
    }

    [Fact]
    public void Activate_completes_the_pending_verification_state()
    {
        var account = CreatePending();
        var activatedAt = CreatedAt.AddMinutes(5);

        account.Activate(activatedAt);

        Assert.Equal(UserAccountState.Active, account.State);
        Assert.Equal(activatedAt.ToUniversalTime(), account.StateChangedAt);
    }

    [Fact]
    public void Temporary_protection_can_be_applied_and_cleared()
    {
        var account = CreateActive();

        account.ProtectTemporarily(CreatedAt.AddMinutes(10));
        Assert.Equal(UserAccountState.TemporarilyProtected, account.State);

        account.RestoreAfterProtection(CreatedAt.AddMinutes(15));
        Assert.Equal(UserAccountState.Active, account.State);
    }

    [Fact]
    public void Administrative_suspension_can_be_reactivated()
    {
        var account = CreateActive();

        account.Suspend(CreatedAt.AddMinutes(10));
        Assert.Equal(UserAccountState.Suspended, account.State);

        account.Reactivate(CreatedAt.AddMinutes(15));
        Assert.Equal(UserAccountState.Active, account.State);
    }

    [Fact]
    public void Deletion_requires_a_pending_state_before_completion()
    {
        var account = CreateActive();

        account.RequestDeletion(CreatedAt.AddMinutes(10));
        Assert.Equal(UserAccountState.DeletionPending, account.State);

        account.CompleteDeletion(CreatedAt.AddMinutes(15));
        Assert.Equal(UserAccountState.Deleted, account.State);
    }

    [Theory]
    [MemberData(nameof(InvalidTransitionCases))]
    public void Each_named_transition_rejects_every_other_source_state(
        UserAccountState sourceState,
        AccountTransition transition,
        UserAccountState requestedState)
    {
        var account = CreateInState(sourceState);

        AssertRejected(
            account,
            requestedState,
            candidate => ApplyTransition(candidate, transition, CreatedAt.AddHours(1)));
    }

    [Fact]
    public void A_transition_cannot_precede_the_previous_state_change()
    {
        var account = CreateActive();
        var stateBeforeAttempt = account.State;
        var changedAtBeforeAttempt = account.StateChangedAt;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => account.Suspend(CreatedAt.AddMinutes(4)));

        Assert.Equal("occurredAt", exception.ParamName);
        Assert.Equal(stateBeforeAttempt, account.State);
        Assert.Equal(changedAtBeforeAttempt, account.StateChangedAt);
    }

    [Fact]
    public void A_transition_rejects_a_default_timestamp_without_mutating_the_account()
    {
        var account = CreatePending();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => account.Activate(default));

        Assert.Equal("occurredAt", exception.ParamName);
        Assert.Equal(UserAccountState.PendingVerification, account.State);
        Assert.Equal(account.CreatedAt, account.StateChangedAt);
    }

    [Fact]
    public void A_transition_normalizes_its_timestamp_to_utc()
    {
        var account = CreatePending();
        var activatedAt = new DateTimeOffset(2026, 8, 29, 12, 15, 0, TimeSpan.FromHours(4));

        account.Activate(activatedAt);

        Assert.Equal(activatedAt.ToUniversalTime(), account.StateChangedAt);
        Assert.Equal(TimeSpan.Zero, account.StateChangedAt.Offset);
    }

    private static UserAccount CreatePending() => UserAccount.Create(UserId, CreatedAt);

    private static UserAccount CreateActive()
    {
        var account = CreatePending();
        account.Activate(CreatedAt.AddMinutes(5));
        return account;
    }

    private static UserAccount CreateProtected()
    {
        var account = CreateActive();
        account.ProtectTemporarily(CreatedAt.AddMinutes(10));
        return account;
    }

    private static UserAccount CreateSuspended()
    {
        var account = CreateActive();
        account.Suspend(CreatedAt.AddMinutes(10));
        return account;
    }

    private static UserAccount CreateDeletionPending()
    {
        var account = CreateActive();
        account.RequestDeletion(CreatedAt.AddMinutes(10));
        return account;
    }

    private static UserAccount CreateDeleted()
    {
        var account = CreateDeletionPending();
        account.CompleteDeletion(CreatedAt.AddMinutes(15));
        return account;
    }

    private static UserAccount CreateInState(UserAccountState state) => state switch
    {
        UserAccountState.PendingVerification => CreatePending(),
        UserAccountState.Active => CreateActive(),
        UserAccountState.TemporarilyProtected => CreateProtected(),
        UserAccountState.Suspended => CreateSuspended(),
        UserAccountState.DeletionPending => CreateDeletionPending(),
        UserAccountState.Deleted => CreateDeleted(),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "The state is not defined."),
    };

    private static void ApplyTransition(
        UserAccount account,
        AccountTransition transition,
        DateTimeOffset occurredAt)
    {
        switch (transition)
        {
            case AccountTransition.Activate:
                account.Activate(occurredAt);
                return;
            case AccountTransition.ProtectTemporarily:
                account.ProtectTemporarily(occurredAt);
                return;
            case AccountTransition.RestoreAfterProtection:
                account.RestoreAfterProtection(occurredAt);
                return;
            case AccountTransition.Suspend:
                account.Suspend(occurredAt);
                return;
            case AccountTransition.Reactivate:
                account.Reactivate(occurredAt);
                return;
            case AccountTransition.RequestDeletion:
                account.RequestDeletion(occurredAt);
                return;
            case AccountTransition.CompleteDeletion:
                account.CompleteDeletion(occurredAt);
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(transition),
                    transition,
                    "The transition is not defined.");
        }
    }

    private static void AssertRejected(
        UserAccount account,
        UserAccountState requestedState,
        Action<UserAccount> transition)
    {
        var stateBeforeAttempt = account.State;
        var changedAtBeforeAttempt = account.StateChangedAt;

        var exception = Assert.Throws<InvalidUserAccountStateTransitionException>(
            () => transition(account));

        Assert.Equal(stateBeforeAttempt, exception.CurrentState);
        Assert.Equal(requestedState, exception.RequestedState);
        Assert.Equal(stateBeforeAttempt, account.State);
        Assert.Equal(changedAtBeforeAttempt, account.StateChangedAt);
    }
}
