using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class ProcedureService : IProcedureService
{
    private readonly JsonDataStore _store;

    public ProcedureService(JsonDataStore store) => _store = store;

    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "right", "bilateral", "the", "a", "an", "of", "for", "scan", "imaging", "study"
    };

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
}
