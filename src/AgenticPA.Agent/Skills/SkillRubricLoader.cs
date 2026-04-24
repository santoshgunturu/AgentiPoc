using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AgenticPA.Agent.Skills;

public class SkillRubricLoader
{
    private readonly string _rubricRoot;
    private readonly IOptionsMonitor<RubricRefreshOptions>? _options;

    // _cache is swapped atomically via Interlocked.Exchange during a refresh so that
    // in-flight reads always see a fully-rendered snapshot.
    private ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SkillRubricLoader()
    {
        _rubricRoot = ResolveRubricRoot();
    }

    public SkillRubricLoader(IOptionsMonitor<RubricRefreshOptions> options)
    {
        _rubricRoot = ResolveRubricRoot();
        _options = options;
    }

    public SkillRubricLoader(string rubricRoot)
    {
        _rubricRoot = rubricRoot;
    }

    public string RubricRoot => _rubricRoot;

    /// <summary>Last time the cache was rebuilt. Updated on every successful RefreshCache().</summary>
    public DateTime LastRefreshedUtc { get; private set; } = DateTime.UtcNow;

    public string Load(string rubricFileName)
    {
        // Dev-mode: always re-read from disk, no cache.
        if (_options?.CurrentValue.AlwaysReloadFromDisk == true)
        {
            return ReadWithIncludes(rubricFileName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        // Snapshot the reference so a concurrent refresh can't break this read.
        ConcurrentDictionary<string, string> snapshot = _cache;
        if (snapshot.TryGetValue(rubricFileName, out string? cached)) return cached;

        string rendered = ReadWithIncludes(rubricFileName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        snapshot[rubricFileName] = rendered;
        return rendered;
    }

    /// <summary>
    /// Rebuild the cache in full: re-read every previously-loaded rubric from disk and swap
    /// the cache reference atomically. Safe to call concurrently with Load().
    /// Returns the number of rubric files that were re-read.
    /// </summary>
    public int RefreshCache()
    {
        ConcurrentDictionary<string, string> previous = _cache;
        ConcurrentDictionary<string, string> next = new(StringComparer.OrdinalIgnoreCase);

        foreach (string fileName in previous.Keys)
        {
            string rendered = ReadWithIncludes(fileName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            next[fileName] = rendered;
        }

        Interlocked.Exchange(ref _cache, next);
        LastRefreshedUtc = DateTime.UtcNow;
        return next.Count;
    }

    private string ReadWithIncludes(string relativePath, HashSet<string> visited)
    {
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (!visited.Add(normalized)) return $"<!-- include cycle: {relativePath} -->";

        string full = Path.Combine(_rubricRoot, normalized);
        if (!File.Exists(full)) return $"<!-- rubric not found: {relativePath} -->";

        string content = File.ReadAllText(full);
        return Regex.Replace(content, @"\{\{include:([^}]+)\}\}", m =>
        {
            string include = m.Groups[1].Value.Trim();
            return ReadWithIncludes(include, visited);
        });
    }

    private static string ResolveRubricRoot()
    {
        string? assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        for (string? dir = assemblyDir; dir is not null; dir = Path.GetDirectoryName(dir))
        {
            string candidate = Path.Combine(dir, "skills");
            if (Directory.Exists(candidate)) return candidate;
        }
        string cwd = Path.Combine(Directory.GetCurrentDirectory(), "skills");
        return Directory.Exists(cwd) ? cwd : string.Empty;
    }
}
