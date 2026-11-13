# Architecture guardrails

The architecture rules in [architecture-layers.md](architecture-layers.md) are enforced by the build and by pull
request checks, not by review comments. Each gate below fails deterministically: a build error, a failing unit
test, or a failing check on the pull request.

| Gate | Where it runs | Fails when |
|---|---|---|
| Onion project references | Every `dotnet build` (`src/Directory.Build.targets`) | Core references any project, or DataAccess references a project other than Core |
| Architecture tests (ArchUnitNET) | Unit tests (`src/UnitTests/Architecture`), the private build and every CI build | A layer's types depend on a layer outside its allowed set, a namespace cycle appears, or a convention breaks |
| Central Package Management | Every restore (`src/Directory.Packages.props`) | A `PackageReference` carries its own `Version` (NU1008) or a `VersionOverride` (NU1013) |
| NuGet package gate | Pull request check **NuGet package gate** (`.github/workflows/nuget-package-gate.yml`) | The pull request adds a package ID that its merge base does not have, without an approval |
| Namespace matches folder | Every `dotnet build` (IDE0130, `src/.editorconfig`) | A file's namespace is not the project's `RootNamespace` plus its folder path |
| Banned APIs in Core | Every build of Core (RS0030, `src/Core/BannedSymbols.txt`) | Core reads the system clock (`DateTime.Now`, `UtcNow`, `Today`, `DateTimeOffset.Now`, `UtcNow`) instead of `TimeProvider` |
| Code owners | Pull request review (`.github/CODEOWNERS`) | A change to project files, package versions, NuGet sources, `Directory.Build.*` or a gate has no code owner approval (when the ruleset requires it) |

## Onion project references

`src/Directory.Build.targets` runs before project references are resolved in every build. It stops the build with
`error ONION001` when `src/Core/Core.csproj` has any `ProjectReference`, or `src/DataAccess/DataAccess.csproj` has a
`ProjectReference` other than Core. The analyzer-only reference that `src/Directory.Build.props` adds to every
project is a build tool, not a layer, and is ignored.

## Architecture tests

The fixtures in `src/UnitTests/Architecture` load the compiled assemblies with ArchUnitNET, which reads the IL, so a
dependency inside a method body counts.

- `OnionLayerDependencyTests`: each layer depends only on the layers listed for it in
  `SolutionArchitecture.Layers`, which mirrors the project references (Core: none; DataAccess and LlmGateway: Core;
  UI.Shared: Core, LlmGateway; and so on). Nothing depends on the composition root, UI.Server. The namespaces inside
  each layer are free of dependency cycles.
- `CodeConventionTests`: interfaces start with `I`; Core does not depend on EF Core, ASP.NET Core, SQL clients,
  NServiceBus, Azure, Lamar or AI SDKs; concrete `IStateCommand` classes live in `Core.Model.StateCommands` and end
  in `Command`; concrete controllers live in a `.Controllers` namespace and end in `Controller`.
- `ArchitectureRulesTests` (older, file based): Core has no project references, UI projects do not reference
  DataAccess beyond the baseline, MediatR handlers end in `Handler`.

Known exception: Core (Model, Queries, Services), UI.Shared (Pages, Components, Models, Services) and McpServer
(Tools, Tools.Arguments) already had namespace cycles when the cycle rule was added. They are listed in
`SolutionArchitecture.LayersWithKnownNamespaceCycles`. The list may only shrink: a listed layer that becomes
cycle-free fails `ShouldStillHaveACycle_WhenLayerIsOnTheKnownCycleList` until it is taken off the list.

## Central Package Management and the NuGet package gate

Every package version lives in `src/Directory.Packages.props`. Project files name packages without versions, and
`VersionOverride` is disabled. The one package that resolved to two versions before Central Package Management,
`Microsoft.AspNetCore.Components.Authorization` (10.0.11 in UnitTests, 10.0.0 elsewhere), keeps both versions as
two `PackageVersion` items keyed by project name.

The **NuGet package gate** check compares the pull request head with its merge base. It reads every tracked
`*.csproj`, `*.props` and `*.targets` file (`PackageVersion`, `PackageReference`, `GlobalPackageReference`,
`PackageDownload` and versioned MSBuild SDKs) and fails with
`New NuGet package '<Id>' is not allowed without approval` for each new package ID. A version change of an existing
package is not a new package. Run it locally with
`pwsh scripts/nuget/Test-NewNuGetPackages.ps1 -BaseRef $(git merge-base origin/master HEAD)`.

NuGet lock files with `RestoreLockedMode` are not enabled: CI builds on Linux x64, Linux ARM64 and Windows with a
`global.json` that rolls forward across SDK patch versions, and the SDK's implicit packages (ILLink, WebAssembly
runtime packs) would make the lock files differ between runners.

## Namespace matches folder

`src/.editorconfig` sets `dotnet_style_namespace_match_folder = true:error` and `IDE0130` to error;
`EnforceCodeStyleInBuild` is on, and `src/Directory.Build.props` makes `RootNamespace` and `ProjectDir` visible to the
analyzer so the rule runs in command-line builds and CI. Generated code (`**/Generated/**`) is exempt.

Known exception: `src/UI.Shared/App_Code/WorkOrderSelectedEvent.cs` keeps the `ClearMeasure.Bootcamp.UI.Shared`
namespace. Its own section at the end of `src/.editorconfig` turns IDE0130 off for that one file, and it stays on the
older `namespace-folder-mismatches.txt` baseline.

## Requesting an exception

- **A new NuGet package:** add `<PackageId> | <issue link or reason>` to `scripts/nuget/approved-new-packages.txt`
  in the same pull request. The file is owned in `.github/CODEOWNERS`, so the code owners approve the package by
  approving the pull request. A line approves only the pull request that adds it.
- **A layer dependency or architecture convention:** change `SolutionArchitecture.Layers` or the rule in
  `src/UnitTests/Architecture`, scoped as narrowly as possible, with a comment that says why. The folder is owned in
  `.github/CODEOWNERS`.
- **A file whose namespace cannot match its folder:** add a section for that one file to `src/.editorconfig` with
  `dotnet_diagnostic.IDE0130.severity = none` and a comment that says why.
- **A banned API in Core:** do not suppress RS0030; inject `TimeProvider` instead. Changing
  `src/Core/BannedSymbols.txt` needs code owner review.

## Required checks

Mark these as required status checks in the `master` branch ruleset, and turn on "Require review from Code Owners":

- `Build result` (already required): covers the build, the MSBuild guard, IDE0130, RS0030 and the architecture tests.
- `secret-scan` (already required).
- `NuGet package gate` (new).
