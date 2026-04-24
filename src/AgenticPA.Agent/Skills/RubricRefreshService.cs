using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticPA.Agent.Skills;

/// <summary>
/// Periodically reloads the rubric cache from disk during a configured nightly window,
/// but only when the system is idle (no in-flight skill turns) to avoid swapping prompts
/// mid-conversation.
/// </summary>
public class RubricRefreshService : BackgroundService
{
    private readonly SkillRubricLoader _loader;
    private readonly InFlightCounter _inFlight;
    private readonly ILogger<RubricRefreshService> _logger;
    private readonly IOptionsMonitor<RubricRefreshOptions> _options;

    public RubricRefreshService(
        SkillRubricLoader loader,
        InFlightCounter inFlight,
        IOptionsMonitor<RubricRefreshOptions> options,
        ILogger<RubricRefreshService> logger)
    {
        _loader = loader;
        _inFlight = inFlight;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Track at most one refresh per window, so we don't reload every poll inside the window.
        DateOnly? lastRefreshedOnDate = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            RubricRefreshOptions opts = _options.CurrentValue;

            if (!opts.Enabled)
            {
                await Delay(TimeSpan.FromSeconds(opts.PollIntervalSeconds), stoppingToken);
                continue;
            }

            DateTime now = DateTime.Now; // local time for the window
            bool inWindow = IsInWindow(now, opts.WindowStartHour, opts.WindowEndHour);
            bool alreadyRefreshedToday = lastRefreshedOnDate == DateOnly.FromDateTime(now);

            if (inWindow && !alreadyRefreshedToday)
            {
                int inFlight = _inFlight.CurrentCount;
                TimeSpan idle = _inFlight.IdleFor;
                TimeSpan requiredIdle = TimeSpan.FromSeconds(opts.QuiescePeriodSeconds);

                if (inFlight == 0 && idle >= requiredIdle)
                {
                    try
                    {
                        int count = _loader.RefreshCache();
                        lastRefreshedOnDate = DateOnly.FromDateTime(now);
                        _logger.LogInformation(
                            "Rubric cache refreshed: {Count} file(s) reloaded. Idle={Idle}s, window={Start}-{End}.",
                            count, (int)idle.TotalSeconds, opts.WindowStartHour, opts.WindowEndHour);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Rubric cache refresh failed; keeping existing cache.");
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Rubric refresh window open but not quiescent: inFlight={InFlight}, idleFor={Idle}s (need {Need}s).",
                        inFlight, (int)idle.TotalSeconds, (int)requiredIdle.TotalSeconds);
                }
            }

            await Delay(TimeSpan.FromSeconds(opts.PollIntervalSeconds), stoppingToken);
        }
    }

    private static bool IsInWindow(DateTime now, int startHour, int endHour)
    {
        int h = now.Hour;
        if (startHour == endHour) return false;
        if (startHour < endHour) return h >= startHour && h < endHour;
        // wraps past midnight, e.g. start=22, end=4
        return h >= startHour || h < endHour;
    }

    private static async Task Delay(TimeSpan span, CancellationToken ct)
    {
        try { await Task.Delay(span, ct); }
        catch (TaskCanceledException) { /* shutting down */ }
    }
}
