using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Applications;

internal sealed class ApplicationProfileConfiguration : IEntityTypeConfiguration<ApplicationProfileRecord>
{
    public void Configure(EntityTypeBuilder<ApplicationProfileRecord> builder)
    {
        builder.ToTable(
            "application_profiles",
            "applications",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_application_profiles_application_id_not_empty",
                    "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_application_profiles_tenant_id_not_empty",
                    "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint(
                    "ck_application_profiles_type",
                    "application_type IN (1, 2, 3, 4, 5, 6)");
                table.HasCheckConstraint(
                    "ck_application_profiles_audience",
                    "application_audience IN (1, 2, 3, 4, 5, 6)");
                table.HasCheckConstraint(
                    "ck_application_profiles_mode",
                    "application_mode IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_application_profiles_name_not_blank",
                    "btrim(application_name) <> ''");
                table.HasCheckConstraint(
                    "ck_application_profiles_locale_not_blank",
                    "btrim(default_locale) <> ''");
                table.HasCheckConstraint(
                    "ck_application_profiles_policy_reference_not_blank",
                    "btrim(authentication_policy_reference) <> ''");
                table.HasCheckConstraint(
                    "ck_application_profiles_schema_reference_not_blank",
                    "registration_schema_reference IS NULL OR btrim(registration_schema_reference) <> ''");
                table.HasCheckConstraint(
                    "ck_application_profiles_version_not_empty",
                    "version <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(profile => profile.ApplicationId)
            .HasName("pk_application_profiles");

        builder.Property(profile => profile.ApplicationId)
            .HasColumnName("application_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(profile => profile.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid");

        builder.Property(profile => profile.Type)
            .HasColumnName("application_type")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(profile => profile.Audience)
            .HasColumnName("application_audience")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(profile => profile.Mode)
            .HasColumnName("application_mode")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(profile => profile.ApplicationName)
            .HasColumnName("application_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(profile => profile.DefaultLocale)
            .HasColumnName("default_locale")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(profile => profile.AuthenticationPolicyReference)
            .HasColumnName("authentication_policy_reference")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(profile => profile.RegistrationSchemaReference)
            .HasColumnName("registration_schema_reference")
            .HasColumnType("text");

        builder.Property(profile => profile.Version)
            .HasColumnName("version")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasMany(profile => profile.AllowedRedirectUris)
            .WithOne()
            .HasForeignKey(redirect => redirect.ApplicationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_application_redirect_uris_application_profiles");
    }
}
