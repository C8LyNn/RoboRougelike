# Large offroad steering wheel

Based on Draft PR #1052. Source #1018 `R4M_WheelL_Steer_Offroad.fbx`, SHA-256
`70575a90663cbd902f040d5d73fa63f7c46ac3ffb9834fb8fce9c383bc1d5d55`.
The upper socket, 0.48 m radius, 0.34 m tire width and 0.455 m axle drop
follow the source geometry. It weighs 16 kg and supplies 1.15x traction
through R6. The knuckle follows MOV-011 on supported four-wheel vehicles;
the tire collider remains fixed as in existing MOV-011 integration. Its
steering forces remain in R6, and it uses the normal wheel purchase price.

- Before integration test failed at `30b0268a39d3a33f53680e69897ca11e116e8a36`:
  `RC2-LargeSteerRed-20261108T090603Z`.
- Unity 6000.3.14f1 native EditMode runner, including tests entering Play mode:
  **6/6 PASS**, exit 0 at `0c057c254195e200eb5b246c22c6376e72d56db3`.
  Includes catalog, geometry, R6/Rigidbody drive and existing steering model.
  Evidence `RC2-LargeSteer-20261108T090832Z`.
- Garage mounting, checkpoint and actual MOV-011 binding for both steering
  wheels: **3/3 PASS**, exit 0 at `9491a99682c9226240adeafdb52c285549876149`.
  Evidence `RC2-LargeSteerGarage-20261108T090947Z`, under `Artifacts/Local/`.
- Windows x64 build `e43b69ea`: Unity exit 0. ZIP extracted and Player launched;
  entered garage, selected and bought the wheel, exited normally. Manual
  mounting, MOV-011 driving, full campaign and standalone PlayMode suite:
  **NOT RUN**. Log: `Artifacts/Local/LargeSteerWheelPlayerBuild.log`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_Kola_Klocki_e43b69ea_Windows_x64.zip`
  is 45,762,617 bytes, SHA-256
  `064aa439b5f2957af63d2ba5c9e3b4135fa95229f2c9639590d5d28a394f4b4e`.

Buy the wheel in the garage, press U for the underside view, and mount its
upper face with adequate space. MOV-011 requires its own functional module
and a supported four-corner wheel layout for steering animation.
