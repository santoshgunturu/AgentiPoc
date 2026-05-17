using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticPA.Web.Services;

public static class SessionSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(SessionSnapshot snap) => JsonSerializer.Serialize(snap, Options);
    public static SessionSnapshot? Deserialize(string? json)
        => string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<SessionSnapshot>(json, Options);
}
