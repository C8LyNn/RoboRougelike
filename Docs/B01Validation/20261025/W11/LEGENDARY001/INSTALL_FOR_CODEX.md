# B01-40 W11 — LEGENDARY-CORE-001 R1 / INSTALL_FOR_CODEX.md

**NOT INSTALLABLE IN LIVE UNITY GAME.** This is a pure .NET8 **domain prototype**; only its logic is eligible for future adapter design, after design/API arbitration and independent QA. **G2 HOLD.**

## Source provenance and proof

Exact [Draft PR #230](https://github.com/C8LyNn/RoboRougelike/pull/230) SHA `7e9d9ca0a0ef6d4c9bf1ce39fdb187b0d960958d`, base historical main `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`; design [PR #229](https://github.com/C8LyNn/RoboRougelike/pull/229) SHA `d5f6bfb9e6c019aadcaec2ae70ca3390c3923f61`. Source is exactly **7 independent files** under `Tools/B01/LEGENDARY-CORE-001/**` and `.github/workflows/b01-legendary-core.yml`, 38,928 bytes; NO `Assets/**`, prefabs, Unity scripts, .meta or game integration.

Real GitHub CI [run 40187129527](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187129527) at exact producer SHA: .NET8 build SUCCESS, 25 distinct regression PASS/25, 2000 deterministic transitions seed224001. Independently downloaded workflow artifact [ID 12596144631](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187129527/artifacts/12596144631): wrapper **2667 bytes**, SHA256 `7dcfb3a0a182218339a170b2104efb70b3a7342ded78ece99c2f632e6f044c08`. B01-40 independently fetched the 7 source files by immutable Git SHA and recalculated 7/7 SHA256 matching `source-sha256.txt`; parsed 25 unique named PASS and `native_unity_tests=NOT_RUN`. No .NET SDK was available in the B01-40 local container to independently rerun executable suite; CI execution is verified evidence, not our re-execution.

## Hard source/API mismatches (DO NOT auto-import)

- Stable unique legendary **definition** ID versus unique **physical** instance ID (`R3PartState.Id`). At most one equipped item of each legendary type **per individual robot** including zero HP, while duplicate owned copies may exist in inventory. This is not a world/global cap.
- Design PR #229 proposes `LEG-WPN-001` **650 kg** and `LEG-MOV-001` **780 kg**; executable R1 prototype proposes **220 kg** and **150 kg**. These are contradictory balance parameters and B01-10/owner must choose; neither is native-physics validated.
- R0 JSON fields `legendaryDefinitionId`, `basePartKind=Weapon`, `partFamily`, `mountFootprintCells`, `hitPoints`, `socketCategory`, `ability` differ from R1 `Id`, `BasePartKind=Gun`, `WeaponFamily`, `Footprint`, `MountTag`, `ProposedEffect`. For movement item R0 `partFamily=Hover` but R1 `WeaponFamily=null` and `BasePartKind=HoverEngine`. **No canonical schema/migration implemented**.
- The live OBJ #210 reference `4c9236a4cdc27814ea2e7679872ec4a524150cc8` carries newer R3 part kinds and occupied-cell geometry. It is NOT a confirmed current Codex target; read-only target ACK remains pending. The game's R3 vehicle parts/inventory and canonical save must remain authoritative; do NOT instantiate LegendaryGarage as a second game inventory.
- R3 `R3VehicleModel.Remove` historically fails to return dismantled **0 HP** items to inventory. A direct adapter can lose legendary physical identity; resolve and test without permitting double-spend. The game weapon family logic historically supports Basic/Automatic/Heavy and four-gun limit; R1 uses Railgun and two weapon **families**, with unlimited physical barrels subject to real physical slots. Await #41 plus BUILD #215 sockets, #140/#141 mounts, ECON/FIRE/R4 decisions before API integration.
- The catalogue's Przebijacz weapon and Serce Zawisu hover ability are **unimplemented designs**: no real damage, recoil, energy, EMP, physical mass, assembly attachment, shaders or prefab. No claim of Unity game features.

## Phase 0 — safe reproducible prototype QA only

1. Read PR230 exact commit + 7-file SHA256 receipt. On a disposable **.NET8** worktree run
```bash
dotnet build Tools/B01/LEGENDARY-CORE-001/tests/LegendaryCore.Tests/LegendaryCore.Tests.csproj -c Release
dotnet run --project Tools/B01/LEGENDARY-CORE-001/tests/LegendaryCore.Tests/LegendaryCore.Tests.csproj -c Release --no-build -- --report artifacts/legendary-core/test-results.json
```
   Capture real exit code, exact SHA, build warnings/errors, JSON with 25 names. Do not call old run new execution.
2. Independently test interrupted mount, 0HP unmount/repair/serialization, two-robot switching of same instance, duplicates/reward replay, save/load and corruption boundaries. Consumer B01-60 reviews results separately.
3. Prepare explicit **future** R0↔R1↔R3 adapter table and B01-10 approved mass/footprint/socket balance. Obtain actual target branch/HEAD/scope ACK. This is design exploration only and MUST NOT modify `Assets/R3/**`, `Assets/R4/**` or Codex worktree.
4. Report source G0, API-target G0 and independent QA as separate statuses. G1 Unity cannot pass on a prototype that has no Unity adapter. If any API implementation is later authorized, isolate it to a new Draft branch, execute full Unity Edit/Play/garage/physical/save tests with B01-60 and re-gate.

## Rollback

For prototype inspection, simply remove the disposable worktree after logs are backed up; no game data was installed, so there is **nothing to rollback in the game**. For any *future* authorized adapter, revert recorded isolated Codex branch commits, restore canonical R3 save/inventory without duplicate ID migration, verify 0HP copy ownership and reward ledger using before/after serialized fixtures. Do not mutate main/develop or game releases.

**Current W11 gates:** `G0_SOURCE=PASS_PROTOTYPE_7_OF_7`; `G0_TARGET=BLOCKED_SCHEMA_AND_LIVE_TARGET_ACK`; `G1_NATIVE=NOT_RUN_UNITY`; `G1_INDEPENDENT_QA=PENDING_B01_60`; `G2_HANDOFF=HOLD`. No CODEX integration command authorized.
