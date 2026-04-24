namespace AgenticPA.Services.Models;

public record Icd10Entry(
    string Icd10,
    string Description,
    string Chapter,
    string Category,
    IReadOnlyList<string> RelatedCodes);
