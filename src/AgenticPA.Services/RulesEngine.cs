using AgenticPA.Services.Data;
using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public class RulesEngine : IRulesEngine
{
    public const string OutcomeAutoApprove = "auto-approve";
    public const string OutcomePend = "pend";
    public const string OutcomeDeny = "deny";

    private readonly JsonDataStore _store;

    public RulesEngine(JsonDataStore store) => _store = store;

    public Task<RulesEvaluation> PreviewAsync(CanonicalPaRequest req)
        => Task.FromResult(Evaluate(req, dryRun: true));

    public Task<RulesEvaluation> SubmitAsync(CanonicalPaRequest req)
        => Task.FromResult(Evaluate(req, dryRun: false));

    private RulesEvaluation Evaluate(CanonicalPaRequest req, bool dryRun)
    {
        string prefix = dryRun ? "[dry-run] " : string.Empty;

        Procedure? proc = _store.Procedures.FirstOrDefault(p =>
            string.Equals(p.Cpt, req.Cpt, StringComparison.OrdinalIgnoreCase));

        if (proc is null)
        {
            return new RulesEvaluation(
                OutcomeDeny,
                new[] { "unknown-procedure" },
                $"{prefix}CPT {req.Cpt} is not recognized.");
        }

        if (!proc.AuthRequired)
        {
            return new RulesEvaluation(
                OutcomeAutoApprove,
                Array.Empty<string>(),
                $"{prefix}CPT {req.Cpt} does not require prior authorization.");
        }

        if (!_store.CriteriaRules.TryGetValue(req.Cpt, out CriteriaRule? rule))
        {
            return new RulesEvaluation(
                OutcomePend,
                new[] { "no-criteria-configured" },
                $"{prefix}No criteria configured for CPT {req.Cpt}; manual review required.");
        }

        List<string> gaps = new();
        List<string> findings = new();

        bool dxMatches = rule.RequiredDiagnosisPrefixes.Any(p =>
            (req.Icd10 ?? string.Empty).StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (!dxMatches)
        {
            gaps.Add("diagnosis-not-covered");
            findings.Add($"Submitted ICD-10 '{req.Icd10}' does not match covered prefixes [{string.Join(", ", rule.RequiredDiagnosisPrefixes)}].");
            return DenyWithPolicy(req, rule, gaps, findings, dryRun);
        }

        if (req.ConservativeTreatmentWeeks < rule.RequiredConservativeTreatmentWeeks)
        {
            gaps.Add("insufficient-conservative-treatment");
            findings.Add($"Conservative treatment {req.ConservativeTreatmentWeeks} weeks is below required {rule.RequiredConservativeTreatmentWeeks} weeks.");
        }

        Facility? fac = _store.Facilities.FirstOrDefault(f =>
            string.Equals(f.Npi, req.FacilityNpi, StringComparison.OrdinalIgnoreCase));
        if (fac is null)
        {
            gaps.Add("facility-not-found");
            findings.Add($"Facility NPI {req.FacilityNpi} not found.");
        }
        else if (!rule.ValidPos.Contains(fac.Pos))
        {
            gaps.Add("invalid-place-of-service");
            findings.Add($"Facility POS {fac.Pos} not in allowed [{string.Join(", ", rule.ValidPos)}].");
        }

        if (gaps.Count > 0)
        {
            return new RulesEvaluation(
                OutcomePend,
                gaps,
                $"{prefix}{string.Join(" ", findings)}",
                rule.PolicyCitation,
                rule.PolicyText);
        }

        return new RulesEvaluation(
            OutcomeAutoApprove,
            Array.Empty<string>(),
            $"{prefix}Request meets all criteria for CPT {req.Cpt}.",
            rule.PolicyCitation,
            rule.PolicyText);
    }

    private RulesEvaluation DenyWithPolicy(CanonicalPaRequest req, CriteriaRule rule, List<string> gaps, List<string> findings, bool dryRun)
    {
        string prefix = dryRun ? "[dry-run] " : string.Empty;
        return new RulesEvaluation(OutcomeDeny, gaps, $"{prefix}{string.Join(" ", findings)}", rule.PolicyCitation, rule.PolicyText);
    }
}
