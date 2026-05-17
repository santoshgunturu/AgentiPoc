# Agentic PA Intake — Production POC

A production-shaped proof-of-concept for an **agentic prior-authorization (PA)
intake workflow** built on the **Microsoft Agent Framework (MAF)**, **.NET 10**,
**MCP**, **.NET Aspire**, and **OpenAI / Azure OpenAI**.

## What it demonstrates

- **Per-skill agents** — each PA workflow step (Member, Procedure, Provider,
  Facility, Clinical, Pre-flight, Submit, Done) is a `ChatClientAgent` from
  Microsoft Agent Framework with its own rubric-driven instructions and a
  whitelist of MCP tools.
- **Rubric-driven prompts** — every agent's system prompt lives in
  `skills/*.md` with `{{include:}}` for shared rules (PHI protection,
  verification, conversation patterns, JSON command emission). Edit a rubric,
  refresh the page, new policy is live (dev mode).
- **Two workflow modes**:
  - **Interactive** — Blazor chat UI driven by `PaWorkflowEngine` (hand-rolled
    state machine with confirmation gates, rewind, slot accumulation).
  - **Express** — single-shot `POST /api/express-pa` driven by a MAF
    `AgentWorkflowBuilder` graph (preflight → submit → audit with conditional
    edges that short-circuit denials).
- **Deterministic decision boundary** — the LLM never decides. A rules engine
  loads `criteria-rules.json` + `policies.json` and produces the verdict.
- **MCP server** with 25 tools over HTTP+SSE.
- **Tool governance** — `ToolApprovalPolicy` wraps state-changing tools
  (`submit_pa`, `audit_submission`) in `ApprovalRequiredAIFunction` so the LLM
  cannot call them without a human round-trip.
- **Multi-surface exposure** — every agent is reachable via:
  - the Blazor UI at `/`
  - the MAF **DevUI** at `/devui`
  - **OpenAI-compatible REST** at `/v1/responses` and `/v1/conversations`
  - **A2A** (agent-to-agent) at `/a2a/<name>`
- **Aspire orchestration** — single AppHost project boots MCP server + Web,
  emits OTel traces / metrics / logs to the Aspire dashboard.
- **Session persistence** — `ISessionRepository` (in-memory by default; swap
  for Cosmos / Redis in prod).
- **RAG abstraction** — `IPolicyRagService` with an in-memory implementation
  and a Qdrant scaffold for production semantic policy search.
- **Containerized** — Dockerfiles for Web + MCP; GitHub Actions CI workflow
  builds, tests, and packages images.

## Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ AgenticPA.AppHost (.NET Aspire)                                              │
│  ├─ MCP server (port 7070)         25 tools, ModelContextProtocol.AspNetCore │
│  └─ Web (port 8080 in container)                                             │
│       ├─ Blazor UI                 interactive PA intake (rubric + skills)   │
│       ├─ /devui                    visual playground for every agent         │
│       ├─ /v1/responses             OpenAI-compatible REST surface            │
│       ├─ /v1/conversations         OpenAI-compatible conversations           │
│       ├─ /a2a/{agent}              agent-to-agent over HTTP+JSON             │
│       ├─ /api/express-pa           MAF workflow single-shot PA               │
│       └─ /health, /alive           Aspire ServiceDefaults health checks      │
│  + Aspire dashboard                OTel traces, metrics, logs                │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Solution layout

```
AgenticPA.slnx
src/
  AgenticPA.Services/        # mock services, rules engine, RAG, JSON data
  AgenticPA.McpServer/       # ASP.NET Core MCP server (25 tools)
  AgenticPA.Agent/           # skills, rubric loader, MCP client, MAF agents, workflows
  AgenticPA.ServiceDefaults/ # Aspire shared infra: OTel, resilience, health
  AgenticPA.Web/             # Blazor + DevUI + OpenAI REST + A2A + express PA
  AgenticPA.AppHost/         # Aspire DistributedApplication
skills/                      # rubric files (markdown with {{include:}} directives)
data/                        # seed JSON: members, procedures, providers, ...
tests/                       # xUnit
.github/workflows/ci.yml     # build, test, docker build on push
```

## Prereqs

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Node (optional, for `npx @modelcontextprotocol/inspector`)
- An OpenAI-compatible API key
- (Production) Docker, Azure CLI, an Azure subscription

## Running locally

### Option A — Aspire (recommended)

```bash
# 1. Set the API key (or appsettings)
setx OPENAI_API_KEY "sk-..."

# 2. Launch the AppHost — boots MCP server + Web + dashboard
dotnet run --project src/AgenticPA.AppHost
```

The Aspire dashboard opens automatically. Click the **DevUI** link on the `web`
resource to test individual agents, or open [http://localhost:5000](http://localhost:5000)
for the Blazor chat.

### Option B — Two terminals (legacy)

```bash
# terminal 1
dotnet run --project src/AgenticPA.McpServer

# terminal 2
dotnet run --project src/AgenticPA.Web --urls http://localhost:5000
```

## Configuration

| Variable | Required | Default | Notes |
|---|---|---|---|
| `OPENAI_API_KEY` | yes (or `Agent:ApiKey` in user-secrets) | — | OpenAI / compatible API key |
| `Agent:Provider` | no | `OpenAI` | `OpenAI` \| `Demo` (no LLM) |
| `Agent:Endpoint` | no | `https://api.openai.com/v1` | Override for Azure OpenAI |
| `Agent:Model` | no | `gpt-4o` | Use `gpt-5-mini` for Azure or `gpt-4o-mini` for cost |
| `Mcp:Url` | no | `http://localhost:7070/` | Aspire overrides via service discovery |
| `Rubric:Refresh:AlwaysReloadFromDisk` | no | `false` in prod, `true` in dev | live-reload rubrics |
| `Rubric:Refresh:WindowStartHour` / `EndHour` | no | 2 / 4 | nightly atomic-swap refresh window |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | no | Aspire injects | OTLP collector address |

User-secrets path (does not commit to git):

```bash
cd src/AgenticPA.Web
dotnet user-secrets init
dotnet user-secrets set Agent:ApiKey "sk-..."
```

## Demo scripts

### Interactive (Blazor at `/`)
- Click **Fill current step** → Send → **Confirm member** → repeats for each step → **Run pre-flight** → **Submit PA**.

### Express (POST `/api/express-pa`)
```json
{
  "memberId": "M1001", "cpt": "73721",
  "requestingNpi": "1111111111", "facilityNpi": "9990001",
  "icd10": "M17.12", "conservativeTreatmentWeeks": 8, "notes": "ice, NSAIDs"
}
```
Returns `auto-approve` with audit case id.

### Multi-surface
- **DevUI**: open `/devui`, pick `MemberAgent` from the dropdown, ask "Jane Smith"
- **OpenAI REST**: `curl POST /v1/responses` with the OpenAI SDK
- **A2A**: any A2A client can call `/a2a/member` / `/a2a/procedure` / etc.

## Production deployment

### Build images

```bash
docker build -t agenticpa-web:latest -f src/AgenticPA.Web/Dockerfile .
docker build -t agenticpa-mcp:latest -f src/AgenticPA.McpServer/Dockerfile .
```

### Compose locally

```bash
docker network create agenticpa
docker run -d --name mcp --network agenticpa -p 7070:7070 agenticpa-mcp:latest
docker run -d --name web --network agenticpa -p 8080:8080 \
  -e OPENAI_API_KEY=$OPENAI_API_KEY \
  -e Mcp__Url=http://mcp:7070/ \
  agenticpa-web:latest
```

### Azure Container Apps (preferred)

```bash
# From the AppHost project
azd up
```

The AppHost's `WithReference(mcp)` automatically wires service discovery and
emits a Bicep manifest. Aspire publishes the manifest; `azd` deploys it.

### Production hardening checklist

- [x] OTel `EnableSensitiveData = false` outside Development
- [x] Health endpoints (`/health`, `/alive`) Dev-only
- [x] HTTP resilience: 5-min total / 3-min per-attempt for LLM calls
- [x] Tool governance: `submit_pa` + `audit_submission` require human approval
- [x] User-secrets / env-vars only — no committed secrets
- [ ] Replace `AzureCliCredential` with `DefaultAzureCredential` (managed identity)
- [ ] Protect `/api/*`, `/v1/*`, `/a2a/*` with JWT / Entra ID
- [ ] Persist sessions to Cosmos / Redis (swap `ISessionRepository`)
- [ ] Enable Qdrant for semantic policy RAG (`QdrantPolicyRagService` scaffold)
- [ ] Container scan in CI (Trivy / Snyk)
- [ ] Add Azure Monitor exporter to ServiceDefaults

## Pluggability — adding a new skill

1. Drop a rubric: `skills/appeals-rubric.md` (use `{{include:_shared/...}}` for shared rules).
2. Add the agent registration in `MafAgentRegistration.AgentRubricMap`:
   ```csharp
   ["AppealsAgent"] = "appeals-rubric.md",
   ```
3. Map its MCP tools in `ResolveToolsForAgent`.
4. (Optional) Add a concrete `Skill` class for the interactive Blazor flow.

The agent automatically appears in DevUI, exposes `/a2a/appeals`, and is
discoverable via `/v1/responses`.

## What ISN'T in scope

- No real EHR / payer integration (mock JSON data).
- No persistence to Cosmos / Redis (in-memory repository scaffolded).
- No tenant isolation / auth (additive — wire JWT bearer + per-tenant
  `ISessionRepository` keys).
- No multi-region / multi-zone failover.

## Tests

```bash
dotnet test
```

Coverage: services (5), rules engine, state machine (3), MCP client + skills (3),
express MAF workflow (2), policy RAG (2). 13 tests total.

## License

Internal POC. Not for production patient data.
