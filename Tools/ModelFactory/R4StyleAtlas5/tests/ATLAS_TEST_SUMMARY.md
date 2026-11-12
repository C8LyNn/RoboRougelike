# Atlas: testy na wyeksportowanych GLB (numpy)

Wynik: **120/120 assetów z kompletem 5/5 stylów PASS** (600 wariantów). Unity: UNITY_NOT_RUN.

T1 files      GLB + LOD1 GLB load; triangle counts equal the Blender QA record; LOD1 <= LOD0; materials are the five paint zones
T2 envelope   rest-pose geometry (FX nodes excluded) inside the declared envelope
T3 swap       the five styles of one asset expose IDENTICAL node names, hierarchy and socket / port empties (position dev < 0.05 mm)
T4 animation  every variant carries animation channels in the GLB for each animated object of its QA record
T5 texture    styles with a detail map (A, B, D, E) embed it in the GLB and keep the paint colour as a factor; C embeds none
T6 different  the five meshes of one asset are really different geometry (no two styles share a vertex-set hash)

| asset | T1 A-E | T2 | T3 | T4 | T5 | T6 | wynik |
|---|---|---|---|---|---|---|---|
| R4B_Arch | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Beam2 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Beam3 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_BigCube2 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Cone | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_CornerL3 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_CornerWedge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_CoveEdge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Cube | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Cylinder | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Dome | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_HalfBlock | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_HalfWedge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_HexColumn | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_InnerCorner | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_LongWedge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_OctColumn | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Plate1x2 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Plate2x2 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Pyramid | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_RoundCorner | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_RoundEdge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_RoundTop | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Slab2x2 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Step | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Tee4 | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Tetra | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_TrussCube | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_Wedge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4B_WideWedge | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_ARC_LONG | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_ARC_SHORT | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_CROSS | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_DIAG_2D_LONG | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_DIAG_2D_SHORT | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_DIAG_3D | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_FRAME | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_LONG | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_PLUS | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_SHORT | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_STANDARD | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_ROD_T | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_ARC | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_CROSS | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_DIAG_2D | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_DIAG_3D | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_LONG | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_RAMP | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_SHORT | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_STANDARD | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_BurnTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_EMPTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_ExplosiveTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_KnockbackTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_SlowTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_SnareTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4M_HoverEngine | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4M_WheelM_Side_Offroad | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4W_Basic | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_AreaEMP | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Battery | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Blink | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Booster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_BurnTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_CargoUplink | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ChargeCapacity | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ChargeRegenerator | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_CoreBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DamageBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DefensiveTurretDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DeployableAmplifier | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DeployableShieldGenerator | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DirectionalEMP | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DirectionalShield | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DoubleShotOverdrive | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DoubleShotPassive | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_DrivePower | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_EMPTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_EnergyRegenerator | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ExplosiveTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_FlyingDroneDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_GroundDroneDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Gyroscope | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_HoverEngine | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_HullBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_IndependentSteering | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_JumpDrive | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_JumpPadDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_KnockbackTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_MagneticGrapple | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Minimap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_MobilityBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ModuleAmplifier | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ModuleDurabilityBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_MovementDurabilityBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_OffensiveTurretDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_OrbAmplifier | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Overdrive | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_PersonalShield | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_PointDefense | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Radar | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RampDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RapidFire | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RechargePulse | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ReloadBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RepairBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RepairPulse | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RepairTurretDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_RewardReroll | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_SalvageOptimizer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ScannerPulse | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_SlowTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_SnareTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Stealth | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_StructuralScanner | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_SupportTurretDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Suspension | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_WallDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_WeakPointScanner | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_WeaponDurabilityBooster | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
