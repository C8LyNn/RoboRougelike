# B01-40 / P17 W30 — independently verify real #873 → #877 garage plus dynamic own-collider mounting

## What this actually adds

**New genuine native Unity Test Framework PlayMode regression**, not another implementation of the garage: `Assets/Tests/Editor/B0140W30DynamicGarageClearanceTests.cs` and one unique `.meta` GUID.

The test opens the **real game scene**, enters actual R1 standard-robot sandbox garage, removes/mounts the existing owned gun converted to Rotary Saw at the updated bounded #877 valid exterior attachment (0,1,4), captures the **actual assembled physical saw-disc center**, removes that exact owned gun again, and uses a **distinct owner-child kinematic `Rigidbody`** whose actual collider moves from outside to the blade/disc via `Rigidbody.MovePosition` and an actual `WaitForFixedUpdate`. Tests preview `OwnedPlacementError` and real model `GarageMount` while physically blocked, asserting **Scrap, inventory count and original module ID remain unchanged**; moves the blocker out and verifies recovery and legal mount with exactly one original part ID. This catches a different dynamic risk from Codex #877's static center-saw reinstallation test.

**Source base is the actual selected, already integrated in source, but unmerged gameplay candidate**:
- #824 W28 main gameplay `ed8cf4bcc131688b9a93968a89683d2496586683`
- #873 current W29 cumulative R5/EMP/5-color garage/JumpPad `d537b7310979b91df27a08b14d0f1c74d0889089`; the original source-G0 source-only Actions are [push #41629343221](https://github.com/C8LyNn/RoboRougelike/actions/runs/41629343221) and [PR #41629349690](https://github.com/C8LyNn/RoboRougelike/actions/runs/41629349690) each 3/3 source PASS. Does not imply native Unity.
- #877 latest Codex same gameplay `9634b1e5b4576aca4304d1d5b4c3d01446737650`, EXACT 2 modified existing `Assets/R3/R3BuildValidation.cs`, `Assets/Tests/Editor/GarageEditFreedomTests.cs` plus new original `Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md`. The actual same original `R3VehicleModel.installationGate` powers preview, `Buy` and `Mount`; real near-barrel physical sphere sweep and RotarySaw contact-disc check are in the source. **This is NOT an authoritative full swept simulation for orange articulated mechanisms**; purple near-barrel is hard safety, long line informational.

This B01-40 branch is created **directly from immutable #877**. Exactly five B01-40-only ADD paths: new Unity test and GUID, own executable source verifier, README, own dual-OS CI. No original R3/EMP/Saw/Save/economy/arena/UI/producer code modified. In particular **do not install #861 Saw**, #826 raw Save/old #797 writer, another validator, or duplicate #810 pad. The original #873 paths must match original byte-for-byte; this independent gate checks all 37.

## Actually executable SOURCE-G0

```bash
python Tools/B01/B01-40/P17W30MovingGarageG0/verify.py --receipt evidence/b0140-w30-877-dynamic-garage.json
```

It checks exact immutable SHA ancestry, original 37-path #873 + 3-path #877 scope and byte identities, only five new QA paths, original canonical R3 preview/commit checks before wallet mutation, radial EMP not converted, no duplicate JumpPad logic, existing five colored visual classes, unique actual complete Unity `.meta` GUID tree via real Git object `cat-file --batch`, new authored native test source invariants, and a **physical disposable staging dirty-code mutation fail-closed witness with foreign player Save bytes and empty directory preservation**. Windows2025 and Ubuntu24.04 Actions each execute it, and the third job compares inner JSON evidence byte-for-byte. The test runner does **not claim to execute Unity native physics**.

## Actual native G1 / Windows Player acceptance (required, NOT_RUN here)

B01-60/Codex operator must use **licensed Unity 6000.3.14f1** on an **isolated exact PR head**, without modifying Codex's active worktree or player's user data:
1. Unity compile/Editor.log; targeted `GarageEditFreedomTests.RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation` plus new `B0140W30DynamicGarageClearanceTests.RealGarage_ReevaluatesMovingOwnedCollider_RejectsWithoutSpending_ThenMounts`; explicitly identify original fixture against real body/actual disc geometry (if new test RED, log exact physical collider and fail gate, do not explain away).
2. Full EditMode, PlayMode, R3 large parts, all five spatial colors visually verified, physically legal/illegal mount, rotated barrel/motor sweep, Scrap/no duplicates, EMP after garage/JumpPad and enemy physical tests. At least two encounters with rewards in the actual live gameplay.
3. Windows x64 **game build from this exact code**: package ZIP, hash SHA-256, fresh extraction & actual process start, Editor.log/NUnit XML/Player.log, actual exit code and native test outcomes. Distinguish API-controlled from manual playtest. Do not rename an old #824 ZIP as W30.
4. **Keep Saw #861 HOLD** until actual native original R3/AI scenarios + PlayMode saturation and Windows Player; no source-G0 substitute. Keep #826 one-writer Save HOLD until approved typed Unity binder and actual cold process G1, even though independent #862 (real OS source) passed. #834 policy issue remains open; the loaner dead-end must not be bypassed by a fake victory or second loaner.
5. If selected game HEAD changes from #873 or #877, **HOLD and rebase only the own QA fixture with new source identity retest**, not a silent drag of other gameplay. No merge to main/develop, no release, no user PC access.

## Gate states

`IMPLEMENTED=YES_NEW_NATIVE_TEST_SOURCE`; `TESTED=CI_SOURCE_ONLY_PENDING`; `SOURCE_QA_ACCEPTED=WAIT_EXACT_HEAD`; `UNITY_G1_NEW_FIXTURE=NOT_RUN`; `WINDOWS_PLAYER_NEW_HEAD=NOT_BUILT`; `INTEGRATED_IN_CANONICAL_GAME=NO`; `CODEX_ACK=NONE`; `RELEASED=NO`. Rollback is closing only this B01-40 Draft branch, leaving exact original #873/#877 gameplay and player saves untouched.


## W30 current selected #877 update (R2 receiver)

Source HEAD advanced from old #877 `20273c3` to **`9634b1e5b4576aca4304d1d5b4c3d01446737650`**, adding bounded preallocated 256-collider PhysX queries, explicit fail-closed overflow reasons, a real 260-collider native PlayMode witness, source guard and CI. **All SIX original #877 delta paths are preserved byte-exact in this R2 receiver.** Original owner's 260-collider test checks saturation; our new native test covers the distinct **one physically moving owner Rigidbody** across valid→blocked→valid placement without charging Scrap. The old R1 QA branch is historical (successful source G0 only for old #877); no PR/merge has been made from old R1. Require fresh G0 for this SHA and fresh native G1. No gameplay producer edits by B01-40.


## W31 latest canonical garage + sole Save producer (independent update)

This existing B01-40 PR is updated **in place** rather than opening a duplicate. New exact source base **#877@a0de194fd382830fab4b7d013d204b0697b25078**, which has absorbed the actual original #887 reverse armor-vs-mounted-saw gate, #883 restoration-path gap fix and native regression test sources, without a competing garage validator. All **7 original current #877 changed paths** are byte-identical. Existing dynamic owner Rigidbody native regression remains in this branch. New checker performs deliberate in-memory guard-removal negative mutations for reverse + restoration checks.

New own `verify_save885.py` on a disposable **separate Git worktree**, with original #885@78c1d54d24a76c931ea80965a50eca03fae4156a and pinned #780/#855, independently runs the original one-writer actual OS .NET C#9 `W30AdmissionQA` (4 real disk tests) on **Windows2025 and Ubuntu24.04**. This is only a selected source/OS compatibility acceptance, not a binary game Save import. **Do not install original #826 + #885 as two stores**. #885 is the successor of the single V3 writer and must be integrated only once with real live R3 world/physics/economy binder, after separate G1. Cross-OS compares exact deterministic inner receipts for the current garage and original save producer.

Gates: `SOURCE_G0=CI_DEPENDENT`; `SAVE885_ORIGINAL_REAL_OS_QA=CI_DEPENDENT`; `UNITY_EDITOR_G1=NOT_RUN`; `W31_WINDOWS_GAME_ZIP=NOT_BUILT`; `ECON_LOANER_OWNER_834=OPEN`; `NO_MERGE/RELEASE`.


## W31 current live #877 source preflight blocker fixed only in B01-40 receiver

At exactly #877@cfae02d274bc8b830709ab41f81a296c6aa6307a CODEX added a conditional true native Unity 6000.3.14f1 CI pipeline. Its first authentic [Actions #41690273393](https://github.com/C8LyNn/RoboRougelike/actions/runs/41690273393) failed BEFORE license detection or native test, due to `Tools/CI/validate_repository.py`: exactly two original MAP-JUMPPAD-001 package files under Unity Assets have **no tracked .meta**: `README.md`, `manifest.json`. These are byte-correct originally imported #810 files and should NOT be rewritten/deleted or swept out of Assets. This isolated B01-40 receiver adds ONLY their corresponding Unity TextScriptImporter .meta with new unique GUIDs, preserves all 37 selected original #873 paths (these .meta are distinct new files), all 10 current original #877 paths, no new map module or gameplay implementations. The existing W31 native workflow is NOT copied and is NOT declared green. Our source CI runs the exact previously failing `python Tools/CI/validate_repository.py` plus W30 garage source checker and `verify_artifacts.py --selftest`. If source preflight becomes green, Codex should select exactly these 2 .meta files into the real #877 game branch, reexecute its original native workflow and record whether real license credentials exist. This is a genuine game-repository build gate patch, not a fake Unity test.


## W31 reconciliation: owner #877 fixed missing meta itself, avoid duplication

Original Codex #877 advanced to exact `bcbd0d034c852ad45b1416d3b4ed679f2996f7fe` and installed two correct **DefaultImporter** .meta for JumpPad `README.md` and `manifest.json` using the owner's canonical GUIDs. This QA PR discards both our earlier W31 extra meta, and now contains exactly SIX ADD-ONLY QA paths only: own Unity physical test/.meta, original source and #885 original real OS G0 verifiers, README and CI. Original selected #877 now has TWELVE changed paths vs #873 and ALL original game/.meta bytes remain unmodified. The earlier B01-40 isolated meta fix was a redundant unmerged candidate and MUST NOT be installed into Unity; use Codex's canonical two GUIDs. This PR tests those new meta in exact owner source and independently executes the original W31 full-repository preflight on our branch. The Codex original W31 native workflow remains sole supported build pipeline; no duplicate CI/native game systems created.
