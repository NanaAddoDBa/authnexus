using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Applications;

internal sealed class ApplicationRedirectUriConfiguration : IEntityTypeConfiguration<ApplicationRedirectUriRecord>
{
    public void Configure(EntityTypeBuilder<ApplicationRedirectUriRecord> builder)
    {
        builder.ToTable(
            "application_redirect_uris",
            "applications",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_application_redirect_uris_application_id_not_empty",
                    "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_application_redirect_uris_sort_order",
                    "sort_order >= 0");
                table.HasCheckConstraint(
                    "ck_application_redirect_uris_uri_not_blank",
                    "btrim(redirect_uri) <> ''");
            });

        builder.HasKey(redirect => new { redirect.ApplicationId, redirect.RedirectUri })
            .HasName("pk_application_redirect_uris");

        builder.Property(redirect => redirect.ApplicationId)
            .HasColumnName("application_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(redirect => redirect.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(redirect => redirect.RedirectUri)
            .HasColumnName("redirect_uri")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(redirect => new { redirect.ApplicationId, redirect.SortOrder })
            .IsUnique()
            .HasDatabaseName("ux_application_redirect_uris_application_sort_order");
    }
}
