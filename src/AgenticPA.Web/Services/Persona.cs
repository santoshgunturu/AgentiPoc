namespace AgenticPA.Web.Services;

public record Persona(string Id, string Name, string Role, string Initials, string Email);

public static class Personas
{
    public static readonly IReadOnlyList<Persona> All = new Persona[]
    {
        new("intake",    "Sam Taylor",    "Intake Coordinator",    "ST", "sam.t@poc.local"),
        new("um-nurse",  "Priya Kumar",   "Utilization Mgmt Nurse","PK", "priya.k@poc.local"),
        new("physician", "Dr. Alex Rhee", "Medical Director",      "AR", "alex.r@poc.local")
    };
}
