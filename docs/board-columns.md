# Board Columns (reviewed)

The demo board (project 679) has these seven Status columns, in order. Option ids are on the project's Status field.

| Column | Option id | Meaning | Exit condition | Who moves the item out |
| --- | --- | --- | --- | --- |
| Todo | `f75ad846` | Not started yet. New work items start here. | The item is claimed for work and moves to In Progress. | The item owner or the AI ops bot. |
| In Progress | `47fc9ee4` | Actively being worked on (design, implementation, or fixes). | A pull request for the item is opened, then the item moves to Review. | Automation when a PR opens (or the bot when it opens the PR). |
| Review | `e19122d0` | A pull request is open. Required checks and review run here. | The pull request is merged to master, then the item moves to Merged — or straight to Done when every changed file is docs-only (`*.md` or under `docs/**`). | Automation on merge. |
| Merged | `8012c69b` | Merged to master, not yet deployed. | An Octopus deploy that includes the item's commits reaches tdd, then the item moves to TDD. | Automation on Octopus deploy to tdd. |
| TDD | `eeb01ba5` | Deployed to tdd. (Same option id as the former Release Queue column, renamed.) | The release is promoted to uat, then the item moves to UAT. | Automation on Octopus deploy to uat. |
| UAT | `ebff1a13` | Promoted to uat. | The release is promoted to prod, then the item moves to Done. | Automation on Octopus deploy to prod. |
| Done | `98236657` | Deployed to prod, or a docs-only change that was merged. | Terminal column; no normal exit. | No one in the normal flow. |

Removed columns (do not reference): Technical Design (`e6ec2abc`), Development (`9fbceb9c`), Functional Testing (`41091342`), and Release Queue (the name; its option id `eeb01ba5` now means **TDD**).

## Automation

| Event | Status move |
| --- | --- |
| Pull request opened for the item | → Review |
| Pull request merged | → Merged |
| Pull request merged and every changed file is docs-only (`*.md` or `docs/**`) | → Done |
| Octopus deploy to tdd / uat / prod that includes the item's commits | → TDD / UAT / Done |

Built-in project workflows such as "Item added to project", "Auto-close issue" and "Auto-add sub-issues" stay as they are. There is no "closed → Done" workflow: a closed issue is not Done until it is in prod (unless it was a docs-only merge that went straight to Done).

## Docs-only changes

A merge that changes only documentation (`*.md` or paths under `docs/**`) publishes no release candidate. Automation moves such a card straight from Review to Done on merge, bypassing Merged, TDD and UAT because there is nothing to deploy.

## Production incidents

When the health check reports unhealthy, the factory files a Production Incident into In Progress and works it immediately.
