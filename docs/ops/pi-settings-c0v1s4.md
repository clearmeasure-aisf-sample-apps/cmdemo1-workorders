# Pi settings group C0V1S4 (factory default since Nov 26, 2026)

C0V1S4 is the set of Pi (local coding agent) settings that the factory bot uses by default for all of its Pi work. Jeffrey made it the default at 7:00pm CT on Nov 26, 2026, after the run below, and at 7:02pm CT he put the RTX 5070 Ti back into factory rotation at 1 slot with these defaults. This page lists each setting, why it is there, the evidence, what was tried and dropped, and where each setting applies.

## Evidence

The settings were proven on the RTX 5070 Ti (DESKTOP-OBS4PVU) against cmdemo1-workorders master `6470121e`, through all four net11 stages, with no test suppressions:

| Stage | Minutes | Result |
|---|---|---|
| Compile | 14.0 | Build succeeded |
| Unit | 23.7 | 3340 / 3340 passed |
| Integration | 7.5 | 433 / 433 passed |
| Acceptance | 4.9 | 5 / 5 passed |
| **Total** | **50.1** | All green |

The run averaged 63.3 tokens/s. For comparison, the failed overnight run took 185.5 minutes.

## The settings

| # | Setting | Why it is there | Where it applies |
|---|---|---|---|
| 1 | **C0 budget:** 180K context window, 16K output per reply, Pi self-update off, llama-server `--ctx-checkpoints` | These are the budget and runtime settings of the winning run. | Context and output: the Pi clients of the box GPU lane on the RTX PRO 6000 (both), RTX PRO 5000 and RTX 5070 Ti (see "Where the defaults are in force"). Self-update off: every box Pi client. `--ctx-checkpoints`: **host only**, a llama-server setting (see "Host settings"). |
| 2 | **Qwen3.8 LoRA, thinking off** | This is the model setup of the winning run on the 5070 Ti. | **RTX 5070 Ti only.** Jeffrey approved thinking-off for that GPU only. Do not change thinking on any other GPU or in any client. |
| 3 | **Supervisor v3** (`pi-watchdog-v3`) | It checks scratch paths relative to the repo and skips git-tracked files and `src/`. | The PVU harness that supervises Pi runs. |
| 4 | **`openssl` on PATH** | Putting `openssl` on PATH fixed `ProtectBundleScript`. On the 5070 Ti machine it lives in `~\.pi\agent\bin`, and the harness appends that folder to PATH. | The PVU harness. The box lanes already have `openssl` on PATH. |
| 5 | **Fresh test database for each test run** (`Use-FreshDb.ps1`) | A reused database grew past the employee query's 1,000-row limit and broke the employee-list tests. A fresh database keeps test data within that limit. | Test runs that use a database. |
| 6 | **Filtered rerun while fixing, then one full suite** | Rerunning only the failing tests (`dotnet test <project> --filter "FullyQualifiedName~<Class>"`) keeps each fix round short, because the full unit stage alone takes about 24 minutes. One full run at the end shows that nothing else broke. | Every lane of the factory bot (prompt rule). |
| 7 | **Side-by-side checker:** unit and integration suites run in parallel | Running the two suites at the same time saves wall-clock time compared with running them one after the other. | Wherever both suites run. |
| 8 | **Supervisor checkpoints every 15 minutes** (was 10) | The winning run used a 15-minute interval instead of 10. | The PVU supervisor and the cloud-agent Pi lane prompt. |

## Tried and dropped

Both of these made runs slower in the comparison, so they are not part of C0V1S4:

- **Quiet build output.**
- **RTK with the filter.**

## Host settings: document only, do not change from a lane

These settings belong to the GPU hosts. The factory bot's lanes and agents never change them, and never start, stop or restart llama-server, LiteLLM, proxies, cloudflared or tunnels to apply them:

- llama-server `--ctx-checkpoints` (setting 1).
- The served model and its thinking mode (setting 2: the Qwen3.8 LoRA with thinking off, on the 5070 Ti only). Lanes send model `auto` and never change thinking.
- The loaded model. Lanes send model `auto` only, with no model loads and no warm-up requests.

## Where the defaults are in force

| Lane / GPU | Context / output | Self-update off | Test-loop rules (6, 5, 7, 4) | Notes |
|---|---|---|---|---|
| Box GPU lane: RTX PRO 6000, RTX PRO 6000 (second), RTX PRO 5000 (2 slots) | 180K / 16K, compaction on | Yes | Yes (prompt rules) | Thinking unchanged. The servers allow 262K per slot. |
| Box GPU lane: RTX 5070 Ti, 1 slot | 180K / 16K, compaction on | Yes | Yes | Thinking-off LoRA: 5070 Ti only (Jeffrey approved). |
| Box GPU lane: RTX 5060 Ti | 64K / 16K (unchanged) | Yes | Yes | Its server context is 64K, so the client stays at 64K. |
| Box Claude Code lane | n/a (not Pi) | n/a | Yes (prompt rules) | |
| Cloud-agent Pi lane | Kit defaults | Kit defaults | Yes, with 15-minute checkpoints | |

## Scripts

The C0V1S4 harness scripts are on DESKTOP-OBS4PVU under `C:\pi-delegate\scratch\pi-opt-1125\staged\`:

- `arms-v\Prepare-ArmV.ps1`, `arms-v\Start-ArmV.ps1`, `arms-v\Apply-ParallelVerify.ps1`, `arms-v\Status.ps1`
- `pi-watchdog-v3\` (supervisor v3)

The fresh-database step (setting 5) is `Use-FreshDb.ps1`.

The full comparison table is at `C:\pi-delegate\scratch\pi-opt-1125\compare-c0v-1126.md`.

## Related

- [Running acceptance shards](../running-acceptance-shards.md): filter syntax for single tests and classes.
- [Testing conventions](../testing-conventions.md)
- [AI workers](../ai-workers.md)
