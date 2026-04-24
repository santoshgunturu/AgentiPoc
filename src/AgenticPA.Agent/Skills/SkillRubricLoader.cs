using System.Reflection;
using System.Text.RegularExpressions;

namespace AgenticPA.Agent.Skills;

public class SkillRubricLoader
{
    private readonly string _rubricRoot;
    private readonly Dictionary<string, string> _cache = new();

    public SkillRubricLoader()
    {
        _rubricRoot = ResolveRubricRoot();
    }

    public SkillRubricLoader(string rubricRoot)
    {
        _rubricRoot = rubricRoot;
    }

    public string Load(string rubricFileName)
    {
        if (_cache.TryGetValue(rubricFileName, out string? cached)) return cached;
        string rendered = ReadWithIncludes(rubricFileName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        _cache[rubricFileName] = rendered;
        return rendered;
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
        // Fall back to CWD/skills
        string cwd = Path.Combine(Directory.GetCurrentDirectory(), "skills");
        return Directory.Exists(cwd) ? cwd : string.Empty;
    }
}
