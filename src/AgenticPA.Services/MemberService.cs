using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class MemberService : IMemberService
{
    private readonly JsonDataStore _store;

    public MemberService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<Member>> SearchAsync(string query)
    {
        string q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Member>>(_store.Members);
        }

        string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IReadOnlyList<Member> matches = _store.Members
            .Where(m =>
            {
                string haystack = $"{m.MemberId} {m.FirstName} {m.LastName} {m.Dob}";
                return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<Member?> GetContextAsync(string memberId)
    {
        Member? m = _store.Members.FirstOrDefault(x =>
            string.Equals(x.MemberId, memberId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(m);
    }
}
