# B01-21 MAP-COVER-001 / Codex receiver and rollback

## Scope and file ownership

This is strictly **offline world content**. Source files in `Research/B01-21/P17/COVER001/**` and dedicated own workflow; *no* `Assets/**`, Unity Scenes, MapGen builder, core or Codex source modified. Requires exactly the immutable **P17 MAP-ROUTES-001** parent `1971c819a99947553d7f28a104aa9601374b3b69` (and its P16 original MAPGEN JSON dependency). **Only Codex** owns any eventual in-game adapter.

## Exact current acceptance

B01-40: verify PR diff against frozen P17 parent, check only COVER001 path and own CI file, inspect strict source Git blob gates, reproducible 3 original map JSON + SVG/manifest, exact final SHA CI success, text/provenance source. B01-60: independent executable tests and negative mutations, distinct from producer's suite. Never claim B01-60 acceptance until a separate verified review exists.

Codex must either **reject/hold** the optional data (zero game change), or, after HQ gate + explicit claim, implement a canonical receiver in Codex-only branch validating `schema`, source `geometryHash`, enemy descriptor count, local XZ frame, source-gated Cover ordinal, body envelope and candidate anchor origins. Cover coordinates are **local to map**, not assumed Unity world position: transform positions via actual MapBuilder host. No imports to MapDocument-v2, no generated-routes parser reuse without explicit format compatibility.

**Native test before gameplay use**: licensed target Unity 6000.3.14f1; exact game SHA; fresh current robot Light/Heavy dimensions; actual placed/destructible/static cover colliders and 3D raycasts from enemy and player camera/muzzle heights; physical drive to anchor with actual chassis; dynamic damage/removal and regeneration invalidation. Record raw NUnit XML, Editor/Player log, native build SHA, video/screen capture and source geometry hash for PASS/FAIL. If not run, report NOT_RUN rather than passing G1/G2. Fail closed if an anchor is no longer sheltered in actual 3D LOS or is untraversable.

## Reproduce and rollback

```shell
python -m unittest discover -s Research/B01-21/P17/COVER001 -p 'test_cover_field.py' -v
python Research/B01-21/P17/COVER001/cover_field.py --check
```

No integration: rollback = discard COVER001 proposed diff or ignore its JSON completely; existing P16/P17 map content and canonical runtime remain untouched. If Codex later integrates: revert **Codex's explicit adapter/consumer commit only**; leave original map assets and save files intact. Do not delete source evidence or overwrite other owners' packages.

**STATE = AWAITING_INDEPENDENT_QA / CODEX_ACK_PENDING / NATIVE_NOT_RUN / NOT_IN_GAME**.
