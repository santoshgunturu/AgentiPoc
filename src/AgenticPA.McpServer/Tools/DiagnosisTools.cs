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

    [McpServerTool(Name = "search_icd10_hierarchy")]
    [Description("Search the ICD-10 hierarchy (returns chapter + category in addition to code + description).")]
    public static async Task<IReadOnlyList<Icd10Entry>> SearchIcd10Hierarchy(
        IDiagnosisService diagnoses,
        [Description("Query string (code or description)")] string query,
        [Description("Optional category filter")] string? category = null)
        => await diagnoses.SearchHierarchyAsync(query, category);

    [McpServerTool(Name = "validate_icd_procedure_pairing")]
    [Description("Validate whether an ICD-10 is an appropriate pairing for a CPT per policy rules.")]
    public static async Task<IcdPairing> ValidateIcdProcedurePairing(
        IDiagnosisService diagnoses,
        [Description("ICD-10 code")] string icd10,
        [Description("CPT code")] string cpt)
        => await diagnoses.ValidatePairingAsync(icd10, cpt);
}
