# B01-24 / PASS17 — ENEMY-FORMATIONS-002 v1.0.0

**New optional game content, not an installed feature:** twelve formation layouts for existing three `R5ArenaKind` arenas, with exact `Vector3[] spawnPositions` input already supported by original `R1EncounterManager.Spawn`. This package **does not modify integrated game code**, existing R5Arena, CHL, PARTSIZE, B01-25 or Codex-owned LD02.

## Content

Source [formations.json](formations.json) defines three original deployment themes, each independently authored for real enemy counts 1, 2, 3, 4:

| Arena (source enum) | ID | Role |
|---|---|---|
| SalvageYard (0) | B0124-SCRAP-PINCER | Two-sided pincer / rear scout reserve |
| Wasteland (1) | B0124-WASTE-CHASE | Wide diagonal pursuit line |
| Industrial (2) | B0124-INDUSTRIAL-CROSSFIRE | Staggered central shooting lane |

That yields **12 distinct spawn layouts / 30 explicitly authored coordinates**, no randomizer or changes to existing encounters. The source `R1EncounterDefinition.SpawnCount` controls the total. On ordinary encounters with 1–3 authored enemies, the original extra Scout escort brings this up to 2–4; boss encounters have one. No changes to stats, rewards or weapons.

## Working tool and actual reproduction

Node.js 22, built-in APIs only:

```sh
node --test Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.test.mjs
node Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.mjs --check
node Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.mjs --emit B0124-SCRAP-PINCER 4
node Research/B01-24/P17/ENEMY-FORMATIONS-002/formation_tool.mjs --svg B0124-INDUSTRIAL-CROSSFIRE 4 > formation.svg
```

The validator reads real unchanged source `R5Arena.cs`, `R1EncounterManager.cs`, `R1RunDefinition.cs`, `R5RunPlan.cs` and **fails closed** if any pinned Git blob SHA changes. It validates conservative **2.5m circular planning footprints**, minimum **7.5m between enemy spawn centers**, **27m from player origin**, absence of footprint overlap with all static R5Arena XZ wall/obstacle AABBs, deterministic export, and malformed layouts. The CLI emits exact Unity `new Vector3[] {...}` copy-ready arguments, plus SVG top-down source-derived previews. Original source geometry here is a **static, conservative planar approximation**; it is not PhysX or proof of any physical prefab envelope.

The dedicated `.github/workflows/b01-24-p17-enemy-formations.yml` runs real original JS and tests independently on Windows and Ubuntu, and archives raw results, source-derived SVGs and `Vector3[]` export. See [manifest.json](manifest.json) for source/input Git blobs and active release gates. **Any native Unity Edit/PlayMode, projectile, AI routing, vehicle colliders, human playtests and Windows Player are NOT_RUN.**

## Codex integration and complete rollback

**Only after B01-40 intake, B01-60 review and explicit Codex owner acceptance:**

1. In Codex's **disposable original-game checkout**, verify the SHA-pinned R5Arena/R1EncounterManager sources. Use the CLI `--check`; do not attempt integration if `SOURCE_DRIFT` appears.
2. Select a formation ID by `R5ArenaKind`, and choose `count = definition.SpawnCount` (not `EnemyCount`). Run `--emit ID COUNT` and pass the resulting `Vector3[]` as the existing **last** `spawnPositions` parameter of `R1EncounterManager.Spawn(template,player,definition,salvage,encounterId,spawnPositions)`. The enum/selection wiring remains the exclusive Codex task; no runtime source or adapter is supplied here.
3. Run native Unity physical tests with **real** 1–4 spawned prefab colliders and enemy path/contact/weapon behavior in each arena. Verify no initial overlap with world, player or allies; preserve intended boss single-spawn. Test in actual Player after scripted checks. Report exact game SHA, XML, Editor.log and exit. Recheck any R5 arena obstacle changes or altered prefab envelopes.
4. To rollback, remove *only* Codex's optional formation-selection wiring, or restore Codex's original `R1EncounterManager.Spawn` call without `spawnPositions`. The original default stagger layout remains intact and our research branch adds no imported `Assets/**` files.

**Ownership and overlap:** This does not replace B01-20/B01-21 MAPGEN arenas, B01-25 LD02 map/levels, earlier B01-24 ENEMY-RELAY-001 catalog, or Codex's own spawner. It uses existing positional extension point without changing game logic. G1/G2 not closed, no merge and no implied Codex ACK.
