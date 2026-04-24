using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public record ProviderNetworkStatus(string Npi, string PlanId, bool InNetwork, string? EffectiveDate, string? TermDate);

public interface IProviderService
{
    Task<IReadOnlyList<Provider>> SearchAsync(string query, string? state);
    Task<Provider?> GetNetworkStatusAsync(string npi);
    Task<ProviderCredentials?> GetCredentialsAsync(string npi);
    Task<ProviderNetworkStatus> VerifyNetworkAsync(string npi, string planId);
}
