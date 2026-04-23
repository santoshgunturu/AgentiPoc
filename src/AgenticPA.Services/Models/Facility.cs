namespace AgenticPA.Services.Models;

public record Facility(
    string Npi,
    string Name,
    string Pos,
    string Type,
    bool InNetwork);
