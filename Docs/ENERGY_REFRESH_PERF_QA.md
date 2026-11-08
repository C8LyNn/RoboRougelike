# Vehicle energy refresh: targeted allocation reduction

The player's energy bank is refreshed by the physics step, status reads and device spending. The previous implementation built a temporary array and multiple LINQ iterators for every refresh. The player-owned branch now aggregates the same live, attached parts in one pass. Battery tier units, passive reservation, core status, EMP blocking of regenerators, regeneration and consumption rules remain unchanged. Enemy refresh is unchanged.

Unity 6000.3.14f1 Editor / real heavy R3 vehicle (85 parts), 500 synchronous refreshes after warm-up and a forced collection:

- Before: `EnergyRefresh-Red-20261108`, **9.28 ms**, Mono used-heap delta **450,560 bytes**, no collection; test failed its 128 KiB budget.
- After: `EnergyRefresh-Final-20261108`, **9.13 ms**, Mono used-heap delta **0 bytes**, no collection; the diagnostic passed. A preceding after run reported 5.79 ms, so these timings are not a reliable speed comparison.
- The control allocating a 1 MiB byte array raised Unity's Mono used-heap counter by 1,052,672 bytes. `GC.GetAllocatedBytesForCurrentThread` incorrectly returned zero for that control, so it is not used for the conclusion. Heap counters are coarse and global to the Editor; zero means no change at their resolution, not a guarantee of zero allocations on all runtimes.
- Final focused run: **5/5 PASS**, process exit 0. It includes real battery/passive destruction, real regenerator operation before/during/after EMP and after destruction, actual tier-2 Automatic damage and energy cost, the heavy-vehicle measurement, and the separate reticle diagnostic.

Raw local evidence is in `Artifacts/Local/` under the names above. Windows Player, sustained combat, performance with multiple enemies and full regression remain NOT RUN. No FPS improvement is claimed.
