# W27 saw baseline native reproduction

Status: BASELINE RED / producer candidate #828 not tested or integrated. Product source `710b7ce01bd0a7bc9a00008cecd56c69ce67e052`, same gameplay as `af33caed0d5b5412c450dd65ef0f1692f08bc695`; Unity Editor 6000.3.14f1 EditMode, Windows, hidden. Test file is QA-only and does not modify the saw implementation or B01 package.

`W27SawClearanceTests.OwnPartInSawDiscBlocksMotorAndHeatUntilRemoved` builds the actual R3 core + saw assembly, parents a physical 0.2 m armor collider to its Rigidbody owner at saw center plus 0.6 m forward, calls the real integrated binding's physics tick twice, and requires zero RPM/heat until clearance. Actual test exit 2: **FAIL**, expected RPM 0, observed `139.999939` RPM. Unity XML and Editor.log: local `Artifacts/Local/W27/Saw-baseline-red/`; exact invocation uses `-runTests -testPlatform EditMode -testFilter W27SawClearanceTests`. The first test fixture run under `Saw-red/` failed with a test-only NullReferenceException from a cached LINQ closure after EnterPlayMode; replaced with explicit loops, then reproduced the actual assertion above. Neither result proves a fixed product.

A full regression on the separate W27 integration worktree was executing concurrently; this diagnostic ran below normal priority in its own worktree, and timing-sensitive full-suite results must be interpreted accordingly. Test uses explicit fixed ticks, not manual gameplay or Windows Player. It has not yet reached its removal/resume assertion because the first assertion fails.

The independently authored producer #828 `887fd18bf0a518ee24776c660f5efa6e3a0491d8` proposes a real disc guard and authored physics tests; its independent G0 and native Unity G1 remain pending. This baseline test does not establish whether #828 fixes it. No source package was imported, no gameplay source changed, no merge.

## Fixture correction and candidate check

The initial baseline fixture placed the disc partly inside its supporting core, so its eventual resume assertion would not isolate the additional obstacle. The corrected test first points the disc upward away from the core, then places/removes the separate owner collider. Against the original baseline it still reproduces the actual failure: expected 0 RPM, observed 139.999939, exit 2 (`Artifacts/Local/W27/Saw-baseline-clearcore/`).

The identical corrected test passed on an isolated QA checkout of #828 `887fd18bf0a518ee24776c660f5efa6e3a0491d8`: the added blocker stopped the motor and disabling it allowed resumption. Candidate suite was 2/4 EditMode, exit 2: existing player saw damage and enemy saw RPM integration tests failed. Original package PlayMode was 19/20, exit 2: its overlap overflow diagnostic flag test failed. Detailed results were posted to #828 and #79. These are candidate QA results, not gameplay integration, G0 approval or a release.
