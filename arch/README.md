# Architecture diagrams

Mermaid C4 diagrams in this folder use **icons from [icones.js.org](https://icones.js.org/)** (Iconify). The icon pack used is **[Tabler](https://icones.js.org/collection/tabler)**.

## Rendering with icons

To render the diagrams with icons, register the Tabler icon pack as described in [Mermaid: Registering icon pack](https://mermaid.js.org/config/icons.html).

**Example (JavaScript):**

```javascript
import mermaid from 'mermaid';

mermaid.registerIconPacks([
  {
    name: 'tabler',
    loader: () =>
      fetch('https://unpkg.com/@iconify-json/tabler@1/icons.json').then((res) => res.json()),
  },
]);
```

For Mermaid CLI examples using raw sources and Markdown blocks, see [the rendering policy](DIAGRAMS.md#mermaid-diagrams).

Icons are specified in C4 elements via the optional `sprite` parameter (e.g. `"tabler:user"`, `"tabler:database"`). If the renderer does not support sprites or icon packs are not registered, the diagrams still render without icons.

## Rendering PlantUML diagrams

The helpers render `.puml` sources recursively under `arch/`, excluding `arch/templates/`, into PNG and SVG. Run from the repository root:

```bash
./arch/render-diagrams.sh
```

```powershell
pwsh arch/render-diagrams.ps1
```

The Bash script requires Docker; PowerShell prefers Docker and has a local `plantuml.jar` fallback. Both use the pinned image `plantuml/plantuml:1.2026.2`. Neither renders Mermaid sources or Markdown blocks. See [DIAGRAMS.md](DIAGRAMS.md) for prerequisites, format selection and rendering policy.

## Pinning remote includes

Several PlantUML files include remote C4 library snippets via `!includeurl`. To avoid unexpected breakage if the upstream repository changes, the diagrams in this repo pin those includes to the `release/1-0` tag. When updating PlantUML includes, prefer using a stable tag or commit hash rather than the `master` branch.

## Diagram files

Each source is listed individually, even when PlantUML and Mermaid describe the same subject. Paths are relative to this folder; rendered PNG/SVG images are not sources.

### PlantUML

| File | Diagram type | Description |
|------|--------------|-------------|
| [arch-c4-system.puml](arch-c4-system.puml) | C4Context | Church Bulletin users and external systems |
| [arch-c4-ai-software-factory.puml](arch-c4-ai-software-factory.puml) | C4Container | AI Software Factory SDLC workflow |
| [arch-c4-component-project-dependencies.puml](arch-c4-component-project-dependencies.puml) | C4Component | Solution projects and their dependencies |
| [arch-c4-class-domain-model.puml](arch-c4-class-domain-model.puml) | Class | Work order domain entities and relationships |
| [arch-class-domain-model-domain.puml](arch-class-domain-model-domain.puml) | Class | Core work order domain model |
| [arch-class-domain-model-commands.puml](arch-class-domain-model-commands.puml) | Class | Guarded state transitions and command objects |
| [arch-state-workorder.puml](arch-state-workorder.puml) | State | Work order states and transitions |
| [arch-devops-pipeline.puml](arch-devops-pipeline.puml) | C4Deployment | Church Bulletin build and deployment pipeline |
| [arch-logging-observability.puml](arch-logging-observability.puml) | Component | Logging and observability architecture |
| [arch-llm-mcp-sequence.puml](arch-llm-mcp-sequence.puml) | Sequence | Assign-to-bot flow through messaging, the worker and LLM/MCP |
| [WorflowForSaveDraftCommand.puml](WorflowForSaveDraftCommand.puml) | Sequence | Saving a work order draft |
| [WorflowForDraftToAssignedCommand.puml](WorflowForDraftToAssignedCommand.puml) | Sequence | Assigning a draft work order |
| [WorflowForAssignedToInProgressCommand.puml](WorflowForAssignedToInProgressCommand.puml) | Sequence | Starting an assigned work order |
| [WorflowForInProgressToCompleteCommand.puml](WorflowForInProgressToCompleteCommand.puml) | Sequence | Completing an in-progress work order |

### Mermaid sources

| File | Diagram type | Description |
|------|--------------|-------------|
| [ai-chat-dependencies.mmd](ai-chat-dependencies.mmd) | Flowchart | AI chat UI, handlers, MCP tools and data-access dependencies |
| [ai-chat-sequence.mmd](ai-chat-sequence.mmd) | Sequence | Application chat request and MCP tool-calling loop |
| [workorder-chat-sequence.mmd](workorder-chat-sequence.mmd) | Sequence | Work order chat request and local tool-calling loop |
| [arch-c4-system.md](arch-c4-system.md) | C4Context | Church Bulletin system context |
| [arch-c4-component-project-dependencies.md](arch-c4-component-project-dependencies.md) | C4Component | Solution/project structure |
| [arch-c4-class-domain-model.md](arch-c4-class-domain-model.md) | C4Component | Work order domain model |
| [arch-state-workorder.md](arch-state-workorder.md) | State | Work order state transitions |
| [arch-devops-pipeline.md](arch-devops-pipeline.md) | C4Deployment | Pipeline diagram and build/release documentation |
| [WorflowForSaveDraftCommand.md](WorflowForSaveDraftCommand.md) | Sequence | Saving a work order draft |
| [WorflowForDraftToAssignedCommand.md](WorflowForDraftToAssignedCommand.md) | Sequence | Assigning a draft work order |
| [WorflowForAssignedToInProgressCommand.md](WorflowForAssignedToInProgressCommand.md) | Sequence | Starting an assigned work order |
| [WorflowForInProgressToAssignedCommand.md](WorflowForInProgressToAssignedCommand.md) | Sequence | Returning an in-progress work order to assigned |
| [WorflowForInProgressToCompleteCommand.md](WorflowForInProgressToCompleteCommand.md) | Sequence | Completing an in-progress work order |
| [WorflowForInProgressToWaitingOnVendorCommand.md](WorflowForInProgressToWaitingOnVendorCommand.md) | Sequence | Pausing an in-progress work order for a vendor |
| [WorflowForWaitingOnVendorToInProgressCommand.md](WorflowForWaitingOnVendorToInProgressCommand.md) | Sequence | Resuming a work order waiting on a vendor |
| [WorflowForAssignedToCancelledCommand.md](WorflowForAssignedToCancelledCommand.md) | Sequence | Cancelling an assigned work order |
| [WorflowForInProgressToCancelledCommand.md](WorflowForInProgressToCancelledCommand.md) | Sequence | Cancelling an in-progress work order |
| [WorflowForWaitingOnVendorToCancelledCommand.md](WorflowForWaitingOnVendorToCancelledCommand.md) | Sequence | Cancelling a work order waiting on a vendor |

### Templates and shared includes

These PlantUML files support diagram authoring and are excluded from both render helpers.

| File | Diagram type | Description |
|------|--------------|-------------|
| [templates/arch-c4-system-context-template.puml](templates/arch-c4-system-context-template.puml) | C4Context template | Starting point for a system context diagram |
| [templates/arch-c4-container-template.puml](templates/arch-c4-container-template.puml) | C4Container template | Starting point for a container diagram |
| [templates/arch-c4-component-template.puml](templates/arch-c4-component-template.puml) | C4Component template | Starting point for a component diagram |
| [templates/arch-c4-class-template.puml](templates/arch-c4-class-template.puml) | Class template | Starting point for a domain class diagram |
| [templates/arch-4+1-development-template.puml](templates/arch-4+1-development-template.puml) | Development template | Starting point for a 4+1 development/DevOps view |
| [templates/plantuml-theme.puml](templates/plantuml-theme.puml) | Shared include | Common PlantUML styling and helpers, not a standalone diagram |

### Other architecture files

| File | Type | Description |
|------|------|-------------|
| [SolutionStructure.argr](SolutionStructure.argr) | Architecture graph | Solution modules and references; not rendered by the helpers |
| [runtime-view.md](runtime-view.md) | Pointer | Where the deployed runtime diagram lives (the platform's health dashboard) |
