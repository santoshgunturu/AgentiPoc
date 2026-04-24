namespace AgenticPA.Services.Models;

public record MemberAddress(string Street, string City, string State, string Zip);

public record MemberEnrollment(string PlanId, string EffectiveDate, string TermDate, string Status);

public record Member(
    string MemberId,
    string FirstName,
    string LastName,
    string Dob,
    string Plan,
    string Pcp,
    bool CoverageActive,
    string? MiddleName = null,
    string? Ssn = null,
    MemberAddress? Address = null,
    string? PlanId = null,
    string? ClientId = null,
    IReadOnlyList<MemberEnrollment>? Enrollments = null);
