using System.Security.Cryptography;
using System.Text;
using AuthNexus.Application.Persistence;
using AuthNexus.Domain;
using AuthNexus.Infrastructure.Persistence.Notifications;
using AuthNexus.Modules.Notifications;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AuthNexus.Persistence.Security.Tests;

public sealed class NotificationDestinationProtectionFixture : PostgreSqlTestFixture;

public sealed class NotificationDestinationProtectionTests :
    IClassFixture<NotificationDestinationProtectionFixture>
{
    private readonly NotificationDestinationProtectionFixture _fixture;

    public NotificationDestinationProtectionTests(
        NotificationDestinationProtectionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Stored_outbox_row_contains_ciphertext_and_no_plaintext_destination_column()
    {
        var destination = $"recipient-{Guid.NewGuid():N}@example.test";
        var message = CreateMessage(destination: destination);
        await AddAsync(message, TestDestinationProtectionOptions.CurrentOnly());

        await using var connection = await _fixture.Database.OpenConnectionAsync();
        var ciphertext = await ReadCiphertextAsync(connection, message.MessageId.Value);
        var destinationBytes = Encoding.UTF8.GetBytes(destination);

        Assert.DoesNotContain(destinationBytes, ciphertext);
        Assert.NotEqual(destinationBytes, ciphertext);

        await using var columnCommand = connection.CreateCommand();
        columnCommand.CommandText =
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'notifications'
              AND table_name = 'outbox_messages'
              AND column_name LIKE 'destination%'
            ORDER BY ordinal_position;
            """;
        var columns = new List<string>();
        await using var reader = await columnCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        Assert.Equal(
            [
                "destination_ciphertext",
                "destination_protection_key_id",
                "destination_format_version",
            ],
            columns);
    }

    [Fact]
    public async Task Ciphertext_tampering_is_detected_without_disclosing_the_destination()
    {
        var destination = $"tamper-{Guid.NewGuid():N}@example.test";
        var message = CreateMessage(destination: destination);
        await AddAsync(message, TestDestinationProtectionOptions.CurrentOnly());
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await ExecuteAsync(
            connection,
            """
            UPDATE notifications.outbox_messages
            SET destination_ciphertext = set_byte(
                destination_ciphertext,
                octet_length(destination_ciphertext) - 1,
                get_byte(destination_ciphertext, octet_length(destination_ciphertext) - 1) # 1)
            WHERE message_id = @message_id;
            """,
            message.MessageId.Value);

        var exception = await Assert.ThrowsAsync<CryptographicException>(
            () => LoadAsync(
                message.MessageId,
                TestDestinationProtectionOptions.CurrentOnly()));

        Assert.DoesNotContain(destination, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_key_id_is_rejected_with_a_non_disclosing_error()
    {
        var destination = $"unknown-key-{Guid.NewGuid():N}@example.test";
        var message = CreateMessage(destination: destination);
        await AddAsync(message, TestDestinationProtectionOptions.CurrentOnly());
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await ExecuteAsync(
            connection,
            """
            UPDATE notifications.outbox_messages
            SET destination_protection_key_id = 'retired-destination-key'
            WHERE message_id = @message_id;
            """,
            message.MessageId.Value);

        var exception = await Assert.ThrowsAsync<CryptographicException>(
            () => LoadAsync(
                message.MessageId,
                TestDestinationProtectionOptions.CurrentOnly()));

        Assert.DoesNotContain(destination, exception.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            "The notification destination cannot be opened with the configured key ring.",
            exception.Message);
    }

    [Fact]
    public async Task Wrong_key_material_for_the_recorded_key_id_is_rejected()
    {
        var message = CreateMessage();
        await AddAsync(message, TestDestinationProtectionOptions.CurrentOnly());
        var wrongKeyOptions = new NotificationDestinationProtectionOptions
        {
            CurrentKeyId = TestDestinationProtectionOptions.CurrentKeyId,
            Keys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [TestDestinationProtectionOptions.CurrentKeyId] =
                    TestDestinationProtectionOptions.PreviousKey,
            },
        };

        await Assert.ThrowsAsync<CryptographicException>(
            () => LoadAsync(message.MessageId, wrongKeyOptions));
    }

    [Fact]
    public async Task Copying_a_valid_ciphertext_to_another_message_is_rejected()
    {
        var first = CreateMessage();
        var second = CreateMessage();
        await AddAsync(first, TestDestinationProtectionOptions.CurrentOnly());
        await AddAsync(second, TestDestinationProtectionOptions.CurrentOnly());
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE notifications.outbox_messages AS target
            SET destination_ciphertext = source.destination_ciphertext,
                destination_protection_key_id = source.destination_protection_key_id,
                destination_format_version = source.destination_format_version
            FROM notifications.outbox_messages AS source
            WHERE target.message_id = @target_id
              AND source.message_id = @source_id;
            """;
        command.Parameters.AddWithValue("target_id", first.MessageId.Value);
        command.Parameters.AddWithValue("source_id", second.MessageId.Value);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());

        await Assert.ThrowsAsync<CryptographicException>(
            () => LoadAsync(
                first.MessageId,
                TestDestinationProtectionOptions.CurrentOnly()));
    }

    [Fact]
    public async Task Rotation_reads_previous_ciphertext_and_reencrypts_updates_with_current_key()
    {
        var oldMessage = CreateMessage();
        await AddAsync(oldMessage, TestDestinationProtectionOptions.PreviousOnly());

        var loadedWithRotatingRing = await LoadAsync(
            oldMessage.MessageId,
            TestDestinationProtectionOptions.Rotating());
        Assert.NotNull(loadedWithRotatingRing);
        Assert.Equal(oldMessage.Destination, loadedWithRotatingRing.Entity.Destination);

        var newMessage = CreateMessage();
        await AddAsync(newMessage, TestDestinationProtectionOptions.Rotating());
        await using var connection = await _fixture.Database.OpenConnectionAsync();

        Assert.Equal(
            TestDestinationProtectionOptions.PreviousKeyId,
            await ReadKeyIdAsync(connection, oldMessage.MessageId.Value));
        Assert.Equal(
            TestDestinationProtectionOptions.CurrentKeyId,
            await ReadKeyIdAsync(connection, newMessage.MessageId.Value));

        using (var provider = _fixture.Database.CreateServiceProvider(
                   TestDestinationProtectionOptions.Rotating()))
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>()
                .Update(loadedWithRotatingRing);
            await scope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync();
        }

        Assert.Equal(
            TestDestinationProtectionOptions.CurrentKeyId,
            await ReadKeyIdAsync(connection, oldMessage.MessageId.Value));

        var reloaded = await LoadAsync(
            oldMessage.MessageId,
            TestDestinationProtectionOptions.CurrentOnly());
        Assert.NotNull(reloaded);
        Assert.Equal(oldMessage.Destination, reloaded.Entity.Destination);
    }

    private async Task AddAsync(
        NotificationOutboxMessage message,
        NotificationDestinationProtectionOptions options)
    {
        using var provider = _fixture.Database.CreateServiceProvider(options);
        using var scope = provider.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>()
            .Add(message);
        await scope.ServiceProvider
            .GetRequiredService<IAuthNexusUnitOfWork>()
            .CommitAsync();
    }

    private async Task<Persisted<NotificationOutboxMessage>> LoadAsync(
        NotificationOutboxMessageId messageId,
        NotificationDestinationProtectionOptions options)
    {
        using var provider = _fixture.Database.CreateServiceProvider(options);
        using var scope = provider.CreateScope();
        var persisted = await scope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>()
            .GetByIdAsync(messageId);

        return Assert.IsType<Persisted<NotificationOutboxMessage>>(persisted);
    }

    private static NotificationOutboxMessage CreateMessage(string? destination = null) =>
        NotificationOutboxMessage.Create(
            new NotificationOutboxMessageId(Guid.NewGuid()),
            new CorrelationId(Guid.NewGuid()),
            targetUserId: null,
            applicationId: null,
            tenantId: null,
            new NotificationType("security.persistence_test"),
            NotificationChannel.Email,
            new NotificationDestination(
                destination ?? $"recipient-{Guid.NewGuid():N}@example.test"),
            ProtectedNotificationPayload.Create(
                [1, 2, 3, 4],
                "test-payload:v1",
                formatVersion: 1),
            new DateTimeOffset(2026, 8, 30, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 30, 18, 5, 0, TimeSpan.Zero));

    private static async Task<byte[]> ReadCiphertextAsync(
        NpgsqlConnection connection,
        Guid messageId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT destination_ciphertext FROM notifications.outbox_messages " +
            "WHERE message_id = @message_id;";
        command.Parameters.AddWithValue("message_id", messageId);
        return (byte[])(await command.ExecuteScalarAsync() ?? Array.Empty<byte>());
    }

    private static async Task<string> ReadKeyIdAsync(
        NpgsqlConnection connection,
        Guid messageId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT destination_protection_key_id FROM notifications.outbox_messages " +
            "WHERE message_id = @message_id;";
        command.Parameters.AddWithValue("message_id", messageId);
        return (string)(await command.ExecuteScalarAsync() ?? string.Empty);
    }

    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        Guid messageId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("message_id", messageId);
        return await command.ExecuteNonQueryAsync();
    }
}
