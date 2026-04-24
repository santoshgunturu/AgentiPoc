using System.Reflection;
using System.Text.Json;
using AgenticPA.Services.Models;

namespace AgenticPA.Services.Data;

public class JsonDataStore
{
    public IReadOnlyList<Member> Members { get; }
    public IReadOnlyList<Procedure> Procedures { get; }
    public IReadOnlyList<Provider> Providers { get; }
    public IReadOnlyList<Facility> Facilities { get; }
    public IReadOnlyList<Diagnosis> Diagnoses { get; }
    public IReadOnlyDictionary<string, CriteriaRule> CriteriaRules { get; }

    public IReadOnlyList<Client> Clients { get; }
    public IReadOnlyList<HealthPlan> HealthPlans { get; }
    public IReadOnlyList<AnthemBcEnrollment> AnthemBcEnrollments { get; }
    public IReadOnlyDictionary<string, ProcedureRule> ProcedureRules { get; }
    public IReadOnlyDictionary<string, ProviderCredentials> ProviderCredentialsByNpi { get; }
    public IReadOnlyDictionary<string, FacilityCertification> FacilityCertificationsByNpi { get; }
    public IReadOnlyList<Icd10Entry> Icd10Hierarchy { get; }
    public IReadOnlyList<Policy> Policies { get; }

    public JsonDataStore()
    {
        string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Unable to resolve assembly directory");
        string dataDir = Path.Combine(assemblyDir, "Data");

        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        Members                      = Load<List<Member>>(dataDir, "members.json", options);
        Procedures                   = Load<List<Procedure>>(dataDir, "procedures.json", options);
        Providers                    = Load<List<Provider>>(dataDir, "providers.json", options);
        Facilities                   = Load<List<Facility>>(dataDir, "facilities.json", options);
        Diagnoses                    = Load<List<Diagnosis>>(dataDir, "diagnoses.json", options);
        CriteriaRules                = Load<Dictionary<string, CriteriaRule>>(dataDir, "criteria-rules.json", options);

        Clients                      = LoadOptional<List<Client>>(dataDir, "clients.json", options) ?? new List<Client>();
        HealthPlans                  = LoadOptional<List<HealthPlan>>(dataDir, "health_plans.json", options) ?? new List<HealthPlan>();
        AnthemBcEnrollments          = LoadOptional<List<AnthemBcEnrollment>>(dataDir, "anthem_bc_enrollments.json", options) ?? new List<AnthemBcEnrollment>();
        ProcedureRules               = LoadOptional<Dictionary<string, ProcedureRule>>(dataDir, "procedure_rules.json", options) ?? new Dictionary<string, ProcedureRule>();
        ProviderCredentialsByNpi     = LoadOptional<Dictionary<string, ProviderCredentials>>(dataDir, "provider_credentials.json", options) ?? new Dictionary<string, ProviderCredentials>();
        FacilityCertificationsByNpi  = LoadOptional<Dictionary<string, FacilityCertification>>(dataDir, "facility_certifications.json", options) ?? new Dictionary<string, FacilityCertification>();
        Icd10Hierarchy               = LoadOptional<List<Icd10Entry>>(dataDir, "icd10_hierarchy.json", options) ?? new List<Icd10Entry>();
        Policies                     = LoadOptional<List<Policy>>(dataDir, "policies.json", options) ?? new List<Policy>();
    }

    private static T Load<T>(string dataDir, string fileName, JsonSerializerOptions options)
    {
        string path = Path.Combine(dataDir, fileName);
        string json = File.ReadAllText(path);
        T? result = JsonSerializer.Deserialize<T>(json, options);
        return result ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }

    private static T? LoadOptional<T>(string dataDir, string fileName, JsonSerializerOptions options) where T : class
    {
        string path = Path.Combine(dataDir, fileName);
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}
