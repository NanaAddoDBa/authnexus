using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Sessions;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Sessions.Tests;

public sealed class SessionTests
{
    private static readonly SessionId SessionId = new(
        Guid.Parse("49679698-4301-464e-8893-becf38a570bd"));

    private static readonly UserId UserId = new(
        Guid.Parse("a50d850a-caf7-47d3-8e9e-51fd4d951b4f"));

    private static readonly ApplicationId ApplicationId = new(
        Guid.Parse("4f27350b-5916-4f75-8a90-199293aa7626"));

    private static readonly TenantId TenantId = new(
        Guid.Parse("97b22d10-d211-42ed-9c2b-59ad1c2ee20a"));

    private static readonly SessionSecretHash SecretHash = new(new string('A', 43));
    private static readonly SessionSecretHash ReplacementHash = new($"{new string('B', 42)}E");

    private static readonly DateTimeOffset AuthenticatedAt = new(
        2026,
        8,
        30,
        8,
        59,
        0,
        TimeSpan.FromHours(2));

    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        8,
        30,
        9,
        0,
        0,
        TimeSpan.FromHours(2));

    private static readonly DateTimeOffset IdleExpiresAt = CreatedAt.AddMinutes(30);
    private static readonly DateTimeOffset AbsoluteExpiresAt = CreatedAt.AddHours(8);

    private static readonly SessionState[] ExpectedStates =
    [
        SessionState.Active,
        SessionState.Revoked,
        SessionState.Expired,
    ];

    private static readonly SessionRevocationReason[] ExpectedRevocationReasons =
    [
        SessionRevocationReason.UserLogout,
        SessionRevocationReason.UserRevoked,
        SessionRevocationReason.LogoutAll,
        SessionRevocationReason.CredentialReset,
        SessionRevocationReason.SecurityStampChanged,
        SessionRevocationReason.AccountProtected,
        SessionRevocationReason.AccountSuspended,
        SessionRevocationReason.AccountDeletion,
        SessionRevocationReason.AdministratorRevoked,
        SessionRevocationReason.SecurityIncident,
    ];

    private static readonly SessionActionDefinition[] ActionDefinitions =
    [
        new(SessionAction.RecordActivity, SessionState.Active, [SessionState.Active]),
        new(SessionAction.RotateSecretHash, SessionState.Active, [SessionState.Active]),
        new(
            SessionAction.Revoke,
            SessionState.Revoked,
            [SessionState.Active, SessionState.Revoked]),
        new(SessionAction.Expire, SessionState.Expired, [SessionState.Active]),
    ];

    public enum SessionAction
    {
        RecordActivity,
        RotateSecretHash,
        Revoke,
        Expire,
    }

    public static TheoryData<SessionState, SessionAction, SessionState> LegalActionCases
    {
        get
        {
            var cases = new TheoryData<SessionState, SessionAction, SessionState>();

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

    public static TheoryData<SessionState, SessionAction, SessionState> ForbiddenActionCases
    {
        get
        {
            var cases = new TheoryData<SessionState, SessionAction, SessionState>();

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
    public void Create_stores_application_scoped_session_context_and_canonical_time()
    {
        var session = CreateSession();

        Assert.Equal(SessionId, session.SessionId);
        Assert.Equal(SecretHash, session.SecretHash);
        Assert.Equal(UserId, session.UserId);
        Assert.Equal(ApplicationId, session.ApplicationId);
        Assert.Equal(TenantId, session.TenantId);
        Assert.Equal(SessionState.Active, session.State);
        Assert.Equal(AuthenticatedAt.ToUniversalTime(), session.AuthenticatedAt);
        Assert.Equal(CreatedAt.ToUniversalTime(), session.CreatedAt);
        Assert.Equal(session.CreatedAt, session.LastSeenAt);
        Assert.Equal(IdleExpiresAt.ToUniversalTime(), session.IdleExpiresAt);
        Assert.Equal(AbsoluteExpiresAt.ToUniversalTime(), session.AbsoluteExpiresAt);
        Assert.Equal(session.IdleExpiresAt, session.EffectiveExpiresAt);
        Assert.Equal(session.CreatedAt, session.UpdatedAt);
        Assert.Equal(session.CreatedAt, session.StateChangedAt);
        Assert.Equal(session.CreatedAt, session.SecretRotatedAt);
        Assert.Equal(0, session.RotationCount);
        Assert.Null(session.RevokedAt);
        Assert.Null(session.RevocationReason);
        Assert.Null(session.ExpiredAt);
        Assert.All(
            new[]
            {
                session.AuthenticatedAt,
                session.CreatedAt,
                session.LastSeenAt,
                session.IdleExpiresAt,
                session.AbsoluteExpiresAt,
                session.UpdatedAt,
                session.StateChangedAt,
                session.SecretRotatedAt,
            },
            timestamp => Assert.Equal(TimeSpan.Zero, timestamp.Offset));
    }

    [Fact]
    public void Create_allows_an_application_session_without_a_tenant()
    {
        var session = Session.Create(
            SessionId,
            SecretHash,
            UserId,
            ApplicationId,
            tenantId: null,
            AuthenticatedAt,
            CreatedAt,
            IdleExpiresAt,
            AbsoluteExpiresAt);

        Assert.Null(session.TenantId);
    }

    [Fact]
    public void Strong_session_id_rejects_an_empty_value()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SessionId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Strong_session_id_uses_canonical_guid_text()
    {
        Assert.Equal(SessionId.Value.ToString("D"), SessionId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA_")]
    public void Session_secret_hash_rejects_noncanonical_values(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => new SessionSecretHash(value));

        Assert.Equal("encodedValue", exception.ParamName);
    }

    [Fact]
    public void Session_secret_hash_accepts_base64url_and_redacts_display()
    {
        var value = $"-{new string('a', 41)}8";
        var hash = new SessionSecretHash($"  {value}  ");

        Assert.Equal(value, hash.EncodedValue);
        Assert.Equal("[session-secret-hash]", hash.ToString());
    }

    [Fact]
    public void Session_secret_hash_uses_value_equality()
    {
        Assert.Equal(SecretHash, new SessionSecretHash(new string('A', 43)));
        Assert.NotEqual(SecretHash, ReplacementHash);
    }

    [Theory]
    [InlineData("sessionId")]
    [InlineData("secretHash")]
    [InlineData("userId")]
    [InlineData("applicationId")]
    public void Create_rejects_a_default_required_identity(string parameter)
    {
        var exception = parameter switch
        {
            "sessionId" => Assert.Throws<ArgumentException>(
                () => CreateSession(sessionId: default(SessionId))),
            "secretHash" => Assert.Throws<ArgumentException>(
                () => CreateSession(secretHash: default(SessionSecretHash))),
            "userId" => Assert.Throws<ArgumentException>(
                () => CreateSession(userId: default(UserId))),
            "applicationId" => Assert.Throws<ArgumentException>(
                () => CreateSession(applicationId: default(ApplicationId))),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
        };

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Create_rejects_an_explicitly_empty_optional_tenant()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateSession(tenantId: default(TenantId)));

        Assert.Equal("tenantId", exception.ParamName);
    }

    [Theory]
    [InlineData("authenticatedAt")]
    [InlineData("createdAt")]
    [InlineData("idleExpiresAt")]
    [InlineData("absoluteExpiresAt")]
    public void Create_rejects_a_default_timestamp(string parameter)
    {
        var exception = parameter switch
        {
            "authenticatedAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateSession(authenticatedAt: default(DateTimeOffset))),
            "createdAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateSession(createdAt: default(DateTimeOffset))),
            "idleExpiresAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateSession(idleExpiresAt: default(DateTimeOffset))),
            "absoluteExpiresAt" => Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateSession(absoluteExpiresAt: default(DateTimeOffset))),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter)),
        };

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Authentication_cannot_be_recorded_after_session_creation()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSession(authenticatedAt: CreatedAt.AddTicks(1)));

        Assert.Equal("authenticatedAt", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Idle_expiry_must_be_later_than_creation(long ticksFromCreation)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSession(idleExpiresAt: CreatedAt.AddTicks(ticksFromCreation)));

        Assert.Equal("idleExpiresAt", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Absolute_expiry_must_be_later_than_creation(long ticksFromCreation)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSession(absoluteExpiresAt: CreatedAt.AddTicks(ticksFromCreation)));

        Assert.Equal("absoluteExpiresAt", exception.ParamName);
    }

    [Fact]
    public void Idle_expiry_cannot_exceed_absolute_expiry()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSession(idleExpiresAt: AbsoluteExpiresAt.AddTicks(1)));

        Assert.Equal("idleExpiresAt", exception.ParamName);
    }

    [Fact]
    public void Session_and_revocation_vocabularies_are_fixed_and_one_based()
    {
        Assert.Equal(ExpectedStates, Enum.GetValues<SessionState>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedStates.Length),
            ExpectedStates.Select(state => (int)state));
        Assert.Equal(ExpectedRevocationReasons, Enum.GetValues<SessionRevocationReason>());
        Assert.Equal(
            Enumerable.Range(1, ExpectedRevocationReasons.Length),
            ExpectedRevocationReasons.Select(reason => (int)reason));
    }

    [Fact]
    public void Usable_lifetime_is_half_open_and_does_not_require_state_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        Assert.False(session.CanBeUsedAt(CreatedAt.AddTicks(-1)));
        Assert.True(session.CanBeUsedAt(CreatedAt));
        Assert.True(session.CanBeUsedAt(IdleExpiresAt.AddTicks(-1)));
        Assert.False(session.CanBeUsedAt(IdleExpiresAt));
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Absolute_deadline_is_also_exclusive_when_it_is_the_effective_expiry()
    {
        var session = CreateSession(idleExpiresAt: AbsoluteExpiresAt);

        Assert.True(session.CanBeUsedAt(AbsoluteExpiresAt.AddTicks(-1)));
        Assert.False(session.CanBeUsedAt(AbsoluteExpiresAt));
    }

    [Fact]
    public void Usability_query_rejects_a_default_timestamp()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSession().CanBeUsedAt(default));

        Assert.Equal("observedAt", exception.ParamName);
    }

    [Fact]
    public void Record_activity_advances_last_seen_and_idle_expiry_without_extending_absolute_life()
    {
        var session = CreateSession();
        var occurredAt = CreatedAt.AddMinutes(10);
        var nextIdleExpiresAt = CreatedAt.AddHours(1);

        session.RecordActivity(occurredAt, nextIdleExpiresAt);

        Assert.Equal(occurredAt.ToUniversalTime(), session.LastSeenAt);
        Assert.Equal(nextIdleExpiresAt.ToUniversalTime(), session.IdleExpiresAt);
        Assert.Equal(AbsoluteExpiresAt.ToUniversalTime(), session.AbsoluteExpiresAt);
        Assert.Equal(occurredAt.ToUniversalTime(), session.UpdatedAt);
        Assert.Equal(SessionState.Active, session.State);
    }

    [Fact]
    public void Record_activity_can_cap_idle_expiry_at_the_absolute_deadline()
    {
        var session = CreateSession();

        session.RecordActivity(CreatedAt.AddMinutes(10), AbsoluteExpiresAt);

        Assert.Equal(session.AbsoluteExpiresAt, session.IdleExpiresAt);
        Assert.Equal(session.AbsoluteExpiresAt, session.EffectiveExpiresAt);
    }

    [Fact]
    public void Record_activity_rejects_the_effective_deadline_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<SessionExpiredException>(
            () => session.RecordActivity(IdleExpiresAt, IdleExpiresAt.AddMinutes(1)));

        Assert.Equal(session.EffectiveExpiresAt, exception.EffectiveExpiresAt);
        Assert.Equal(IdleExpiresAt.ToUniversalTime(), exception.AttemptedAt);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Record_activity_cannot_shorten_the_idle_deadline()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RecordActivity(CreatedAt.AddMinutes(10), CreatedAt.AddMinutes(20)));

        Assert.Equal("nextIdleExpiresAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Record_activity_requires_a_future_idle_deadline()
    {
        var session = CreateSession();
        var occurredAt = CreatedAt.AddMinutes(10);
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RecordActivity(occurredAt, occurredAt));

        Assert.Equal("nextIdleExpiresAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Record_activity_cannot_extend_beyond_absolute_expiry()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RecordActivity(
                CreatedAt.AddMinutes(10),
                AbsoluteExpiresAt.AddTicks(1)));

        Assert.Equal("nextIdleExpiresAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Rotate_secret_hash_replaces_only_the_verifier_state()
    {
        var session = CreateSession();
        var occurredAt = CreatedAt.AddMinutes(5);
        var identityBefore = (session.SessionId, session.UserId, session.ApplicationId, session.TenantId);
        var lifetimesBefore = (session.CreatedAt, session.IdleExpiresAt, session.AbsoluteExpiresAt);

        session.RotateSecretHash(ReplacementHash, occurredAt);

        Assert.Equal(ReplacementHash, session.SecretHash);
        Assert.Equal(1, session.RotationCount);
        Assert.Equal(occurredAt.ToUniversalTime(), session.SecretRotatedAt);
        Assert.Equal(occurredAt.ToUniversalTime(), session.UpdatedAt);
        Assert.Equal(identityBefore, (session.SessionId, session.UserId, session.ApplicationId, session.TenantId));
        Assert.Equal(lifetimesBefore, (session.CreatedAt, session.IdleExpiresAt, session.AbsoluteExpiresAt));
    }

    [Fact]
    public void Rotate_secret_hash_rejects_verifier_reuse_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentException>(
            () => session.RotateSecretHash(SecretHash, CreatedAt.AddMinutes(5)));

        Assert.Equal("replacementHash", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Rotate_secret_hash_rejects_a_default_replacement_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentException>(
            () => session.RotateSecretHash(default, CreatedAt.AddMinutes(5)));

        Assert.Equal("replacementHash", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Rotate_secret_hash_rejects_the_effective_deadline_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<SessionExpiredException>(
            () => session.RotateSecretHash(ReplacementHash, IdleExpiresAt));

        Assert.Equal(IdleExpiresAt.ToUniversalTime(), exception.EffectiveExpiresAt);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Revoke_records_a_machine_reason_and_is_immediately_unusable()
    {
        var session = CreateSession();
        var occurredAt = CreatedAt.AddMinutes(5);

        session.Revoke(SessionRevocationReason.UserLogout, occurredAt);

        Assert.Equal(SessionState.Revoked, session.State);
        Assert.Equal(occurredAt.ToUniversalTime(), session.RevokedAt);
        Assert.Equal(SessionRevocationReason.UserLogout, session.RevocationReason);
        Assert.Null(session.ExpiredAt);
        Assert.False(session.CanBeUsedAt(occurredAt));
    }

    [Fact]
    public void Revocation_is_allowed_after_elapsed_lifetime_because_it_only_removes_access()
    {
        var session = CreateSession();
        var revokedAt = IdleExpiresAt.AddMinutes(5);

        session.Revoke(SessionRevocationReason.SecurityIncident, revokedAt);

        Assert.Equal(SessionState.Revoked, session.State);
        Assert.Equal(revokedAt.ToUniversalTime(), session.RevokedAt);
    }

    [Fact]
    public void Repeated_revocation_is_idempotent_and_preserves_the_first_reason()
    {
        var session = CreateSession();
        session.Revoke(SessionRevocationReason.UserLogout, CreatedAt.AddMinutes(5));
        var before = Snapshot(session);

        session.Revoke(SessionRevocationReason.SecurityIncident, CreatedAt.AddMinutes(10));

        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Revoke_rejects_an_undefined_reason_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);
        const SessionRevocationReason undefinedReason = (SessionRevocationReason)999;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.Revoke(undefinedReason, CreatedAt.AddMinutes(5)));

        Assert.Equal("reason", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void Expire_rejects_time_before_the_effective_deadline_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.Expire(IdleExpiresAt.AddTicks(-1)));

        Assert.Equal("occurredAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Expire_accepts_the_effective_deadline_or_later(long ticksAfterDeadline)
    {
        var session = CreateSession();
        var occurredAt = IdleExpiresAt.AddTicks(ticksAfterDeadline);

        session.Expire(occurredAt);

        Assert.Equal(SessionState.Expired, session.State);
        Assert.Equal(occurredAt.ToUniversalTime(), session.ExpiredAt);
        Assert.Null(session.RevokedAt);
        Assert.Null(session.RevocationReason);
        Assert.False(session.CanBeUsedAt(occurredAt));
    }

    [Fact]
    public void Expire_accepts_the_absolute_deadline_when_it_is_the_effective_expiry()
    {
        var session = CreateSession(idleExpiresAt: AbsoluteExpiresAt);

        session.Expire(AbsoluteExpiresAt);

        Assert.Equal(SessionState.Expired, session.State);
        Assert.Equal(AbsoluteExpiresAt.ToUniversalTime(), session.ExpiredAt);
    }

    [Theory]
    [MemberData(nameof(LegalActionCases))]
    public void Every_declared_legal_state_action_pair_is_accepted(
        SessionState sourceState,
        SessionAction action,
        SessionState requestedState)
    {
        var session = CreateInState(sourceState);

        ApplyAction(session, action);

        Assert.Equal(requestedState, session.State);
    }

    [Theory]
    [MemberData(nameof(ForbiddenActionCases))]
    public void Every_other_state_action_pair_is_rejected_without_mutation(
        SessionState sourceState,
        SessionAction action,
        SessionState requestedState)
    {
        var session = CreateInState(sourceState);
        var before = Snapshot(session);

        var exception = Assert.Throws<InvalidSessionStateTransitionException>(
            () => ApplyAction(session, action));

        Assert.Equal(sourceState, exception.CurrentState);
        Assert.Equal(requestedState, exception.RequestedState);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void A_revoked_session_does_not_become_expired_after_its_deadline()
    {
        var session = CreateSession();
        session.Revoke(SessionRevocationReason.UserLogout, CreatedAt.AddMinutes(5));
        var before = Snapshot(session);

        var exception = Assert.Throws<InvalidSessionStateTransitionException>(
            () => session.Expire(IdleExpiresAt));

        Assert.Equal(SessionState.Revoked, exception.CurrentState);
        Assert.Equal(SessionState.Expired, exception.RequestedState);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void An_operation_cannot_precede_the_previous_accepted_operation()
    {
        var session = CreateSession();
        session.RotateSecretHash(ReplacementHash, CreatedAt.AddMinutes(10));
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RecordActivity(
                CreatedAt.AddMinutes(9),
                CreatedAt.AddHours(1)));

        Assert.Equal("occurredAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    [Fact]
    public void An_operation_rejects_a_default_timestamp_without_mutation()
    {
        var session = CreateSession();
        var before = Snapshot(session);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => session.RotateSecretHash(ReplacementHash, default));

        Assert.Equal("occurredAt", exception.ParamName);
        Assert.Equal(before, Snapshot(session));
    }

    private static Session CreateSession(
        SessionId? sessionId = null,
        SessionSecretHash? secretHash = null,
        UserId? userId = null,
        ApplicationId? applicationId = null,
        TenantId? tenantId = null,
        DateTimeOffset? authenticatedAt = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? idleExpiresAt = null,
        DateTimeOffset? absoluteExpiresAt = null) =>
        Session.Create(
            sessionId ?? SessionTests.SessionId,
            secretHash ?? SecretHash,
            userId ?? SessionTests.UserId,
            applicationId ?? SessionTests.ApplicationId,
            tenantId ?? SessionTests.TenantId,
            authenticatedAt ?? AuthenticatedAt,
            createdAt ?? CreatedAt,
            idleExpiresAt ?? IdleExpiresAt,
            absoluteExpiresAt ?? AbsoluteExpiresAt);

    private static Session CreateInState(SessionState state)
    {
        var session = CreateSession(tenantId: TenantId);

        switch (state)
        {
            case SessionState.Active:
                return session;
            case SessionState.Revoked:
                session.Revoke(SessionRevocationReason.UserLogout, CreatedAt.AddMinutes(5));
                return session;
            case SessionState.Expired:
                session.Expire(IdleExpiresAt);
                return session;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private static void ApplyAction(Session session, SessionAction action)
    {
        var occurredAt = action == SessionAction.Expire
            ? session.EffectiveExpiresAt
            : CreatedAt.AddMinutes(10);

        switch (action)
        {
            case SessionAction.RecordActivity:
                session.RecordActivity(occurredAt, CreatedAt.AddHours(1));
                break;
            case SessionAction.RotateSecretHash:
                session.RotateSecretHash(ReplacementHash, occurredAt);
                break;
            case SessionAction.Revoke:
                session.Revoke(SessionRevocationReason.UserRevoked, occurredAt);
                break;
            case SessionAction.Expire:
                session.Expire(occurredAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private static SessionSnapshot Snapshot(Session session) =>
        new(
            session.SecretHash,
            session.State,
            session.LastSeenAt,
            session.IdleExpiresAt,
            session.UpdatedAt,
            session.StateChangedAt,
            session.SecretRotatedAt,
            session.RotationCount,
            session.RevokedAt,
            session.RevocationReason,
            session.ExpiredAt);

    private sealed record SessionActionDefinition(
        SessionAction Action,
        SessionState RequestedState,
        SessionState[] LegalSources);

    private sealed record SessionSnapshot(
        SessionSecretHash SecretHash,
        SessionState State,
        DateTimeOffset LastSeenAt,
        DateTimeOffset IdleExpiresAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset StateChangedAt,
        DateTimeOffset SecretRotatedAt,
        int RotationCount,
        DateTimeOffset? RevokedAt,
        SessionRevocationReason? RevocationReason,
        DateTimeOffset? ExpiredAt);
}
