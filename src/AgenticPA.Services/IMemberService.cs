using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IMemberService
{
    Task<IReadOnlyList<Member>> SearchAsync(string query);
    Task<Member?> GetContextAsync(string memberId);

    Task<IReadOnlyList<Member>> SearchClientSpecificAsync(
        string? firstName, string? lastName, string? dob, string? memberId, string? healthPlanOrState);

    Task<IReadOnlyList<MemberEnrollment>> GetEnrollmentsAsync(string memberId, string? dateOfService);

    Task<IReadOnlyList<AnthemBcEnrollment>> SearchAnthemBcEnrollmentsAsync(string memberId, string clientId);
}
