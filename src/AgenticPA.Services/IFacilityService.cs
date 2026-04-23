using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public record PosValidation(bool Valid, string FacilityPos, IReadOnlyList<string> AllowedPos, string Message);

public interface IFacilityService
{
    Task<IReadOnlyList<Facility>> SearchAsync(string query);
    Task<PosValidation> ValidatePosForCptAsync(string facilityNpi, string cpt);
}
