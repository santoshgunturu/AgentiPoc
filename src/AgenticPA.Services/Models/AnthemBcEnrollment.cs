namespace AgenticPA.Services.Models;

public record AnthemBcEnrollment(
    string MemberId,
    string ClientId,
    string ContractNumber,
    string EffectiveDate,
    string TermDate,
    string Status);
