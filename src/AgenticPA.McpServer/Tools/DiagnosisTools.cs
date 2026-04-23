using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class DiagnosisTools
{
    [McpServerTool(Name = "search_diagnosis_codes")]
    [Description("Search ICD-10 diagnosis codes by code fragment or description.")]
    public static async Task<IReadOnlyList<Diagnosis>> SearchDiagnosisCodes(
        IDiagnosisService diagnoses,
        [Description("Query: ICD-10 fragment or clinical description")] string query)
        => await diagnoses.SearchAsync(query);
}
