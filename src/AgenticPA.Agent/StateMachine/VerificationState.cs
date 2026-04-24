namespace AgenticPA.Agent.StateMachine;

public record VerificationState(
    IReadOnlyList<string> VerifiedFields,
    int Required,
    int Attempts)
{
    public bool IsComplete => VerifiedFields.Count >= Required;
    public static VerificationState Empty(int required) => new(Array.Empty<string>(), required, 0);

    public VerificationState WithVerified(string field)
    {
        if (VerifiedFields.Contains(field, StringComparer.OrdinalIgnoreCase)) return this;
        List<string> next = new(VerifiedFields) { field.ToLowerInvariant() };
        return this with { VerifiedFields = next };
    }

    public VerificationState IncrementAttempt() => this with { Attempts = Attempts + 1 };
}
