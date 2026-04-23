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

        Members = Load<List<Member>>(dataDir, "members.json", options);
        Procedures = Load<List<Procedure>>(dataDir, "procedures.json", options);
        Providers = Load<List<Provider>>(dataDir, "providers.json", options);
        Facilities = Load<List<Facility>>(dataDir, "facilities.json", options);
        Diagnoses = Load<List<Diagnosis>>(dataDir, "diagnoses.json", options);
        CriteriaRules = Load<Dictionary<string, CriteriaRule>>(dataDir, "criteria-rules.json", options);
    }

    private static T Load<T>(string dataDir, string fileName, JsonSerializerOptions options)
    {
        string path = Path.Combine(dataDir, fileName);
        string json = File.ReadAllText(path);
        T? result = JsonSerializer.Deserialize<T>(json, options);
        return result ?? throw new InvalidOperationException($"Failed to deserialize {fileName}");
    }
}
