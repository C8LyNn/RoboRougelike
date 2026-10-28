# PARTSIZE-001 / P17-SPECIALIST-RIGS R1 — three new modular robot blueprints

**Producer:** B01-23. **Status:** NEW GAME-CONTENT CANDIDATES, SOURCE-PROVEN LOGICAL GRID ONLY. **Not merged, not yet selectable in garage, not Unity/PhysX/typed-socket accepted.**

These three **original, non-default** robot layouts use only existing canonical R3 parts (no MODEL_JOB, mesh or new R3PartKind) and can be consumed by Codex as additional, optional vehicle designs after integration approval. They do not replace original `R3VehicleCatalog.Light / Standard / Heavy`.

| Stable blueprint ID | Drive selection | Physical owned parts | Explicit gun parts | Role |
|---|---|---:|---:|---|
| P17-FLANKER-WHEELS | Wheels | 14 | 2 Basic | Compact four-wheel flanking platform |
| P17-BULWARK-TRACKS | Tracks | 14 | 2 Basic | 3×3-armored two-track front-line platform |
| P17-SKIRMISHER-HOVER | Hover | 10 | 2 Basic | Two-engine mobile skirmisher with lighter armor |

All cells and serial IDs are deterministic, one owner per part; `localId` is **not** a live inventory GUID. `contactMask:63` indicates the existing all-faces logical mask. It is **not** a proven typed physical socket. `recommendedBaseProfile` is an optional gameplay-design suggestion (Light / Heavy / Standard respectively), **not a new or approved Speed/HP/Mass value**.

## Real files

The 3 original `*.json` and complete stable Unity `*.meta` assets are in `Assets/PARTSIZE001/Blueprints/P17/`. They import into Unity 6 as editable `TextAsset` data. Their schema fields are:

- `blueprintId`, `displayName`, `description`, `selectedDrive`, `recommendedBaseProfile`;
- `parts[]` each with `localId`, exact canonical `kind`, integer `cell:{x,y,z}`, `rotationQuarterTurns`, `contactMask`, and `weapon:"Basic"` only for Guns (else null);
- exact `sourcePartSizeCommit` and `nativePhysicalStatus:"UNMEASURED_COLLIDER_AND_SOCKET"`. Never turn source-only data into claimed native collision clearance.

The actual frozen original `Assets/PARTSIZE001/Runtime/PartSizeContract.cs` and `PartSizeBaselineCatalog.cs` are **compiled directly** by the standalone `.NET8` validator, not reimplemented or copied. It checks canonical kinds, 1x1x1/2x1x2 original occupied cell geometry, unique owner cells and ids, BFS six-neighbor core connectivity, both sides of selected wheel/track drive, Hover engine availability, >=1 and <=2 Basic guns, source SHA, contact-mask policy and explicit physical unknowns. It does not claim `Physics.ComputePenetration` or actual compatibility with all Codex game rules.

## Reproduce / preview (Windows or Ubuntu, .NET8)

From the repository root of this exact P17 branch:

```bash
dotnet build Tools/B01/PARTSIZE001/P17Rigs/P17Rigs.csproj -c Release --warnaserror
dotnet run --project Tools/B01/PARTSIZE001/P17Rigs/P17Rigs.csproj -c Release --no-build -- --selftest
dotnet run --project Tools/B01/PARTSIZE001/P17Rigs/P17Rigs.csproj -c Release --no-build -- --preview
```

`--preview` prints a deterministic layered X/Z robot plan where C=Core, #=Armor, W=Wheel, T=Track, H=HoverEngine and G=Gun. It is a **source-grid plan**, not Unity rendered output. The dedicated `.github/workflows/b01-23-pass17-rig-blueprints.yml` executes both commands, source pin checks and invalid-CLI rejection on **Windows2025 + Ubuntu24.04**, then uploads `P17-rig-layer-preview-<OS>`. Native Unity never runs in this workflow.

## Codex install decision (NOT yet authorized)

1. Inspect exact PR SHA / JSON and stable `.meta` GUIDs; run independent B01-40 G0 and B01-60 source review before promoting. Preserve producer #286 immutable.
2. Stage **only** `Assets/PARTSIZE001/Blueprints/` as optional `TextAsset` data into an isolated licensed Unity 6000.3.14f1 test branch; never auto-register as garage default or alter canonical `Assets/R3/R3VehicleCatalog.cs` outside Codex scope.
3. Codex (if it explicitly ACKs and elects to integrate) maps each validated JSON row to a **new** `R3PartState(new Vector3Int(x,y,z), R3PartKind.<kind>)`, `RotationQuarterTurns`, `ContactMask`, and `R4WeaponKind.Basic` for Gun; **create fresh runtime GUIDs**. Read `selectedDrive` as `R3DriveKind`, and decide approved speed/turn/HP/balance via existing `R3VehicleCatalog` profiles. Use the existing placement/inventory rules, no free purchasing or duplicate rewards.
4. In real Unity, actually construct each rig, verify connected physical parts, supported drive modes, weapon fire/camera/framing, terrain contact, overlap, reach, save/reload and damage/repair. The source `PartSizeBaselineCatalog` explicitly leaves Wheel/Gun/CrawlerTrack/HoverEngine physical envelopes **UNMEASURED**; the logical passing test **cannot replace G1 PhysX**.
5. If any part fails native placement or design rules, do not override them or merge. Return exact failing Unity input to B01-23/B01-40 and ask HQ/Codex for an authorized new revision. Rollback = delete only the staged optional `Assets/PARTSIZE001/Blueprints/` files + their own `.meta` from isolated branch. Never reset/clean Codex workspace or delete raw QA evidence.

**Dependencies:** original [PARTSIZE producer #286](https://github.com/C8LyNn/RoboRougelike/pull/286) exact `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`, existing Codex `R3VehicleModel` and `R3VehicleCatalog` (read-only; live SHA must be rechecked by Codex). **No new art job, no R3 code edits.**

**Known limits:** two Basic guns only; no new enemy AI, camera logic, economy or gameplay loop. No actual Unity scene/Player run, independent QA verdict or Codex ACK. All 3 rigs are content candidates, **not confirmed playable in the shipped game**.
