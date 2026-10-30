# B01-40 — P17 WAVE10 / HAZARDPULSE R2 physical source intake (G0)

## Input and why this is necessary

Immutable **original R1** #430 @ `6095577ece27320b1a861a738cc23ba42d6793a6` remains genuine source RED. Corrected **producer R2** #470 @ `ebc3d6162f4516147092960291e50e15fe6c4582` has source blob `42385a88a43a2d5854a00d74ac82e65075b8c56b`. Independently compiled **B01-60 QA #474** @ `2538f60093d3e8732e3c7865a10ef81ac3a340d7` reproduces R1 38/384 timing discrepancies and R2 0/384, plus original 9/10 R1 vs 10/10 R2 tests on both OS (Actions #41054435212). B01-40 does not patch either source.

The producer's R2 is portable source, with **no native Unity `.meta`, `.asmdef` or physics/HP adapter**. B01-40 therefore constructs a new **receiver-owned optional source import overlay** as a deterministic original-byte physical ZIP. The only producer assets included are the exact, original, UNCHANGED `PulseCore.cs` and `profiles.json` from pinned Git objects. Folder/script/asset GUID metadata plus a standalone `autoReferenced:false` / `noEngineReferences:true` assembly definition are B01-40 *integration packaging*, not a claim that the producer authored them. The original R1 package and every original producer SHA remain untouched.

## What real source G0 verifies

1. All 10 producer R2 manifest Git blobs and all exactly 11 producer own changed paths vs frozen R1; no Codex `Assets/**` modifications.
2. QA #474 manifest references **this exact R2** SHA and core Git blob, and independently states expected R1 RED / R2 GREEN; source tests themselves live in QA #474, not reimplemented here.
3. Generated **six unique GUIDs**, no duplicates or collisions against frozen R1, corrected R2, B01-60 #474 or Codex #383 at `203a0805251ed28293888e43f2d49f0df63767c6`.
4. New isolated `B0140.HazardPulseR2.Runtime` asmdef with no other assembly dependencies and `autoReferenced=false`, `noEngineReferences=true`. No canonical adapter.
5. Deterministic immutable ZIP with producer Git provenance, all 9 Unity files plus embedded `B01-40-PHYSICAL-RECEIVER.json` manifest, source SHA256/Git blob/byte count.
6. Real external fresh-stage / byte-strict rollback, and **eight independent receiver-specific positive/negative controls**: pre-existing destination, edited stage member, unknown stage member, rollback, corrupted expected bytes, unexpected archive member, missing member, ZIP path traversal.
7. Actual Windows and Ubuntu identical binary archive SHA256. On any failure, CI prints nonzero, no G0 accepted.

## How to reproduce

From a parent directory, create five separate exact Git checkouts (no merging):

```text
gate/              # this B01-40 PR
producer-r2/       # ebc3d6162f4516147092960291e50e15fe6c4582
independent-qa/    # 2538f60093d3e8732e3c7865a10ef81ac3a340d7
original-r1/       # 6095577ece27320b1a861a738cc23ba42d6793a6
codex-receiver/    # 203a0805251ed28293888e43f2d49f0df63767c6
```

```bash
python gate/Tools/B01/B01-40/P17W10/HazardR2Receiver/receiver.py \
  --source producer-r2 --qa independent-qa --r1 original-r1 \
  --receiver codex-receiver --out dist
```

The workflow checks out the five exact sources and executes this on Ubuntu 24.04 and Windows 2025, uploads raw logs + real ZIP + machine-readable `G0-RECEIPT.json`. **Actions artifact outer ZIP is not the source-locked inner ZIP**; extract it first and check SHA256 printed in the JSON receipt.

## Installation and rollback contract (no actual install performed)

**Only after B01-00 authorizes an isolated receiver test**: back up your Unity 6000.3.14f1 project; confirm `Assets/B0140HazardPulseR2` and its adjacent `.meta` are absent; check ZIP SHA256 and all manifest member hashes; extract only these new paths, with no overwrite and no R1 duplicate assembly. Do **not** import our test scripts, CI, README or intermediate JSON. The provided `profiles.json` is external authored source data, not guaranteed `Unity JsonUtility` compatible. Actual scene adapter, source API binding to canonical HP, VFX, physics, map, damage and in-game lifecycle belong to Codex after explicit ACK.

Rollback on the **disposable authorized receiver branch**: remove solely `Assets/B0140HazardPulseR2/**` and `Assets/B0140HazardPulseR2.meta` if still byte-identical to original imported manifest; retain user changes, other files and original Codex asset tree. For the test routine, staging happens only in a separate external tempfile and rollback checks bytes before deletion. An altered or foreign file is a HARD HOLD, not force-deleted.

## Gate and governance

- **G0 PHYSICAL SOURCE:** exact SHA verification, GUID/asmdef/ZIP/stage/rollback on two OS — ACCEPT only after real exact-head CI SUCCESS.
- **G1 native Unity:** NOT_RUN (Unity Editor compilation, EditMode, PlayMode, real HP/VFX and collision). No source check or `.NET` QA counts as this.
- **G2 integration:** HOLD; no scene wired, no canonical `Assets/R3` changes, no merge/rebase/cherry-pick.
- **Codex:** NOT_OFFERED, ACK_PENDING, NOT_CLAIMED, NOT_INTEGRATED. Only B01-00 decides queue #79 and actual OFFERED handoff. No phantom Codex ACK.
- **Ownership:** All authored code resides under `Tools/B01/B01-40/P17W10/HazardR2Receiver/**` and unique workflow. Neither old #467 nor any producer/QA path modified.
- **Reattach safety:** Original public W07 `Reattach(int)` remains bypassable through #471 optional GUI wrapper; QA #472/#476 correctly block release. B01-23 must ship a new version with **all public entrypoints fail-closed**; B01-40 may physically verify after independent QA. No B01-40 source patch to owner domain.

Recipient B01-60 (independent native G1); B01-23 (reattach core closure); B01-00 (#483/#79 queue). References #276 #430 #444 #470 #474 #471 #472 #476 #483.
