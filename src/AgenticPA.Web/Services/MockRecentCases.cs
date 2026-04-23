namespace AgenticPA.Web.Services;

public record RecentCase(string CaseId, string Member, string Procedure, string Outcome, string Age);

public static class MockRecentCases
{
    public static readonly IReadOnlyList<RecentCase> All = new RecentCase[]
    {
        new("A-2026-04812", "M. Garcia",    "MRI L-spine",  "auto-approve", "2h ago"),
        new("A-2026-04811", "R. Johnson",   "Cardiac cath", "auto-approve", "3h ago"),
        new("A-2026-04810", "E. Williams",  "MRI shoulder", "pend",         "4h ago"),
        new("A-2026-04809", "M. Brown",     "CT abdomen",   "auto-approve", "5h ago"),
        new("A-2026-04808", "S. Davis",     "Knee arthro.", "pend",         "6h ago"),
        new("A-2026-04807", "D. Martinez",  "MRI brain",    "auto-approve", "yest."),
        new("A-2026-04806", "J. Hernandez", "MRI knee",     "deny",         "yest.")
    };
}
