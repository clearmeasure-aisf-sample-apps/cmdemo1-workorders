# AI ops bot board flow

The bot's operating instructions are version-controlled in `cmdemo1-system`: [`factory/operating-instructions.md`](https://github.com/clearmeasure-aisf-sample-apps/cmdemo1-system/blob/main/factory/operating-instructions.md). That file wins where this summary differs.

Issues assigned to `cm-ai-ops-bot` on repo `clearmeasure-aisf-sample-apps/cmdemo1-workorders` are worked end-to-end by the AI ops bot on the project board [Clear Measure work orders demo 1](https://github.com/orgs/clearmeasure-aisf-sample-apps/projects/679).

## Column-by-column flow

Status columns on project 679, in order:

1. Todo (`f75ad846`) — not started
2. In Progress (`47fc9ee4`) — actively worked
3. Review (`e19122d0`) — pull request open
4. Merged (`8012c69b`) — merged to master, not yet deployed
5. TDD (`eeb01ba5`) — deployed to tdd
6. UAT (`ebff1a13`) — promoted to uat
7. Done (`98236657`) — deployed to prod, or a merged docs-only change

## Automation that advances the card

| Event | Move |
| --- | --- |
| Pull request opened | → Review |
| Pull request merged | → Merged |
| Docs-only merge (`*.md` / `docs/**` only) | → Done |
| Octopus deploy to tdd / uat / prod | → TDD / UAT / Done |

A closed issue is not Done until it is in prod (docs-only merges excepted).

## What the bot posts and opens

While working an item the bot:

- Posts a taking-it comment when it claims the issue
- Posts a plan for the work
- Posts a Cursor session link
- Opens a pull request that links the issue

## Merge and deploy verification

After required gates pass — the **Build result** check and the **secret-scan** check — the bot squash-merges the pull request.

Deploy verification then follows the release path:

- Confirms the `master` **Build** and the **Release** workflow
- Octopus project `cmdemo1-ui` promotes through `tdd`, `uat`, and `prod`; each successful deploy moves the card to **TDD**, **UAT**, or **Done**
- Verifies `/_build` and `/_healthcheck/detailed` on each environment

The issue moves to **Done** when production verification has succeeded, or immediately on merge when the change is docs-only.
