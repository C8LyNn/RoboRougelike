# Twin road wheel

Based on Draft PR #1050. Source #1018 `R4M_WheelM_Twin_Road.fbx`, SHA-256
`d517b0074789c1ebd597aa13f95b6370a4bfe6f90e5540478e6840cf604e9f86`.
Two independently colliding 0.16 m wide tires at 0.18 m center separation
are one wheel module, inventory item and R6 drive unit. Radius is 0.33 m,
axle drop 0.20 m, upper port and mass 12 kg. Tire contact queries and force
continue to use the existing R6 system. The normal wheel price applies.

- Missing-feature test at `a34eb8c2be4b73307ad45687349f4d1b99f67684` failed
  before the new wheel was added: `RC2-TwinWheelRed-20261108T081906Z`.
- Unity 6000.3.14f1 native EditMode runner, including tests entering Play
  mode: **6/6 PASS**, exit 0 at `aca747f4cbf66f17f26eade3881e8e7e8cef97ca`.
  Actual R6/Rigidbody run traveled 9.55 m in 1.6 s. Includes catalog, rotated
  sockets and two collider positions. Evidence: `RC2-TwinWheel-20261108T082132Z`.
- Real garage mounting, both colliders and checkpoint roundtrip: **1/1 PASS**,
  exit 0 at `41c01b2961f9a964720d0446f256cf2e2292acf8`; evidence:
  `RC2-TwinGarage-20261108T082240Z`. All evidence is under `Artifacts/Local/`.
- Windows x64 build `d9daf17f`: Unity exit 0. Extracted ZIP launched; garage
  entry, selection and purchase observed, followed by normal exit. Manual
  mounting, uneven terrain, full campaign and standalone PlayMode suite:
  **NOT RUN**. Build log `Artifacts/Local/TwinWheelPlayerBuild.log`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_Kola_Klocki_d9daf17f_Windows_x64.zip`:
  45,394,033 bytes; SHA-256
  `cdb00b66a39e3a9c26f2ba9496639cba8b8fdd38811102f08bd703692caa8c05`.

Buy the wheel in the garage catalog; press U for the underside view and mount
at its upper face. Place it with room for both tires.
