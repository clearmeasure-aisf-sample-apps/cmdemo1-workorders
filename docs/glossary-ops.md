# Operations Glossary (reviewed)

- **Health check** — the endpoint the monitor polls to decide whether the app is up.
- **Production incident** — a work item raised automatically when the health check fails.
- **Escaped defect** — a bug found in production that testing did not catch.
- **Rework** — a work item that moves backwards on the board.
- **Release candidate** — a versioned build published from master that UX Testing environments deploy.
- **Auto Merge** — a label that lets the AI Factory merge a pull request as soon as required checks pass while the item is in Review.
- **AI Factory** — the automation that moves work items across the board and opens pull requests.
- **Factory worker** — an AI session that takes a work item from the board and carries it through implementation and verification.
- **AI ops bot** — machine user `cm-ai-ops-bot` that works issues assigned to it on the Clear Measure work orders demo 1 board through to production.
- **Plant manager** — the supervising AI session that answers the questions factory workers raise about their work items.
- **Todo** — the board column where a work item waits before work starts (option id `f75ad846`).
- **In Progress** — the board column where the item is actively being worked (option id `47fc9ee4`).
- **Review** — the board column where a pull request is open (option id `e19122d0`).
- **Merged** — the board column where the change is on master but not yet deployed (option id `8012c69b`).
- **TDD** — the board column where the change is deployed to tdd (option id `eeb01ba5`; formerly named Release Queue).
- **UAT** — the board column where the change is promoted to uat (option id `ebff1a13`).
- **Done** — the board column where the change is in prod, or a docs-only merge has completed (option id `98236657`). A closed issue is not Done until it is in prod.
- **Agent run** — one execution of a cloud coding agent for a single phase of a work item.
