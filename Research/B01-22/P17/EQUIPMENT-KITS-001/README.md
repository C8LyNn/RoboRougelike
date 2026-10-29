# EQUIPMENT-KITS-001 / B01-22 / PASS17

**Real game CONTENT as data:** 10 curated, usable garage inventory kit definitions EK01–EK10 using the **existing** R3PartKind / R4WeaponKind enum values. No new MODEL_JOB, no built-in mounting. Each kit has an actual bill of parts and computed sum of the existing host unit prices, with zero discount/rarity assumptions. 10/10 price calculations and 16 other V8 checks executed in the isolated source harness. Native Unity is NOT_RUN.

## Commands (Node >=20)

```sh
node Research/B01-22/P17/EQUIPMENT-KITS-001/kit_cli.mjs validate
node Research/B01-22/P17/EQUIPMENT-KITS-001/kit_cli.mjs list
node Research/B01-22/P17/EQUIPMENT-KITS-001/kit_cli.mjs quote EK08 240 197
node --test Research/B01-22/P17/EQUIPMENT-KITS-001/kit_core.test.mjs
```

The quote is an **offline preview**, not a purchase or reward. Inventory capacity is 200 owned objects in host R3, and allowed vehicle weapon types are <=2. The price snapshot is source-pinned to Codex #310 `aa7a917f177a9b1f07203868fb7d1d871eb3ee46`, R3 blob `8cc68faf89793c3a803e5807594c9faf0df10091`, R4 blob `502f6d7f70d4e8078260de632ab181f244a4d730`. Do not use these prices as runtime authority after a Codex change.

## Wariant 1.0.1 — weryfikacja cen w oryginalnym źródle

`verify_host_source.py` czyta oryginalne pliki C# z podanego commita Codexa przez git show (bez modyfikacji). Weryfikuje enumy, cenniki R3/R4 i oryginalne blob SHA, dodatkowo sprawdza bieżący PR #310 oraz 7 testów negatywnych. Skrypt uruchamia dedykowany CI na Windows i Ubuntu, *nie* wykonuje Unity ani transakcji gry. Status natywnego Unity = NOT_RUN.

## WAVE02 / v1.0.2 — rzeczywisty safe intake

`stage_kit_payload.py` sprawdza SHA-256 i rozmiar każdego z 13 oryginalnych plików manifestu, waliduje 10 kitów, a następnie tworzy w nowym pustym folderze `catalog.json`, flatten `offers.json` i odporny na zmiany `receipt.json`. Refuses overwrites, symlinks and modified rollback data. No wallet, R3 inventory, save or Unity Assets are written.

```
python Research/B01-22/P17/EQUIPMENT-KITS-001/stage_kit_payload.py --verify-only
python Research/B01-22/P17/EQUIPMENT-KITS-001/stage_kit_payload.py --stage /tmp/NEW-CODEX-INTAKE
python Research/B01-22/P17/EQUIPMENT-KITS-001/stage_kit_payload.py --rollback /tmp/NEW-CODEX-INTAKE
python -m unittest discover -s Research/B01-22/P17/EQUIPMENT-KITS-001 -p test_stage_kit_payload.py -v
```

## 1.0.3 / PASS17 W04 - approved-source candidate
Identical ten EK01-EK10 kit bills of materials and prices, but new Codex #383 source provenance: gameplay 203a0805251ed28293888e43f2d49f0df63767c6 / R3 blob 1d0f378dc598e08b3d7c16975f419d84231511bb / R4 unchanged. This is a separate immutable revision branched from original frozen #333 v1.0.2. Strict original source blob gate is retained without bypass. B01-40 independently verified prior R3 source difference was ONLY addition of partial modifier. Do not auto-merge, purchase/mount or claim Codex ACK.
