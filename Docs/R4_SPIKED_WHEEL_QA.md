# Spiked fork wheel

Based on Draft PR #1049. Source #1018 `R4M_WheelM_Fork_Spiked.fbx`,
SHA-256 `5c2c72e88de7a75e1ad8579637bf48cfe6537ea3f9e340b000c0bbcd43b8f911`.
The source rotor and upper mounting socket align to the physical tire and
0.55 m grid: radius 0.37 m, width 0.30 m and axle drop 0.24 m. The wheel
uses the existing R6 drive, grounded traction, collision and persistence;
mass is 12 kg. The spikes are currently decorative and deal no damage.

- Missing-feature test at `c570b8c6dab8b9438d213ce0716e6062ab5d5787` failed
  as expected because the new wheel could not be purchased. Evidence:
  `Artifacts/Local/RC2-SpikedWheelRed-20261108T081021Z`.
- Unity 6000.3.14f1 EditMode runner, including tests entering Play mode,
  **23/23 PASS**, exit 0, at `5cbb782109e363f653f9445345ab6852fa506c83`.
  It covers catalog, existing variants, rotated sockets and actual
  R6/Rigidbody driving: 9.37 m in 1.6 s. Evidence:
  `Artifacts/Local/RC2-SpikedWheel-20261108T081318Z`.
- Actual garage installation, original model and checkpoint restoration:
  **1/1 PASS**, exit 0, at `dd5e28749422aa384c9bfdc9d22e3d8542e1be66`.
  Evidence: `Artifacts/Local/RC2-SpikedGarage-20261108T081512Z`.
- Windows x64 build `db69ea92` exit 0; ZIP extracted, Player launched,
  entered garage and bought the spiked wheel; normal menu exit. Manual
  installation in Player, full campaign and standalone PlayMode suite:
  **NOT RUN**. Build log: `Artifacts/Local/SpikedWheelPlayerBuild.log`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_Kola_Klocki_db69ea92_Windows_x64.zip`
  is 45,134,259 bytes. SHA-256
  `1f95e082160c6390be1445d5e9353c9d8d7b0103f810fa24e9f67500fbb554db`.

Purchase the wheel in the garage catalog. Press U to view under the vehicle
and mount it by its upper socket, with sufficient clearance for the tire.
