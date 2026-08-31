using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Authentication;

internal sealed class AuthenticationTransactionRecordConfiguration
    : IEntityTypeConfiguration<AuthenticationTransactionRecord>
{
    public void Configure(EntityTypeBuilder<AuthenticationTransactionRecord> builder)
    {
        builder.ToTable(
            "authentication_transactions",
            "authentication",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_authentication_transactions_transaction_id",
                    "transaction_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_application_id",
                    "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_tenant_id",
                    "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_user_id",
                    "user_id IS NULL OR user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_correlation_id",
                    "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_purpose",
                    "purpose BETWEEN 1 AND 14");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_state",
                    "state BETWEEN 1 AND 8");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_lifetime",
                    "expires_at > created_at AND state_changed_at >= created_at");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_initial_state",
                    "state <> 1 OR state_changed_at = created_at");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_expiry_state",
                    "(state = 7 AND state_changed_at >= expires_at) OR " +
                    "(state <> 7 AND state_changed_at < expires_at)");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_terminal_state",
                    "(state = 5 AND completed_at IS NOT NULL AND " +
                    "completed_at = state_changed_at AND failed_at IS NULL) OR " +
                    "(state = 6 AND failed_at IS NOT NULL AND " +
                    "failed_at = state_changed_at AND completed_at IS NULL) OR " +
                    "(state NOT IN (5, 6) AND completed_at IS NULL AND failed_at IS NULL)");
                table.HasCheckConstraint(
                    "ck_authentication_transactions_version",
                    "version <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(record => record.TransactionId)
            .HasName("pk_authentication_transactions");

        builder.Property(record => record.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(record => record.ApplicationId)
            .HasColumnName("application_id")
            .HasColumnType("uuid");
        builder.Property(record => record.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid");
        builder.Property(record => record.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");
        builder.Property(record => record.Purpose)
            .HasColumnName("purpose")
            .HasColumnType("smallint");
        builder.Property(record => record.CorrelationId)
            .HasColumnName("correlation_id")
            .HasColumnType("uuid");
        builder.Property(record => record.State)
            .HasColumnName("state")
            .HasColumnType("smallint");
        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.StateChangedAt)
            .HasColumnName("state_changed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.Version)
            .HasColumnName("version")
            .HasColumnType("uuid")
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(record => record.ApplicationId)
            .HasDatabaseName("ix_authentication_transactions_application_id");
        builder.HasIndex(record => record.UserId)
            .HasDatabaseName("ix_authentication_transactions_user_id");
        builder.HasIndex(record => record.CorrelationId)
            .HasDatabaseName("ix_authentication_transactions_correlation_id");
        builder.HasIndex(record => new { record.State, record.ExpiresAt })
            .HasDatabaseName("ix_authentication_transactions_state_expires_at");
        builder.HasIndex(record => record.ExpiresAt)
            .HasDatabaseName("ix_authentication_transactions_expires_at");
    }
}
