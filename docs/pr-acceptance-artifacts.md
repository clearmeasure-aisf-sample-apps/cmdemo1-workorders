# PR acceptance videos and traces (#1671)

Failed acceptance tests and tests whose UI changed ([test-to-page map](../src/AcceptanceTests/test-to-page-map.json), [Get-TestsForChangedRazor.ps1](../scripts/acceptance/Get-TestsForChangedRazor.ps1)) link a video from a **sticky PR comment**. Videos and Playwright traces are stored in **private Azure Blob Storage** from the pull-request pool stack (cmdemo1-system item 4: `infra/pool/modules/artifacts.bicep`): containers `videos` and `traces`, 14-day lifecycle delete, `allowBlobPublicAccess: false`, `allowSharedKeyAccess: false`.

Plain videos and traces are **not** uploaded as GitHub Actions artifacts (this repository is public). Shard jobs may upload **ciphertext only** (short retention) for hand-off to a trusted Upload job.

## Auth

GitHub Actions uses the existing ACR-push OIDC app (`vars.AZURE_CLIENT_ID_ACR_PUSH`) via `azure/login` and `az storage … --auth-mode login`, only in jobs that set `environment: release` (federated credential subject). No account key, connection string, or SAS token is introduced. If a key or SAS would be required, stop and post a Blocker on #1671.

Set Actions variable `POOL_ARTIFACTS_STORAGE_ACCOUNT` to the applied account name (not a secret).

## Scrubbing

[Scrub-PlaywrightTraceSecrets.ps1](../scripts/acceptance/Scrub-PlaywrightTraceSecrets.ps1) parses Playwright trace zip JSON (`trace.trace` / `trace.network` / resources) and redacts denylisted header values in every shape (name/value arrays, header maps, `[name,value]` pairs, cookies, split-line JSON). Built-in denylist: `X-Test-Support-Secret`, `Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`, `Proxy-Authorization`. Extend with `PR_ACCEPTANCE_SECRET_HEADERS`. Live secret values from `TestSupport__Secret` / `ACCEPTANCE_TEST_SUPPORT_SECRET` / `PR_ACCEPTANCE_SECRET_VALUES` are redacted without logging. Upload calls `Assert-TraceZipScrubbed` and fails if any denylist value survives.

## Trusted checkout and OIDC isolation

Shard jobs check out the branch head and **must not** have `id-token: write` or Azure login. A trusted `mint-artifact-wrap` job (`environment: release`) mints an RSA wrap key: public PEM as an Actions artifact; private PEM in private Azure blob storage. Shards encrypt videos/traces with [Protect-PrAcceptanceArtifactBundle.ps1](../scripts/acceptance/Protect-PrAcceptanceArtifactBundle.ps1) and upload ciphertext only. Trusted `upload-acceptance-artifacts` (`environment: release`, default-branch `_trusted` scripts) decrypts (rejecting symlink/`..` tar entries), then runs `Publish-PrAcceptanceArtifacts.ps1 -Mode Upload`. Gate: `PrEnvironmentOidcJobGate` / `PrEnvironmentOidcCheckoutRule` (matches `name:`+`uses:` checkouts; same-repo checkouts must pin `default_branch`; only allow-listed external repo is `cmdemo1-system` @ `main`).

[pr-environment-artifacts.yml](../.github/workflows/pr-environment-artifacts.yml) is a `workflow_run` job with `id-token: write` for the sticky comment. It checks out `github.sha` only (`persist-credentials: false`). Gate: [Test-PrEnvironmentArtifactsWorkflow.ps1](../scripts/acceptance/Test-PrEnvironmentArtifactsWorkflow.ps1).

## Timing

When `vars.POOL_ARTIFACTS_STORAGE_ACCOUNT` is set, [pr-environment.yml](../.github/workflows/pr-environment.yml) enables `ACCEPTANCE_RECORD_VIDEO`, encrypts on shards, and uploads from the trusted Upload job. When the variable is unset, gates warn and skip (no recording, no mint, no Upload) without failing the workflow.

Workflow [pr-environment-artifacts.yml](../.github/workflows/pr-environment-artifacts.yml) runs on `workflow_run` of **PR environment** and runs `-Mode Comment`, so sticky-comment / list time is off that check’s clock.

## Local video recording

```bash
export ACCEPTANCE_RECORD_VIDEO=true
```

Videos land under the test work directory `playwright-videos/<Class>.<Method>/`.

## Operator apply (live storage)

Jeffrey approved creating the private account (#1671). The Bicep already lives in cmdemo1-system; apply `stack-cmdemo1-pool` (or the artifacts module) as the nonprod deploy identity, grant the existing GitHub OIDC principal **Storage Blob Data Contributor** on `videos` and `traces`, then set `POOL_ARTIFACTS_STORAGE_ACCOUNT`.
