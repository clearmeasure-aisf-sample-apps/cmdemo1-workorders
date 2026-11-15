# PR environment check

Workflow: `.github/workflows/pr-environment.yml` (issue #1664). GitHub check name: **PR environment**.

## What it does

On every push to a non-master branch that has an open pull request:

1. Detects docs-only changes (same allowlist as Build). Docs-only PRs report success without leasing a slot or running the suite.
2. If `vars.POOL_LEASE_STORAGE_ACCOUNT` or `vars.POOL_RESOURCE_GROUP` is **unset** (lease store / pool stack not applied): **shadow mode** — the check succeeds without leasing, ACR, Azure login, or Octopus. No DryRun half-deploy.
3. If both are configured (**live**) and the lease is not DryRun:
   - Cancels in-progress Octopus tasks for earlier releases of the same PR (supersede).
   - Leases a warm pool slot via `cmdemo1-system` `scripts/pool/lease.ps1` (2 warm slots preferred). While the pool is empty the job shows `waiting for a slot`; a wait over 5 minutes posts on the PR.
   - Waits for a **Publish image** run on the same SHA that actually uploaded `churchbulletin-nuget-packages-pr` (skipped “no open PR” publishes are ignored; the workflow may dispatch Publish image once).
   - Retags the image as the release number (`2.4.N-pr.<n>`), creates a **Pull requests** channel release, and deploys to `pool-N`.
   - Runs 12 parallel acceptance shards × 4 browser workers (48), then the serial lane on the same slot.
   - Reports results and releases the lease (`if: always()`). On cancel/failure, cancels this run’s Octopus server task when known.
4. If a live job’s lease still returns `dry_run=true`, every Azure/Octopus mutation step is skipped and the check reports success (no deploy to tdd/uat/prod or `pool-N`).

The check tests the branch-head SHA (push), not the `pull_request` merge ref — the same commit Build tests.

## Live safety and privilege

- Workflow `permissions: {}`; jobs opt into least privilege.
- `lease-and-deploy` and `release-slot` run only when `pool_live=true`. They use `environment: release` and OIDC.
- Privileged scripts are checked out from the **default branch** into `_trusted` with `persist-credentials: false`. Branch SHA / PR number are data only. Acceptance tests check out the PR SHA and must not receive `id-token: write`. Artifact mint/Upload (#1671) use separate `environment: release` jobs with OIDC and default-branch scripts only; shards hand off **ciphertext** only.
- Mutation steps are also gated on `steps.mutate.outputs.allow == 'true'` (false when the lease is DryRun).
- No new Octopus API keys or secrets. Video recording and Upload run only when `vars.POOL_ARTIFACTS_STORAGE_ACCOUNT` is set; the sticky PR comment stays on `pr-environment-artifacts.yml` off this check’s clock.

## Not required

Shadow mode (phases 1–2): this check is **not** required on the default branch. Do not change the ruleset here (that is issue #1672).

## Dependencies

| Piece | Where |
| --- | --- |
| Pool lease / warm / sweep | `cmdemo1-system` PR #727 / issue #656 (`scripts/pool/`) |
| Lease store (live) | `cmdemo1-system` #652 — sets `vars.POOL_LEASE_STORAGE_ACCOUNT` |
| Pool Azure stack | `cmdemo1-system` #653 — sets `vars.POOL_RESOURCE_GROUP` |
| Pull requests channel + slot step | `cmdemo1-system` #654 |
| Publish-only image | `publish-image.yml` (issue #1660) |
