# B01-25-MAPDRAFT-IMPORT-001 / R1 — verify TWO original arenas through the REAL Codex C# loader

**Scope:** one new runnable acceptance harness for existing original game content, NOT another map, generator, backup utility, or speculative UX prototype. **Owner:** B01-25 LEVEL & CHALLENGE DESIGN. **Receiver:** B01-40 scope/artifacts, B01-60 independently re-run, B01-00 controls Codex queue #79.

## What physically executes

The test compiles **unchanged actual original C# `Assets/MapEditor/MapDraft.cs`**, fetched directly from immutable Codex #310 commit `be2c4b6ac63e04eff5b046e50641ec8d1da4837f` (Git blob `0331a54a1545ecd1ea18b616ef13fed113474b8c`). The same blob is present on current inspected Codex #382 and #383. The source is NOT copied into the committed producer package and is **never edited**.

Input A = original complete [CLEARANCELADDER-001 Draft #359](https://github.com/C8LyNn/RoboRougelike/pull/359) at immutable commit `ad52a604ffa4f143d98e607049d6f79e0c7e580f`, original `arena.json` blob `53eb696950121465befea2855eda4d7268750016`, 415 items.

Input B = original complete [SLALOM-001 Draft #346](https://github.com/C8LyNn/RoboRougelike/pull/346) at immutable commit `f565e250659692541d672cdab108c4cfb8cfdd31`, original `arena.json` blob `013f5fe2058e76233f580a439e8402c0fe09f124`, 360 items.

**Original code paths exercised:** `MapDraft.TryLoad`, `TestError`, `Footprint`, `ToJson`, `Undo`, `Redo`; strict version, enum, coordinate, scale, rotation, wall overlaps, duplicate markers, ground cap and missing underfoot floor. Original MapItem `Platform` vertical scale preserved in v2; original v1 semantics downgrade scaleY to 1, so **do not stage these maps as v1**.

**Critical boundary:** real Codex `MapDraft.cs` compiles and runs under .NET 8 using *minimal external substitutes* for `UnityEngine.Vector2Int` and `UnityEngine.JsonUtility` (`System.Text.Json` with public-fields support). This is **actual original application C# parser/validator code**, but **NOT Unity JsonUtility itself**, not the real MapWorld GameObject/Collider generator, and not Unity/PhysX/Player. Both supplied v2 JSON files contain every field needed to reduce serialization-surrogate discrepancies; native import must still be checked by independent authorized Unity QA. Do not call this native Unity G1/G2.

## Reproduce: immutable-source loader test

CI workflow [`.github/workflows/b01-25-w03-mapdraft-import.yml`](https://github.com/C8LyNn/RoboRougelike/blob/research/b01-25-p17w03-mapdraft-import-proof/.github/workflows/b01-25-w03-mapdraft-import.yml) performs all steps on `windows-latest` and `ubuntu-latest` with dotnet 8. From a clone at the Draft PR HEAD, use:

```bash
export P=Research/B01-25/P17/MAPDRAFT-IMPORT-001
mkdir -p "$P/.source"
git fetch --no-tags origin be2c4b6ac63e04eff5b046e50641ec8d1da4837f ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
git show be2c4b6ac63e04eff5b046e50641ec8d1da4837f:Assets/MapEditor/MapDraft.cs > "$P/.source/MapDraft.cs"
git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.source/clearance.json"
git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.source/slalom.json"
git hash-object "$P/.source/MapDraft.cs"     # must equal 0331a54a1545ecd1ea18b616ef13fed113474b8c
git hash-object "$P/.source/clearance.json"   # must equal 53eb696950121465befea2855eda4d7268750016
git hash-object "$P/.source/slalom.json"      # must equal 013f5fe2058e76233f580a439e8402c0fe09f124
dotnet run --project "$P/MapDraftImportProof.csproj" --configuration Release -- "$P/.source/clearance.json" "$P/.source/slalom.json" "$P/.source/receipt.json"
```

Expected: exact blob gate PASS before compiling; **23 actual C# checks PASS** including **12 negative or negative-semantics tests**. Reports `receipt.json` and console log are physically produced on each OS, uploaded by workflow with `include-hidden-files: true`; missing files fail the upload step (`if-no-files-found: error`). Tests log `UNITY_JSONUTILITY=STUB NOT_RUN; PHYSX NOT_RUN`.

## Codex / independent QA handoff, installation, rollback

**No direct install into game.** This package is a read-only original-source acceptance gate for the producer map files, not an editor patch. It uses two already published JSONs by immutable blob SHA. Do not copy `UnityEngineStubs.cs` or `.source/MapDraft.cs` into Unity `Assets/**`; doing so would conflict with real Unity assemblies. Branch only adds this QA harness within `Research/B01-25/P17/MAPDRAFT-IMPORT-001/**` plus one uniquely scoped test workflow.

B01-40: verify exact source/payload SHA and report files/artifact; check no conflicts with current Codex integration. B01-60: independently re-run against pinned commits, then test BOTH `MapDraft.TryLoad` and `TestError` under genuine **Unity 6000.3.14f1** with real `JsonUtility`, inspect generated `MapWorld` colliders, light/heavy physical drive, eight perimeter probes for each arena, actual logs/XML/exit and no map overwrite. Record current actual Codex runtime SHA. **Do not claim independent QA or G1 solely from this producer CI.** B01-00: queue only after B01-40/60 accepted; Codex explicit ACK required. No merge/release authority.

**Rollback/cleanup:** Delete only `Research/B01-25/P17/MAPDRAFT-IMPORT-001/.source/` and transient `bin/obj` on the test checkout. No player map, game save or original map bytes are written. In a future authorized native disposable test profile, delete only the uniquely named temporary map files; do not delete user saves. The input producer commits are immutable and never changed here.

**Status:** original C# loader + 2 game content inputs SOURCE_TESTED, .NET 8 Windows+Ubuntu, physical receipt evidence. NATIVE_UNITY/REAL_JSONUTILITY/PHYSX/PLAYER/HUMAN NOT_RUN; B01-40/60 independent QA pending; Codex ACK pending; NOT integrated.
