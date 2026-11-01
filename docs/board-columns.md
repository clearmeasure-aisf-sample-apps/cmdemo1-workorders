# Board Columns (reviewed)

The demo board (project 679) has these seven columns, in order. Factory workers complete their columns through the gateway; the gateway performs the board moves.

| Column | Meaning | Exit condition | Who moves the item out |
| --- | --- | --- | --- |
| Todo | Backlog of work not yet started. | The item is selected for preparation and moves to In Progress. | The item owner. |
| In Progress | The item owner is clarifying the concept, scope and acceptance criteria. | The item is ready for technical design and moves to Technical Design. | The item owner. |
| Technical Design | The factory worker defines the implementation approach and fixes the scope. | The technical design is recorded and reviewed, then the item moves to Development. | The gateway, after the factory worker completes the design column. |
| Development | The factory worker implements the change on a branch and opens a pull request. | The change is reviewed and required checks pass, then the item moves to Functional Testing. | The gateway, after the factory worker completes the development column. |
| Functional Testing | The factory worker validates the pull request against the acceptance criteria before merge. | Validation passes and the applicable approvals and checks permit merge; the merged item proceeds to Release Queue, or directly to Done for docs-only changes. | The gateway, after validation and merge. |
| Release Queue | The merged change waits for its release candidate to reach production. | The release containing the change is deployed successfully to production, then the item moves to Done. | The gateway's post-merge release settlement. |
| Done | The item is complete: its change is deployed, or its docs-only pull request is merged. | Terminal column; no normal exit. | No one in the normal flow. |

## Docs-only changes

A merge that changes only documentation publishes no release candidate. The gateway moves such a card straight from Functional Testing to Done, bypassing Release Queue because there is nothing to deploy.

## Production incidents

When the health check reports unhealthy, the factory files a Production Incident into Release Queue and works it immediately.
