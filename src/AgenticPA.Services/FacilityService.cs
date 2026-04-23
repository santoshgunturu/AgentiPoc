using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class FacilityService : IFacilityService
{
    private readonly JsonDataStore _store;

    public FacilityService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<Facility>> SearchAsync(string query)
    {
        string q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<Facility>>(_store.Facilities);
        }

        string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IReadOnlyList<Facility> matches = _store.Facilities
            .Where(f =>
            {
                string haystack = $"{f.Npi} {f.Name} {f.Type}";
                return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<PosValidation> ValidatePosForCptAsync(string facilityNpi, string cpt)
    {
        Facility? fac = _store.Facilities.FirstOrDefault(f =>
            string.Equals(f.Npi, facilityNpi, StringComparison.OrdinalIgnoreCase));
        if (fac is null)
        {
            return Task.FromResult(new PosValidation(false, string.Empty, Array.Empty<string>(), $"Facility {facilityNpi} not found"));
        }

        if (!_store.CriteriaRules.TryGetValue(cpt, out CriteriaRule? rule))
        {
            return Task.FromResult(new PosValidation(true, fac.Pos, Array.Empty<string>(), $"No POS restriction configured for CPT {cpt}"));
        }

        bool ok = rule.ValidPos.Contains(fac.Pos);
        string msg = ok
            ? $"POS {fac.Pos} is valid for CPT {cpt}"
            : $"POS {fac.Pos} is not among allowed [{string.Join(", ", rule.ValidPos)}] for CPT {cpt}";
        return Task.FromResult(new PosValidation(ok, fac.Pos, rule.ValidPos, msg));
    }
}
