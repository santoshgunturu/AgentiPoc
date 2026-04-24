using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class DiagnosisService : IDiagnosisService
{
    private static readonly HashSet<string> IgnoredSearchTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "right", "bilateral", "the", "a", "an", "of", "for"
    };

    private readonly JsonDataStore _store;

    public DiagnosisService(JsonDataStore store) => _store = store;

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

    public Task<IReadOnlyList<Icd10Entry>> SearchHierarchyAsync(string query, string? category)
    {
        string q = (query ?? string.Empty).Trim();
        IEnumerable<Icd10Entry> seq = _store.Icd10Hierarchy;

        if (q.Length > 0)
        {
            string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !IgnoredSearchTokens.Contains(t))
                .ToArray();
            if (tokens.Length > 0)
            {
                seq = seq.Where(e =>
                {
                    string haystack = $"{e.Icd10} {e.Description} {e.Chapter} {e.Category}";
                    return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            seq = seq.Where(e => e.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<Icd10Entry>>(seq.ToList());
    }

    public Task<IcdPairing> ValidatePairingAsync(string icd10, string cpt)
    {
        if (!_store.CriteriaRules.TryGetValue(cpt, out CriteriaRule? rule))
        {
            return Task.FromResult(new IcdPairing(true, $"No pairing rule configured for CPT {cpt}"));
        }

        bool ok = rule.RequiredDiagnosisPrefixes.Any(p =>
            (icd10 ?? string.Empty).StartsWith(p, StringComparison.OrdinalIgnoreCase));

        string msg = ok
            ? $"ICD-10 {icd10} is an appropriate pairing for CPT {cpt}"
            : $"ICD-10 {icd10} is not in covered prefixes [{string.Join(", ", rule.RequiredDiagnosisPrefixes)}] for CPT {cpt}";
        return Task.FromResult(new IcdPairing(ok, msg));
    }
}
