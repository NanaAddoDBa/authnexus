using AuthNexus.Modules.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Sessions;

internal sealed class SessionRecordConfiguration : IEntityTypeConfiguration<SessionRecord>
{
    public void Configure(EntityTypeBuilder<SessionRecord> builder)
    {
        builder.ToTable(
            "sessions",
            "sessions",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_sessions_session_id",
                    "session_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_sessions_user_id",
                    "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_sessions_application_id",
                    "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_sessions_tenant_id",
                    "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_sessions_state",
                    "state BETWEEN 1 AND 3");
                table.HasCheckConstraint(
                    "ck_sessions_secret_hash",
                    "char_length(session_secret_hash) = 43 AND " +
                    "session_secret_hash ~ '^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$'");
                table.HasCheckConstraint(
                    "ck_sessions_revocation_reason",
                    "revocation_reason IS NULL OR revocation_reason BETWEEN 1 AND 10");
                table.HasCheckConstraint(
                    "ck_sessions_lifetime",
                    "authenticated_at <= created_at AND " +
                    "created_at < idle_expires_at AND " +
                    "idle_expires_at <= absolute_expires_at");
                table.HasCheckConstraint(
                    "ck_sessions_operation_timestamps",
                    "last_seen_at >= created_at AND " +
                    "last_seen_at <= updated_at AND " +
                    "last_seen_at < idle_expires_at AND " +
                    "updated_at >= created_at AND " +
                    "state_changed_at >= created_at AND " +
                    "state_changed_at <= updated_at AND " +
                    "secret_rotated_at >= created_at AND " +
                    "secret_rotated_at <= updated_at AND " +
                    "secret_rotated_at < idle_expires_at");
                table.HasCheckConstraint(
                    "ck_sessions_rotation_count",
                    "rotation_count >= 0");
                table.HasCheckConstraint(
                    "ck_sessions_terminal_state",
                    "(state = 1 AND state_changed_at = created_at AND " +
                    "revoked_at IS NULL AND revocation_reason IS NULL AND expired_at IS NULL) OR " +
                    "(state = 2 AND revoked_at IS NOT NULL AND revoked_at = state_changed_at AND " +
                    "state_changed_at = updated_at AND revocation_reason IS NOT NULL AND " +
                    "expired_at IS NULL) OR " +
                    "(state = 3 AND expired_at IS NOT NULL AND expired_at = state_changed_at AND " +
                    "state_changed_at = updated_at AND expired_at >= idle_expires_at AND " +
                    "revoked_at IS NULL AND revocation_reason IS NULL)");
                table.HasCheckConstraint(
                    "ck_sessions_version",
                    "version <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(record => record.SessionId)
            .HasName("pk_sessions");

        builder.Property(record => record.SessionId)
            .HasColumnName("session_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(record => record.SecretHash)
            .HasColumnName("session_secret_hash")
            .HasColumnType("character varying(43)")
            .HasMaxLength(SessionSecretHash.EncodedLength)
            .IsRequired();
        builder.Property(record => record.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");
        builder.Property(record => record.ApplicationId)
            .HasColumnName("application_id")
            .HasColumnType("uuid");
        builder.Property(record => record.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid");
        builder.Property(record => record.State)
            .HasColumnName("state")
            .HasColumnType("smallint");
        builder.Property(record => record.AuthenticatedAt)
            .HasColumnName("authenticated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.LastSeenAt)
            .HasColumnName("last_seen_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.IdleExpiresAt)
            .HasColumnName("idle_expires_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.AbsoluteExpiresAt)
            .HasColumnName("absolute_expires_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.StateChangedAt)
            .HasColumnName("state_changed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.SecretRotatedAt)
            .HasColumnName("secret_rotated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.RotationCount)
            .HasColumnName("rotation_count");
        builder.Property(record => record.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasColumnType("smallint");
        builder.Property(record => record.ExpiredAt)
            .HasColumnName("expired_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.Version)
            .HasColumnName("version")
            .HasColumnType("uuid")
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(record => record.SecretHash)
            .IsUnique()
            .HasDatabaseName("ux_sessions_session_secret_hash");
        builder.HasIndex(record => new { record.UserId, record.State })
            .HasDatabaseName("ix_sessions_user_id_state");
        builder.HasIndex(record => new { record.ApplicationId, record.State })
            .HasDatabaseName("ix_sessions_application_id_state");
        builder.HasIndex(record => new { record.State, record.IdleExpiresAt })
            .HasDatabaseName("ix_sessions_state_idle_expires_at");
        builder.HasIndex(record => new { record.State, record.AbsoluteExpiresAt })
            .HasDatabaseName("ix_sessions_state_absolute_expires_at");
    }
}
