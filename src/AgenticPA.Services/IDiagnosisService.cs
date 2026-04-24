using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public record IcdPairing(bool Valid, string Message);

public interface IDiagnosisService
{
    Task<IReadOnlyList<Diagnosis>> SearchAsync(string query);
    Task<IReadOnlyList<Icd10Entry>> SearchHierarchyAsync(string query, string? category);
    Task<IcdPairing> ValidatePairingAsync(string icd10, string cpt);
}
