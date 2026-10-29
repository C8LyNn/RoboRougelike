# B01-60 — ENEMY-FORMATIONS-SOURCE-QA-001 (PASS17 WAVE03)

Independent executable source acceptance for B01-24 [Draft PR #355](https://github.com/C8LyNn/RoboRougelike/pull/355) at immutable `c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a`.

Unlike the producer's hand-transcribed geometry in a Node tool, this receiver compiles and **executes unchanged original Assets/R5/R5Arena.cs**, calls original `R5Arena.Build` for each of the 3 arenas, captures the real C# `GameObject.CreatePrimitive` Box positions/sizes through disposable Unity API shims, and checks 12 original spawn layouts and 30 authored positions. Eight independent mutations must produce their specific expected rejection codes. The current corrected Codex #383 gameplay commit is pinned to `203a0805251ed28293888e43f2d49f0df63767c6`; source blobs are independently verified against both producer and Codex.

## Reproduce (separate QA branch, .NET SDK 8, Git and Python 3.11)

```sh
git fetch --no-tags origin 203a0805251ed28293888e43f2d49f0df63767c6
python Tools/B01/ENEMY-FORMATIONS-SOURCE-QA-001/verify_inputs.py
dotnet run --project Tools/B01/ENEMY-FORMATIONS-SOURCE-QA-001/Harness.csproj --configuration Release -- Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json
```

Dedicated GitHub Actions uses Ubuntu 24.04 and Windows 2025, preserving JSON stdout for provenance and actual C# results. The branch is stacked solely on producer #355 and changes only B01-60-owned QA files/workflow. No `Assets/**`, B01-24, Codex, CHL or PARTSIZE mutation.

## Acceptance and limitations

- Positive source acceptance: 3 original C# arena constructions; 12 source formations, 30 locations checked against actual original static box collision footprints, real original arena border, original player spawn, and minimum 7.5m enemy separation. Explicit 2.5m radial clearance is a *planning assumption*, not measured prefab geometry.
- Negative controls: original box overlap, player spawn collision, duplicate enemies, incorrect count, unsupported arena, outside-world point, weakened margin and duplicate layout ID must each fail with the expected reason.
- **Native Unity 6000.3.14f1 / PhysX / actual enemy colliders / AI / combat / Player / GUI / human playtesting = NOT_RUN.** Disposable managed shims are not evidence of native Unity. No game integration or Codex ACK is asserted.
- Reception after successful executable results: B01-40 verifies manifest/scope/rollback, B01-60 attaches source-acceptance receipt to #355, B01-00 decides any order to #79. Codex alone may wire the optional `Vector3[] spawnPositions` argument after proper game-native testing.

**Rollback:** delete only this QA directory and `.github/workflows/b01-60-p17w03-enemy-formations-receiver.yml`. Do not merge or alter frozen original producer data/game files.
