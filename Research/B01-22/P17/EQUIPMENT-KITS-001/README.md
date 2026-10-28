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
