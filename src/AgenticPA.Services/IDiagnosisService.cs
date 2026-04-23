using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IDiagnosisService
{
    Task<IReadOnlyList<Diagnosis>> SearchAsync(string query);
}
