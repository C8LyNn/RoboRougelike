# Large side-mounted wheel

Windows build source `fb4ebf15`, based on Draft PR #1044. The L off-road FBX
comes from the R4 wheel library on #1018; SHA-256
`ec6961c0ac764441ac469912c5b6c17308acc5e9effc38ba413f03d8f5ccd2cf`.

The large wheel reserves one cell, has an actual 0.56 m tire radius and 0.42 m
width, a side-facing socket, 18 kg mass and a 1.25 multiplier to its grounded
R6 traction contribution. Existing medium off-road and slick appearances stay
at 12 kg with multiplier 1.0. A physical size change requires removal and a
new validated mount; the visual-only switch between medium appearances remains.
The prototype prices currently use the existing wheel price.

Unity 6000.3.14f1 EditMode runner, with tests entering Play mode: **10 passed,
0 failed, 0 skipped**, native exit 0 on
`88134e39fddbdc6155fe601280c954493e5700b5`. It verifies actual socket
orientation, physical clearance (a medium tire clears an obstacle while the
large tire is blocked), real contact and grounded traction, mass, save/restore,
and an actual R6/Rigidbody flat-ground drive of about 10 m in 1.6 simulated
seconds. The drive fixture applies at most one force per physics tick.
Evidence: `Artifacts/Local/RC2-LargeWheelSingleForce-20261108T002714Z/`.

Earlier drive attempts were not accepted: the first fixture did not advance
the physics clock reliably and a subsequent attempt applied repeated forces
within one tick. The final fixture explicitly guards and records fixed time.
The standalone PlayMode suite, terrain comparisons, long-term balance and a
full campaign were **NOT RUN**.

Windows build succeeded with Unity exit 0. In the actual Player, the large
wheel was purchased and mounted on the side of the construction; its model
rendered, and the game exited through the menu. ZIP extraction and required
files passed; the Player was run from the build folder, not the extracted ZIP.

Archive: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_DuzeKolo_fb4ebf15_Windows_x64.zip`
(44,529,070 bytes), SHA-256
`21f5d14d410859436e4163b8d5de5e095aecb4fe879336cee9d2b741a1bfea9a`.
