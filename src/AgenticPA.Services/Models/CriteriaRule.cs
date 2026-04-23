namespace AgenticPA.Services.Models;

public record CriteriaRule(
    IReadOnlyList<string> RequiredDiagnosisPrefixes,
    int RequiredConservativeTreatmentWeeks,
    IReadOnlyList<string> ValidPos,
    string? PolicyCitation = null,
    string? PolicyText = null);
