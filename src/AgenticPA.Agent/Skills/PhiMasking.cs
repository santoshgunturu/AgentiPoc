namespace AgenticPA.Agent.Skills;

public static class PhiMasking
{
    public static string MaskSsn(string? ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn)) return string.Empty;
        string digits = new string(ssn.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "XXX-XX-XXXX";
        return $"XXX-XX-{digits[^4..]}";
    }

    public static string MaskMemberId(string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId)) return string.Empty;
        if (memberId.Length <= 4) return new string('*', memberId.Length);
        return new string('*', memberId.Length - 4) + memberId[^4..];
    }

    public static string MaskDob(string? dob)
    {
        if (string.IsNullOrWhiteSpace(dob)) return string.Empty;
        // yyyy-MM-dd → XXXX-XX-XX
        return "XXXX-XX-XX";
    }

    public static string Vague(string kind) => kind switch
    {
        "member"   => "a potential match",
        "provider" => "a potential provider",
        "facility" => "a potential facility",
        _          => "a potential match"
    };
}
