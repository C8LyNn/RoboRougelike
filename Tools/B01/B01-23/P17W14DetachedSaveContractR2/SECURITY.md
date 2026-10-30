# W14 SELECTED #549 R2 / #550 security regressions
Only #549 Save/Load lineage selected; #544 ALT_HOLD. Original parent SHA 6da2c523e03766cd5a6deaed0cb3cccf8765354b remains untouched, no merge. Frozen gameplay #510 d94400d95d13db7f27e1533df05c9292d5380431.
- Snapshot.Parts public property always defensive-copies its private immutable storage.
- Original W06 mutable ledger is private inside ColdRestore; only read-only count+contains results are public.
- Cold session reserves reentry guard across callback and staging; second idle check before logical pointer publish.
- Wire schema version1 unchanged, packaging version2.0.0.
- #550 original RED must be retested against new exact SHA by independent B01-61.
- Does not restore original #510 live Unity Rigidbody or its private per-rig authoritative AtomicDockLedger; NO SAVELOAD_HOST acceptance, NO Unity G1.
