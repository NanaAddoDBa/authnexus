using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Identity;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccountRecord>
{
    public void Configure(EntityTypeBuilder<UserAccountRecord> builder)
    {
        builder.ToTable(
            "user_accounts",
            "identity",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_user_accounts_user_id_not_empty",
                    "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_user_accounts_state",
                    "state IN (1, 2, 3, 4, 5, 6)");
                table.HasCheckConstraint(
                    "ck_user_accounts_state_changed_at",
                    "(state = 1 AND state_changed_at = created_at) OR " +
                    "(state <> 1 AND state_changed_at >= created_at)");
                table.HasCheckConstraint(
                    "ck_user_accounts_version_not_empty",
                    "version <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(account => account.UserId)
            .HasName("pk_user_accounts");

        builder.Property(account => account.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(account => account.State)
            .HasColumnName("state")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(account => account.StateChangedAt)
            .HasColumnName("state_changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(account => account.Version)
            .HasColumnName("version")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsConcurrencyToken()
            .IsRequired();
    }
}
