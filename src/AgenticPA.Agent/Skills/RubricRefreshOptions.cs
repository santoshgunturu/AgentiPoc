namespace AgenticPA.Agent.Skills;

public class RubricRefreshOptions
{
    public const string SectionName = "Rubric:Refresh";

    /// <summary>If false, the background refresh service does nothing. Rubrics are still loaded on demand and cached for the lifetime of the process.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Local hour (0-23) at which the nightly refresh window opens.</summary>
    public int WindowStartHour { get; set; } = 2;

    /// <summary>Local hour (0-23) at which the nightly refresh window closes. May be smaller than start to wrap past midnight (e.g. Start=22, End=4).</summary>
    public int WindowEndHour { get; set; } = 4;

    /// <summary>How often the service wakes to check the window + quiescence.</summary>
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Only refresh if the system has been idle (zero in-flight turns) for this many seconds.</summary>
    public int QuiescePeriodSeconds { get; set; } = 60;

    /// <summary>
    /// Dev-mode: if true, SkillRubricLoader re-reads the rubric from disk on EVERY call
    /// (no caching). Convenient for iterating on rubric content without restarting.
    /// Leave false in production — use the nightly refresh instead.
    /// </summary>
    public bool AlwaysReloadFromDisk { get; set; } = false;
}
