using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class ProviderService : IProviderService
{
    private readonly JsonDataStore _store;

    public ProviderService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<Provider>> SearchAsync(string query, string? state)
    {
        string q = (query ?? string.Empty).Trim();
        IEnumerable<Provider> seq = _store.Providers;
        if (q.Length > 0)
        {
            string[] tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            seq = seq.Where(p =>
            {
                string haystack = $"{p.Npi} {p.Name} {p.Specialty} {p.State}";
                return tokens.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
            });
        }
        if (!string.IsNullOrWhiteSpace(state))
        {
            seq = seq.Where(p => string.Equals(p.State, state, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<Provider>>(seq.ToList());
    }

    public Task<Provider?> GetNetworkStatusAsync(string npi)
    {
        Provider? p = _store.Providers.FirstOrDefault(x =>
            string.Equals(x.Npi, npi, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(p);
    }

    public Task<ProviderCredentials?> GetCredentialsAsync(string npi)
    {
        _store.ProviderCredentialsByNpi.TryGetValue(npi, out ProviderCredentials? creds);
        return Task.FromResult(creds);
    }

    public Task<ProviderNetworkStatus> VerifyNetworkAsync(string npi, string planId)
    {
        Provider? p = _store.Providers.FirstOrDefault(x =>
            string.Equals(x.Npi, npi, StringComparison.OrdinalIgnoreCase));
        if (p is null)
            return Task.FromResult(new ProviderNetworkStatus(npi, planId, false, null, null));
        // POC heuristic: in-network if provider.InNetwork and plan exists.
        bool planExists = _store.HealthPlans.Any(h => string.Equals(h.PlanId, planId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(new ProviderNetworkStatus(npi, planId, p.InNetwork && planExists, "2024-01-01", "2026-12-31"));
    }
}
