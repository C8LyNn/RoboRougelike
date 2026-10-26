# RAC-001 / R3 — Physical Demo Production Evidence
Issue #8, source parent existing Draft PR #13 at SHA 7884f2737197e26769842c087dcd2a1ead75ac95. Separate stacked production branch feature/b01-rac001-s02-r3-physics-demo. Receiver Codex only after G2.

ACTUAL CODE DELIVERED: changed CheckpointRaceDemoController.cs from Transform.MoveTowards to Rigidbody AddForce(PD velocity target/acceleration cap), added floor, gravity-enabled rigid body and nonblocking marker objects in existing unsaved Additive demo scene builder, and new RAC001PhysicalDemoTests.cs + .meta for native PlayMode force-driven checkpoint.
No change to authoritative CheckpointRaceState.cs or race gameplay control. Entire PR #13 parent source needed, not only this delta.

TESTS: Unity C# compile NOT_RUN. Unity EditMode NOT_RUN. Unity PlayMode (new actual AddForce traversal definition) NOT_RUN. Human real Unity demo NOT_RUN. Old PR#13 reported repository static CI pass, Unity jobs skipped due license preflight, which is NOT native gameplay PASS.
B01-60 must independently checkout frozen R3 SHA (after manifest), invoke Unity 6000.3.14f1 PlayMode: physical cube arrives at checkpoint <30s, motion with force not teleport, real gravity collision and failure on removed floor; run existing 11 EditMode and 3 PlayMode tests plus new 1 case. Capture XML/log/process exit and 720p/1080p screenshots. FAIL -> B01-20 R3.1 patch and retest new SHA.
G0 isolated source provenance preliminary; G1 BLOCKED/NOT_RUN; G2 HOLD.
