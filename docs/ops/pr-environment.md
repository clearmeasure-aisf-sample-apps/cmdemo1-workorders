# PR environment check

Workflow: `.github/workflows/pr-environment.yml` (issue #1664). GitHub check name: **PR environment**.

## What it does

On every push to a non-master branch that has an open pull request:

1. Detects docs-only changes (same allowlist as Build). Docs-only PRs report success without leasing a slot or running the suite.
2. If `vars.POOL_LEASE_STORAGE_ACCOUNT` is **unset** (lease store / system #652 not applied): **shadow mode** — the check succeeds without leasing, ACR, or Octopus. No DryRun half-deploy.
3. If the lease store **is** configured (**live**):
   - Cancels in-progress Octopus tasks for earlier releases of the same PR (supersede).
   - Leases a warm pool slot via `cmdemo1-system` `scripts/pool/lease.ps1` (2 warm slots in shadow warm count). While the pool is empty the job shows `waiting for a slot`; a wait over 5 minutes posts on the PR.
   - Waits for a **Publish image** run on the same SHA that actually uploaded `churchbulletin-nuget-packages-pr` (skipped “no open PR” publishes are ignored; the workflow may dispatch Publish image once).
   - Retags the image as the release number (`2.4.N-pr.<n>`), creates a **Pull requests** channel release, and deploys to `pool-N`.
   - Runs 12 parallel acceptance shards × 4 browser workers (48), then the serial lane on the same slot.
   - Reports results and releases the lease. On cancel/failure, cancels this run’s Octopus server task when known.

The check tests the branch-head SHA (push), not the `pull_request` merge ref — the same commit Build tests.

## Live safety and privilege

- `lease-and-deploy` and `release-slot` run only when `pool_live=true` (`POOL_LEASE_STORAGE_ACCOUNT` set). They use `environment: release` and OIDC.
- Privileged scripts are checked out from the **default branch** (`_trusted`), not from the pushed PR head. Acceptance tests still check out the PR SHA.
- No new Octopus API keys or secrets. Video/trace upload stays off this check’s clock (issue #1671).

## Not required

Shadow mode (phases 1–2): this check is **not** required on the default branch. Do not change the ruleset here (that is issue #1672).

## Dependencies

| Piece | Where |
| --- | --- |
| Pool lease / warm / sweep | `cmdemo1-system` PR #727 / issue #656 (`scripts/pool/`) |
| Lease store (live) | `cmdemo1-system` #652 — sets `vars.POOL_LEASE_STORAGE_ACCOUNT` |
| Pool Azure stack | `cmdemo1-system` #653 |
| Pull requests channel + slot step | `cmdemo1-system` #654 |
| Publish-only image | `publish-image.yml` (issue #1660) |
