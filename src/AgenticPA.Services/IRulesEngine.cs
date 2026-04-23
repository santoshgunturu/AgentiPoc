using AgenticPA.Services.Models;

namespace AgenticPA.Services;

public interface IRulesEngine
{
    Task<RulesEvaluation> PreviewAsync(CanonicalPaRequest req);
    Task<RulesEvaluation> SubmitAsync(CanonicalPaRequest req);
}
