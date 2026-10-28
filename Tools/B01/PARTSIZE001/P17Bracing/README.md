# B01-23 / PASS17 WAVE02 — PARTSIZE001-P17-W02-STRUCTURAL-BRACING-R1

**NEW optional robot construction content + executable original-source placement/structural safety gate.** Not a rewrite of #348 static rigs, #361 weakpoints, #373 independent two-loss QA, B01-12 garage repairs or B01-22 equipment kits. This pack contains **new physical Armor placements** that provide alternate structural paths around previously identified single-point disconnections. No new PartKind/mesh/MODEL_JOB, no price, unlock, owned-ID mutation, actual physics/damage or Codex runtime code.

## Actual Unity-importable game content

Authoring folder: `Assets/PARTSIZE001/Bracing/P17/`. Four new `.json` Unity `TextAsset` files, plus six matching stable `.meta` records (two folders, four texts).

| Kit ID | Original source | New Armor modules | New variant | Source-derived targeted recovery |
| --- | --- | --- | --- | --- |
| P17-BRACE-FLANKER-ROOFLINK | Flanker, 14 parts | 1 at (0,2,1) | 15-part Flanker Reinforced | Detach Armor-006 no longer orphans Gun-013 |
| P17-BRACE-BULWARK-TRACKLOOPS | Bulwark, 14 | 2 at (-2,0,1) and (2,0,1) | 16-part Bulwark Reinforced | Armor-003 no longer orphans Track-011; Armor-008 no longer orphans Track-012 |
| P17-BRACE-SKIRMISHER-FRAME | Skirmisher, 10 | 2 at (0,-1,0) and (0,2,1) | 12-part Skirmisher Reinforced | Side Armor-002 / Armor-003 removal preserves respective HoverEngine-007 / -008, and Armor-006 removal preserves Gun-009 |

The kit catalog `P17_StructuralBracingKits.json` identifies all six break/recovery goals and the exact two predecessor commits. The other three files contain **full new authored vehicle `P17Rig` JSON blueprints**, including original modules with unchanged original physical IDs and five new uniquely owned armor modules across the three variants. These are optional content variants; they are not starting designs until Codex imports and authorizes them.

**Scope of validity:** `contactMask=63` is **existing logical six-face source contact**, *not measured typed sockets or a physical mount*. The original `PartSizeBaselineCatalog` marks Wheel/Gun/Track/Hover physical envelopes **UNMEASURED**. No physical collision, shooting, actual Rigidbody detachment or native Unity gameplay has been tested. Underlying Rigidbody and damage behavior remains Codex-only.

## Real executable source and regression

`Tools/B01/PARTSIZE001/P17Bracing/BracingEngine.cs` and `Program.cs` compile these four **unchanged originals directly**:
- `Assets/PARTSIZE001/Runtime/PartSizeContract.cs`, blob `07455919a5544875fee5a78ecbbce36dc6cb49c2`
- `Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs`, blob `6153d1758e800ec5c4210323610b061c06e8ea85`
- `Tools/B01/PARTSIZE001/P17Rigs/RigValidator.cs`, blob `988fc31d8633e5429fc4ba377856187e5ee7601c`
- `Tools/B01/PARTSIZE001/P17Weakpoints/WeakpointEngine.cs`, blob `a3abe5a24afdd365a5fb162a12b3947fb2f0d10c`

`--selftest` tests actual 3 new variants, original 3+5 armor content, all six positive weakspot mitigation goals, and **every predecessor single-removal case** for non-regression of all originally connected modules, mobility and connected guns; also rejects deliberately forged native status, weapon-in-brace, invalid contact mask, duplicate target, duplicate physical ID, overlapping or disconnected armor, bad source SHA and base-rig mismatch. `--check` independently re-creates each full variant using the new kit data and compares it **against the committed Unity JSON game asset**. `--preview` creates a replayable six-goal before/after text view, archived under each OS by CI. The Python gate checks six exact Unity GUIDs against all existing `Assets/**/*.meta`, four intentionally malformed GUIDs and false PHYSX/PASS tags.

From the exact branch root with .NET8 and Python3:

```bash
python Tools/B01/PARTSIZE001/P17Bracing/check_bracing_assets.py
dotnet build Tools/B01/PARTSIZE001/P17Bracing/P17Bracing.csproj -c Release --warnaserror
dotnet run --project Tools/B01/PARTSIZE001/P17Bracing/P17Bracing.csproj -c Release --no-build -- --selftest
dotnet run --project Tools/B01/PARTSIZE001/P17Bracing/P17Bracing.csproj -c Release --no-build -- --check
dotnet run --project Tools/B01/PARTSIZE001/P17Bracing/P17Bracing.csproj -c Release --no-build -- --preview
```

Dedicated `.github/workflows/b01-23-p17-wave02-bracing.yml` verifies exact original blobs and source ancestry, builds actual immutable source on Windows 2025 and Ubuntu 24.04, executes all checks, deliberately changes a variant into disconnected geometry, asserts checker **rejects it**, restores exact Git-tracked JSON, and uploads machine-generated before/after source reports. All tests are original C#/Python **source-only** until a licensed Unity host is available.

## Codex handoff, ownership, dependencies, rollback

1. **Stacked immutable prerequisites (not merged):** frozen original PARTSIZE producer [#286](https://github.com/C8LyNn/RoboRougelike/pull/286) `be2be0e4802bc3f450179901f9ae91c6d84bcb5b` → original 3 P17 robot rigs [#348](https://github.com/C8LyNn/RoboRougelike/pull/348) `0978497561c8b6400490235d3e5a401ac32bea2d` → single-break weakpoint source [#361](https://github.com/C8LyNn/RoboRougelike/pull/361) `098ccdbe8f129253fa4ada52318436b7cc2b4dc4`. This package is **new content based on #361**, never modifies any prior original path. Independent B01-61 source [#373](https://github.com/C8LyNn/RoboRougelike/pull/373) verified predecessor 38/38 and produced additional two-break cases. It does not measure Unity physics.
2. B01-40: inspect source path boundaries and exact Git manifest, check builds and rollback on a *detached clean branch*. B01-60: independently compile and evaluate original source, then request separately authorized Unity G1. Neither test is claimed accepted by this producer.
3. After explicit B01-00 approval and Codex claim, Codex **only** imports the four new `Assets/PARTSIZE001/Bracing/P17/*.json` and own `.meta` dependencies in isolated staging. It may choose one of the full new reinforced vehicle JSONs, allocating **fresh runtime part IDs** while preserving original source reference identity only in source design. Never treat authored `localId` as a persistent live GUID. Do not silently change garage economics; five new Armor items have no authorized scrap cost/unlock rules in this candidate.
4. Actual Unity 6000.3.14f1 G1 needs physical mounting/sockets, all four yaw orientations if rotated, part/HP owner IDs, Collider.bounds + narrowphase `Physics.ComputePenetration`, moving suspension/wheel sweeps, ground/roof clearance, build/save/load, actual drive and guns after destruction, PlayMode. If real physical placement fails, **stop import**, report offending case and raw original Editor.log/NUnit XML/exit; this source package must not override existing placement gates.
5. Rollback = remove only newly staged `Assets/PARTSIZE001/Bracing/` and dedicated `.meta` files from the isolated integration branch; never mutate original PARTSIZE producer, upstream P17 assets, original weakpoint data, existing PRs, main/develop, Codex active worktree or raw QA evidence.

**Native status:** Unity Editor/PhysX/Player `NOT_RUN`; `G1=NOT_RUN`, `G2=HOLD`, independent `B01-40/60=AWAITING REVIEW`, `CODEX_ACK=PENDING`, `INTEGRATED=NO`. These source-geometry fixes are **design predictions**, not validated physical vehicle improvements.
