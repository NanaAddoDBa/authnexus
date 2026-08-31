using AuthNexus.Modules.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal sealed class NotificationOutboxMessageRecordConfiguration :
    IEntityTypeConfiguration<NotificationOutboxMessageRecord>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxMessageRecord> builder)
    {
        builder.ToTable("outbox_messages", "notifications", table =>
        {
            table.HasCheckConstraint("ck_outbox_message_id", "message_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_outbox_correlation_id", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_outbox_optional_ids", "(target_user_id IS NULL OR target_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (application_id IS NULL OR application_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid)");
            table.HasCheckConstraint("ck_outbox_channel", "channel BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_outbox_state", "state BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_outbox_times", "available_at >= created_at AND state_changed_at >= created_at");
            table.HasCheckConstraint("ck_outbox_attempt_count", "attempt_count >= 0");
            table.HasCheckConstraint("ck_outbox_notification_type", "notification_type ~ '^[a-z][a-z0-9._-]{0,99}$'");
            table.HasCheckConstraint("ck_outbox_destination_ciphertext", $"octet_length(destination_ciphertext) BETWEEN {ProtectedNotificationDestination.MinimumCiphertextLength} AND {ProtectedNotificationDestination.MaximumCiphertextLength}");
            table.HasCheckConstraint("ck_outbox_destination_key_id", "destination_protection_key_id ~ '^[A-Za-z0-9_.:-]{1,128}$'");
            table.HasCheckConstraint("ck_outbox_destination_format", $"destination_format_version = {ProtectedNotificationDestination.CurrentFormatVersion}");
            table.HasCheckConstraint("ck_outbox_payload_ciphertext", $"octet_length(payload_ciphertext) BETWEEN 1 AND {ProtectedNotificationPayload.MaximumCiphertextLength}");
            table.HasCheckConstraint("ck_outbox_payload_key_id", "payload_protection_key_id ~ '^[A-Za-z0-9_.:-]{1,128}$'");
            table.HasCheckConstraint("ck_outbox_payload_format", "payload_format_version > 0");
            table.HasCheckConstraint("ck_outbox_failure_code", "last_failure_code IS NULL OR last_failure_code ~ '^[a-z][a-z0-9._-]{0,63}$'");
            table.HasCheckConstraint("ck_outbox_version", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_outbox_delivery_shape", BuildDeliveryShapeConstraint());
        });

        builder.HasKey(record => record.MessageId).HasName("pk_outbox_messages");

        builder.Property(record => record.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();
        builder.Property(record => record.CorrelationId).HasColumnName("correlation_id");
        builder.Property(record => record.TargetUserId).HasColumnName("target_user_id");
        builder.Property(record => record.ApplicationId).HasColumnName("application_id");
        builder.Property(record => record.TenantId).HasColumnName("tenant_id");
        builder.Property(record => record.NotificationType)
            .HasColumnName("notification_type")
            .HasMaxLength(NotificationType.MaximumLength);
        builder.Property(record => record.Channel).HasColumnName("channel");
        builder.Property(record => record.DestinationCiphertext)
            .HasColumnName("destination_ciphertext")
            .HasColumnType("bytea");
        builder.Property(record => record.DestinationProtectionKeyId)
            .HasColumnName("destination_protection_key_id")
            .HasMaxLength(ProtectedNotificationDestination.MaximumKeyIdLength);
        builder.Property(record => record.DestinationFormatVersion)
            .HasColumnName("destination_format_version");
        builder.Property(record => record.PayloadCiphertext)
            .HasColumnName("payload_ciphertext")
            .HasColumnType("bytea");
        builder.Property(record => record.PayloadProtectionKeyId)
            .HasColumnName("payload_protection_key_id")
            .HasMaxLength(ProtectedNotificationPayload.MaximumProtectionKeyIdLength);
        builder.Property(record => record.PayloadFormatVersion)
            .HasColumnName("payload_format_version");
        builder.Property(record => record.State).HasColumnName("state");
        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.AvailableAt)
            .HasColumnName("available_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.StateChangedAt)
            .HasColumnName("state_changed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.AttemptCount).HasColumnName("attempt_count");
        builder.Property(record => record.LastAttemptedAt)
            .HasColumnName("last_attempted_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.DeliveredAt)
            .HasColumnName("delivered_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.PermanentlyFailedAt)
            .HasColumnName("permanently_failed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(record => record.LastFailureCode)
            .HasColumnName("last_failure_code")
            .HasMaxLength(NotificationDeliveryFailureCode.MaximumLength);
        builder.Property(record => record.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.HasIndex(record => record.CorrelationId)
            .HasDatabaseName("ix_outbox_messages_correlation_id");
        builder.HasIndex(record => new { record.State, record.NextAttemptAt })
            .HasDatabaseName("ix_outbox_messages_due");
        builder.HasIndex(record => new { record.TargetUserId, record.CreatedAt })
            .HasDatabaseName("ix_outbox_messages_target_user_created_at");
    }

    private static string BuildDeliveryShapeConstraint() =>
        "(state = 1 AND state_changed_at = created_at " +
        "AND attempt_count = 0 AND last_attempted_at IS NULL " +
        "AND next_attempt_at IS NOT NULL AND next_attempt_at = available_at " +
        "AND delivered_at IS NULL " +
        "AND permanently_failed_at IS NULL AND last_failure_code IS NULL) OR " +
        "(state = 2 AND state_changed_at >= available_at " +
        "AND attempt_count > 0 AND last_attempted_at IS NOT NULL " +
        "AND last_attempted_at = state_changed_at AND next_attempt_at IS NOT NULL " +
        "AND next_attempt_at > last_attempted_at AND delivered_at IS NULL " +
        "AND permanently_failed_at IS NULL AND last_failure_code IS NOT NULL) OR " +
        "(state = 3 AND state_changed_at >= available_at " +
        "AND attempt_count > 0 AND last_attempted_at IS NOT NULL " +
        "AND last_attempted_at = state_changed_at AND delivered_at IS NOT NULL " +
        "AND delivered_at = state_changed_at AND next_attempt_at IS NULL " +
        "AND permanently_failed_at IS NULL AND last_failure_code IS NULL) OR " +
        "(state = 4 AND state_changed_at >= available_at " +
        "AND attempt_count > 0 AND last_attempted_at IS NOT NULL " +
        "AND last_attempted_at = state_changed_at AND permanently_failed_at IS NOT NULL " +
        "AND permanently_failed_at = state_changed_at AND next_attempt_at IS NULL " +
        "AND delivered_at IS NULL AND last_failure_code IS NOT NULL)";
}
