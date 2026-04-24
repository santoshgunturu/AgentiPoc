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
                string haystack = $"{m.MemberId} {m.FirstName} {m.MiddleName} {m.LastName} {m.Dob}";
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

    public Task<IReadOnlyList<Member>> SearchClientSpecificAsync(
        string? firstName, string? lastName, string? dob, string? memberId, string? healthPlanOrState)
    {
        IEnumerable<Member> seq = _store.Members;

        if (!string.IsNullOrWhiteSpace(memberId))
            seq = seq.Where(m => string.Equals(m.MemberId, memberId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(firstName))
            seq = seq.Where(m => m.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(lastName))
            seq = seq.Where(m => m.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(dob))
            seq = seq.Where(m => string.Equals(m.Dob, dob, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(healthPlanOrState))
        {
            string hp = healthPlanOrState.Trim();
            seq = seq.Where(m =>
                (m.Plan?.Contains(hp, StringComparison.OrdinalIgnoreCase) ?? false) ||
                string.Equals(m.Address?.State, hp, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<Member>>(seq.ToList());
    }

    public Task<IReadOnlyList<MemberEnrollment>> GetEnrollmentsAsync(string memberId, string? dateOfService)
    {
        Member? m = _store.Members.FirstOrDefault(x =>
            string.Equals(x.MemberId, memberId, StringComparison.OrdinalIgnoreCase));
        if (m is null || m.Enrollments is null)
            return Task.FromResult<IReadOnlyList<MemberEnrollment>>(Array.Empty<MemberEnrollment>());

        if (string.IsNullOrWhiteSpace(dateOfService))
            return Task.FromResult(m.Enrollments);

        IReadOnlyList<MemberEnrollment> filtered = m.Enrollments
            .Where(e => string.Compare(e.EffectiveDate, dateOfService, StringComparison.Ordinal) <= 0
                     && string.Compare(e.TermDate,       dateOfService, StringComparison.Ordinal) >= 0)
            .ToList();
        return Task.FromResult(filtered);
    }

    public Task<IReadOnlyList<AnthemBcEnrollment>> SearchAnthemBcEnrollmentsAsync(string memberId, string clientId)
    {
        IReadOnlyList<AnthemBcEnrollment> matches = _store.AnthemBcEnrollments
            .Where(e => string.Equals(e.MemberId, memberId, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(e.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(matches);
    }
}
