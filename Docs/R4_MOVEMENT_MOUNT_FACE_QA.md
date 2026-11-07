# Correct R4 movement visual mounting face

The art integration contract in #963 `03af5207143da76faeeb8004543a51f26c7f9c0d` places movement-model `Socket_Mount` on the top face of its 0.52 m block. The previous adapters for Booster, Gyroscope, Suspension and JumpDrive placed the socket at the block center, shifting their visuals by 0.26 m at normal scale. The correction aligns each socket with local `(0, 0.5, 0)` on the actual scaled and rotated block. Physics, colliders, stats and original asset files are unchanged. Wheel mounting was not changed.

Unity 6000.3.14f1: prior to the fix, **4/4 alignment tests failed**, each by 0.26 m (`r4-mount-face-red.xml`). After correction **6/6 targeted tests PASS**, including the four models and real movement regressions (`r4-mount-face-green.xml`, exit 0). A separate Unity test **PASS** checked all four models at four quarter-turn orientations and two uniform scales, i.e. 32 combinations, including socket normal (`r4-mount-rotation.xml`, exit 0). This is one test case containing 32 checked combinations, not 32 independently reported test cases.

**NOT RUN:** manual visual review, all possible combined rotations, Windows Player at this revision and full Unity suites. The prior Windows ZIP `d1a33a74` predates this fix and still contains the 0.26 m offset. The automated shutdown C0000005 and raw FBX axis failures remain unresolved.
