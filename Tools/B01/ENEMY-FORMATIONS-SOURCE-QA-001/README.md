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

## PASS17 W04 — Physical B01-40 Codex ZIP reception (existing PR #393, no new QA PR)

The source-only original C# R5Arena tests from W03 remain part of the same CI. The **new independent physical receiver** `verify_handoff_zip.py` reads B01-40 [original Draft #398](https://github.com/C8LyNn/RoboRougelike/pull/398) **at frozen `f4165fd2bac86741326253800c6160d8ca8bea02`**, materializes its exact original Python receiver in a disposable directory and actually executes its code to generate an immutable six-member Codex ZIP. The original generator runs B01-24's real unchanged 35 Node tests and 12 actual `new Vector3[]` exports. B01-60 *separately* decodes the ZIP and verifies bytes, SHA/CRC, entry names/metadata, original producer JSON byte identity and all 12 exported calls/30 coordinates, matching the exact B01-40 published ZIP SHA256 `ef25e8759a02a9b5f0c22d19198e6fcbf8e6c9d13ee7963c7d1fa2f091d82d8a`.

Eight new adversarial physical member/receipt corruption checks must each fail for the **expected independent reason**, including a malicious matching-receipt hash on falsified binding coordinates, forged Unity G1 and forged Codex ACK. These are independent of the producer's 18 B01-40 receiver tests; both tools are read-only for canonical game content.

On a clean checkout with original package and target commits reachable, Python3.11, Git, Node22, .NET8:

```sh
python Tools/B01/ENEMY-FORMATIONS-SOURCE-QA-001/verify_inputs.py
dotnet run --project Tools/B01/ENEMY-FORMATIONS-SOURCE-QA-001/Harness.csproj --configuration Release -- Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json
python Tools/B01/ENEMY-FORMATIONS-SOURCE-QA-001/verify_handoff_zip.py --output qa-results/physical-zip-independent-qa.json
```

The dedicated CI fetches the exact B01-40 commit, producer, Codex gameplay/report and frozen equipment package and runs all three tests on Windows2025 and Ubuntu24.04. Receipt output is downloadable per OS.

**QA scope and acceptance:** `INDEPENDENT_PHYSICAL_SOURCE_G0_PASS` after successful dual-OS run means the original content ZIP is byte-correct and correctly supplied for Codex review. It does **not** mean `READY_FOR_CODEX`, which B01-00 alone can authorize, nor `SENT`, `ACK`, `INTEGRATED` or a native Unity G1. Native enemy collider/PhysX, navigation, AI, player and Unity runtime **remain NOT_RUN**. The 2.5m original planning circle is not a measured enemy prefab collider. No merge. Rollback deletes only B01-60 files and workflow; the original B01-24 #355 and B01-40 #398 sources are untouched.
