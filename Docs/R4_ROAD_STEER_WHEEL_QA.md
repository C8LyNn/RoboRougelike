# Road steering wheel

Based on Draft PR #1051. Source #1018 `R4M_WheelM_Steer_Road.fbx`, SHA-256
`1af692974ddb82a6d2cfc1b23222ded040739867ad36c89f1be899f46628a249`.
The top mounted wheel has a 0.34 m tire radius, 0.26 m width, 0.315 m axle
drop and weighs 12 kg. It uses existing R6 contacts, drive and persistence.
The articulated knuckle rotates with the existing MOV-011 steering pivot,
leaving the static mounting socket in place. MOV-011 requires a working
device and its existing four-corner layout. As with existing MOV-011 wheels,
the physical tire collider stays fixed and steering forces are resolved by
R6; the knuckle motion is visual. The ordinary wheel price applies.

- Missing-feature test at `c2cc275804df7cdb5af352eecd250f9796a75b28` failed
  before implementation: `RC2-RoadSteerRed-20261108T085705Z`.
- Unity 6000.3.14f1 EditMode runner, including tests entering Play mode:
  **15/15 PASS**, exit 0 at `f26b318dfb4d11fb018bc083759122727896822c`.
  Includes catalog, regression tests for MOV-011 and R6/Rigidbody driving:
  9.24 m in 1.6 s. Evidence: `RC2-RoadSteer-20261108T085923Z`.
- Real MOV-011 binding with four new wheels, moving knuckle and stationary
  socket: **1/1 PASS**, exit 0 at `12de0c348125dc1ab652e43fc29155bfa55354ca`;
  evidence `RC2-RoadSteerMOV011-20261108T090125Z`.
- Garage installation and checkpoint: **1/1 PASS**, exit 0 at
  `648ee6805dfbb90cf0388380842b3ec8265af7f6`; evidence
  `RC2-RoadSteerGarage-20261108T090215Z`. Evidence is under `Artifacts/Local/`.
- Windows x64 build `63da2f25`: Unity exit 0; extracted ZIP launched and
  garage selection and purchase were observed, followed by normal exit.
  Manual mounting, in-game MOV-011 driving, full campaign and standalone
  PlayMode suite: **NOT RUN**. Build log `Artifacts/Local/RoadSteerWheelPlayerBuild.log`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_Kola_Klocki_63da2f25_Windows_x64.zip`:
  45,557,752 bytes; SHA-256
  `db5bbaf4fdf4da8e7115ac32bbbf6afaf074a948ee933cbcb378adb4a190d814`.

Buy the wheel in the garage catalog, press U for the underside view, and
mount its upper face with sufficient clearance. Install the independent
steering device on a supported four-wheel layout to operate the knuckle.
