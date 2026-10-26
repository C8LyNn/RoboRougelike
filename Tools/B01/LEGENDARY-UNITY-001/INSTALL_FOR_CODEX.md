# INSTALL_FOR_CODEX — gated, no automatic merge
1. Verify exact Draft SHA and artifact ZIP SHA256 from successful Actions. Backup project and saves.
2. Import the ZIP `Assets/B01Packages/LEGENDARY001/**`, preserving stable `.meta` GUIDs; verify Unity 6000.3.14f1 asmdef compile and NUnit EditMode.
3. The optional `R3LegendaryPort.install.cs` is receiver-side source (NOT in ZIP); copy to host Assembly-CSharp only after explicit source reservation. It uses **verified OBJ #210** public API `Parts`, `Inventory`, `Mount`, `Remove`, `Repair`, `RepairInventory`, `ReceiveSalvage`, `PlacementError`.
4. Host must supply TWO real predicates: registered typed socket proof for `WeaponBase` and later `MobilityHub`; and complete multi-cell collision/clearance/connected footprint preflight. Missing callbacks are fail-closed. No Structural fallback for MobilityHub. Do not enable real effects or mass before physics measurements.
5. Provide one `LegendaryUnityService` instance over all authoritative live robot ports (never DeepCopy preview). Route all legendary salvage/mount/repair/unmount entry points through it. **Existing direct R3 Mount can bypass metadata uniqueness until gated host hooks added; NOT INTEGRATED.**
6. Save R3 physical host state + service metadata as one crash-safe checkpoint; restore all R3 hosts first then `TryRestoreMetadata`. Block unbound or duplicate IDs. Prevent metadata-only restore from re-adding physical copies.
7. Current OBJ R3 `Remove` discards 0HP parts: the sample refuses 0HP unmount, requires actual paid repair. Public R3 lacks cross-vehicle inventory extraction: transfer refuses until future atomic API.
8. Finish two-family LMB/RMB integration (#41), full multi-cell PhysX, mass/COM, negative 0HP and 2-robot Save/Load; independent B01-60 and B01-40 reviews, B01-00 G2 approval. No merge now.

**Rollback:** stop game and saving; backup metadata and physical inventory; remove only package directory/metas and optional new host bridge; revert only new event hooks; restore baseline scenes/build/saves, rerun native baseline tests. Never wipe/clone existing R3 inventory to recover from a failed install.
