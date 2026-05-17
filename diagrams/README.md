# Architecture diagrams

Four standalone SVGs designed for slide decks (16:9 friendly), printed handouts,
and architecture review meetings. Open them in a browser to view, or embed
directly in PowerPoint / Google Slides / Markdown (most tools render SVG).

## Which one to show whom

| Audience | Diagram | Why |
|---|---|---|
| Director / VP | **01 + 04** | Big-picture system + deployment story; covers value + production path. |
| Engineering manager | **01 + 02 + 03** | System + agent internals + protocol surfaces; covers feasibility + extensibility. |
| Compliance / security | **02 + 04** | Shows the deterministic decision boundary + auth/observability story. |
| New engineer onboarding | All four, in order | End-to-end mental model. |
| Partner integration team | **03** | Just the contract surfaces — what they call, where it lives. |

## The four diagrams

| File | Title | One-line summary |
|---|---|---|
| [`01-system-architecture.svg`](01-system-architecture.svg) | System architecture | The whole picture: Aspire host with MCP + Web inside, OpenAI outside, multi-surface endpoint list at the bottom of the Web box. |
| [`02-agent-anatomy.svg`](02-agent-anatomy.svg) | How one chat turn produces one workflow step | Left-to-right sequence: user → orchestrator → agent + rubric + tools → OpenAI loop → JSON command → state machine transition. Side panels explain the confirmation gate and why agents are rebuilt per turn. |
| [`03-multi-surface.svg`](03-multi-surface.svg) | One agent registry · five callers | Hub-and-spoke: 8 agents in the center, surrounded by Blazor / DevUI / OpenAI REST / A2A / Express PA / health. Bottom strip describes pluggability. |
| [`04-deployment.svg`](04-deployment.svg) | Local → Docker → Azure | Three side-by-side columns showing what changes between dev, on-prem container, and Azure Container Apps. Bottom strip lists what stays the same. |

## Tips for the presentation

- **Open diagram 01 first.** It contains the most context. Spend 2-3 minutes on it.
- **Diagram 02 is the "trust me, the LLM doesn't decide" slide.** Use it to defend determinism with compliance.
- **Diagram 03 is the "we're not building a one-off" slide.** Use it to defend reuse and platform investment.
- **Diagram 04 is the "we know how to ship this" slide.** Use it when leadership asks about TCO or migration risk.

## Tech credits

- **.NET 10** · the runtime
- **Microsoft Agent Framework (MAF)** · `ChatClientAgent`, `AgentWorkflowBuilder`,
  `AddAIAgent`, `MapDevUI`, `MapOpenAIResponses`, `MapA2AHttpJson`
- **Model Context Protocol (MCP)** · `ModelContextProtocol.AspNetCore` for the
  tool-server side, `ModelContextProtocol.Client` for the agent's tool consumer
- **.NET Aspire** · distributed app orchestration, OTel dashboard, service discovery
- **OpenAI / Azure OpenAI** · the LLM backend (swappable to Anthropic, Ollama,
  or any OpenAI-compatible endpoint)
