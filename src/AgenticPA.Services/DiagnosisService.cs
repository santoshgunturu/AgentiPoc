using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class DiagnosisService : IDiagnosisService
{
    private readonly JsonDataStore _store;

    public DiagnosisService(JsonDataStore store) => _store = store;

    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "right", "bilateral", "the", "a", "an", "of", "for"
    };

    public Task<IReadOnlyList<Diagnosis>> SearchAsync(string query)
    {
        string q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Diagnosis>>(_store.Diagnoses);
        }

        string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !IgnoredSearchTokens.Contains(t))
            .ToArray();

        if (tokens.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Diagnosis>>(_store.Diagnoses);
        }

        IReadOnlyList<Diagnosis> matches = _store.Diagnoses
            .Where(d =>
            {
                string haystack = $"{d.Icd10} {d.Description}";
                return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
        return Task.FromResult(matches);
    }
}
