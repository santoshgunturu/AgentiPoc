using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class ProcedureService : IProcedureService
{
    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "right", "bilateral", "the", "a", "an", "of", "for", "scan", "imaging", "study"
    };

    private readonly JsonDataStore _store;

    public ProcedureService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<Procedure>> SearchAsync(string query)
    {
        string q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Procedure>>(_store.Procedures);
        }

        string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !IgnoredSearchTokens.Contains(t))
            .ToArray();

        if (tokens.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Procedure>>(_store.Procedures);
        }

        IReadOnlyList<Procedure> matches = _store.Procedures
            .Where(p =>
            {
                string haystack = $"{p.Cpt} {p.Description} {p.BodyPart}";
                return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<bool> CheckAuthRequiredAsync(string cpt)
    {
        Procedure? p = _store.Procedures.FirstOrDefault(x =>
            string.Equals(x.Cpt, cpt, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(p?.AuthRequired ?? false);
    }

    public Task<ProcedureRule?> GetProcedureRulesAsync(string cpt)
    {
        _store.ProcedureRules.TryGetValue(cpt, out ProcedureRule? rule);
        return Task.FromResult(rule);
    }

    public Task<string> CheckCoverageAsync(string cpt, string planId)
    {
        Procedure? p = _store.Procedures.FirstOrDefault(x =>
            string.Equals(x.Cpt, cpt, StringComparison.OrdinalIgnoreCase));
        if (p is null) return Task.FromResult("not-covered");
        if (!p.AuthRequired) return Task.FromResult("covered");
        // For the POC, assume covered for all recognized plans in health_plans.json.
        bool planExists = _store.HealthPlans.Any(h => string.Equals(h.PlanId, planId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(planExists ? "requires-pa" : "not-covered");
    }
}
