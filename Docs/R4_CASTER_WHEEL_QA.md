# Caster wheel

Based on Draft PR #1054. Source #1018 `R4M_WheelS_Caster_Slick.fbx`, SHA-256
`063848ebbed93767e2f4cd868de7e16c7e87cc6972e4d102b5d2553b9db9e98e`.
Top mount; radius 0.17 m, width 0.12 m, axle drop 0.13 m and trail 0.075 m.
The physical tire and authored swivel turn together toward the reverse of the
chassis point velocity in physics updates. It uses the existing R6 solver,
4 kg mass and 0.5x traction; movement speed/weight are initial tuning. It is
not controlled as a powered steering knuckle by MOV-011. When nearly still,
it holds the last direction. The existing wheel price applies.

- Original missing-feature test failed at `29b1ab06`; the first implementation
  exposed a real imported-axis mismatch. Early fixture runs also needed an
  isolated location and unpaused physics. Corrected axis and actual-time
  alignment test **1/1 PASS** at `b9fe3c3d444d88ed4c8a90c0c3b7b1f265ed266f`:
  `Artifacts/Local/RC2-CasterAligned-20261108T092917Z`.
- Targeted actual garage mounting, checkpoint, R6/Rigidbody driving and wheel
  catalog **3/3 PASS** at `f271005d4a64601ee9bf90436183dabb9419772a`:
  `Artifacts/Local/RC2-CasterGameplay-20261108T093014Z`.
- Real Windows Player, manual driving and full regression: **NOT RUN** at this
  SHA; planned after combining the outstanding work into one build.
