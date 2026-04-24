using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IPolicyService
{
    Task<Policy?> GetAsync(string cpt, string? planId, string? asOf);
}
