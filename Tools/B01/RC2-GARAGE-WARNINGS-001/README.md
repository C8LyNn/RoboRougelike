# B01-10 / W14 RC2 — critical garage-warning acceptance implementation

**Owner:** B01-10 Game Design & Balance, B01-00 [W14 authorization](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6416680391). **Purpose:** executable *C#* policy and exact negative cases, ready for conditional adapter use after native gameplay authority verification. NOT a competing Unity producer. No `Assets/`, R3, R1, PACK, BUILD #260, Codex tree, merge or G2 changes.

## Exact source grounding (frozen)
Codex experimental [RC2 #258](https://github.com/C8LyNn/RoboRougelike/pull/258) production SHA `46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f`:
- `Assets/R3/R3VehicleModel.cs:284-307`: `DeployError` tests live Core, `Connected(alive)`, live Gun, `HasMobility(alive,SelectedDrive)`; Wheels/Tracks require at least one of selected type at X<0 and X>0; Omni/Hover one of selected type. Merely installed vehicle parts are not enough when 0HP.
- `Assets/R1/R1RunController.cs:142-147`: `StartNextEncounter` uses `!VehicleModel.CanDeploy` regardless activity. This contract must not overwrite or bypass. **Owner decision on whether critical warnings block start is OPEN**, so exported `LaunchDecision` is `UNDECIDED_OWNER_APPROVAL_REQUIRED` in every case.
- `Assets/R3/R3GarageView.cs`: currently shows placement/repair/rebuild info; no proof of *activity-aware* always-visible critical readiness. Note future special weaponless race/escape **not necessarily integrated in current RC2**.

## Execute real C# checks
```bash
dotnet run --project Tools/B01/RC2-GARAGE-WARNINGS-001/Tests/WarningTests.csproj -- Tools/B01/RC2-GARAGE-WARNINGS-001/fixtures.json
```
Real .NET8 original C# policy compiled/executed. Tests include 33 source-specified fixtures × three assertions, 2,000 deterministic property cases × two assertions, three negative controls. These are portable policy tests, **not Unity/PhysX, gameplay, human or B01-60 native PASS**. The source does not modify the live game or grant launch.

## Decisions that minimize Codex changes
- **Critical always visible** when the usual building-assistant suggestions setting is OFF. Separate `Critical` and `Suggestions` outputs. Missing mobility always critical for a garage rover even if weaponless mission, regardless selected activity. If a combat activity requires a weapon, missing/nonfunctional gun always critical. A *proven* weaponless mission must set `ActivityRequiresWeapon=false`; unknown activity yields `ACTIVITY_WEAPON_REQUIREMENT_UNKNOWN` instead of assuming.
- **Operational, not presence**: only mounted live parts with canonical valid Core connectivity and confirmed persistent operational capability qualify. One wheel on each side for Wheels and likewise both sides for Tracks. Don't count 0HP, disconnected, EMP-disabled module, inventory item or mere prefab attachment as functioning. No silent fallback to another installed but unselected drive type. Unknown booleans produce `*_AUTHORITY_UNVERIFIED`, not a false green.
- A Core dead or more than one active Core triggers `CORE_NONFUNCTIONAL_OR_AMBIGUOUS`; disconnected parts `STRUCTURE_DISCONNECTED`. Foreign, duplicate-ID, or corrupted snapshots -> `SNAPSHOT_UNTRUSTWORTHY`.
- **Owner authority boundary**: this only returns warnings. `CanDeploy`, run outcome, loaner, win/loss, repair economy remain canonical and unchanged. The decision whether to require click confirmation or block the start is explicitly unresolved, never auto-made by this policy.

## Native receiver ask (bounded, not speculative)
One licensed Win64 R3 garage run with actual canonical part states & chosen activity:
1. Light/Heavy, Wheels, remove right wheel physically (or destroy right wheel to 0HP), disable ordinary guidance: critical `MOBILITY_NONFUNCTIONAL` remains on screen, right wheel not counted.
2. Destroy last attached Gun to 0HP in actual Combat selection: critical `WEAPON_NONFUNCTIONAL` remains visible when tips OFF.
3. Restore/fix both: warning disappears without editor restart or double-read; try activity that explicitly permits no weapon only if such activity really exists, else mark this subcase **NOT_RUN**. Do not silently force a new activity into RC2.
4. Capture original source SHA/Player build SHA, 720p/1080p screenshots, state dumps and EditMode NUnit/Codex integration. Confirm game *still* uses canonical start rules until explicit owner decision. No G1 from this policy's .NET results.

## Handoff and rollback
- **Producer handoff:** B01-23 only for BUILD-mounted validity, or Codex after B01-00 explicit receiver slot for existing `R3GarageView` cosmetic UI. No code change to their sources by B01-10. B01-60 independently tests actual delivered Player; B01-40 validates packaging/target.
- `INTEGRATION_FOR_CODEX.md` pins one-way read-only adapter fields and receipt without new R3 authority. Remove this isolated Tools folder/workflow to rollback; no gameplay/save change.
