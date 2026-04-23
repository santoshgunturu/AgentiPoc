namespace AgenticPA.Services.Models;

public record Member(
    string MemberId,
    string FirstName,
    string LastName,
    string Dob,
    string Plan,
    string Pcp,
    bool CoverageActive);
