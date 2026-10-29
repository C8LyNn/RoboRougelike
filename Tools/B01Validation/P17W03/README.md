# B01-40 — PASS17/WAVE03 — original game-content acceptance and Codex queue

**PACKAGE_ID:** `B01-40-P17W03-ENEMY-FORMATION-INGRESS-001`, B01-40-owned immutable original-source *receiver*. This is a working executable handoff, not a second spawner, editor, or gameplay patch.

## Highest priority #355 — 12 real authored R5 formation inputs

Producer [B01-24 Draft #355](https://github.com/C8LyNn/RoboRougelike/pull/355) exact original commit `c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a`; original `formations.json` Git blob `4c28b6289f23bcbd82c976bccb1567cbcb847371`. Runtime compatibility compared with Codex's exact gameplay commit `203a0805251ed28293888e43f2d49f0df63767c6` and report branch `f92c873bb1e397a598b133e107ae704e15f86768` (stack #310→#382→#383).

The receiver:
- Independently reads real immutable Git objects (not live checkout), checks all source manifest original file blob IDs, the producer's exact own diff scope and **all four actual Codex R5/R1 contract blobs** against both current commits.
- Checks all three arenas, each for `SpawnCount=1..4`, 12 total layouts and 30 actual authored XZ positions; actually executes original unmodified `formation_tool.test.mjs` **35 tests** and original `formation_tool.mjs --check` in an isolated detached Git worktree.
- Executes the **real original author exporter** for **all twelve distinct copy-ready `new Vector3[]` expressions** and **three author-generated 720px preview SVGs**, not synthetically reconstructed ones.
- Physically exports a deterministic inner `B01-40-W03-Codex-EnemyFormations.zip`, containing original `formations.json` unchanged, `binding/optional-spawn-arguments.json`, three original preview SVGs and cryptographically inspectable `intake-receipt.json`. External machine-readable `receiver-receipt.json` + `codex-queue.json` identify the exactly accepted/refused content. Dedicated CI tests the **real ZIP** and 18 adversarial cases on Windows + Ubuntu.

**Source executable from an authorized empty QA checkout** with Git, Python 3.11+, Node 22:

```bash
git fetch --no-tags --depth=1 origin c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a
git fetch --no-tags --depth=1 origin be2c4b6ac63e04eff5b046e50641ec8d1da4837f
git fetch --no-tags --depth=1 origin 203a0805251ed28293888e43f2d49f0df63767c6
git fetch --no-tags --depth=1 origin f92c873bb1e397a598b133e107ae704e15f86768
git fetch --no-tags --depth=1 origin 0d212a706c1e633b773d2e03b7b40c28bb63521b
python Tools/B01Validation/P17W03/receive_formations.py --repo . --out /tmp/b0140-w03-NEW
B0140_W03_OUTPUT=/tmp/b0140-w03-NEW python Tools/B01Validation/P17W03/test_receiver.py -v
```

Output must be a **NEW EXTERNAL directory**, not any tracked `Assets` location. Detached original-owner worktree is removed; the game remains unmodified. Both tests compare original SHA bytes and native author Node actions; **Unity process/PhysX NOT_RUN**.

## Second priority #333 — HOLD on newer real Codex R3 file

Producer [B01-22 Draft #333](https://github.com/C8LyNn/RoboRougelike/pull/333) exact `0d212a706c1e633b773d2e03b7b40c28bb63521b`, 10 inventory offer kits, has its own source-pin verifier and staging operator. Its original catalog pins R3 `8cc68faf89793c3a803e5807594c9faf0df10091`, but latest Codex gameplay `203a0805251ed28293888e43f2d49f0df63767c6` references R3 `1d0f378dc598e08b3d7c16975f419d84231511bb`. The R4 blob matches. The previous R3 `class` became `partial class`; its displayed garage-price switch appears unchanged, **but this receiver does NOT certify full code/pricing semantics**. Original B01-22 `verify_host_source.py` deliberately rejects byte drift and must not be bypassed by B01-40. State `HOLD_SOURCE_R3_BLOB_DRIFT`, not `G0 PASS`; require B01-22/HQ/Codex explicit compatibility approval or author-accepted new immutable version, before any source handoff. **No copy of #333's assets in #355 ZIP.**

## Codex handoff (requires B01-60 independent source QA and B01-00 scheduling)

1. B01-60 independently review immutable original manifest and rerun the real 35 original Node tests and 18 B01-40 physical ZIP/negative cases at **the SAME** source commits; do not call original producer CI independent QA.
2. B01-00 records `B01-40_SOURCE_G0_PASS / B01-60_QA_PENDING / CODEX_ACK_PENDING / UNITY_G1_NOT_RUN` and determines whether to OFFER **the frozen source ZIP** to [CODEX INBOX #79](https://github.com/C8LyNn/RoboRougelike/issues/79). Neither this receiver nor its #384 comment claims B01-00 already sent it. Source acceptance is not native gameplay acceptance.
3. Copy-ready Codex integration proposal: **after explicit acknowledgement**, in a disposable authorized game worktree use `binding/optional-spawn-arguments.json` to choose `formation_id` by `R5ArenaKind`, select **`definition.SpawnCount`**, then pass that row's *actual* `new Vector3[]` to the existing **last** parameter `R1EncounterManager.Spawn(..., spawnPositions)`. Do not change default R5 arena geometry, rewards, save/checkpoint, CHL, PARTSIZE or player physics. The original `spawnPositions=null` path must be preserved for fallback.
4. Codex should physically instantiate 1..4 real enemy prefabs in each of three arenas, test real collider/PhysX clearances vs obstacles and player, navigation, line of sight and friendly-fire, win/loss/save/resume, deterministic restarts; capture real Unity Editor/PlayMode XML, exit codes and Player evidence. These are **NOT_RUN** by this B01-40 receiver.
5. Rollback **only** any optional Codex-owned spawn-selection adapter to the existing null/default spawn path. This B01-40 ZIP is read-only and its output rollback is simply deleting the external ZIP+receipt. No changes to `main`, `develop` or canonical `Assets/**` by this PR.

**Gates:** B01-40 source G0 only after real current-sha CI; independent B01-60 QA `PENDING`; Unity G1 `NOT_RUN`; HQ G2 `HOLD`; Codex ACK `PENDING`. No direct CODEX_CLAIMED or INTEGRATED state without its owner.
