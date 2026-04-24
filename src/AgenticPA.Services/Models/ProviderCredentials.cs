namespace AgenticPA.Services.Models;

public record ProviderCredentials(
    string LicenseState,
    string LicenseStatus,
    string LicenseExpires,
    IReadOnlyList<string> BoardCertifications,
    IReadOnlyList<string> Sanctions);
