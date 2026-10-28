# INSTALL_FOR_CODEX

1. Inspect immutable branch/PR SHA and manifest; run `node --test Research/B01-22/P17/EQUIPMENT-KITS-001/kit_core.test.mjs` and CLI `list`.
2. Compare current Codex game `R3PartKind`, `R4WeaponKind`, `R3GarageEconomy.Price`, `R4WeaponDefinition.Get(...).Price` to pinned source. Price/source drift => HOLD, request producer revision; no bypass.
3. If Codex elects to expose curated equipment stock, consume `catalog.json/kits[].parts` as **data only** using the EXISTING host inventory and purchase APIs. Do not create a second wallet, new save, reward loop, or fabricated physical instance GUID. Actual transaction/ownership validation is Codex-owned.
4. Kit rows have no anchors, socket contacts, rotations, 1×2×2 geometry or validated mount. Import as available items, never as mounted vehicle; host independently checks capacity, budget, price and idempotency.
5. No edits to canonical `Assets/**`, frozen ECON #252/#263, LEGENDARY #242, CHL, PARTSIZE or live Codex workspace are proposed by this PR.

**Rollback:** remove staged EK01–EK10 catalog entries from Codex's future integration branch; leave all existing owned item GUIDs untouched. Nothing in current game needs rollback because this data-only PR is isolated.
