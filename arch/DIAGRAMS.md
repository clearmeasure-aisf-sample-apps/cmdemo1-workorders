# Diagram versioning & rendering policy

See [README.md](README.md#diagram-files) for the source index. PlantUML (`.puml`), raw Mermaid (`.mmd`) and Markdown Mermaid blocks are separate source formats; the two render helpers handle **PlantUML only**. The solution architecture graph (`.argr`) is also outside their scope.

## PlantUML helpers

Run these commands from the repository root:

```bash
./arch/render-diagrams.sh
```

```powershell
pwsh arch/render-diagrams.ps1
```

Both scripts recursively discover `.puml` files under `arch/`, excluding `arch/templates/`, and render PNG and SVG beside each source with the same basename. Templates and the shared theme are authoring inputs, not batch-render targets.

- **Bash:** requires Bash and Docker with a running daemon; uses `plantuml/plantuml:1.2026.2`. It attempts a repository-mounted container so local includes resolve, then falls back to pipe mode for each format if the mounted command fails. There is no local-JAR fallback in this script.
- **PowerShell:** requires `pwsh`; prefers Docker when the command is available, using the same pinned image and attempting mount mode before pipe mode. If Docker is absent, it is configured to use `.tools/plantuml.jar` at the repository root or `arch/.tools/plantuml.jar`, with the repository-root JAR preferred and `java` required on PATH. A Docker rendering failure does not select the JAR fallback. The script locates `arch/` from its own path and changes its working directory there.

PowerShell defaults to both formats and exposes `-Formats` to select one:

```powershell
pwsh arch/render-diagrams.ps1 -Formats svg
```

The Bash helper always requests both PNG and SVG.

## Mermaid diagrams

Neither helper processes `.mmd` files or Mermaid blocks in `.md` files, including the `WorflowFor*.md` documents. Use a Mermaid-capable Markdown viewer for the documents, or Mermaid CLI (`mmdc`, installed separately) to produce images.

For a raw Mermaid source, run from the repository root:

```bash
mmdc -i arch/ai-chat-sequence.mmd -o arch/ai-chat-sequence.svg
```

Choose a `.png` output filename instead for PNG. For a Markdown document, copy the contents of its `mermaid` fence (without the fences or surrounding Markdown) into a temporary `.mmd` file, then pass that file to `mmdc`. Keep the original Markdown source unchanged. For C4 icon registration, see [README.md](README.md#rendering-with-icons); rendering without icon support still shows the diagram without icons.

## Versioning and CI

- When changing a non-template `.puml` source, run a helper and commit the source with its generated PNG and SVG. Do not edit generated images manually.
- When changing a Mermaid source, use Mermaid tooling to update any corresponding checked-in images; the PlantUML helpers do not update them.
- Keep remote PlantUML includes pinned to a stable tag or commit. The C4 library includes use RicardoNiepel/C4-PlantUML's `release/1-0` tag.
- The pull-request job in `.github/workflows/render-diagrams.yml` runs for changes under `arch/` (and its workflow/normalizer). It renders non-template PlantUML sources with the pinned Docker image and performs a normalized SVG comparison. It does not render Mermaid sources.
- An index or rendering-policy text change does not require regenerating unchanged diagrams.

## Troubleshooting

- If remote includes fail, check network access and the inclusion URLs. Pipe mode may not resolve local includes; the mounted repository or local JAR retains that context.
- Without Docker, use PowerShell's local-JAR path with Java available; the Bash helper cannot render in that environment.
- If rendering cannot be run locally, note that in the PR so reviewers can help produce the images.

## Optional pre-commit hook

Enable the repository's sample hook with:

```bash
git config core.hooksPath .githooks
```

When `.puml` files are staged, `.githooks/pre-commit` invokes the **Bash** helper and stages generated `arch/*.png` and `arch/*.svg` files. It therefore requires Docker even when PowerShell's local-JAR fallback is available. It does not render Mermaid files.

## PR checklist

- Update the relevant source and its entry in the index when adding a diagram.
- Re-render changed diagrams with the tool for their source format.
- Commit corresponding generated images with changed sources.
- Keep the Render diagrams CI check green.
