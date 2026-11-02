# B01-40 W28 — Three independent gameplay package source-G0 (no new gameplay implementation)

**Goal:** verify real existing owner-owned Unity runtime code before one-by-one Codex intake. No fake compiled game or generated prefab.

| Producer | Exact SHA | Source root | Native owner scene/prefab |
|---|---|---|---|
| #804 ENEMY-CHARGE-001 | `4dcec50d1a9a245393ea9f63d1f6925eb6566465` | `Assets/B01Packages/EnemyCharge001` | Editor action exists; physical prefab NOT generated/tested |
| #810 MAP-JUMPPAD-001 | `05db0ddf0a48da592d924e68b7658eea6bba764e` | `Assets/B01Packages/MapJumpPad001` | Editor action exists; physical prefab NOT generated/tested |
| #814 WEAPON-FEEDBACK-001 | `f92341c7788c3581b965d6baa47e6943325348fc` | `Assets/B01Packages/WeaponFeedback001` | Editor action exists; physical prefab NOT generated/tested |

Baseline `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`, published receiver/weapon baseline `#824@e03d36f2b86f8c73a067e574b67237a8921f622a` read-only. The last known #824 gameplay source is NOT a guarantee about unpublished Codex W28 worktree.

## Actual verification performed by this package

`intake.py` reads original bytes using exact Git commit: verifies 21/19/19 owner-only add paths, producer manifest list, all original source bytes + blob hashes + SHA256, Unity `.meta` GUID uniqueness including each top-level folder, 3 unique asmdefs per producer (runtime, editor, tests), collision with original published #824 files/GUIDs/asmdefs, namespaced source-only physical ZIP and byte-exact safe scratch stage/rollback. Tests occupied/mutated file and alien EMPTY directory denials without altering other data. On Windows2025 + Ubuntu24.04, run independent twice and compare downloaded actual source-only inner ZIP and JSON exactly (full byte equality).

All three producers contain REAL Unity C# behaviors (Rigidbody charge, trigger VelocityChange jump, visual-only feedback). This G0 does NOT validate their native MonoBehaviour lifecycle, actual PhysX contacts, runtime-generated prefab/scene, or in-game integration. Tests in each producer are **authored not executed**, so `G1=NOT_RUN`.

## Safe Codex intake instructions

1. Fetch only manifest-selected original commit SHAs — never clone/install complete producer branches over gameplay project. Download receipt from GitHub Actions and compare INNER ZIP SHA256, per-file SHA256 and GUID list.
2. Do NOT unzip to current Codex or player project. Stage into newly created disposable Unity project/scratch first. Verify actual target SHA and paths with Codex scope ACK. Disallow any occupied path/symlink, foreign file or empty dir, unexpected .meta GUID and asmdef name.
3. After B01-00 authorizes, select ONE producer. **Recommended first G1 candidate: #810 JumpPad** because it has a standalone runtime trigger/scene creator; it is not yet proven gameplay-integrated. Do not insert into generator until true Unity prefab + a scene works.
4. B01-60: use activated native Unity 6000.3.14f1 on Windows, run the ORIGINAL three `[UnityTest]` cases and actual Editor scene generator for the selected producer, capture Editor.log, NUnit XML, actual prefab/scene hashes, full exit code. Test disabled/wrong-layer/kinematic/no duplicate wheel launch/cooldown and real contacts for #810. For #804 test actual Rigidbody obstruction/team collision/stop and damage. For #814 verify only visuals change and authoritative ammo/cooldown/health do not.
5. Codex integrates into original game only after G0, owner selection and actual target-only Unity compatibility. Verify 0.55m map grid and scene references, actual gameplay binding, Windows Player 2-fight smoke and errors before claiming game complete.
6. Never mix branch HEADs or substitute #787/#797 Save fix here. No Source PASS⇒Unity PASS. No protected merge, release, live player Save, Codex worktree or other owners' files touched.

**Risk:** Source transport LOW/MEDIUM; gameplay collision, physics and player experience HIGH until native G1. **Rollback:** delete only exact created files in newly owned, empty disposable receiver after verifying unchanged bytes and no foreign files/empty dirs/symlinks; never original producer rollback against live game.

**Statuses:** SOURCE_TRANSPORT_G0=PENDING_REAL_CI | NATIVE_G1=NOT_RUN | LIVE_GAME=NOT_INTEGRATED | G2=HOLD | NEW_CODEX_ACK=NONE.
