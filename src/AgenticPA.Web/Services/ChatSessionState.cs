using AgenticPA.Agent;
using AgenticPA.Agent.Skills;
using AgenticPA.Agent.StateMachine;

namespace AgenticPA.Web.Services;

public class ChatSessionState
{
    private readonly List<ChatTurn> _transcript = new();
    private readonly List<SessionLogEntry> _log = new();

    public PaWorkflowContext Context { get; private set; } = PaWorkflowContext.Initial();
    public IReadOnlyList<ChatTurn> Transcript => _transcript;
    public IReadOnlyList<SessionLogEntry> Log => _log;
    public Persona Persona { get; private set; } = Personas.All[0];
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;

    public event Action? Changed;

    public void AppendUser(string text) { _transcript.Add(new ChatTurn("user", text)); RaiseChanged(); }
    public void AppendAssistant(string text) { _transcript.Add(new ChatTurn("assistant", text)); RaiseChanged(); }
    public void AppendCoach(string text) { _transcript.Add(new ChatTurn("coach", text)); RaiseChanged(); }
    public void AppendLog(SessionLogEntry entry) { _log.Add(entry); RaiseChanged(); }

    public void UpdateContext(PaWorkflowContext ctx) { Context = ctx; RaiseChanged(); }
    public void SetPersona(Persona persona) { Persona = persona; RaiseChanged(); }
    public void SetUrgency(PaUrgency urgency) { Context = Context with { Urgency = urgency }; RaiseChanged(); }

    public void Reset()
    {
        _transcript.Clear();
        _log.Clear();
        Context = PaWorkflowContext.Initial();
        StartedAt = DateTime.UtcNow;
        RaiseChanged();
    }

    public int ToolCallCount => _log.Count(l => l.Kind == "tool-call");
    public int GapsCaught => Context.PreflightResult?.Gaps.Count ?? 0;
    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

    public TimeSpan SlaBudget => Context.Urgency switch
    {
        PaUrgency.Expedited => TimeSpan.FromHours(24),
        PaUrgency.Retro     => TimeSpan.FromDays(30),
        _                   => TimeSpan.FromHours(72)
    };

    // --- Persistence ---

    public SessionSnapshot ToSnapshot() => new(
        SchemaVersion: 1,
        Context: Context,
        Transcript: _transcript.ToList(),
        Log: _log.ToList(),
        PersonaId: Persona.Id,
        StartedAt: StartedAt);

    public void LoadSnapshot(SessionSnapshot snap)
    {
        Context = snap.Context;
        _transcript.Clear();
        _transcript.AddRange(snap.Transcript);
        _log.Clear();
        _log.AddRange(snap.Log);
        Persona = Personas.All.FirstOrDefault(p => p.Id == snap.PersonaId) ?? Personas.All[0];
        StartedAt = snap.StartedAt;
        RaiseChanged();
    }

    private bool _suspendChanged;
    public IDisposable BatchChanges() { _suspendChanged = true; return new ResumeOnDispose(this); }
    private void RaiseChanged() { if (!_suspendChanged) Changed?.Invoke(); }

    private sealed class ResumeOnDispose : IDisposable
    {
        private readonly ChatSessionState _owner;
        public ResumeOnDispose(ChatSessionState owner) => _owner = owner;
        public void Dispose() { _owner._suspendChanged = false; _owner.Changed?.Invoke(); }
    }
}

public record SessionLogEntry(DateTime Ts, string Kind, string Title, string? Detail);

public record SessionSnapshot(
    int SchemaVersion,
    PaWorkflowContext Context,
    List<ChatTurn> Transcript,
    List<SessionLogEntry> Log,
    string PersonaId,
    DateTime StartedAt);
