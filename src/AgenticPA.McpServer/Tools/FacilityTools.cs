using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class FacilityTools
{
    [McpServerTool(Name = "search_facilities")]
    [Description("Search facilities by NPI, name, or type.")]
    public static async Task<IReadOnlyList<Facility>> SearchFacilities(
        IFacilityService facilities,
        [Description("Query: NPI, facility name fragment, or type")] string query)
        => await facilities.SearchAsync(query);

    [McpServerTool(Name = "validate_pos_for_cpt")]
    [Description("Validate whether a facility's place-of-service is acceptable for the given CPT.")]
    public static async Task<PosValidation> ValidatePosForCpt(
        IFacilityService facilities,
        [Description("Facility NPI")] string facilityNpi,
        [Description("CPT code")] string cpt)
        => await facilities.ValidatePosForCptAsync(facilityNpi, cpt);

    [McpServerTool(Name = "get_facility_certifications")]
    [Description("Return accreditations, POS types, and capabilities for a facility by NPI.")]
    public static async Task<FacilityCertification?> GetFacilityCertifications(
        IFacilityService facilities,
        [Description("Facility NPI")] string facilityNpi)
        => await facilities.GetCertificationsAsync(facilityNpi);

    [McpServerTool(Name = "validate_facility_for_procedure")]
    [Description("Check the facility has the capabilities needed for the CPT (beyond POS — e.g. MRI-capable, Arthroscopy, Cardiac Cath).")]
    public static async Task<FacilityCapabilityCheck> ValidateFacilityForProcedure(
        IFacilityService facilities,
        [Description("Facility NPI")] string facilityNpi,
        [Description("CPT code")] string cpt)
        => await facilities.ValidateForProcedureAsync(facilityNpi, cpt);
}
