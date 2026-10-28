# CX-07 — continuous trigger release when camera is lost

Base `a31a1853097da6d20bb1144f4ee7ea6259da758f`. Native real scene fixture mounted a real Beam Cutter, held its assigned group and disabled the actual main camera; the original controller LateUpdate left the real RRLBeamBinding trigger held. The fix releases both group triggers and clears stale aim feedback when no camera or weapon rig is available. Existing normal firing, input gating, pause and destruction code remain in place.

Unity 6000.3.14f1 Windows: valid real-scene repro 0/1 RED exit 2, after correcting the first fixture's incorrect A/B assumption; final targeted EditMode/UnityTest suite 11/11 PASS exit 0, including actual no-camera beam release, group firing, pause and beam/saw tests. XML and exits in Evidence, local full logs `Artifacts/Local/CX07-trigger/`. This is API-driven Unity, not manual Windows Player. Independent B01 QA and G1/G2 NOT_RUN; no merge.
