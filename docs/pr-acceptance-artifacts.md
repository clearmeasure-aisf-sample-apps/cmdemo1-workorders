# PR acceptance videos and traces (#1671)

Failed acceptance tests and tests whose UI changed ([test-to-page map](../src/AcceptanceTests/test-to-page-map.json), [Get-TestsForChangedRazor.ps1](../scripts/acceptance/Get-TestsForChangedRazor.ps1)) link a video from a **sticky PR comment**. Videos and Playwright traces are stored in **private Azure Blob Storage** from the pull-request pool stack (cmdemo1-system item 4: `infra/pool/modules/artifacts.bicep`): containers `videos` and `traces`, 14-day lifecycle delete, `allowBlobPublicAccess: false`, `allowSharedKeyAccess: false`.

They are **not** uploaded as GitHub Actions artifacts (this repository is public).

## Auth

GitHub Actions uses the existing ACR-push OIDC app (`vars.AZURE_CLIENT_ID_ACR_PUSH`) via `azure/login` and `az storage … --auth-mode login`. No account key, connection string, or SAS token is introduced. If a key or SAS would be required, stop and post a Blocker on #1671.

Set Actions variable `POOL_ARTIFACTS_STORAGE_ACCOUNT` to the applied account name (not a secret).

## Scrubbing

[Scrub-PlaywrightTraceSecrets.ps1](../scripts/acceptance/Scrub-PlaywrightTraceSecrets.ps1) parses Playwright trace zip JSON (`trace.trace` / `trace.network` / resources) and redacts denylisted header values in every shape (name/value arrays, header maps, `[name,value]` pairs, cookies, split-line JSON). Built-in denylist: `X-Test-Support-Secret`, `Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`, `Proxy-Authorization`. Extend with `PR_ACCEPTANCE_SECRET_HEADERS`. Live secret values from `TestSupport__Secret` / `ACCEPTANCE_TEST_SUPPORT_SECRET` / `PR_ACCEPTANCE_SECRET_VALUES` are redacted without logging. Upload calls `Assert-TraceZipScrubbed` and fails if any denylist value survives.

## Trusted checkout

[pr-environment-artifacts.yml](../.github/workflows/pr-environment-artifacts.yml) is a `workflow_run` job with `id-token: write`. It checks out `github.sha` only (`persist-credentials: false`). `workflow_run.head_sha` / `run_id` are data for PR lookup and blob prefixes — never a checkout ref or script source. Gate: [Test-PrEnvironmentArtifactsWorkflow.ps1](../scripts/acceptance/Test-PrEnvironmentArtifactsWorkflow.ps1).

## Timing

[pr-environment.yml](../.github/workflows/pr-environment.yml) shard jobs (`parallel-shards`, `serial-lane`) call `Publish-PrAcceptanceArtifacts.ps1 -Mode Upload` after tests when `vars.POOL_ARTIFACTS_STORAGE_ACCOUNT` is set (OIDC via `vars.AZURE_CLIENT_ID_ACR_PUSH`, scripts from the default-branch `_trusted` checkout). When the variable is unset, the gate step warns and skips without failing. Files stay on the runner; never `actions/upload-artifact` for videos/traces.

Workflow [pr-environment-artifacts.yml](../.github/workflows/pr-environment-artifacts.yml) runs on `workflow_run` of **PR environment** and runs `-Mode Comment`, so sticky-comment / list time is off that check’s clock.

## Local video recording

```bash
export ACCEPTANCE_RECORD_VIDEO=true
```

Videos land under the test work directory `playwright-videos/<Class>.<Method>/`.

## Operator apply (live storage)

Jeffrey approved creating the private account (#1671). The Bicep already lives in cmdemo1-system; apply `stack-cmdemo1-pool` (or the artifacts module) as the nonprod deploy identity, grant the existing GitHub OIDC principal **Storage Blob Data Contributor** on `videos` and `traces`, then set `POOL_ARTIFACTS_STORAGE_ACCOUNT`.
