using AuthNexus.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Audit;

internal sealed class SecurityEventRecordConfiguration : IEntityTypeConfiguration<SecurityEventRecord>
{
    public void Configure(EntityTypeBuilder<SecurityEventRecord> builder)
    {
        builder.ToTable("security_events", "audit", table =>
        {
            table.HasCheckConstraint("ck_security_events_event_id", "event_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_security_events_correlation_id", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_security_events_optional_ids", "(actor_user_id IS NULL OR actor_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (target_user_id IS NULL OR target_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (application_id IS NULL OR application_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id IS NULL OR session_id <> '00000000-0000-0000-0000-000000000000'::uuid)");
            table.HasCheckConstraint("ck_security_events_type", $"event_type IN ({BuildEventTypeValues()})");
            table.HasCheckConstraint("ck_security_events_result", "result BETWEEN 1 AND 6");
            table.HasCheckConstraint("ck_security_events_metadata_object", "jsonb_typeof(metadata) = 'object'");
        });

        builder.HasKey(record => record.EventId)
            .HasName("pk_security_events");

        builder.Property(record => record.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();
        builder.Property(record => record.Timestamp)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(64);
        builder.Property(record => record.Result)
            .HasColumnName("result");
        builder.Property(record => record.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(record => record.TargetUserId).HasColumnName("target_user_id");
        builder.Property(record => record.ApplicationId).HasColumnName("application_id");
        builder.Property(record => record.TenantId).HasColumnName("tenant_id");
        builder.Property(record => record.SessionId).HasColumnName("session_id");
        builder.Property(record => record.CorrelationId).HasColumnName("correlation_id");
        builder.Property(record => record.NetworkSummary)
            .HasColumnName("network_summary")
            .HasMaxLength(SecurityEvent.MaximumNetworkSummaryLength);
        builder.Property(record => record.UserAgentSummary)
            .HasColumnName("user_agent_summary")
            .HasMaxLength(SecurityEvent.MaximumUserAgentSummaryLength);
        builder.Property(record => record.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.HasIndex(record => record.Timestamp)
            .HasDatabaseName("ix_security_events_occurred_at");
        builder.HasIndex(record => record.CorrelationId)
            .HasDatabaseName("ix_security_events_correlation_id");
        builder.HasIndex(record => new { record.TargetUserId, record.Timestamp })
            .HasDatabaseName("ix_security_events_target_user_occurred_at");
        builder.HasIndex(record => new { record.ApplicationId, record.Timestamp })
            .HasDatabaseName("ix_security_events_application_occurred_at");
    }

    private static string BuildEventTypeValues() =>
        string.Join(
            ", ",
            Enum.GetValues<SecurityEventType>()
                .Select(eventType => $"'{eventType.ToCode()}'"));
}
