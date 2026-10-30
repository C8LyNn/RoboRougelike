# B01-61 PASS17 / W12 — EXCLUSIVE #510 RECEIVER MIGRATION GUARD (read-only)

**Package ID** `B01-61-P17W12-RECEIVER-MIGRATION-STRICT-001 v1.0.0`. **Role:** CODEX REGRESSION QA; independent **missing receiving-game migration** check, NOT a second #517 transaction harness nor a second B01-40 ZIP packager.

**Single frozen producer** [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) `d94400d95d13db7f27e1533df05c9292d5380431`; W06 `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. Parent #488, competing #511 `baf65f2664e38f3b9c38815c3f07285f8107c1b6` and #494 are NOT install targets. **Never stage both**. Historical source RED in #486/#502 and independent #510 RED→GREEN #517 (167 checks per OS, synthetic Unity boundary) are valid existing evidence; this package does NOT duplicate or falsely upgrade those results.

## What this new gate actually does
`preflight.py` takes a **read-only path to a PRE-EXISTING disposable migrated receiver**. It verifies exact original **34 source files** byte-for-byte from frozen #510 Git, matching selected source manifest and version 2.0.0, including the *actual* AtomicDockLedger Git blob and script .meta. It rejects code from alternative #511 even though it uses the *same Unity MonoScript GUID* as #510. It also checks the 4 original grounded script GUIDs of selected V2 and legacy W07/W08/W09, duplication of any .meta GUID, references to obsolete MonoBehaviours in serialized Unity `.unity`, `.prefab`, `.asset` files, dynamic `AddComponent/GetComponent<Legacy>` callers, and exact **one** selected V2 component in the specified target scene. Missing/empty/binary scene, malformed/forged manifest, unexpected selected namespace files, old W07 source, symlinks all **HOLD**.

**No receiver writes**. Unlike unsafe 'auto-migration', gate never modifies or deletes any asset, file, metadata, scene or directory, even on failure; no Codex-owned paths are opened for write.

CI runs the exact pinned source and representative actual Git-source byte-filled receiver fixtures in temporary scratch, positive and hostile negative cases, on Ubuntu24.04 + Windows2025. Passing those checks means the **gate is usable**. It does **NOT** prove that any real Codex/game receiver was migrated (none provided). Native Unity/PhysX/Editor/Player = **NOT_RUN**.

## Usage by B01-40 / B01-00 / Codex (authorized receiver only)
1. B01-40 downloads, checks and stages the **actual** original #510 35-member ZIP only into an **empty, external/disposable receiver**, using #510 owner-provided `package.py` and verified SHA256 `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`. NEVER copy #511/#494 or W07/W08/W09 into that tree.
2. On a **licensed Unity** host, create the V2-only isolated scene via original Editor `SecureReattachSceneBuilder` if needed; for a real game, perform authorized migration separately and export YAML serializations. Do not fabricate a game scene or assume a staged `.cs` is a working prefab.
3. From repo root run:
```sh
python -m unittest discover -s Tools/B01/B01-61/P17W12ReceiverMigrationGuard -p test_preflight.py -v
python Tools/B01/B01-61/P17W12ReceiverMigrationGuard/preflight.py --receiver /absolute/disposable/path --scene Assets/B0123SecureReattachV2/Generated/B0123SecureReattachV2.unity
```
Only a real inspected receiver is eligible for a receiver-specific `G0_MIGRATION_PREFLIGHT_PASS`. Passing CI scratch fixtures is **NOT** real receiver G0. Scanner exits **3** on HOLD and **0** on preflight pass, prints machine-readable JSON. No UI claims, no native Unity checks. If installed into Codex canonical gameplay, that installation requires Codex explicit ACK; not present here.
4. **Rollback** belongs to B01-40 / #510 owner `package.py rollback` after verifying original staged contents; this tool has nothing to roll back. On a foreign/modified target, rollback must HOLD rather than recursively delete changes. Closing this unmerged Draft removes ONLY own 4 QA paths; no producer or Codex change.

## Hard boundaries
- Scanner's managed-code search covers simple direct legacy generic API calls, not reflection, dynamic assemblies or runtime-instantiated scenes. Text-YAML asset parsing requires Unity ForceText; binary assets are HARD HOLD. A prefab imported after scan, runtime save/load or serialized scene absent from the provided receiver are **not verified**.
- **Neither independent CI source acceptance nor this read-only preflight grants G1/G2**. G0 final physical ZIP/GUID/asmdef/source conflict acceptance B01-40; native Edit/PlayMode/PhysX B01-60; save/ID persistence B01-62; Codex host ownership and ACK B01-00.
- Historical older W07 public bypass remains unsafe and is deliberately not migrated by this QA PR.
