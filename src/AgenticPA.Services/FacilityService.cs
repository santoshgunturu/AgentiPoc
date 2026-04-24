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

    public Task<FacilityCertification?> GetCertificationsAsync(string facilityNpi)
    {
        _store.FacilityCertificationsByNpi.TryGetValue(facilityNpi, out FacilityCertification? cert);
        return Task.FromResult(cert);
    }

    public Task<FacilityCapabilityCheck> ValidateForProcedureAsync(string facilityNpi, string cpt)
    {
        if (!_store.FacilityCertificationsByNpi.TryGetValue(facilityNpi, out FacilityCertification? cert))
        {
            return Task.FromResult(new FacilityCapabilityCheck(false, Array.Empty<string>(), $"Facility {facilityNpi} has no certification data"));
        }

        Procedure? proc = _store.Procedures.FirstOrDefault(p =>
            string.Equals(p.Cpt, cpt, StringComparison.OrdinalIgnoreCase));
        if (proc is null)
        {
            return Task.FromResult(new FacilityCapabilityCheck(false, Array.Empty<string>(), $"CPT {cpt} not recognized"));
        }

        List<string> missing = new();
        string desc = proc.Description.ToLowerInvariant();
        if (desc.Contains("mri") && !cert.Capabilities.Any(c => c.Contains("MRI", StringComparison.OrdinalIgnoreCase))) missing.Add("MRI");
        if (desc.Contains("ct")  && !cert.Capabilities.Any(c => c.Contains("CT",  StringComparison.OrdinalIgnoreCase))) missing.Add("CT");
        if (desc.Contains("arthroscopy") && !cert.Capabilities.Any(c => c.Contains("Arthroscopy", StringComparison.OrdinalIgnoreCase))) missing.Add("Arthroscopy");
        if (desc.Contains("catheterization") && !cert.Capabilities.Any(c => c.Contains("Cardiac Cath", StringComparison.OrdinalIgnoreCase))) missing.Add("Cardiac Cath");

        bool ok = missing.Count == 0;
        string msg = ok
            ? $"Facility capable of performing CPT {cpt}"
            : $"Facility missing required capabilities: {string.Join(", ", missing)}";
        return Task.FromResult(new FacilityCapabilityCheck(ok, missing, msg));
    }
}
