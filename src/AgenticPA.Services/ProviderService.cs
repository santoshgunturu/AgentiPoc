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
}
