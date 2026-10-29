# B01-10 · PASS17 WAVE03 · RUNSCORE001/R1 — source-native post-run scorecards

**Producer:** B01-10 — Game Design & Balance · **Runtime ownership:** Codex only · **State:** original portable C# feature, NOT integrated into the game.

## One genuinely new gameplay feature

A deterministic **post-encounter / post-run scoreboard** for the existing SIX-stage R5 run: difficulty-relative score, Bronze/Silver/Gold stage medals, a single run medal after all six victories, and three non-monetary challenge achievements. It makes existing risk/reward choices legible as a result without altering any existing combat, Scrap, checkpoint, progression, physical parts, garage, CHL or PARTSIZE code. It is **not** a third encounter editor, more enemy configurations, a UI mockup, or a report-only simulator.

**Original code:** \`RunScore.cs\` is a self-contained, Unity-compatible .NET-standard-style C# source file with no UnityEngine or third-party dependencies. It has no effects on the game until Codex chooses an authorized integration.

**Real executable:** \`RunScore.Runner.csproj\` / \`Program.cs\` compile and execute original same C# source under .NET 8; \`example-run.json\` is a source-pinned, six-stage example matching [B01-10 BAL17 R5 deck PR #344](https://github.com/C8LyNn/RoboRougelike/pull/344) first-route IDs, original threat calculations and exact source SpawnCount including Scout escort and single boss. Elapsed times, core HP ratios and recoveries in this JSON are **explicitly illustrative hypothetical input values** — there is **no claim of Unity play or observed player results**.

## Evaluation rules, always fail closed on impossible inputs

- Input is ONE actual stage observation: StageNumber 1–6, stable encounter Id, Victory, actual R5 Threat, real SpawnCount, actual defeated enemies (0..count), real duration (excluding pause if collector can guarantee that), vehicle/core HP ratio at start/end, actual Scrap delta (display only), recovered physical part delta (0..3).
- Victory only if all spawned enemies were defeated and player remains alive. It earns \`200 + 20*stage + round(20*threat)\` base, plus max 60 time points, max 80 end-of-encounter condition points, plus 15 per recovered physical part (max 45).
- The par time is \`50+10*stage+4*threat\` seconds. Full 60 at time ≤ par; gradually drops to zero at 2×par. Exceeding par never creates negative points. The surviving HP fraction is capped to [0,1]; healing above 100% is rejected.
- Medals: Gold if the variable performance bonus ≥130, Silver if ≥75, Bronze for any valid victory below 75. A defeat gets **zero encounter points** and no medal; no later stage after loss is accepted.
- The run must contain sequential stages starting at 1, **at most six**. Only six complete victories can win a full-run medal. Gold requires at least four Gold stages and every stage at least Silver; Silver requires at least four stages at least Silver; Bronze otherwise. Incomplete/defeated runs show points already earned but no full-run medal.
- Optional **purely cosmetic, zero-currency achievement IDs** after all six victories: \`PAR_MASTER\` (each stage completed by par), \`HULL_KEEPER\` (≥65% remaining core/hull HP after all stages), \`SALVAGE_EXPERT\` (≥8 actual recovered physical modules across six stages). The source never modifies Scrap, item ownership or player condition. These IDs only become visible if Codex supplies a localized UI.
- It does **not** reward repeatedly farmed Scrap (input \`ScrapEarned\` included for display only). No global/local persistent ranking, no player identity, no save-schema changes or network. Run scoring is reconstructed from trusted observations only.

All computations are deterministic, no RNG/system clock. Numeric rejects: NaN/infinite Threat/time/HP, duration ≤0 or >10h, invalid IDs/stage, impossible enemy/salvage count, invalid reward delta, duplicate IDs, skipped stages or continuation after defeat.

## Real tests, working command line

From repository root with .NET SDK 8:

\`\`\`bash
dotnet build Research/B01-10/P17RunScore/RunScore.Runner.csproj --configuration Release --warnaserror
dotnet run --project Research/B01-10/P17RunScore/RunScore.Runner.csproj --configuration Release --no-build -- --test
dotnet run --project Research/B01-10/P17RunScore/RunScore.Runner.csproj --configuration Release --no-build -- --input Research/B01-10/P17RunScore/example-run.json
\`\`\`

The \`--test\` suite exercises **56 independently named original C# positive/negative checks** plus 120 deterministic generated six-stage variation cases; the input mode produces machine-readable JSON. Invalid inputs return process exit **2**. The own isolated GitHub workflow \`.github/workflows/b01-10-p17-runscore.yml\` compiles with warnings-as-errors on Windows 2025 + Ubuntu 24.04, executes original source, source-based example, bad-input CLI and uploads logs+JSON.

**Actual CI run (before README/manifest-only additions):** [GitHub Actions #40911060167](https://github.com/C8LyNn/RoboRougelike/actions/runs/40911060167) **SUCCESS** on both platforms, original compiled C# **56/56 PASS each**, generated 120-run input test PASS, real JSON scoring PASS, negative invalid-path exit 2 PASS. [Original Windows artifact #12914146272](https://github.com/C8LyNn/RoboRougelike/actions/runs/40911060167) and [Ubuntu #12913468111](https://github.com/C8LyNn/RoboRougelike/actions/runs/40911060167). Exact final HEAD should be retested after content/manifest commits; source Git blob parity is included separately in MANIFEST.

**NOT_RUN:** native Unity Editor/PlayMode, actual Windows Player, actual R1RunController game callsite, B01-40 G0 and B01-60 independent QA. Original C# compile in .NET8 **does not** prove Unity integration.

## Original game API and exact ownership boundaries

Codex gameplay reference branch [#383](https://github.com/C8LyNn/RoboRougelike/pull/383) underlying gameplay commit \`203a0805251ed28293888e43f2d49f0df63767c6\`. Existing \`R1RunController.CurrentEncounter\` and \`R5RunPlan.Threat(CurrentEncounter)\` expose challenge difficulty; \`CurrentEncounter.SpawnCount\` accounts for original extra Scout escort (1 for SiegeBoss); \`DefeatedEnemies\`, \`EarnedScrap\`, \`RecoveredModules\` in \`R1RunController\` are cumulative — **Codex must collect per-stage deltas** and compare only authoritative runtime facts. \`R1RunProgress.CompletedEncounters\` is available, but stage number must be captured at the correct transition. Collect duration and core HP ratio from authoritative actual runtime components; **the feature does not provide such a collector**. Pause periods must be excluded and host input must not be accepted from UI/user JSON without validation.

This scoring is deliberately **ephemeral**: no checkpoint ownership, no serialization into current Codex #382/#383 save data, no automatic reward payouts and no duplicated economics. Codex should decide if/when a dedicated score collector and scoreboard UI merit integration. Refuse integration if required observables cannot be reliably captured.

## Copy-ready Codex handoff — ONLY AFTER B01-00 / B01-40 / B01-60 gates

> B01-10 RUNSCORE001/R1, source pinned Draft PR/commit below. Read exact \`RunScore.cs\` and tests. This is new original pure C# gameplay result scoring, NOT integrated or owned by the game. After B01-00 orders this feature and B01-40 verifies G0/source SHA and B01-60 independently tests, use a disposable worktree on the **current** authorized Codex game revision. Integrate a **copy** of \`RunScore.cs\` into an authorized runtime location only if the existing assembly accepts the namespace/API. Provide a new authoritative stage-outcome adapter that gathers actual R5 threat/spawns, real kills, measured unpaused duration, core health ratio, real earned Scrap and salvaged part deltas; display results in a separate UI with optional localized labels. Do **not** alter current combat reward, stage transition, save schema, CHL, PARTSIZE, equipment kits, or upgrade selection without separate authorization. Run real native Unity EditMode and PlayMode stage victory/defeat/rollback regression and a full six-stage run with score accuracy; record Unity logs/XML/Player SHA. **Rollback** by removing only the new adapter/UI/score file from the disposable worktree, restoring prior serialized scenes and validating zero GUID drift. Do not cherry-pick producer PR or merge to main/develop without explicit HQ permission.

## Dependencies and exact source provenance

\`R5RunPlan.cs\` @ original gameplay Git blob \`6fde58f1fc621cde6488d9ee81c3510aa729839a\`, \`R1RunDefinition.cs\` blob \`5665e64d3d665c67d352eb5a9809a66fc5136698\`, \`R1RunController.cs\` source blob \`09a6776db67662e8960d1f446ba5c5168cd3e4b2\` at gameplay commit \`203a0805251ed28293888e43f2d49f0df63767c6\`. The native six-stage example is pinned to PR #344 \`R5_EncounterDeck_BAL17.json\` blob \`762009d1bde7978aeb5c842dd08d2efdddcec646\`, not a canonical game install.

**Receivers:** B01-40 G0/source/interface/rollback → B01-60 independent C# QA → B01-00 queue #79 only if accepted → Codex ACK and native Unity integration only if explicit. \`PRODUCER_EXECUTABLE_GREEN\` ≠ \`OFFERED/FROZEN\` ≠ \`CODEX_CLAIMED\` ≠ \`INTEGRATED\`.

**Source package paths:** exclusively \`Research/B01-10/P17RunScore/**\` + a single isolated, own CI workflow. No \`Assets/**\` modifications, no model jobs, no merge.


## WAVE 04 — RUNSCORE001/R2: actual two-phase game-ready source collector

**This is an extension to the same existing producer package #401, not a new duplicate scoring package.** The earlier score engine \`RunScore.cs\` requires a single \`StageScoreInput\` with the actual per-stage kill/recovery/scrap deltas. Native source review uncovered an important R1 timeline boundary. Source at current Codex gameplay baseline \`203a0805251ed28293888e43f2d49f0df63767c6\`, original \`Assets/R1/R1RunController.cs\` Git blob \`09a6776db67662e8960d1f446ba5c5168cd3e4b2\`:

- \`StartRun\` resets \`DefeatedEnemies=EarnedScrap=RecoveredModules=0\` (line 106).
- At \`ChooseRoute\`, the stage instance is selected (lines 112–121); \`BeginEncounter\` creates and spawns it (lines 216–241).
- At **victory**, \`Update\` calls \`Progress.WinEncounter\`, awards Scrap, increments **cumulative** \`EarnedScrap\` and \`DefeatedEnemies\` (lines 270–290), **then opens the reward selection**. At this moment \`RecoveredModules\` has not increased.
- **Only after successful \`ConfirmRewards\`** does \`RecoveredModules+=ids.Length\` (lines 174–188). Selecting zero/one/two/three parts is optional, and failed selections must not grant scores.

The original scorer's single-stage observation must therefore **not** be constructed at the moment of combat victory using \`RecoveredModules\`. W04 adds actual original **\`RunScoreCollector.cs\`** with a two-phase event contract: \`new RunScoreCollector(... cumulative baseline ...)\`, \`Advance(unscaledDelta,paused)\` only while active combat, \`FinishCombat(victory,cumulativeKills,endCoreHpRatio,cumulativeScrap)\` once after the valid win/loss transition, and for **victory only**, \`FinishRewards(cumulativeRecovered,cumulativeScrap)\` once after successful \`ConfirmRewards\`. On defeat, \`FinishCombat\` immediately returns the terminal zero-point observation.

**Key API behavior**: both finalization stages validate **before mutating internal state**, so a rejected counter mutation/invalid kill or premature reward confirmation remains recoverable. The collector rejects double victory/recovery; does not permit unpaused time accumulation after combat or count paused time; uses integer-long checked monotonic **deltas**, not absolute run-level Scrap/recovery counters; rejects an impossible recovered-parts count greater than three. \`StageScoreInput\` still passes to the **unchanged** original \`RunScorer.ScoreStage\` and \`ScoreRun\`. The original balance/medal formula and anti-farming policy remain unchanged.

**Minimal integration-facing outline** (illustrative — Codex alone owns the real game adapter; no game files modified by producer):

\`\`\`csharp
// At the start of actual R5 combat, snapshot BEFORE any kills/rewards:
var collector = new RunScoreCollector(
    stageNumber, actualEncounter.Id, R5RunPlan.Threat(actualEncounter),
    actualEncounter.SpawnCount, authoritativeCoreHpRatio,
    run.DefeatedEnemies, run.EarnedScrap, run.RecoveredModules);

// During active combat only, including pause-aware unscaled elapsed:
collector.Advance(authoritativeUnscaledDeltaSeconds, run.Paused);

// After R1 victory/defeat already incremented its cumulative counters:
StageScoreInput loss = collector.FinishCombat(
    victory, run.DefeatedEnemies, authoritativeRemainingCoreHpRatio,
    run.EarnedScrap);
// If defeat: loss is non-null and final; do NOT call FinishRewards.

// If and only if R1 ConfirmRewards() successfully committed:
StageScoreInput victoryObservation = collector.FinishRewards(
    run.RecoveredModules, run.EarnedScrap);
// RunScorer.ScoreStage(victoryObservation) is now safe to display.
\`\`\`

**Important integration limitations:** The current R1 controller also supports optional **races/road/reactor** and map activities; this collector and existing scoring policy are **for normal R5 combat stages only (1–4 spawned combat enemies, counting the source-authored Scout escort)**. Non-combat objectives must not be falsely scored as kills, and a non-combat all-activities run requires a separately approved policy before integration. The collector does not decide exact input tick scheduling or where core health ratio is captured; only Codex can wire/verify those against real Unity lifecycle and state. Do not start measuring while the game is transitioning or within paused screen; the constructor is for an actual live encounter with an authoritative baseline. Likewise, no source-engine claim that native Unity can successfully compile or install this without the future authorized adapter.

**W04 executable validation:** \`Program.cs\` adds **18 real compiled C# checks** (#57–74) covering delayed reward accrual, real cumulative counter deltas, pause-exclusion, terminal defeat, invalid replay/double rewards, abnormal kill/recovery counters, time overflow and non-monetary score invariance; old #1–56 and 120 seeded six-stage data checks retained. Own .NET8 Windows+Ubuntu workflow now asserts **74 PASS / 0 FAIL** per OS and saves JSON/negative exit evidence. GitHub Actions and exact SHA below MANIFEST provide the authoritative actual outcome. **B01-60 independent QA, B01-40 G0, native Unity/Player and Codex ACK all remain pending.** Receiver must not mark this as Unity tested.


## WAVE 05 — physical SOURCE_ZIP handoff to B01-40 and independent B01-60

**No new gameplay feature or additional competing PR.** This W05 build operation turns existing \`RUNSCORE001/R2\` into an independently inspectable **physical ZIP of the exact C# source and GUID-pinned Unity-import layout**. Original \`RunScore.cs\` and \`RunScoreCollector.cs\` remain unchanged; ownership and game code boundaries remain intact.

The new [\`pack_handoff.py\`](pack_handoff.py) uses only standard Python 3.12. It checks the Git-blob SHA of **all existing compiled C# source, test runner, example and README** against the existing manifest, then produces a byte-reproducible ZIP from the exact PR commit SHA. It stores file entries with stable names, timestamp, permissions and no compression variance, plus a machine-readable \`SOURCE_RECEIPT.json\` and **three Unity \`.meta\` files**: one folder GUID and two MonoImporter GUIDs. It never writes to the canonical \`Assets/**\` tree.

Extracted ZIP member roots:

- \`UnityInstall/Assets/B01RunScore.meta\`
- \`UnityInstall/Assets/B01RunScore/RunScore.cs\`
- \`UnityInstall/Assets/B01RunScore/RunScore.cs.meta\`
- \`UnityInstall/Assets/B01RunScore/RunScoreCollector.cs\`
- \`UnityInstall/Assets/B01RunScore/RunScoreCollector.cs.meta\`
- \`Evidence/{RunScore.Runner.csproj,Program.cs,example-run.json,README.md,MANIFEST.json}\`
- \`SOURCE_RECEIPT.json\`, including the exact PR SHA, per-member SHA256/size, explicit \`nativeUnity=NOT_RUN\`, \`G0=PENDING\`, \`CODEX_ACK=PENDING\`.

**Source bundle is not a pre-imported Unity package.** The \`.meta\` files are syntactically prepared only; Unity serialization, actual GUID conflicts and final plugin/asmdef integration still require Codex and a legitimate Unity environment. An independent review must check all GUIDs against the selected actual Codex worktree before installation.

Run from repository root (replace \`<exact40hexhead>\`):

\`\`\`bash
python Research/B01-10/P17RunScore/pack_handoff.py pack --sha <exact40hexhead> --archive /tmp/RUNSCORE_SOURCE.zip --report /tmp/zip-build.json
python Research/B01-10/P17RunScore/pack_handoff.py verify --sha <exact40hexhead> --archive /tmp/RUNSCORE_SOURCE.zip --report /tmp/zip-verify.json
python Research/B01-10/P17RunScore/pack_handoff.py negatives --sha <exact40hexhead> --archive /tmp/zip-malicious-tests.zip --report /tmp/zip-negative.json
\`\`\`

\`negatives\` executes **four actual malformed archive rejection cases**, including injected Codex game path \`../Assets/R1/R1RunController.cs\`, altered C# source, drifted source GUID and forged native Unity pass. The isolated GitHub Actions workflow runs this packaging, reopens the ZIP, executes all negatives **on both Windows and Ubuntu**, and uploads the actual ZIP and readable receipts together with 74/74 original source C# tests.

**Receiver flow:** B01-40 checks that exact original GitHub Actions ZIP from **both OS** has the same SHA256 and all **11 expected ZIP members**, that \`SOURCE_RECEIPT.json\` references the producer branch's actual immutable SHA, and that none of the existing game assets have been modified. B01-60 independently unpacks/tests the original files, checks copied C# compiler behavior and negative ZIP mutations, and reports a separate QA verdict. These steps are **not themselves an independent receiver ACK** until another agent explicitly confirms. The Codex-specific request must be routed by B01-00 through #79 **only after successful source G0 and independent QA**, or held as \`ACK_PENDING\`.

**Codex install after authorization:** from an independently accepted exact-source ZIP, copy only the \`UnityInstall/Assets/B01RunScore\` folder **and its adjacent \`B01RunScore.meta\`** into the authorized current game worktree's \`Assets/\` folder, after confirming that the destination does not already exist and that each GUID is unused. Do not run a broad archive extraction over the repository. Then write the adapter and scoreboard in Codex-owned sources, test native Editor/PlayMode/Player, log acceptance and record exactly touched files. **Rollback removes only these new imported files/meta and newly authorized adapter/UI**, restoring previous scene/prefab/asmdef files byte-for-byte. No self-authorized main/develop merge.
