# B01-40 / PASS17 W04 — exact R3 compatibility receiver for EQUIPMENT-KITS-001

**Package:** `B01-40-P17W04-KIT-R3-COMPAT-RECEIVER-001`; B01-40-owned **executable, source-only QA and read-only compatibility proof**, not an economy/gameplay source adapter, not a new owner version of #333.

## The precise blocker and what is proven

Immutable B01-22 producer [#333](https://github.com/C8LyNn/RoboRougelike/pull/333) v1.0.2 source `0d212a706c1e633b773d2e03b7b40c28bb63521b` pins original Codex `Assets/R3/R3VehicleModel.cs` Git blob `8cc68faf89793c3a803e5807594c9faf0df10091` at `aa7a917f177a9b1f07203868fb7d1d871eb3ee46`. Latest corrected Codex gameplay [#383](https://github.com/C8LyNn/RoboRougelike/pull/383) at `203a0805251ed28293888e43f2d49f0df63767c6` contains blob `1d0f378dc598e08b3d7c16975f419d84231511bb` in R3, also unchanged in report HEAD `f92c873bb1e397a598b133e107ae704e15f86768`.

**Exact original source comparison:** entire original R3 Git blob is byte-identical to the newer R3 Git blob after precisely one **8-byte insertion**: `public sealed class R3VehicleModel` → `public sealed partial class R3VehicleModel`. The full old and new R4 `R4WeaponDefinition.cs` bytes and Git blobs stay identical `502f6d7f70d4e8078260de632ab181f244a4d730`. This is a narrowly demonstrated source-level price/enum equivalence; it does not prove native garage buy, save/load or transaction integration.

The **unmodified producer gate** `verify_host_source.py --current-ref 203a...` is deliberately executed and **MUST fail with exit 2 / `LATEST_CODEX_BLOB_DRIFT`**. This receiver **does not modify, suppress, or bypass that gate**: the B01-22 v1.0.2 SHA remains frozen and its G0 release remains **HOLD**, pending author re-pin/version and B01-00/Codex agreement. An independent separate check calls the original producer's actual `check(catalog,currentR3,currentR4)`, confirms all 10 original kit-price/enum rows, and explicitly injects wrong Booster price for rejection.

## Actual work performed by this B01-40 code

On both Windows and Ubuntu CI, `receive_kit_compat.py` creates a temporary detached **original B01-22 Git worktree** at the immutable SHA. It independently verifies all **13 original v1.0.2 payload SHA256 and byte sizes**, original owner-only Git changed paths, actual original R3/R4 blob pins and only permitted 8-byte R3 change. It then executes **unmodified original author** Node tests (**26/26**), original Python stage/rollback test suite (**18/18**), all **7 actual original malformed host/data negative cases** via producer `--selftest` on pinned ref, and the current-ref **RED** provenance gate. It physically stages and reads back **10 curated offers** into three output JSON files using original author tool, then exercises its original rollback and verifies deletion of only those staged files.

A deterministic **four-member QA-only ZIP** is generated: original `catalog.json`, original producer-generated `offers.json`, original author receipt, and B01-40 compatibility receipt. Exported separately is the real hash-locked `compatibility-receipt.json` referencing the exact inner ZIP SHA256. **Not intended for Codex import until author/HQ explicitly accept new provenance**.

Execute from own authorized isolated repo checkout (Git, Python 3.11+, Node 22; output must not exist and must be outside repo):

```bash
git fetch --no-tags --depth=1 origin 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1
git fetch --no-tags --depth=1 origin 0d212a706c1e633b773d2e03b7b40c28bb63521b
git fetch --no-tags --depth=1 origin aa7a917f177a9b1f07203868fb7d1d871eb3ee46
git fetch --no-tags --depth=1 origin 203a0805251ed28293888e43f2d49f0df63767c6
git fetch --no-tags --depth=1 origin f92c873bb1e397a598b133e107ae704e15f86768
python Tools/B01Validation/P17W04/receive_kit_compat.py --repo . --out /tmp/NEW-b0140-kit-proof
B0140_W04_OUTPUT=/tmp/NEW-b0140-kit-proof python Tools/B01Validation/P17W04/test_kit_receiver.py -v
```

Only B01-22 (original content owner) may publish an updated **new immutable v1.0.3+** manifest pin for current Codex Git blobs after authorization; Codex exclusively implements any canonical R3 garage stock/transactions and required Unity native Edit/Play/Player tests. **B01-60** must independently check the current exact R3/R4, test negative and false-compatibility cases, and approve *only* source semantic equivalence. **B01-00** owns #79 CODEX INBOX queue; nobody claims ACK by writing a report.

## Rollback and explicit states

This tool never modifies actual game/Assets/B01-22 producer source. Rollback is automatic in the temp owner worktree; delete only external QA-only ZIP and receipt. For any future Codex-owned game implementation, only Codex may roll back exactly its added stock registration, never original wallet/owned GUIDs. Do not conflate this with a merge.

`ORIGINAL_AUTHOR_TESTS=PASS (only after actual CI)`; `LATEST_CODEX_R3_ONLY_PARTIAL_DIFFERENCE=VERIFIED (only after actual CI)`; `PRODUCER_FROZEN_SHA_GATE=EXPECTED_FAIL/HOLD`; `B01-60_INDEPENDENT_QA=PENDING`; `NATIVE_UNITY_G1=NOT_RUN`; `G2=HOLD`; `CODEX_ACK=PENDING`; `INTEGRATED=NO`.
