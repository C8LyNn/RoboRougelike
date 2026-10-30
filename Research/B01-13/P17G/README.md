# INPUT-DISCOVERY-001/R1 — B01-13 technical input discoverability inspector

**PASS17 WAVE09, B01-13 Human Playtesting.** This is an **actual Unity 6 runtime C# input-observer MonoBehaviour plus a standalone prefab**, not an HTTP mock, a JSONL analyzer or a change to the game's controls. This is distinct from the manual task-marker overlay (W08 Draft #458), operator JSONL (W06 Draft #426), and cohort aggregation (W07 Draft #435).

## Real function

When explicitly imported and enabled in an isolated technical PlayMode scene, the MonoBehaviour polls legacy Unity Input.GetKeyDown/GetMouseButtonDown *without consuming or remapping input*. It detects the first press of exactly seven raw signals: **W, A, S, D, SPACE, left mouse, right mouse**. It records **in memory only** first-press latency since OnEnable using Time.realtimeSinceStartupAsDouble and bounded repeat counters. F8 shows/hides an IMGUI overlay that displays detected/not detected and milliseconds. The local reset button clears *only* this observer. On a project with **New Input System only**, the adapter reports that the legacy Input Manager is unsupported instead of claiming successful detection.

**Important:** a key press is not evidence of movement, damage, weapon fire or boost. SPACE is a raw key event, not presumed boost. Nothing is inferred about participant intent or game success. F8 can conflict with a game hotkey; only run on a separate technical test scene and check game controls still respond. Observation state is lost on scene unload/disable. No identities, free text, network, audio, video, screenshot collection or persistent file output.

## Authorized isolated Unity 6 URP staging

1. Run the exact byte and GUID verifier (commands below).
2. Only an authorized receiver copies **UnityPackage/Runtime and UnityPackage/Prefabs including .meta files** to an isolated, authorized Unity 6 URP project folder under Assets/B01_13_InputDiscovery/. **Never copy Tests/DotNetOnly/UnityStubs.cs into Unity**; those are test-only mock UnityEngine classes. This PR never modifies canonical Assets or Codex sources.
3. In the *separate test project only*, set Project Settings / Player / Active Input Handling to **Input Manager (Old)** or **Both**. Do not alter canonical project's Input Handling.
4. Place **B01_13_InputDiscovery_Optional.prefab** into an isolated technical test scene. Play with Game view focus and press W/A/S/D, SPACE, left and right mouse buttons. Confirm in-game first-input timestamps, F8 visibility, reset, and that the game still receives controls. Test incompatible input mode separately and record the visible warning. Confirm imported prefab script GUID resolution.
5. Remove only the prefab instance and the imported isolated folder. Verify no scene references remain.

**Native Unity EditMode, PlayMode, actual Player executable and actual human participant tests are NOT_RUN in this package**, pending authorized host and independent QA. Source-green .NET tests are not Unity G1 PASS. Any study with people requires explicit HQ GATE D, privacy controls and valid consent. This tool stores no actual participant data.

## Actual executable source and negative tests

Run:

    python Research/B01-13/P17G/verify_package.py
    dotnet build Research/B01-13/P17G/Tests/DotNetOnly/InputDiscoveryTests.csproj -c Release --warnaserror
    dotnet run --project Research/B01-13/P17G/Tests/DotNetOnly/InputDiscoveryTests.csproj -c Release --no-build

The .NET8 harness compiles **the two original production C# files** with **test-only in-memory Unity Input/GUI/Time stubs**, then executes 51 actual positive, negative and adapter tests, including original private OnEnable/Update/OnGUI via reflection. Cases cover first press, latency, repeat, all seven inputs, invalid time/action, reset, six-hour session bound, non-consuming F8 visibility, legacy Input System fail-closed warning, mouse buttons and visible labels.

GitHub Actions runs the same original cases on Ubuntu 24.04 and Windows 2025, records physical per-runner test output evidence, checks **all 12 exact source+asset SHA256 and byte counts** before running, and statically verifies prefab script .meta GUID and three unique Unity GUIDs. CI is not native Unity runtime execution.

## Ownership, QA and rollback

- Scope only Research/B01-13/P17G/** and dedicated .github/workflows/b01-13-p17w09-input-discovery.yml, base main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1. No modifications to frozen W03/W06/W07/W08, shared Assets, Codex camera/garage/input mappings, CHL/PARTSIZE/MODEL_JOB or common workflows.
- B01-40 independently checks frozen HEAD, 13-path scope, original asset bytes, prefab GUIDs, dependencies and rollback. B01-60 independently runs original source tests and checks negative cases, no-data persistence and legacy-input limitations. B01-00 owns study fit and real human GATE D. Native import is solely an authorized Unity receiver action. No CODEX SENT, CLAIMED, ACK or integration is asserted without evidence.
- Before independent receipt leave Draft unmerged. On explicit RETURNED_FOR_REWORK create another version/branch, never mutate a frozen offered exact SHA. To roll back, discard the unmerged source branch; in external authorized isolated Unity staging remove the prefab instance and copied directory. No persistent evidence to delete from this source.
