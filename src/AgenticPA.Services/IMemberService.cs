using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IMemberService
{
    Task<IReadOnlyList<Member>> SearchAsync(string query);
    Task<Member?> GetContextAsync(string memberId);
}
