# R4 crawler track appearance

The R4 crawler track from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` is attached to existing player CrawlerTrack blocks, mirrored for positive-X vehicle cells. The original physical collider, contact probes, force solver, damage and drive controller remain authoritative. The existing primitive remains the fallback if the model or animation clip is unavailable.

The supplied `Travel` clip is referenced by the existing Resources catalog and sampled on the visual model according to local Rigidbody point velocity and the manifest's 0.0517 m link pitch. This drives the 40 decorative links and wheels without writing vehicle physics. Backward velocity samples the same cyclic clip backward; reverse direction and visual rate still need human review. The 31,842-triangle-per-module source model is not LOD-optimized here; performance with many tracks is **NOT RUN**.

Unity 6000.3.14f1 **3/3 targeted tests PASS**, exit 0 (`r4-tracks-first.xml`): actual light robot drives and its links move while retaining the original collider, all three vehicle classes drive forward/reverse and turn with existing mass response, and the Omni appearance regression passes. A subsequent focused test **1/1 PASS** (`r4-tracks-mount.xml`) also checks the mount remains fixed after animation sampling. The pre-implementation test failed because the model was absent (`r4-tracks-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this revision, source Reverse/Stop clips, all rotated mounts, mirrored appearance confirmation, full Unity suites, enemy/garage ghost appearance and performance under heavy track counts. The `11501df7` ZIP predates this change. Automated shutdown C0000005 and source FBX axis issues remain unresolved.
