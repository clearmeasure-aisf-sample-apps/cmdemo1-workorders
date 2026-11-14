# PR environment check

Workflow: `.github/workflows/pr-environment.yml` (issue #1664). GitHub check name: **PR environment**.

## What it does

On every push to a non-master branch that has an open pull request:

1. Detects docs-only changes (same allowlist as Build). Docs-only PRs report success without leasing a slot or running the suite.
2. Cancels in-progress Octopus tasks for earlier releases of the same PR (supersede).
3. Leases a warm pool slot via `cmdemo1-system` `scripts/pool/lease.ps1` (2 warm slots in shadow mode). While the pool is empty the job shows `waiting for a slot`; a wait over 5 minutes posts on the PR.
4. Waits for the **Publish image** workflow on the same SHA, downloads its packages, retags the image as the release number (`2.4.N-pr.<n>`), creates a **Pull requests** channel release, and deploys to `pool-N`.
5. Runs 12 parallel acceptance shards × 4 browser workers (48), then the serial lane on the same slot (`scripts/acceptance/Invoke-PrEnvironmentAcceptanceLane.ps1`).
6. Reports results and releases the lease. On cancel/failure, cancels this run’s Octopus server task when known.

The check tests the branch-head SHA (push), not the `pull_request` merge ref — the same commit Build tests.

## Not required

Shadow mode (phases 1–2): this check is **not** required on the default branch. Do not change the ruleset here (that is issue #1672).

## Video and traces

Upload of videos/traces (issue #1671) must not sit on this check’s clock. This workflow does not upload them; a follow-up path runs after the check completes.

## Dependencies

| Piece | Where |
| --- | --- |
| Pool lease / warm / sweep | `cmdemo1-system` PR #727 / issue #656 (`scripts/pool/`) |
| Lease store (live) | `cmdemo1-system` #652 |
| Pool Azure stack | `cmdemo1-system` #653 |
| Pull requests channel + slot step | `cmdemo1-system` #654 |
| Publish-only image | `publish-image.yml` (issue #1660) |

Until `vars.POOL_LEASE_STORAGE_ACCOUNT` is set, leasing uses DryRun and selects `pool-1` (shadow). Live Octopus deploy still needs the Pull requests channel and pool environments.
