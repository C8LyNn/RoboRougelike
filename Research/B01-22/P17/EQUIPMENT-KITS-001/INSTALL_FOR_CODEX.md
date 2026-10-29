# INSTALL_FOR_CODEX

1. Inspect immutable branch/PR SHA and manifest; run `node --test Research/B01-22/P17/EQUIPMENT-KITS-001/kit_core.test.mjs` and CLI `list`.
2. Compare current Codex game `R3PartKind`, `R4WeaponKind`, `R3GarageEconomy.Price`, `R4WeaponDefinition.Get(...).Price` to pinned source. Price/source drift => HOLD, request producer revision; no bypass.
3. If Codex elects to expose curated equipment stock, consume `catalog.json/kits[].parts` as **data only** using the EXISTING host inventory and purchase APIs. Do not create a second wallet, new save, reward loop, or fabricated physical instance GUID. Actual transaction/ownership validation is Codex-owned.
4. Kit rows have no anchors, socket contacts, rotations, 1×2×2 geometry or validated mount. Import as available items, never as mounted vehicle; host independently checks capacity, budget, price and idempotency.
5. No edits to canonical `Assets/**`, frozen ECON #252/#263, LEGENDARY #242, CHL, PARTSIZE or live Codex workspace are proposed by this PR.

**Rollback:** remove staged EK01–EK10 catalog entries from Codex's future integration branch; leave all existing owned item GUIDs untouched. Nothing in current game needs rollback because this data-only PR is isolated.

## WAVE02 / safe receiver 1.0.2

1. Checkout pinned PR commit and run `stage_kit_payload.py --verify-only` + Node/Python tests.
2. Execute `python stage_kit_payload.py --stage /EXISTING_PARENT/NEW_EMPTY_DESTINATION`; rejects a previously existing target. Read 10 flattened `offers.json` equipment stocks, price snapshot, `receipt.json` SHA256 checksums.
3. Codex is sole owner of any actual R3 wallet / GUID / inventory or mounting integration; no host gameplay is changed by this source. Recheck current Codex original R3/R4 enum/prices before import.
4. Rollback only staged directory with `python stage_kit_payload.py --rollback /EXISTING_PARENT/NEW_EMPTY_DESTINATION`. Any staged mutation or extra user file blocks deletion; manually inspect. This rollback does not undo any separate future Codex-side integration.

## 1.0.3 exact-source receiver
Use only game source Codex #383 SHA 203a0805251ed28293888e43f2d49f0df63767c6. Run original Node tests, Python staging and unchanged strict Python C# source gate against that exact ref. Stage only to new disposable folder; do not alter original #333, wallet or inventory. B01-40 G0 and B01-60 independent QA first; B01-00 controls #79; rollback = discard staged data.
