using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IProviderService
{
    Task<IReadOnlyList<Provider>> SearchAsync(string query, string? state);
    Task<Provider?> GetNetworkStatusAsync(string npi);
}
