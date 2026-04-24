namespace AgenticPA.Services.Models;

public record Policy(
    string Cpt,
    string PolicyId,
    string Version,
    string Citation,
    string EffectiveDate,
    string FullText);
