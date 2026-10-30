# B01-61 — PASS 17 / WAVE 12 — #510 receiver migration barrier and rollback QA

**Selected/frozen by HQ #518:** B01-23 Secure Reattach V2 R2 [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) source SHA `d94400d95d13db7f27e1533df05c9292d5380431`. Exact physical INNER source ZIP from the actual original `package.py`: `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387` (35 entries/34 original files/14 .meta GUIDs/3 asmdefs). **#511 and #494 are alternatives, not installed**. This new QA-only scope does not patch original producer/previous frozen source, canonical game, Codex or the receiving workspace.

## Purpose — an independent migration boundary, not another ledger fuzz harness

Historical original W10 transaction RED [#502](https://github.com/C8LyNn/RoboRougelike/pull/502) is preserved. Exact current corrected #510 original C# with original W06 ledger was independently source tested in [#517](https://github.com/C8LyNn/RoboRougelike/pull/517), 167/167 original source assertions per OS, including all three public routes, Light/Heavy, lost ID, rollback and reentrancy. **This W12 package adds a different acceptance surface**: all serialized inactive components, foreign script references, duplicate Unity GUIDs, selected #510 raw source-byte identity, and fail-closed injection during physical original archive staging/rollback.

The `receiver_gate.py` imports B01-23's **real original immutable `package.py`**, creates a disposable detached **Git worktree at the exact selected #510 SHA without checkout**, calls the original producer `build()/verify()/stage()/rollback()`, and validates the independently known archive SHA. It never makes an archive from the QA branch HEAD or substitutes another version's source. It scans actual Unity serialized `.unity/.prefab/.asset/.controller/.overridecontroller/.playable/.anim` files (including inactive objects) for pinned legacy MonoScript GUIDs, checks external C# runtime type references to W07/W08/W09 (stripping comments), checks duplicate and legacy `.meta` GUIDs, and verifies all 35 selected #510 staged files by exact byte equality.

Twenty-one executable independent Python `unittest` cases exercise actual producer bytes and disposable receiver fixtures, including: original successful clean stage/rollback, disabled prefabs and inactive scenes referencing legacy W07/W08/W09 GUIDs, foreign legacy C# route, comment-only nonfalse-positive, unscannable non-UTF8, duplicate selected GUID, existing legacy `.meta`, changed source at the #511 competing path, missing selected source, modified/foreign rollback refusal preserving evidence, preexisting destination denial, simulated disk write failure at stages 1/2/16/35 with absence of orphan destination, missing receiver and symlinks (skip only if Windows filesystem disallows links).

**Acceptance distinction:** the disposable simulated receiver is **not** the actual Codex workspace; the scanner is read-only for `--receiver`, does not migrate canonical scenes, cannot authorize Codex ACK or Unity G1. CI `--release` deliberately returns exit code **3** despite passing source tests, because licensed native Unity, actual owner-approved receiver/branch snapshot and migration proof are missing. Passing CI means **source-receiver precheck PASS / release HOLD**, not full G0.

## Commands

```bash
python Tools/B01/B01-61/P17W12ReceiverMigrationQA/receiver_gate.py
python -m unittest discover -s Tools/B01/B01-61/P17W12ReceiverMigrationQA -p test_receiver_gate.py -v
# Optional read-only receiver investigation (requires actual authorized external receiver):
python Tools/B01/B01-61/P17W12ReceiverMigrationQA/receiver_gate.py --receiver /EXTERNAL/UnityProject
# This always intentionally fails closed until formal G0/G1:
python Tools/B01/B01-61/P17W12ReceiverMigrationQA/receiver_gate.py --release
```

**Gates & receivers:** B01-40 performs independent formal physical G0 on exact frozen #510 ZIP/SHA/14 GUID/3 asmdefs with actual external receiving-worktree conflict check, staging/rollback; B01-60 native Unity 6000.3.14f1 EditMode, PlayMode, PhysX, JSON+XML and Player logs G1 (NOT_RUN here); B01-62 independent ID-save persistence; B01-23 source owner confirms selected immutable #510; B01-00 alone manages #79 offer/ACK. G2 HOLD; do not install #511/#494 concurrently. No main/develop merge, no Codex ACK, no claim of runtime integration.

**Rollback:** close unmerged isolated B01-61 Draft branch (own `Tools/B01/B01-61/P17W12ReceiverMigrationQA/**` plus unique workflow); all test stage directories are disposable and original `package.py.rollback` verifies exact staged byte identity before removal, refuses foreign/changed content.
