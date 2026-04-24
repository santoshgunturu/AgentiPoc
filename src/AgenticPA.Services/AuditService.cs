using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class AuditService : IAuditService
{
    private int _counter = 4831;

    public Task<AuditRecord> RecordAsync(CanonicalPaRequest req, string outcome)
    {
        string caseId = $"A-{DateTime.UtcNow:yyyy}-{Interlocked.Increment(ref _counter):00000}";
        return Task.FromResult(new AuditRecord(caseId, DateTime.UtcNow, req, outcome));
    }
}
