namespace AuthNexus.Infrastructure.Persistence;

internal interface IConcurrencyTrackedRecord
{
    Guid Version { get; set; }
}
