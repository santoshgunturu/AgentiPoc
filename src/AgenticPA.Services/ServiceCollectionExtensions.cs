using AgenticPA.Services.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticPA.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgenticPaServices(this IServiceCollection services)
    {
        services.AddSingleton<JsonDataStore>();
        services.AddSingleton<IMemberService, MemberService>();
        services.AddSingleton<IProcedureService, ProcedureService>();
        services.AddSingleton<IProviderService, ProviderService>();
        services.AddSingleton<IFacilityService, FacilityService>();
        services.AddSingleton<IDiagnosisService, DiagnosisService>();
        services.AddSingleton<IClientService, ClientService>();
        services.AddSingleton<IPolicyService, PolicyService>();
        services.AddSingleton<IAuditService, AuditService>();
        services.AddSingleton<IRulesEngine, RulesEngine>();
        return services;
    }
}
