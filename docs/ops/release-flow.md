# Release flow

A merged work item reaches production through these stages. The factory worker gateway promotes the release when the release qualifies.

- **Merged work item** — the change is on master.
- **Master build** — the master build runs for that commit.
- **cmdemo1-ui release** — that build produces the cmdemo1-ui release.
- **tdd** — the release is promoted to tdd.
- **uat** — the release is promoted to uat.
- **prod** — the release is promoted to prod.
