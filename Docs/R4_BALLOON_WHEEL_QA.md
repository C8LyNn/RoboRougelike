# Balloon fork wheel intake

Based on Draft PR #1048. Adds the L balloon fork wheel from #1018,
`R4M_WheelL_Fork_Balloon.fbx`, SHA-256
`b7a768b8627a0098c0a3305b939c7a4fc6f243102cc0c618a0749b31e84739ab`.
It uses the authored upper socket, tire radius 0.50 m, width 0.43 m,
16 kg mass and 1.15× grounded traction through the existing R6 drive.
Its individual save, mounting port, collision and destruction use the
existing wheel systems. Price uses the existing wheel price for now.

- Unity 6000.3.14f1 EditMode runner with tests entering Play mode at
  `7929952c4e4b25518eb505d6643ee6041c1fe7fb`: **20/21 PASS**; the failed
  mounting fixture attempted to place the large tire through an existing
  wheel, correctly blocked by clearance validation. Actual four-wheel R6
  Rigidbody driving passed, approximately 8.66 m in 1.6 s. Evidence:
  `Artifacts/Local/RC2-BalloonFork-20261108T010834Z`.
- Corrected fixture explicitly verifies rejection at the obstructed position,
  then mounts the same tire to a higher unobstructed support. Targeted native
  EditMode rerun **2/2 PASS**, exit 0, at
  `28c505432412c16f5e909096ca8d2892c2b56f83`; evidence:
  `Artifacts/Local/RC2-BalloonForkRetest-20261108T011218Z`.
- Windows x64 build at `63643b5c`: Unity exit 0. The ZIP was extracted and
  the Player actually launched; entering garage and selecting and purchasing
  the balloon wheel were observed, followed by normal menu exit. Manual
  mounting in this Player: **NOT RUN**. Full campaign and standalone PlayMode
  suite: **NOT RUN**. Log: `Artifacts/Local/BalloonWheelPlayerBuild.log`.
- Archive `RoboRougelike_Kolo_Balonowe_63643b5c_Windows_x64.zip`:
  44,937,161 bytes; SHA-256
  `c9c9bb8feca7ed499c1bf3df9d8e5bcb0378502434d92a25fd6ea670d540583f`.
  Located at `C:/Users/kubap/Desktop/Prototypy/`.

Use the garage catalog to buy the wheel; `U` reveals the underside for its
upper mounting face. Large tires need sufficient clearance. Distinct prior
wheel and armor variants remain present; larger-than-one-cell armor blocks
remain out of scope under the current one-cell requirement.
