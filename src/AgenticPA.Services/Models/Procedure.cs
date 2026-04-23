namespace AgenticPA.Services.Models;

public record Procedure(
    string Cpt,
    string Description,
    bool AuthRequired,
    string BodyPart);
