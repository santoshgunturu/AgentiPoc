using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public record PosValidation(bool Valid, string FacilityPos, IReadOnlyList<string> AllowedPos, string Message);

public record FacilityCapabilityCheck(bool Valid, IReadOnlyList<string> MissingCapabilities, string Message);

public interface IFacilityService
{
    Task<IReadOnlyList<Facility>> SearchAsync(string query);
    Task<PosValidation> ValidatePosForCptAsync(string facilityNpi, string cpt);
    Task<FacilityCertification?> GetCertificationsAsync(string facilityNpi);
    Task<FacilityCapabilityCheck> ValidateForProcedureAsync(string facilityNpi, string cpt);
}
