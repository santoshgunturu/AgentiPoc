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
}
