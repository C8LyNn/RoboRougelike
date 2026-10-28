# B01-23 / PASS17 — PARTSIZE001-P17-WEAKPOINTS-R1

**Package type: original game-data candidate + standalone executable source processor.** This is the **NEXT INDEPENDENT CAPABILITY AFTER [PR #348](https://github.com/C8LyNn/RoboRougelike/pull/348)**: single physical module-loss vulnerability for each authored specialist vehicle. It is NOT a repeat of the three rig blueprints, the P16 driver safety policy, or Codex's `R3VehicleModel.Connected`. That existing predicate answers only whether a current build is connected; this package enumerates **all 38 potential one-part destruction outcomes**, listing exact separate survivor part IDs lost from the live core, operational drive eligibility, and surviving connected guns **before an actual battle**.

## Real original content (Unity TextAsset)

`Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json` is an importable Unity `TextAsset` (with three new dedicated stable `.meta` GUIDs, including the folder metadata).

| Original rig | Source owned parts / break scenarios | Example material consequence |
| --- | --- | --- |
| Flanker / Wheels | 14 | Destroy roof Armor-006 → Gun-013 becomes detached; destroy Wheel-009 → the other three remain |
| Bulwark / Tracks | 14 | Destroy side Armor-003 → CrawlerTrack-011 loses connectivity; directly destroy track-011 → selected Tracks no longer operable |
| Skirmisher / Hover | 10 | Destroy Armor-002 → HoverEngine-007 becomes disconnected; direct loss of HoverEngine-007 alone does not disable all hover propulsion |

**All 38 records are derived from individually authored original physical-part IDs, not voxel/item duplicates.** They include removedPartId, removedKind, coreLost, sorted detachedPartIds, detachedCount, selectedDriveOperable, connectedGunCount and **nativePhysicalStatus: NOT_RUN**. The engine intentionally processes **all cells owned by each real PartSizeDefinition**, so a future wide part remains one owner, not four independent pseudo-HP objects.

### Compiled algorithm

`Tools/B01/PARTSIZE001/P17Weakpoints/WeakpointEngine.cs` compiles unmodified `PartSizeContract.cs`, `PartSizeBaselineCatalog.cs`, and P17 `RigValidator.cs` from prior PR #348. It uses source-canonical 0.55m logical grid occupied cells and six-face port assumptions, removes exactly one owner, then performs BFS from the live Core. It lists **orphaned physical owned IDs**, distinguishes the destroyed module from collateral detachments, and derives selected drive / surviving guns. It never mutates input rig data.

**It does NOT implement actual detach, Rigidbody physics, collider/typed socket contact, health/HP, damage application, forced child destruction, inventory, the garage, AI targeting, or canonical game integration.** The source graph does not prove a side-mounted wheel or barrel physically fits, that it is reachable by a projectile, or that anything in a production vehicle actually falls off. A native G1 check must verify all physical behavior.

## Reproduce the actual data and inspect it

Requires .NET8 and Python3. Execute from repository root:

```bash
dotnet build Tools/B01/PARTSIZE001/P17Weakpoints/P17Weakpoints.csproj -c Release --warnaserror
dotnet run --project Tools/B01/PARTSIZE001/P17Weakpoints/P17Weakpoints.csproj -c Release --no-build -- --selftest
dotnet run --project Tools/B01/PARTSIZE001/P17Weakpoints/P17Weakpoints.csproj -c Release --no-build -- --check
dotnet run --project Tools/B01/PARTSIZE001/P17Weakpoints/P17Weakpoints.csproj -c Release --no-build -- --preview
python Tools/B01/PARTSIZE001/P17Weakpoints/check_weakpoint_assets.py
```

`--selftest` exercises actual original compiled source and every committed case, including original deterministic weak spots, Core destruction, mobility loss and malformed blueprint negative cases. `--check` independently rederives all 38 cases from original C# graph/PartSizeGrid and checks the entire committed JSON semantically **without turning native NOT_RUN into success**. `--preview` writes a readable source-only report, archived in both Windows and Ubuntu GitHub Actions. The CI also *deliberately corrupts one single-break record*, proves `--check` exits nonzero, restores the original and verifies it is unchanged in Git.

## Exact upstream ownership

- Frozen original B01-23 producer [#286](https://github.com/C8LyNn/RoboRougelike/pull/286) HEAD `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`.
- Original B01-23 new rig-content [#348](https://github.com/C8LyNn/RoboRougelike/pull/348) HEAD `0978497561c8b6400490235d3e5a401ac32bea2d`. This PR **stacks on #348** but only adds new `Assets/PARTSIZE001/Weakpoints/**`, `Tools/B01/PARTSIZE001/P17Weakpoints/**`, dedicated CI and own manifest. It does not edit the base PR.
- Existing Codex R3 `Connected` and `CriticalReadinessMessage` remain canonical. Current Codex code and actual run/AI/gameplay integration are not changed.
- No new MODEL_JOB, asset geometry, weapon group, balance rule, garage UI or map generation.

## Codex intake / safe rollback

1. B01-40 verifies 10+ new file Git blobs, original upstream source pins, the GitHub Actions dual-platform original compilation/tests, untouched old PR HEADs and isolated stage/rollback. B01-60 validates independently. **Neither independent approval nor native G1 has yet occurred.**
2. HQ first approves optional intake after source acceptance. **Codex explicitly acknowledges** scope and (if #348 is already installed) can import this one JSON TextAsset and its own Unity `.meta` folder metadata, preserving original runtime IDs and original R3 code.
3. An optional *new Codex-owned host bridge* could expose consequences as a construction-analysis/weak-point preview or deterministic combat challenge indicator; avoid auto-inflicting damage or authorizing attack behavior from this tool. Merge nothing until the host proves current physical socket connections, actual disconnect semantics and performance on 2/6/12+ parts in licensed Unity.
4. Rollback *only this weak-point JSON and its own meta directories* from the isolated staging branch; leave old P17 blueprint data, original source, other worktrees and all evidence unchanged. If a different integrated algorithm disagrees with source predictions, classify it as integration/physical test discrepancy — never quietly overwrite old frozen producer.

**Gate state:** original source logical tests may be PASS. **Unity EditMode/PlayMode = NOT_RUN; PhysX/typed-socket collider = NOT_RUN; G1=NOT_RUN; G2=HOLD; CODEX_ACK=PENDING; INTEGRATED=NO.**
