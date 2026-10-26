# INSTALL_FOR_CODEX — isolated ECONREBUILD001, no unattended integration

## Pinned inputs
- Owner approval/policy: [Issue #136](https://github.com/C8LyNn/RoboRougelike/issues/136). Some design choices are OPEN; do not silently finalize balancing.
- Unity target: 6000.3.14f1 URP; public `R3VehicleModel` verified from OBJ #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`.
- Source: Draft PR #252, **use its current exact HEAD and matching Actions artifact receipt**, not a SHA copied from a superseded description.

## Installation sequence — ONLY after B01-00 G2
1. Work in a **disposable** clean repository checkout; use B01-40 package auto verifier from PR #238. Preserve destination `Assets/B01Packages.meta`, including its canonical GUID; verify all inner ZIP entries and `source-manifest.json` hash entries, then backup Saves/Library/Assembly-CSharp/R3 routes.
2. Import only `Assets/B01Packages/ECONREBUILD001/**` and its `.meta` files. This is source-only; no scene or existing R3 files overwritten.
3. Run genuine Unity compile and authored Editor NUnit tests; report raw XML/exit before claiming Unity PASS.
4. Compile **receiver-only** `Tools/B01/ECONREBUILD001/R3EconRebuildPort.install.cs` into host Assembly-CSharp after preserving its exact IDs and signatures. The bridge only reads existing `R3VehicleModel.Parts`, `Inventory`, `Price`; never instantiate a second R3 inventory.
5. Create the host-owned `IAtomicRebuildR3Hooks`, atomically across the existing real wallet, physical part instance and journal. `ReserveAndDebit` must check funds and 0HP, debit **once**, persist `reservationKey`, mark `REBUILDING` and return false with NO side effects on failure. `CompleteBatch` must restore all matching reserved IDs to 1HP in one transaction on a **genuine unique victorious completed challenge**; cannot use the legacy immediate `Repair` entrypoint.
6. Add hooks to the existing R3 garage, run lifecycle and save pathway. Block mounted/redeploy/sell/transfer/use while `IsRebuilding(id)` and preserve all other identity-bearing properties, levels and upgrade IDs. Keep ordinary damaged-part (>0HP) repair path unchanged.
7. Save/reload the host wallet+parts+reservation ledger and queue `TrySave` metadata under one crash-consistent checkpoint. Restore host first, then queue `TryRestore`; quarantine if any mismatch. Reentrant, repeated victory and failure must not duplicate restore or deplete scrap twice.
8. Coordinate RUN-SAFETY #154 loaner anti-softlock policy with HQ and B01-10; do not introduce fallback free spawn/instant heal automatically. Cost is quote `0 < rebuild < same-part purchase`, but numeric 65–75% and part rarity discount remain **unapproved**.
9. Real Unity R3 integration+PhysX, 1WIN queue, reload, 0HP mount exclusion, enemy damage, correct garage visibility, win/fail transitions, 2 builds and actual Windows x64 screenshot/manual playtests must be G1 independently checked by B01-60; independent G0 by B01-40; only B01-00 G2.

## Rollback
Stop gameplay/save jobs, backup all host+queue transactional journals and open reservations. Undo only new event hooks/bridge and ECONREBUILD001 Unity folder. Preserve shared parent meta. Do NOT discard queue metadata while a reservation has already debited scrap; migrate/refund explicitly with owner approval. Restore verified backup and rerun known RC1/native baselines in a disposable environment before returning to active game.
