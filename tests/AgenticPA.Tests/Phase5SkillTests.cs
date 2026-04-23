using AgenticPA.Agent.Mcp;
using AgenticPA.Agent.Skills;
using AgenticPA.Agent.StateMachine;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgenticPA.Tests;

public class Phase5SkillTests
{
    private sealed class StubChatClient : IChatClient
    {
        private readonly string _reply;
        public StubChatClient(string reply) { _reply = reply; }
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _reply)));
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private static McpToolClient NewMcpClientStub()
    {
        // construct without connecting — EnsureConnectedAsync will try to connect, so skills that
        // call it in tests require a live server. For skill unit tests with stub chat, we swap
        // the IChatClient but EnsureConnectedAsync is still invoked. We therefore point at a
        // non-existent endpoint and catch — instead use an already-connected null strategy by
        // skipping the call. Easiest: point at the running phase-3 server.
        return new McpToolClient(new Uri("http://127.0.0.1:7070/"), NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task MemberSkill_AsksForDobWhenAmbiguous()
    {
        StubChatClient chat = new("""
        I found three members named Jane Smith. Could you provide the DOB so I can pick the right one?

        ```json
        {"action":"none"}
        ```
        """);
        MemberSkill skill = new(chat, NewMcpClientStub(), NullLogger<MemberSkill>.Instance);
        PaWorkflowContext ctx = PaWorkflowContext.Initial();

        SkillResponse resp = await skill.HandleTurnAsync(ctx, Array.Empty<ChatTurn>(), "Jane Smith", CancellationToken.None);

        resp.ReplyToUser.Should().Contain("DOB");
        resp.CommandToApply.Should().BeNull();
    }

    [Fact]
    public async Task MemberSkill_EmitsSetMemberCommandWhenResolved()
    {
        StubChatClient chat = new("""
        Got it — Jane Smith (DOB 1978-04-12), member M1001.

        ```json
        {"action":"set_member","memberId":"M1001"}
        ```
        """);
        MemberSkill skill = new(chat, NewMcpClientStub(), NullLogger<MemberSkill>.Instance);
        PaWorkflowContext ctx = PaWorkflowContext.Initial();

        SkillResponse resp = await skill.HandleTurnAsync(ctx, Array.Empty<ChatTurn>(), "1978-04-12", CancellationToken.None);

        resp.CommandToApply.Should().BeOfType<SetMemberCommand>();
        ((SetMemberCommand)resp.CommandToApply!).MemberId.Should().Be("M1001");
    }
}
