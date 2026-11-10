# AI ops bot board flow

Issues assigned to `cm-ai-ops-bot` on repo `clearmeasure-aisf-sample-apps/cmdemo1-workorders` are worked end-to-end by the AI ops bot on the project board [Clear Measure work orders demo 1](https://github.com/orgs/clearmeasure-aisf-sample-apps/projects/679).

## Column-by-column flow

The bot advances each issue one column at a time and never skips columns. Before every board move it posts a journal comment on the issue. The columns, in order, are:

1. Todo
2. In Progress
3. Technical Design
4. Development
5. Functional Testing
6. Release Queue
7. Done

## What the bot posts and opens

While working an item the bot:

- Posts a taking-it comment when it claims the issue
- Posts a plan for the work
- Posts a Cursor session link
- Opens a pull request that links the issue

## Merge and deploy verification

After required gates pass — the **Build result** check and the **secret-scan** check — the bot squash-merges the pull request.

It then shepherds deploy verification through production:

- Confirms the `master` **Build** and the **Release** workflow
- Drives Octopus project `cmdemo1-ui` through `tdd`, `uat`, and `prod`
- Verifies `/_build` and `/_healthcheck/detailed` on each environment

The issue moves to **Done** only when production verification has succeeded.
