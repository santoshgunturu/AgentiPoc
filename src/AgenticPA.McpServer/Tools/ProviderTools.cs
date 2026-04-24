using System.ComponentModel;
using AgenticPA.Services;
using AgenticPA.Services.Models;
using ModelContextProtocol.Server;

namespace AgenticPA.McpServer.Tools;

[McpServerToolType]
public static class ProviderTools
{
    [McpServerTool(Name = "search_providers")]
    [Description("Search requesting providers by NPI, name, or specialty. Optional state filter.")]
    public static async Task<IReadOnlyList<Provider>> SearchProviders(
        IProviderService providers,
        [Description("Query: NPI, name fragment, or specialty")] string query,
        [Description("Optional 2-letter state code, e.g. FL")] string? state = null)
        => await providers.SearchAsync(query, state);

    [McpServerTool(Name = "get_network_status")]
    [Description("Return the network status for a provider by NPI.")]
    public static async Task<Provider?> GetNetworkStatus(
        IProviderService providers,
        [Description("Provider NPI")] string npi)
        => await providers.GetNetworkStatusAsync(npi);

    [McpServerTool(Name = "get_provider_credentials")]
    [Description("Return license, board certifications, and sanctions for a provider by NPI.")]
    public static async Task<ProviderCredentials?> GetProviderCredentials(
        IProviderService providers,
        [Description("Provider NPI")] string npi)
        => await providers.GetCredentialsAsync(npi);

    [McpServerTool(Name = "verify_provider_network")]
    [Description("Verify whether a provider is in-network for a specific plan and return effective dates.")]
    public static async Task<ProviderNetworkStatus> VerifyProviderNetwork(
        IProviderService providers,
        [Description("Provider NPI")] string npi,
        [Description("Plan id, e.g. P001")] string planId)
        => await providers.VerifyNetworkAsync(npi, planId);
}
