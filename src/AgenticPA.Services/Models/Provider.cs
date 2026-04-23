namespace AgenticPA.Services.Models;

public record Provider(
    string Npi,
    string Name,
    string Specialty,
    bool InNetwork,
    string State);
