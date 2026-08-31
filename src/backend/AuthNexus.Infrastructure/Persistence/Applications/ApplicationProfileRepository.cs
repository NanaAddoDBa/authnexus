using AuthNexus.Application.Persistence;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Applications;
using Microsoft.EntityFrameworkCore;
using DomainApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Infrastructure.Persistence.Applications;

internal sealed class ApplicationProfileRepository(AuthNexusDbContext dbContext)
    : IApplicationProfileRepository
{
    public async Task<Persisted<ApplicationProfile>?> GetByIdAsync(
        DomainApplicationId applicationId,
        CancellationToken cancellationToken = default)
    {
        if (applicationId.IsEmpty)
        {
            throw new ArgumentException("An application ID is required.", nameof(applicationId));
        }

        var record = await dbContext.Set<ApplicationProfileRecord>()
            .AsNoTracking()
            .Include(profile => profile.AllowedRedirectUris)
            .SingleOrDefaultAsync(
                profile => profile.ApplicationId == applicationId.Value,
                cancellationToken);

        return record is null ? null : ToPersisted(record);
    }

    public void Add(ApplicationProfile entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        dbContext.Set<ApplicationProfileRecord>().Add(ToRecord(entity));
    }

    private static ApplicationProfileRecord ToRecord(ApplicationProfile entity)
    {
        var redirects = entity.AllowedRedirectUris
            .Select(
                (redirect, index) => new ApplicationRedirectUriRecord(
                    entity.ApplicationId.Value,
                    index,
                    redirect.Value));

        return new ApplicationProfileRecord(
            entity.ApplicationId.Value,
            entity.TenantId?.Value,
            checked((short)entity.Type),
            checked((short)entity.Audience),
            checked((short)entity.Mode),
            entity.ApplicationName,
            entity.DefaultLocale,
            entity.AuthenticationPolicyReference,
            entity.RegistrationSchemaReference,
            Guid.NewGuid(),
            redirects);
    }

    private static Persisted<ApplicationProfile> ToPersisted(ApplicationProfileRecord record)
    {
        var entity = ApplicationProfile.Create(
            new DomainApplicationId(record.ApplicationId),
            record.TenantId is null ? null : new TenantId(record.TenantId.Value),
            (ApplicationType)record.Type,
            (ApplicationAudience)record.Audience,
            (ApplicationMode)record.Mode,
            record.ApplicationName,
            record.DefaultLocale,
            record.AuthenticationPolicyReference,
            record.RegistrationSchemaReference,
            record.AllowedRedirectUris
                .OrderBy(redirect => redirect.SortOrder)
                .Select(redirect => RedirectUri.Create(redirect.RedirectUri)));

        return new Persisted<ApplicationProfile>(entity, new PersistenceVersion(record.Version));
    }
}
