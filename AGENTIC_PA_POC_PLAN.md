# Agentic PA Intake POC — Implementation Plan

> **For Claude Code.** Build this project in the phases below. Do not skip phases.
> After each phase, **stop, run the verification commands, and report results** before starting the next phase. Do not mark a phase complete until its verification passes.

---

## 0. Context & Goal

We are building a **proof-of-concept** that demonstrates an *agentic prior-authorization (PA) intake workflow* with these non-negotiable architectural properties:

1. **Deterministic state machine** drives the workflow (7 fixed steps).
2. **Per-step skills** — each state has its own focused LLM agent with its own system prompt and its own narrow set of tools.
3. **MCP server** exposes backend services as tools over **HTTP/SSE**.
4. **Mock backend services + rules engine** return JSON from sample data files.
5. **Blazor Server chat UI** for the demo.
6. **The LLM never makes the final medical decision.** The rules engine is deterministic and runs unchanged. The agent only *prepares* the request and *explains* the outcome.

The workflow steps are:
`member → procedure → requesting provider → facility → clinical context → pre-flight → submit`

Use **.NET 9** (LTS-adjacent, current stable). Use **Microsoft.Extensions.AI** as the agent abstraction — no Semantic Kernel. Use the official **ModelContextProtocol** NuGet package for MCP (version 0.1.x or later — check the latest on NuGet).

---

## Solution Layout (target)

```
AgenticPA.sln
src/
  AgenticPA.Services/        # class lib — mock services, models, rules engine, JSON data
  AgenticPA.McpServer/       # ASP.NET Core minimal API — HTTP/SSE MCP server wrapping services
  AgenticPA.Agent/           # class lib — skills, state machine, MCP client, orchestrator
  AgenticPA.Web/             # Blazor Server — chat UI, hosts the agent orchestrator
data/                        # JSON seed files (copied to AgenticPA.Services output)
tests/
  AgenticPA.Tests/           # xUnit — one fact per phase's verification
```

**Coding conventions:**
- File-scoped namespaces.
- Nullable enabled everywhere.
- `record` for DTOs, `class` for services.
- No `var` for public API returns; prefer explicit types in records/DTOs.
- No `Console.WriteLine` in library projects — use `ILogger<T>`.
- One class per file.
- xUnit + FluentAssertions for tests.

---

# Phase 1 — Solution scaffold + sample data

**Goal:** Empty but buildable solution with all four projects wired up and JSON seed data in place.

## Tasks

1. Create solution and projects:
   ```bash
   dotnet new sln -n AgenticPA
   dotnet new classlib   -n AgenticPA.Services  -o src/AgenticPA.Services  -f net9.0
   dotnet new web        -n AgenticPA.McpServer -o src/AgenticPA.McpServer -f net9.0
   dotnet new classlib   -n AgenticPA.Agent     -o src/AgenticPA.Agent     -f net9.0
   dotnet new blazorserver -n AgenticPA.Web     -o src/AgenticPA.Web       -f net9.0
   dotnet new xunit      -n AgenticPA.Tests     -o tests/AgenticPA.Tests   -f net9.0
   dotnet sln add src/**/*.csproj tests/**/*.csproj
   ```
2. Project references:
   - `AgenticPA.McpServer` → `AgenticPA.Services`
   - `AgenticPA.Agent` → `AgenticPA.Services` (only for shared DTO/contract types — **not** for direct service calls; agent reaches services via MCP)
   - `AgenticPA.Web` → `AgenticPA.Agent`
   - `AgenticPA.Tests` → all of the above
3. Create `data/` folder at repo root with these seed JSON files. Copy them to `AgenticPA.Services` output via `<None Include="..\..\data\*.json" CopyToOutputDirectory="PreserveNewest" Link="Data\%(Filename)%(Extension)" />` in the csproj.

### `data/members.json`
```json
[
  { "memberId": "M1001", "firstName": "Jane",  "lastName": "Smith", "dob": "1978-04-12", "plan": "BlueCare PPO",   "pcp": "Dr. Ramirez", "coverageActive": true },
  { "memberId": "M1002", "firstName": "Jane",  "lastName": "Smith", "dob": "1985-11-03", "plan": "BlueCare HMO",   "pcp": "Dr. Chen",    "coverageActive": true },
  { "memberId": "M1003", "firstName": "Jane",  "lastName": "Smith", "dob": "1992-07-21", "plan": "BlueCare PPO",   "pcp": "Dr. Patel",   "coverageActive": false },
  { "memberId": "M1004", "firstName": "John",  "lastName": "Doe",   "dob": "1965-02-28", "plan": "Medicare Adv.",  "pcp": "Dr. Ramirez", "coverageActive": true }
]
```

### `data/procedures.json`
```json
[
  { "cpt": "73721", "description": "MRI lower extremity without contrast",          "authRequired": true,  "bodyPart": "knee" },
  { "cpt": "73722", "description": "MRI lower extremity with contrast",             "authRequired": true,  "bodyPart": "knee" },
  { "cpt": "73723", "description": "MRI lower extremity with and without contrast", "authRequired": true,  "bodyPart": "knee" },
  { "cpt": "72148", "description": "MRI lumbar spine without contrast",             "authRequired": true,  "bodyPart": "spine" },
  { "cpt": "99213", "description": "Office visit, established patient",             "authRequired": false, "bodyPart": "n/a" }
]
```

### `data/providers.json`
```json
[
  { "npi": "1111111111", "name": "Dr. Elena Ramirez", "specialty": "Orthopedic Surgery", "inNetwork": true,  "state": "FL" },
  { "npi": "2222222222", "name": "Dr. Michael Chen",  "specialty": "Radiology",          "inNetwork": true,  "state": "FL" },
  { "npi": "3333333333", "name": "Dr. Priya Patel",   "specialty": "Family Medicine",    "inNetwork": false, "state": "GA" }
]
```

### `data/facilities.json`
```json
[
  { "npi": "9990001", "name": "Capital Imaging Center", "pos": "22", "type": "outpatient-hospital", "inNetwork": true  },
  { "npi": "9990002", "name": "Tallahassee ASC",        "pos": "24", "type": "ambulatory-surgery",  "inNetwork": true  },
  { "npi": "9990003", "name": "Downtown Radiology",     "pos": "11", "type": "office",              "inNetwork": false }
]
```

### `data/diagnoses.json`
```json
[
  { "icd10": "M17.11", "description": "Unilateral primary osteoarthritis, right knee" },
  { "icd10": "M17.12", "description": "Unilateral primary osteoarthritis, left knee"  },
  { "icd10": "S83.512A", "description": "Sprain of anterior cruciate ligament of left knee, initial encounter" }
]
```

### `data/criteria-rules.json` (the "rules engine" input)
```json
{
  "73721": {
    "requiredDiagnosisPrefixes": ["M17", "S83", "M23", "M25"],
    "requiredConservativeTreatmentWeeks": 6,
    "validPos": ["11", "22", "24"]
  },
  "72148": {
    "requiredDiagnosisPrefixes": ["M51", "M54", "G55"],
    "requiredConservativeTreatmentWeeks": 6,
    "validPos": ["11", "22"]
  }
}
```

## Verification

```bash
dotnet build AgenticPA.sln -c Debug
```
- Zero warnings, zero errors.
- Confirm JSON files appear in `src/AgenticPA.Services/bin/Debug/net9.0/Data/`.

**Stop here. Report build output. Do not proceed until green.**

---

# Phase 2 — Mock services + rules engine

**Goal:** `AgenticPA.Services` loads the JSON files and exposes strongly-typed services. The rules engine runs against the canonical PA request.

## Tasks

1. Create `Models/` with records — one per JSON file shape, plus:
   - `CanonicalPaRequest` (memberId, cpt, requestingNpi, facilityNpi, icd10, conservativeTreatmentWeeks, notes)
   - `RulesEvaluation { Outcome: "auto-approve" | "pend" | "deny", Gaps: string[], Explanation: string }`
2. Create a `JsonDataStore` singleton that loads all six JSON files once at startup from the `Data/` folder next to the assembly. Use `System.Text.Json` with camelCase.
3. Create services — plain interfaces, plain implementations, DI-ready:
   - `IMemberService`  — `SearchAsync(string query)`, `GetContextAsync(string memberId)`
   - `IProcedureService` — `SearchAsync(string query)`, `CheckAuthRequiredAsync(string cpt)`
   - `IProviderService` — `SearchAsync(string query, string? state)`, `GetNetworkStatusAsync(string npi)`
   - `IFacilityService` — `SearchAsync(string query)`, `ValidatePosForCptAsync(string facilityNpi, string cpt)`
   - `IDiagnosisService` — `SearchAsync(string query)`
   - `IRulesEngine` — `PreviewAsync(CanonicalPaRequest req)`, `SubmitAsync(CanonicalPaRequest req)`
     - Both methods run the same logic against `criteria-rules.json`. `PreviewAsync` is tagged as dry-run in the returned `Explanation`.
4. Rules logic (keep simple and deterministic):
   - If `authRequired == false` → auto-approve.
   - If `requiredDiagnosisPrefixes` has none matching the submitted ICD-10 → **deny** with gap `"diagnosis-not-covered"`.
   - If `conservativeTreatmentWeeks < required` → **pend** with gap `"insufficient-conservative-treatment"`.
   - If `facility.pos` not in `validPos` → **pend** with gap `"invalid-place-of-service"`.
   - Else → **auto-approve**.
5. Add `ServiceCollectionExtensions.AddAgenticPaServices()` extension method that registers all of the above.

## Tests (add to `AgenticPA.Tests`)

- `MemberService_FindsThreeJanes()` — search "Jane Smith" returns 3.
- `ProcedureService_MriKneeRequiresAuth()` — CPT 73721 → `authRequired = true`.
- `RulesEngine_AutoApprovesWhenAllGood()` — submit a clean request for 73721 + M17.12 + 8 weeks PT + POS 22 → `auto-approve`.
- `RulesEngine_PendsWhenPtInsufficient()` — same but 2 weeks PT → `pend` with `"insufficient-conservative-treatment"` gap.
- `RulesEngine_DeniesWhenDxMismatch()` — 73721 + ICD Z00.00 → `deny`.

## Verification

```bash
dotnet test --filter "FullyQualifiedName~AgenticPA.Tests"
```
All five tests pass. **Stop and report.**

---

# Phase 3 — MCP server over HTTP/SSE

**Goal:** `AgenticPA.McpServer` exposes every service method above as an MCP tool, discoverable and callable over HTTP/SSE.

## Tasks

1. Add NuGet packages to `AgenticPA.McpServer`:
   - `ModelContextProtocol` (latest preview; check NuGet for current version)
   - `ModelContextProtocol.AspNetCore`
2. In `Program.cs`:
   - `builder.Services.AddAgenticPaServices();`
   - Register the MCP server with HTTP/SSE transport and tool discovery via attributes.
   - Map the MCP endpoint (commonly `/sse` or `/mcp` — follow the package docs).
3. In `Tools/`, create one static class per service, each method decorated with `[McpServerTool]` and a clear `[Description]`:
   - `MemberTools`: `search_members(query)`, `get_member_context(memberId)`
   - `ProcedureTools`: `search_procedure_codes(query)`, `check_auth_required(cpt)`
   - `ProviderTools`: `search_providers(query, state?)`, `get_network_status(npi)`
   - `FacilityTools`: `search_facilities(query)`, `validate_pos_for_cpt(facilityNpi, cpt)`
   - `DiagnosisTools`: `search_diagnosis_codes(query)`
   - `RulesTools`: `preview_criteria_evaluation(canonicalRequest)`, `submit_pa(canonicalRequest)`
   - Each tool resolves its dependency from DI via a parameter (the MCP runtime supports parameter-level DI).
4. Bind the server to `http://localhost:7070`.

## Verification

1. Run the server: `dotnet run --project src/AgenticPA.McpServer`
2. Expected console output shows the server listening on `:7070` and the tool list.
3. Use the **MCP Inspector** (`npx @modelcontextprotocol/inspector`) or `curl` to:
   - Connect to `http://localhost:7070/sse`
   - List tools → should return all 11 tools with descriptions.
   - Call `search_members` with `{"query":"Jane"}` → returns 3 members.
   - Call `check_auth_required` with `{"cpt":"73721"}` → returns `true`.

**Stop and report the Inspector output.**

---

# Phase 4 — State machine

**Goal:** `AgenticPA.Agent/StateMachine/` holds the deterministic workflow. No LLM calls here — pure transitions and validation.

## Tasks

1. Define the state enum:
   ```csharp
   public enum PaState {
       MemberPending, ProcedurePending, ReqProviderPending,
       FacilityPending, ClinicalPending, Preflight, Submit, Done
   }
   ```
2. `PaWorkflowContext` record — carries all accumulated slot values (memberId, cpt, requestingNpi, facilityNpi, icd10, conservativeTreatmentWeeks, notes, preflightResult).
3. `IWorkflowCommand` marker interface and one command per transition:
   - `SetMemberCommand(string MemberId)`
   - `SetProcedureCommand(string Cpt)`
   - `SetRequestingProviderCommand(string Npi)`
   - `SetFacilityCommand(string FacilityNpi)`
   - `SetClinicalCommand(string Icd10, int WeeksPt, string Notes)`
   - `RunPreflightCommand()`
   - `SubmitCommand()`
4. `PaWorkflowEngine` class with a single method:
   `Task<PaWorkflowContext> HandleAsync(PaWorkflowContext ctx, IWorkflowCommand cmd)`
   - Validates the command is legal for the current state.
   - Throws `InvalidTransitionException` if not.
   - Updates the context and advances the state.
   - For `RunPreflightCommand` and `SubmitCommand`, calls the MCP client (injected) to hit `preview_criteria_evaluation` / `submit_pa`.
5. Keep this class **purely mechanical** — no LLM, no conversational logic.

## Tests

- `StateMachine_AdvancesMemberToProcedure()`
- `StateMachine_RejectsOutOfOrderCommand()` — throws when calling `SetFacilityCommand` while in `MemberPending`.
- `StateMachine_PreflightReturnsRulesOutcome()` — end-to-end happy path with a fake MCP client that returns a canned `auto-approve`.

## Verification

```bash
dotnet test --filter "StateMachine"
```
All green. **Stop and report.**

---

# Phase 5 — MCP client + skills

**Goal:** Each state has its own skill — a focused LLM call with a narrow system prompt and a whitelist of MCP tools. Skills are composed via Microsoft.Extensions.AI.

## Tasks

1. Add NuGet to `AgenticPA.Agent`:
   - `Microsoft.Extensions.AI`
   - `Microsoft.Extensions.AI.OpenAI` (works with any OpenAI-compatible endpoint — Anthropic via proxy, OpenAI, Azure OpenAI, or a local Ollama)
   - `ModelContextProtocol` (client side)
2. `Mcp/McpToolClient.cs` — thin wrapper around the MCP client SDK. Connects to `http://localhost:7070/sse`, lists tools, and can invoke one by name. Exposes the tools as `AIFunction` instances that Microsoft.Extensions.AI can pass to the chat client.
3. `Skills/ISkill` interface:
   ```csharp
   public interface ISkill {
       PaState Handles { get; }
       Task<SkillResponse> HandleTurnAsync(PaWorkflowContext ctx, string userMessage, CancellationToken ct);
   }
   public record SkillResponse(string ReplyToUser, IWorkflowCommand? CommandToApply);
   ```
4. `Skills/SkillBase.cs` abstract — holds an `IChatClient`, a system prompt, and a whitelisted tool subset (by name) pulled from `McpToolClient`.
5. One concrete skill per state (all in `Skills/`):
   - `MemberSkill` — tools: `search_members`, `get_member_context`. Prompt: *"You help the user pick a member. If multiple matches, ask for DOB. When you have one member, respond with a JSON block `{action:'set_member', memberId:'...'}`."*
   - `ProcedureSkill` — tools: `search_procedure_codes`, `check_auth_required`. Prompt covers contrast disambiguation and short-circuit if no auth needed.
   - `RequestingProviderSkill` — tools: `search_providers`, `get_network_status`.
   - `FacilitySkill` — tools: `search_facilities`, `validate_pos_for_cpt`.
   - `ClinicalSkill` — tools: `search_diagnosis_codes`. Prompts for ICD-10, weeks of PT, and free-text notes.
   - `PreflightSkill` — tools: `preview_criteria_evaluation`. Explains the preview outcome in plain English. Emits a `run_preflight` command if missing.
   - `SubmitSkill` — no tools. Confirms with user, emits `submit` command, then *after* the rules engine runs it explains the final outcome.
6. Each skill enforces structured output: the LLM is told to **end its reply with a fenced `json` block** containing either `{"action":"<command>", ...args}` or `{"action":"none"}`. The skill parses that block, produces the matching `IWorkflowCommand`, and strips the JSON from the user-facing reply.
7. `Orchestrator` class: given a user message and the current `PaWorkflowContext`, it:
   - Looks up the skill for the current state.
   - Calls `HandleTurnAsync`.
   - If a command is returned, feeds it to `PaWorkflowEngine`.
   - Returns the skill's user-facing reply and the updated context.

## Configuration

Add to `AgenticPA.Web/appsettings.Development.json`:
```json
{
  "Agent": {
    "Endpoint": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini",
    "ApiKey": "set-via-user-secrets-or-env"
  },
  "Mcp": {
    "Url": "http://localhost:7070/sse"
  }
}
```
Use `dotnet user-secrets set "Agent:ApiKey" "..."` — never commit the key.

## Tests

- `McpClient_ListsElevenTools()` — integration test that spins up the MCP server in-process and asserts tool count.
- `MemberSkill_AsksForDobWhenAmbiguous()` — use a **stub `IChatClient`** that returns a canned response; assert the skill's reply contains the word "DOB" and no command is emitted.
- `MemberSkill_EmitsSetMemberCommandWhenResolved()` — stub returns a response ending with `{"action":"set_member","memberId":"M1001"}`; assert a `SetMemberCommand` is produced.

## Verification

```bash
dotnet test --filter "Skill|McpClient"
```
All green. **Stop and report.**

---

# Phase 6 — Blazor Server chat UI

**Goal:** A working chat page where a user walks through a PA intake end-to-end. Visibly show the current state, the tools being called, and the rules engine verdict.

## Tasks

1. In `AgenticPA.Web`:
   - Add project ref to `AgenticPA.Agent`.
   - Register the orchestrator, MCP client, skills, and `IChatClient` in `Program.cs`.
   - Configure HttpClient lifetime for the MCP SSE connection (keep-alive).
2. Replace the default `Index.razor` with `Chat.razor`:
   - Three panes in a single page:
     - **Left rail (narrow):** Workflow progress — one row per state, current state highlighted, completed states checkmarked.
     - **Center (main):** Chat transcript — user bubbles on the right, agent bubbles on the left. Auto-scroll.
     - **Right rail (narrow):** "Under the hood" log — every MCP tool call with args + result, every state transition. Collapsible entries.
3. Session state:
   - One `PaWorkflowContext` per circuit, held in a scoped service (`ChatSessionState`).
   - On submit of a user message: push user bubble → call orchestrator → push agent bubble → re-render the rails.
4. Styling (from the frontend-design skill — **commit to a clear direction**):
   - Pick a refined, editorial aesthetic: cream background `#F1EFE8`, slate text `#2C2C2A`, one strong accent color (deep indigo `#3C3489`), monospace for tool names, serif display font for headings (e.g., *Fraunces* from Google Fonts), sans-serif body (*Inter Tight* or *Söhne*-equivalent like *General Sans*). **Do not** use generic Tailwind blue or purple-gradient-on-white.
   - The pre-flight state uses the pink accent (`#FBEAF0` background, `#4B1528` text) from the reference diagram — this visually marks it as the quality gate.
   - Rounded 8px corners on bubbles. Subtle shadow. No heavy borders.
   - Tool-call entries in the right rail: small monospace pill with the tool name, expandable to show JSON args/results.
5. Add a **"Start over"** button that resets the session and a **"Seed demo"** button that pre-fills with a plausible Jane Smith / MRI knee scenario for quick demos.

## Verification (manual)

1. Terminal 1: `dotnet run --project src/AgenticPA.McpServer` → listens on :7070.
2. Terminal 2: `dotnet run --project src/AgenticPA.Web` → serves the chat UI on :5000.
3. Open the browser, walk through this demo script:
   - User: *"I need a PA for Jane Smith."* → agent asks for DOB.
   - User: *"1978-04-12"* → agent confirms Jane, advances to procedure.
   - User: *"MRI left knee."* → agent confirms CPT 73721, auth required.
   - User: *"Dr. Ramirez."* → agent confirms NPI, advances to facility.
   - User: *"Capital Imaging."* → agent validates POS, advances to clinical.
   - User: *"M17.12, 8 weeks of PT, ice and NSAIDs."* → agent runs pre-flight.
   - Agent: *"Pre-flight looks clean — likely auto-approve. Submit?"*
   - User: *"yes"* → rules engine runs, agent reports outcome.
4. Also verify the **pend path**: re-run with only 2 weeks PT. Pre-flight should warn, user fixes, submit succeeds.

**Stop and report. Include a screenshot if possible.**

---

# Phase 7 — Polish & demo readiness

## Tasks

1. Add `README.md` at repo root with:
   - Architecture diagram reference (link to the SVG).
   - Prereqs: .NET 9 SDK, Node (for MCP inspector), an OpenAI-compatible API key.
   - Step-by-step run instructions (both terminals).
   - The two demo scripts (happy path + pend path).
   - A "what is / is not agentic here" callout: the agent never decides, the rules engine does.
2. Add a `Directory.Packages.props` with centrally managed package versions.
3. Add a `.gitignore` (standard .NET + `appsettings.*.Local.json`).
4. Add structured logging (`Microsoft.Extensions.Logging`) at INFO level for state transitions and tool calls. Make sure the right-rail log in the UI reads from the same log source (via an in-memory `ILoggerProvider`).
5. Final pass: no `TODO`, no commented-out code, no unused usings. Run `dotnet format`.

## Verification

```bash
dotnet build  -c Release
dotnet test   -c Release
dotnet format --verify-no-changes
```
All three green. Take a screen recording (or screenshots) of the two demo paths.

---

# Guardrails for Claude Code

- **Do not add features not listed in this plan.** No authentication, no database, no Docker, no CI. This is a POC.
- **Do not replace Microsoft.Extensions.AI with Semantic Kernel** or any other framework.
- **Do not let the agent call `submit_pa` directly.** Only the state machine calls it, and only after the user confirms in the Submit state.
- **Do not hardcode Jane Smith or CPT 73721 anywhere except the JSON seed files and the README's demo script.** Everything else reads from the JSON.
- **Do not skip the verification step at the end of any phase.** If a phase fails verification, fix it before moving on. Report failures verbatim; do not paper over them.
- **Ask for confirmation** before making any architectural choice that diverges from this doc (e.g., different MCP transport, different state machine library, different UI framework).

When all phases are complete and verified, produce a short summary (max 200 words) covering: what was built, the two demo scripts working, known limitations, and what the next iteration would add (e.g., persistence, auth, multi-tenant, real EHR integration).
