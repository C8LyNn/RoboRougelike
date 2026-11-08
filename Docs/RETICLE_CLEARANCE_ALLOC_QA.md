# Shot-path and reticle allocation work

The real firing-path query now reuses per-vehicle physics hit and overlap buffers, expanding and retrying when full. At the 8192-entry cap it falls back to the original allocating Unity queries instead of discarding hits. Stale overlap references are cleared. Parent-component lookup and EMP availability checks use `TryGetComponent` while preserving existing ownership and hit rules. No aim, weapon damage, energy or fire-rate settings change.

## Native targeted evidence

Unity 6000.3.14f1 Editor, synthetic 12 gun / 80 owner-collider fixture with an external target. Unity `ProfilerRecorder` `GC Allocated In Frame`; 100 calls are made within one frame after warm-up. A separate 1 MiB probe produced **1,048,978 bytes**, confirming this counter sees a known allocation. Values include Editor frame overhead and are not Player/FPS measurements.

| Operation | Before, bytes | After, bytes |
| --- | ---: | ---: |
| Idle test frame | 370 | 450 |
| 1200 shot-path checks | 1,546,770 | 450 |
| 1200 EMP availability checks | 677,570 | 450 |
| 100 caption + 100 marker updates | 4,447,570 | 450 |

Initial buffer reuse alone still measured 1,354,770 bytes for the path queries; the targeted test remained FAIL. Missing-component lookups were isolated and changed before the final PASS. These numbers reflect this one Editor test and should not be generalized to fully constructed R3 vehicles, art-heavy scenes, other machines, or gameplay frame rate.

- `ReticleAlloc-Red-20261108`: 1/2 PASS, exit 2; 150 external colliders still correctly expose own armor, but allocation threshold failed.
- `ReticleAlloc-Green-20261108`: 6/7 PASS, exit 2; first buffer attempt did not meet the allocation diagnostic.
- `ReticleAlloc-Green2-20261108`: 7/7 PASS, exit 0; included shotgun spread, beam, Automatic, own-armor warning and saturated-buffer behavior.
- `ReticleAlloc-Final2-20261108`: 7/7 PASS, exit 0; actual beam and projectile blocking/resuming, two Automatic muzzles, 150-collider saturation, and real regenerator EMP/damage behavior. The diagnostic is observational rather than a fixed global-Editor allocation threshold, to avoid flaky builds.
- `ReticleAlloc-SaturationProof-20261108`: 1/1 PASS, exit 0; additionally verifies the fixture really returns over 128 cast hits before checking own-armor blocking.

XML, Editor.log and exit codes are in the named local `Artifacts/Local/` directories. The 8192-entry fallback, full native suite, Windows Player performance and human combat are NOT RUN. Earlier energy refresh optimization and invalid thread-allocation-counter correction are described in `ENERGY_REFRESH_PERF_QA.md` and `RETICLE_COST_DIAGNOSTIC.md`.
