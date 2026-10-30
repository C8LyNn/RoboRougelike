# LD02-TECHOVERLAY-001 / R1 — real Unity 6 technical playtest prompt prefab

**B01-13 Human Playtesting — PASS17 WAVE08.** This is an actual **Unity C# MonoBehaviour overlay**, an original **prefab with stable `.meta` GUIDs**, plus a separate pure C# deterministic four-stage protocol. It is distinct from previous B01-13 screenshot research, operator JSONL capture (W06 #426) and JSONL aggregation (W07 #435).

## What the package actually does

In a technical test scene the operator gets a fixed four-task sequence: (1) fight, (2) garage, (3) repair, (4) checkpoint. Each trial uses a deterministic 1:1 counterbalanced hint/no-hint assignment: consecutive trial numbers swap variants for **each stage**, and every trial contains two neutral and two assisted stages. No random generators, identifiers or network calls. The **IMGUI panel is visible at Play**, can be hidden using F9, and offers manual **PASS, FAIL, SKIP** buttons. The assisted variant can reveal a hint; the neutral variant cannot. The protocol validates stage order and invalid outcomes, and reset clears the four outcomes.

**The buttons are not wired to actual combat, garage, repair or checkpoint game state.** They capture facilitator *judgment only*. Panel neither writes data nor modifies game mechanics, vehicle movement, weapons, input mappings, camera, garage, save, or integrated UI. It is a technical facilitator aid, **not an approved human experiment**. For genuine playtest with people, independent HQ GATE D and consent required; this source has no participant tracking and must not be silently used to gather user data. No actual sessions/players claimed.

## Unity 6 URP safe staging instructions — NOT automatically integrated

1. Read the manifest and verify all pinned SHA256/byte counts with `python Research/B01-13/P17F/verify_package.py`.
2. In an **isolated Unity 6 URP test project or scene only**, copy the *complete directory* `Research/B01-13/P17F/UnityPackage/Runtime/` including both `.cs.meta` and `Research/B01-13/P17F/UnityPackage/Prefabs/` including `.prefab.meta` under an isolated `Assets/B01_13_LD02_TechOverlay/`. Import package; check for compilation and GUID resolution. **This copy into actual Unity `Assets/**` is reserved to an authorized B01-40/Codex receiver**, not performed by this producer.
3. Add `LD02_Technical_Overlay_Optional.prefab` to the isolated technical study scene. Set `technicalTrialNumber=0` for neutral first stage, `1` for assisted first stage; alternate trial number on successive *technical facilitator sessions*. Enter Play mode and validate rendering and F9 hide/show, task buttons, hint variant and reset. **Native Unity import/PlayMode/Player test is NOT_RUN in this PR**, awaiting receiver with Unity host.
4. After test, remove just the isolated prefab instance and imported package folder. No script data or persistent session files to wipe; no code merges and no modifications to canonical Codex game.

The hints are *proposed comprehension prompts*, not verified labels/instructions for the current Codex build. **B01-00 should approve wording and trial-design validity** before usage, especially garage and checkpoint phrasing. GUI may obscure the game view; reposition from Inspector, or keep hidden via F9.

## Real automated QA

```bash
python Research/B01-13/P17F/verify_package.py
dotnet run --project Research/B01-13/P17F/Tests/DotNetOnly/Ld02ProtocolTests.csproj --configuration Release
```

The independent **.NET8 console harness** compiles the exact two Unity-package original C# files with small `UnityEngine` **test-only stubs**. The stubs are in `Tests/DotNetOnly/` and must **NEVER be copied into Unity**. Its 35 executable positive/negative tests cover variants/counterbalance, four stages, success/failure/skip, no double complete, invalid stages/outcomes, reset, and compile-time overlay API compatibility. The Python gate validates original source/test/prefab/`.meta` SHA256 and byte size and checks prefab GUID reference and YAML structure. CI executes Windows2025 and Ubuntu24.04 with .NET8; this **does not prove native Unity compiles or renders IMGUI**, prefab resolves, or actual game behavior. Independent B01-40 checks GUID/rollback/scope, B01-60 source-API/negative assertions, then separately B01-40/Codex or authorized Unity test receiver performs native G1.

## Dependencies, owner, rollback

- **Exclusive owner:** B01-13 Human Playtesting. Branch `research/b01-13-pass17-wave08-ld02-unity-overlay`, base `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
- Only `Research/B01-13/P17F/**` and dedicated `.github/workflows/b01-13-p17w08-technical-overlay.yml` are in scope. Two .cs, 2 .cs.meta, one .prefab+.meta, test harness/stubs, doc, manifest and CI. No code from W06/W07 or Codex is imported.
- No runtime external libraries, no telemetry or persistence, no changes to game systems. Unity 6 URP compatibility is a **target**, not a native-tested claim.
- **Rollback:** keep Draft unmerged; discard branch. In separate test project, delete prefab instance, exact imported isolated folder; reopen scene/reimport to verify no references remain. Preserve original git GUID metadata in any authorized retry, do not regenerate/reuse GUIDs.
- **Receiver:** B01-40 immutable provenance and manifest, B01-60 executable independent QA, B01-00 design/consent/UI approval, Codex only if specifically authorized for isolated Unity-native integration. `OFFERED != ACK != INTEGRATED`.
