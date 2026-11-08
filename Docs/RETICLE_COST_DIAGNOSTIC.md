# Twelve-gun aim feedback diagnostic

Measured Unity 6000.3.14f1 EditMode on the unchanged product code at parent `fc6105c82bb83570a7913dc66da399b6e098e884`. Each sample calls the actual aim caption and per-gun marker calculations together, including their own-hull checks. The synthetic fixture has 12 gun components, 80 owned cube colliders, a camera and a 30-metre-wide collider at 50 m. It is not a complete R3 vehicle, actual firing or an FPS benchmark.

After three warm-ups, 100 consecutive samples reported **31.57 ms total, 0 managed bytes** as measured by `GC.GetAllocatedBytesForCurrentThread` in the test thread. The same synthetic fixture without a target collider reported **23.56 ms total, 0 managed bytes**. Unity Editor and physical scene results depend on machine, geometry, runtime and physics state; neither measurement establishes Windows Player performance under combat, many enemies, high-poly art or sustained play.

Both focused runs passed 1/1, process exit 0. Local XML and Editor.log: `Artifacts/Local/ReticlePerf-Baseline-20261108/` and `Artifacts/Local/ReticlePerf-TargetBaseline-20261108/`. No production optimization is claimed from this measurement. In particular, both presentation paths still perform their own clearance check.
