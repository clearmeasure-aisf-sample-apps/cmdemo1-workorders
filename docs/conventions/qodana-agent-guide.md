# Qodana for agents: conventions, suppressions and the baseline

Read when Qodana fails, when narrowing visibility or renaming members, when suppressing a finding, or when working a
Qodana remediation / baseline-refresh item. Moved from `AGENTS.md` and `CLAUDE.md`. The always-on rules (never add
baseline entries, delete ABSENT entries, no unused members) stay in [AGENTS.md](../../AGENTS.md). See also
[qodana-gating-scope.md](../qodana-gating-scope.md) and [qodana-remediation-work-plan.md](../qodana-remediation-work-plan.md).

## Gate facts

- Qodana runs in CI with `failThreshold: 0`; findings are tracked in `qodana.sarif.json` (repository root).
- Local: `docker run --rm -v "$PWD":/data/project jetbrains/qodana-cdnet:latest --solution src/ChurchBulletin.sln --baseline qodana.sarif.json --fail-threshold 0`.
- A baselined finding the change removes (ABSENT) fails exactly like a NEW finding: delete its entry.
- Removing a baseline entry WITHOUT fixing or suppressing the code is wrong: the next run reports it as NEW.
- When fixing P1 mechanical cleanups, read the SARIF for the exact snippet context (`contextRegion.snippet`); SARIF
  line numbers may differ from the current file. Use `python3 -c "import json; ..."` to extract it. Run
  `dotnet build` before committing (warnings are errors, `src/Directory.Build.props`).
- Suppression format (project convention), one line, with a reason:

```csharp
// ReSharper disable once ClassNeverInstantiated.Global -- registered by DI (FluentValidation assembly scan)
public sealed class MyValidator : AbstractValidator<MyType>;
```

## Common findings to pre-empt

- **`InconsistentNaming`** (`non_field_members_should_use_upper_camel_case`): abbreviations in PascalCase names
  capitalize only the first letter: `EnUs` not `EnUS`, `Id` not `ID` (unless the framework demands it). Applies to
  test method names too; rename consistently across all test files.
- **Nullability** (`ReturnTypeCanBeNotNullable`, `ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract`): if a
  private/helper method always returns non-null, declare `T` not `T?` and update the matching locals.
- After any rename touching test methods, check no caller outside the file uses the old name (`grep -r "OldName" src/`).

## Narrowing visibility (`MemberCanBePrivate.Global`, `MemberCanBeProtected.Global`)

Before narrowing any property or method, run these four checks in order:

1. **Blazor `[Parameter]`**: a property with `[Parameter]` or `[SupplyParameterFromQuery]` stays `public`. Razor
   templates in the same partial class can access `private` members; only cross-component parameters need `public`.
2. **JSON serialization**: `System.Text.Json` (used by `WebServiceMessage` remoting) cannot populate `private set`
   properties. Any property on a class in the remoting round-trip (`IRemotableRequest`) keeps `public set`. Verify
   with the `ShouldSerialize` / `AssertRemotable` test in `RemotableRequestTests`.
3. **Cross-assembly callers**: `grep -r "MemberName" src/` first. A `public static` test helper called from
   `IntegrationTests` or another test project stays `public`.
4. **Object-initializer setters**: `new SomeType { Property = value }` needs at least `internal set`; search for
   `{ Property =` across the solution first.

Then:

- Members that cannot be narrowed: `// ReSharper disable once MemberCanBePrivate.Global -- <reason>`.
- **Private field naming**: when narrowing a `public` field to `private`, rename it to `_camelCase` and update all
  references in both the `.cs` file and the `.razor` template (same partial class).
- **Private auto-property setters**: if the setter is never assigned after construction, change `{ get; set; }` to
  `{ get; }` (avoids `AutoPropertyCanBeMadeGetOnly.Local`). If it was `protected virtual` (`.Global` in the
  baseline), also remove that baseline entry: it becomes ABSENT and fails the gate.
- **`virtual`**: `private virtual` is illegal; remove `virtual` when narrowing a `protected virtual` member.
- **Blazor `[Inject]`**: declare `[Inject]` properties `private` unless tests or other components need them, e.g.
  `[Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;` (see
  `WorkOrderManage.razor.cs`). Prevents `MemberCanBePrivate.Global`.
- After all visibility changes: remove the baseline entry of every finding now fixed or suppressed (filter by
  `partialFingerprints.equalIndicator/v1`); keep only findings still present. Then
  `dotnet build src/ChurchBulletin.sln --configuration Release` and the full `UnitTests` run.

## Dead / unused symbols (`ClassNeverInstantiated.Global`, `UnusedMember.*`, ...)

When a symbol is required by DI, reflection, NServiceBus or an interface contract, prefer a one-line suppression to
deletion:

1. DI-registered validators (FluentValidation `AbstractValidator<T>`): suppress `ClassNeverInstantiated.Global`.
2. Assembly-anchor marker classes (`WebApplicationFactory<TEntryPoint>`): suppress `ClassNeverInstantiated.Global`.
3. NServiceBus message handlers (`IHandleMessages<T>`): suppress `ClassNeverInstantiated.Global`.
4. OTel / JSON deserialization default constructors (`EventEntry`, `MetricEntry`, ...): suppress `UnusedMember.Global`.
5. Blazor state-notification events (`event Action? OnChange`): suppress `EventNeverSubscribedTo.Global`.
6. Interface members flagged `UnusedMemberInSuper.Global`: suppress on the interface, not each implementor.
7. Abstract test-base members overridden by all subclasses: suppress `UnusedMember.Global` on the declaration.
8. `IUiBus.Notify(object)` in test stubs: suppress `UnusedMember.Global`.
9. `UnusedAutoPropertyAccessor.Global` / setter never externally assigned: `{ get; set; }` → `{ get; }` when the
   class controls all mutation.

Fingerprint removal: always use the full `equalIndicator/v1` value (64+ hex chars); truncating at 40 chars produces
partial matches that do not filter. Use `fp=r.get(...,'')` and print untruncated.

## Baseline remediation workflow

1. **Audit absent vs present before touching code.** Check each finding's `charOffset + snippet` against the current
   source:

   ```python
   python3 << 'EOF'
   import json, os
   with open('qodana.sarif.json') as f: data = json.load(f)
   for r in data['runs'][0]['results']:
       for loc in r.get('locations',[]):
           pl = loc['physicalLocation']
           uri, offset, snippet = pl['artifactLocation']['uri'], pl['region'].get('charOffset',-1), pl['region'].get('snippet',{}).get('text','')
           path = os.path.join('src', uri)
           if os.path.exists(path) and offset >= 0:
               with open(path) as f: content = f.read()
               status = 'PRESENT' if content[offset:offset+len(snippet)]==snippet else 'ABSENT'
               print(f"{status}|{r['ruleId']}|{uri}:{pl['region'].get('startLine','?')}")
   EOF
   ```

   ABSENT entries must be removed from the baseline.
2. **For each PRESENT finding, fix or suppress**: fix when trivial (remove async, add `CancellationToken.None`,
   narrow scope, remove a redundant prefix); suppress with `// ReSharper disable once <RuleId> -- <reason>` when by
   design.
3. **Rebuild the baseline** from the pre-change audit plus the change list (not by comparing modified sources):

   ```python
   remove_fps = { '<fp1>', '<fp2>', ... }  # all absent + all fixed/suppressed
   kept = [r for r in data['runs'][0]['results']
           if r.get('partialFingerprints',{}).get('equalIndicator/v1','') not in remove_fps]
   data['runs'][0]['results'] = kept
   with open('qodana.sarif.json', 'w') as f: json.dump(data, f, indent=2)
   ```
4. `Html.AttributeValueNotResolved` at `Settings.razor:16` is in the baseline AND excluded in `qodana.yaml`; safe to
   leave.
5. `ConvertToPrimaryConstructor` is an epic guardrail: do NOT convert; suppress with
   `// ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion`.
6. `AutoPropertyCanBeMadeGetOnly.Global` on IOptions classes: suppress with
   `// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global -- required for IOptions<T> configuration binding`.
7. `ParameterOnlyUsedForPreconditionCheck.Local` on test stubs: `// ReSharper disable/restore ParameterOnlyUsedForPreconditionCheck.Local` around the class.

## Baseline refresh after a merged remediation batch

After the batch merges and CI has run a full Qodana scan, download the new `qodana.sarif.json` artifact from the CI
run and replace the file at the repository root. Do NOT edit fingerprints by hand; always replace from a real scan.
Safe order: fix code → commit → CI green → download artifact → replace `qodana.sarif.json` → commit baseline.

## Left-baselined by design (~75 findings)

These inspection IDs stay in `qodana.sarif.json` on purpose (mass rewrites, negative ROI). Do NOT remove these entries
without first fixing the underlying code.

| Inspection ID | Count | Reason left |
|---|---|---|
| `ConvertToPrimaryConstructor` | 26 | Mass rewrite; style only; breaks constructor-injection readability |
| `AutoPropertyCanBeMadeGetOnly.Global` | 11 | NHibernate / STJ / test-object-init requires mutable setters |
| `UseNameOfInsteadOfToString` | 4 | Minor; fix batch TBD |
| `PropertyCanBeMadeInitOnly.Local` | 4 | NHibernate/STJ set-by-convention requires mutable setters |
| `MergeIntoPattern` | 3 | Style; low value |
| `MethodSupportsCancellation` | 3 | Requires API-surface change; deferred |
| `ParameterOnlyUsedForPreconditionCheck.Local` | 3 | Style; low value |
| `PreferConcreteValueOverDefault` | 3 | Style; low value |
| `ArrangeNamespaceBody` | 3 | Formatter style; no semantic change |
| `AsyncMethodWithoutAwait` | 2 | Override stubs required by interface contracts |
| `FieldCanBeMadeReadOnly.Global` | 2 | Reflection/NHibernate access |
| `RedundantUsingDirective` | 2 | Already excluded in prior batches; residual |
| `VariableHidesOuterVariable` | 2 | Style; low value |
| `InconsistentlySynchronizedField` | 1 | Deferred to concurrency review |
| `PartialTypeWithSinglePart` | 1 | Blazor codegen pattern |
| `RedundantStringInterpolation` | 1 | Style; trivial |
| `RedundantTypeArgumentsInsideNameof` | 1 | Style; trivial |
| `RedundantVerbatimStringPrefix` | 1 | Style; trivial |
| `TooWideLocalVariableScope` | 1 | Style; low value |
| `UsingStatementResourceInitialization` | 1 | Style; low value |

The remaining baselined findings (`InconsistentNaming` ×4, `MemberCanBePrivate.Global` ×2,
`ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract` ×1, `Html.AttributeValueNotResolved` ×1,
`ReturnTypeCanBeNotNullable` ×1, `CSharpWarnings::CS0618` ×1) are targeted in dedicated sibling work items and leave
the baseline when those fixes merge.
