using AuthNexus.Application.Persistence;
using AuthNexus.Infrastructure.Persistence.Applications;
using AuthNexus.Infrastructure.Persistence.Audit;
using AuthNexus.Infrastructure.Persistence.Authentication;
using AuthNexus.Infrastructure.Persistence.Identity;
using AuthNexus.Infrastructure.Persistence.Notifications;
using AuthNexus.Infrastructure.Persistence.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAuthNexusPersistence(
        this IServiceCollection services,
        string connectionString,
        NotificationDestinationProtectionOptions destinationProtection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(destinationProtection);

        var protectionOptions = CopyProtectionOptions(destinationProtection);

        services.AddDbContext<AuthNexusDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsHistoryTable(
                    AuthNexusDbContext.MigrationHistoryTable,
                    AuthNexusDbContext.MigrationHistorySchema)));

        services.AddScoped<IAuthNexusUnitOfWork>(provider =>
            provider.GetRequiredService<AuthNexusDbContext>());
        services.AddScoped<IApplicationProfileRepository>(provider =>
            new ApplicationProfileRepository(
                provider.GetRequiredService<AuthNexusDbContext>()));
        services.AddScoped<IUserAccountRepository>(provider =>
            new UserAccountRepository(
                provider.GetRequiredService<AuthNexusDbContext>()));
        services.AddScoped<IAuthenticationTransactionRepository>(provider =>
            new AuthenticationTransactionRepository(
                provider.GetRequiredService<AuthNexusDbContext>()));
        services.AddScoped<ISessionRepository>(provider =>
            new SessionRepository(
                provider.GetRequiredService<AuthNexusDbContext>()));
        services.AddScoped<ISecurityEventRepository>(provider =>
            new SecurityEventRepository(
                provider.GetRequiredService<AuthNexusDbContext>()));

        services.AddSingleton(protectionOptions);
        services.AddSingleton<INotificationDestinationProtector>(provider =>
            new AesGcmNotificationDestinationProtector(
                provider.GetRequiredService<NotificationDestinationProtectionOptions>()));
        services.AddScoped<INotificationOutboxRepository>(provider =>
            new NotificationOutboxRepository(
                provider.GetRequiredService<AuthNexusDbContext>(),
                provider.GetRequiredService<INotificationDestinationProtector>()));

        return services;
    }

    private static NotificationDestinationProtectionOptions CopyProtectionOptions(
        NotificationDestinationProtectionOptions source)
    {
        var keys = source.Keys.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);

        return new NotificationDestinationProtectionOptions
        {
            CurrentKeyId = source.CurrentKeyId,
            Keys = keys,
        };
    }
}
