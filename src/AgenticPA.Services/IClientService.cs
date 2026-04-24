using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync();
    Task<Client?> FindAsync(string clientIdOrName);
}
