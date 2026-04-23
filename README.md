# Agentic PA Intake POC

A proof-of-concept that demonstrates an **agentic prior-authorization (PA) intake workflow** where:

- a **deterministic state machine** drives a 7-step flow,
- each state is handled by its own **focused LLM skill** with its own system prompt and whitelist of tools,
- a **HTTP MCP server** exposes mock backend services and a rules engine as tools,
- a **Blazor Server chat UI** visualizes the conversation, the state machine, and every tool call live.

> The LLM never makes the medical decision. The rules engine is deterministic and runs unchanged. The agent only *prepares* the canonical PA request and *explains* the outcome.

## Architecture

```
┌────────────────────────┐    HTTP/MCP     ┌────────────────────────────┐
│ AgenticPA.Web (Blazor) │ ──────────────▶ │ AgenticPA.McpServer        │
│  ChatSessionState      │                 │  11 tools over streamable- │
│  Orchestrator          │                 │  HTTP (MapMcp)             │
│  7 × Skill             │                 │                            │
│  McpToolClient         │                 │  AgenticPA.Services        │
│  PaWorkflowEngine      │                 │  + rules engine            │
│  IChatClient (OpenAI)  │                 │  + JSON seed data          │
└────────────────────────┘                 └────────────────────────────┘
```

Workflow: `member → procedure → requesting provider → facility → clinical context → pre-flight → submit`

## Prereqs

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or .NET 10 — the projects target `net9.0`)
- Node (only if you want to probe the MCP server with `npx @modelcontextprotocol/inspector`)
- An OpenAI-compatible API key (OpenAI, Azure OpenAI, or a local Ollama with OpenAI-compat endpoint)

## Configure the OpenAI key

Pick one of:

```bash
# user-secrets (recommended — never committed)
cd src/AgenticPA.Web
dotnet user-secrets init
dotnet user-secrets set "Agent:ApiKey" "sk-..."
```

```bash
# env var (picked up by AgenticPA.Web at startup)
setx OPENAI_API_KEY "sk-..."
```

To point at something other than OpenAI, edit `src/AgenticPA.Web/appsettings.Development.json`:

```json
"Agent": { "Endpoint": "https://api.openai.com/v1", "Model": "gpt-4o-mini" }
```

## Run (two terminals)

```bash
# terminal 1 — MCP server on :7070
dotnet run --project src/AgenticPA.McpServer

# terminal 2 — Blazor chat UI on :5000
dotnet run --project src/AgenticPA.Web --urls http://localhost:5000
```

Then open [http://localhost:5000](http://localhost:5000).

## Demo scripts

### Happy path (auto-approve)

1. User: *"I need a PA for Jane Smith."* → agent calls `search_members`, finds three Janes, asks for DOB.
2. User: *"1978-04-12"* → resolves M1001, advances to procedure.
3. User: *"MRI left knee without contrast."* → confirms CPT 73721, auth required.
4. User: *"Dr. Ramirez."* → confirms NPI 1111111111, advances to facility.
5. User: *"Capital Imaging."* → validates POS 22, advances to clinical.
6. User: *"M17.12, 8 weeks of PT, ice and NSAIDs."* → advances to pre-flight.
7. Agent runs `preview_criteria_evaluation` → *"Pre-flight looks clean — likely auto-approve. Submit?"*
8. User: *"yes"* → rules engine submits → **auto-approve**.

### Pend path

Run the same flow but say *"2 weeks of PT"* at step 6. Pre-flight warns `insufficient-conservative-treatment`. On submit, the rules engine returns **pend**.

## What IS and IS NOT agentic here

- ✅ **Agentic:** per-state LLM skills choose which tool to call from a narrow whitelist, disambiguate natural-language input, and translate rules-engine output into plain English.
- ❌ **Not agentic:** the final approve/pend/deny decision is made by `RulesEngine.cs` from `data/criteria-rules.json`. The agent never invents a verdict.

## Probe the MCP server directly

```bash
# list tools
curl -s -X POST http://localhost:7070/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'

# call search_members
curl -s -X POST http://localhost:7070/ \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call",
       "params":{"name":"search_members","arguments":{"query":"Jane"}}}'

# or use the official inspector
npx @modelcontextprotocol/inspector
# connect to: http://localhost:7070/
```

## Tests

```bash
dotnet test
```

Covers:

- `Phase2ServicesTests` — rules engine, member/procedure lookups (5 facts)
- `Phase4StateMachineTests` — transitions, illegal commands, preflight outcome (3 facts)
- `Phase5McpClientTests` / `Phase5SkillTests` — MCP tool discovery and two stubbed-chat skill behaviors (3 facts)

## Limitations / Next iteration

- Single-turn-per-message orchestration: the user has to type "yes" to move preflight → submit. A richer UX would let the preflight skill auto-advance.
- No persistence — session state lives only in memory per Blazor circuit.
- No authentication, multi-tenant split, or EHR integration (out of scope for POC).
- `submit_pa` is callable by the MCP server — the state machine guards it, but a production build would additionally restrict the tool to the agent's server-side identity.
