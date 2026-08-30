namespace AuthNexus.Modules.Audit;

public enum SecurityEventResult
{
    Succeeded = 1,
    Failed = 2,
    Denied = 3,
    Throttled = 4,
    Cancelled = 5,
    Informational = 6,
}
