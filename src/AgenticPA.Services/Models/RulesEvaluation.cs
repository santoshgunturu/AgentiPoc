namespace AgenticPA.Services.Models;

public record RulesEvaluation(
    string Outcome,
    IReadOnlyList<string> Gaps,
    string Explanation,
    string? PolicyCitation = null,
    string? PolicyText = null);
