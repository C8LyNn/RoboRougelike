# MAPGEN-001 R3.3 — W11 installation / rollback PREPARATION ONLY

**STATUS: BLOCKED — G2_HANDOFF=HOLD; NOT SENT; this file is NOT permission to integrate.**
**Frozen exact producer SOURCE SHA:** `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`.
**Complete stacked source:** Draft [PR #99](https://github.com/C8LyNn/RoboRougelike/pull/99) + [#103](https://github.com/C8LyNn/RoboRougelike/pull/103) + [#211](https://github.com/C8LyNn/RoboRougelike/pull/211) + [#225](https://github.com/C8LyNn/RoboRougelike/pull/225). Never copy only the diff from #225. **Recipient:** CODEX, after B01-00/HQ approval only (historical GPT OSS is superseded). **Published comparison-only host:** OBJ [PR #210](https://github.com/C8LyNn/RoboRougelike/pull/210) `4c9236a4cdc27814ea2e7679872ec4a524150cc8`, with PHY [#125](https://github.com/C8LyNn/RoboRougelike/pull/125). This published SHA is **not** live Codex target ACK.

## Independently verified package bytes

GitHub Actions [run 40187335381](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187335381), wrapper [artifact 12596980172](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187335381/artifacts/12596980172):
- Actual outer GitHub artifact ZIP: **64,544 bytes**, SHA256 `4cad43d9381d92541b287cbd33e54a599da649db971cf4343b4c3f9f5078b918`.
- Actual **inner** `MAPGEN001_R3_3_FULL_SOURCE_AND_QA.zip`: **71,667 bytes**, **56 unique entries**, SHA256 `384ef6909c1ebcb361c55a6b23b22eb105b2aaaed0270e229cbad37a250155a0`. Both values independently recalculated by B01-40 from downloaded bytes.
- `Docs/B01/MAPGEN001/manifest.json`: 50 source/docs + 5 QA tool SHA256/length entries, **55/55 match independently hashed extracted files**; the 56th entry is manifest itself (out-of-band self hash). Git blob SHA-1/byte sizes also match exact immutable producer tree; no duplicate ZIP names or unsafe traversal names.
- All **20 Unity .meta** have unique GUIDs and required source/scene/asmdef pairs. Three asmdefs: runtime `B01.MAPGEN001.Runtime`, EditMode and PlayMode tests reference runtime via assembly name and `optionalUnityReferences: ["TestAssemblies"]`, no direct duplicate TestRunner references. Demo scene MonoScript GUID resolves to `MapGenDemo.cs.meta`.
- The full package imports `Assets/B01Packages.meta` AND owned `Assets/B01Packages/MAPGEN001.meta`. The shared parent `Assets/B01Packages.meta` has **GUID `b01a0001000000000000000000000001`**. CAMERA-001 #214 separately defines the SAME path with GUID `b01c0000000000000000000000000001`. **Never overwrite the target's existing shared parent .meta.** If shared parent already exists, keep its GUID; import only package-owned subtree and meta, and update documentation of the resolved shared parent. This is a concrete cross-package pathname/GUID conflict, not a reason to rewrite the 20 MAPGEN GUIDs.

## W11 actual native FAILED evidence — hard gate

Separate CODEX native [QA PR #232](https://github.com/C8LyNn/RoboRougelike/pull/232), exact QA HEAD `b80faa121e4022d772caa3b93d8b2a482d9a53af`, on this exact R3.3 SHA and published OBJ210: Unity 6000.3.14f1 compilation PASS exit0; EditMode **24/24 PASS** exit0; PlayMode **6 PASS / 12 FAIL** (18 cases) exit2; all **12 independently enumerated primitive AddForce cube traversals FAIL** near waypoint0 (roughly 0.56m displacement). **Real R6/OBJ-mounted-wheel R3.3 trials NOT_RUN** in that QA phase. A separate minimal standalone URP Player builds but generation FAILS because `Shader.Find("Universal Render Pipeline/Lit")` and Simple Lit return null; Editor material test passed. The original R3.2 12+7 successful real-wheel runs are historical, not R3.3 native G1.

**Shader-stripping/player retention risk is confirmed by actual Player failure, although the precise underlying build stripping cause remains unproven.** The producer must provide a reliably retained **serialized URP material/shader dependency** included in the build (e.g. an owned material asset referenced by built scene/prefab/Resources and preserved .meta; or a documented Always Included Shader / ShaderVariantCollection strategy) rather than trusting runtime Shader.Find alone. B01-60 must independently test a minimal clean Player AND the intended real host Player, verify shader availability, visible non-magenta rendered blocks, regeneration and material cleanup. Do not silently change project-wide graphics settings or add unintended assets to Codex's shared game.

## Phase 0 — safe disposable proof (ALLOWED after operator initiates, no live worktree)

1. **Read-only ACK:** Codex must declare actual current target branch/HEAD/worktree dirty/unpushed state, owned/reserved paths, existing B01Packages root GUID, active camera/garage/OBJ/PHY changes and dependencies. Without ACK, set `G0_TARGET=PENDING`.
2. Obtain complete source from exact `7334f3b...` tree OR verified inner ZIP; rehash SHA256 and file/size pairs; no partial PR patch. Do not treat tools-only .NET `UnitySurrogate.cs` as a Unity plugin.
3. Create a **separate disposable, clean** Unity 6000.3.14f1 URP worktree from an authorized published QA baseline, with no edits to Codex live checkout. Inspect destination .meta before writing; if a different existing GUID is on a parent folder, retain target GUID, log mapping, never overwrite. No `Assets/R3/**`, `Assets/R6/**`, `Assets/Pack/**`, `ProjectSettings/**`, `Packages/**`, `Assets/scene 1.unity` changes.
4. Run real import/compile and capture raw `Editor.log`, command, exit. Then EditMode and PlayMode with NUnit XML and test counts; do not gloss over 12 FAIL cube cases. Test every case separately (Arena/Course × seeds7/31/83 × Light/Heavy); verify 3×generate/regenerate/clear and intentionally impossible geometry.
5. Independently test URP standalone Player **with referenced retained shader/material**; require BOTH visible geometry and no shader missing exception. Check 720p/1080p and inspect material lifecycle.
6. Separately test real **R6WheelContacts** Light/Heavy from PHY125+OBJ210: 12 matrix routes, plus historical Heavy normal2/7/31 and hard0/2/7/19, with wheel contacts, displacement, stalls, seams/ramp, completion and timestamps. Mark API-controlled vs human.
7. Archive real XML/process exit/log/screens and B01-60 independent judgement at the EXACT repaired future SHA. Native/QA FAIL => stop, producer B01-20 fixes on a new isolated R3.4 branch with fresh hashes and independent retests.

## Rollback

For a **disposable QA worktree**, close Unity, copy evidence to a separate safe directory, remove only MAPGEN-owned temporary copied subtree, preserve any preexisting `Assets/B01Packages.meta` and all preexisting package files, ensure Git changes list matches known import paths, then remove the clean disposable worktree through `git worktree remove`. Do NOT run blanket `git reset --hard` or `git clean -fd` on Codex worktrees. For **future approved Codex-owned isolated integration only**, revert recorded MAPGEN adapter commits, clear generated roots and restore original level/run selection, preserve host owned parent GUID, rerun baseline level, PHY and Windows smoke. Never revert unrelated host changes.

## Gate requirements

`G0_SOURCE=PASS_STATIC` (archive + source + GUID), `G0_TARGET=PENDING_LIVE_ACK`, `G1_NATIVE=FAIL` (Codex R3.3 QA), `G1_INDEPENDENT_QA=PENDING_B01_60`, `G2_HANDOFF=HOLD`. G2 requires new repaired SHA with native fixes verified, fully replayed independent B01-60 evidence, exact live target scope/GUID ACK and explicit B01-00/HQ GO. **NO CODEX integration prompt sent; NO MERGE/DEPLOY/RELEASE.**
