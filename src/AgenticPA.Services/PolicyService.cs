using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class PolicyService : IPolicyService
{
    private readonly JsonDataStore _store;
    public PolicyService(JsonDataStore store) => _store = store;

    public Task<Policy?> GetAsync(string cpt, string? planId, string? asOf)
    {
        Policy? p = _store.Policies.FirstOrDefault(x => string.Equals(x.Cpt, cpt, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(p);
    }
}
