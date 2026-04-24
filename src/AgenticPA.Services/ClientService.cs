using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class ClientService : IClientService
{
    private readonly JsonDataStore _store;
    public ClientService(JsonDataStore store) => _store = store;

    public Task<IReadOnlyList<Client>> GetAllAsync() => Task.FromResult(_store.Clients);

    public Task<Client?> FindAsync(string clientIdOrName)
    {
        string q = (clientIdOrName ?? string.Empty).Trim();
        Client? c = _store.Clients.FirstOrDefault(x =>
            string.Equals(x.ClientId, q, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(c);
    }
}
