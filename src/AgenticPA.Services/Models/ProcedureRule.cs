namespace AgenticPA.Services.Models;

public record ProcedureRule(
    IReadOnlyList<string> Contraindications,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<string> Alternatives,
    int ExpectedDurationMinutes);
