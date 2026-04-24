using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public record AuditRecord(string CaseId, DateTime SubmittedAt, CanonicalPaRequest Request, string Outcome);

public interface IAuditService
{
    Task<AuditRecord> RecordAsync(CanonicalPaRequest req, string outcome);
}
