namespace AgenticPA.Services.Models;

public record FacilityCertification(
    IReadOnlyList<string> Accreditations,
    IReadOnlyList<string> PosTypes,
    IReadOnlyList<string> Capabilities);
