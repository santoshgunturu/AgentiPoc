using AgenticPA.Services.Models;

namespace AgenticPA.Agent.StateMachine;

public interface IRulesEngineClient
{
    Task<RulesEvaluation> PreviewAsync(CanonicalPaRequest request, CancellationToken ct);
    Task<RulesEvaluation> SubmitAsync(CanonicalPaRequest request, CancellationToken ct);
}
