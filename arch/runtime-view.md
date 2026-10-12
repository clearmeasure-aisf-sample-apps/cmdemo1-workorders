# Runtime architecture

The deployed runtime of this system (Azure resource groups, container apps, database, supporting resources, the
arrows between them with live request counts) is not drawn in this repository. The platform's health dashboard
regenerates it from the system definition and the environments' resources at every deployment of the dashboard:

- prod: https://blue-bush-041023510.1.azurestaticapps.net/#runtime/prod (also `#runtime/tdd`, `#runtime/uat`)
- files on the same site: `/runtime/<env>.svg`, `/runtime/<env>.puml`, `/runtime/<env>.json`; the node list is `/topology.json`

This folder holds the application's own architecture: domain model, state, command workflows, component
dependencies and AI chat sequences.

Removed on 2026-10-11 (still in git history): `runtime-c4.puml`, `runtime-c4.png`, `runtime-c4-deployment.png` and
`arch-c4-container-deployment.*`. They described the upstream bootcamp's own Azure resources and pipeline
(`churchbulletingithubacr`, `bootcamp-tdd/uat/prod`), not this system. When taking an upstream change that touches
those files, keep this removal.
