# Testy na wyeksportowanych plikach GLB (numpy, bez Blendera)

Wynik: **67/67 PASS**. Unity: UNITY_NOT_RUN, Windows player: WINDOWS_PLAYER_NOT_RUN.

M1 file / budget     GLB + LOD1 load, triangle counts equal the Blender QA record, materials are the five paint zones
M2 envelope          rest-pose geometry inside the declared envelope (module: 0.52 m block, world trap: one cell footprint on the ground)
M3 contacts          every declared Port_* empty exists at its place; approved patch_test ruler on the exported triangles (LOD0 and LOD1)
M4 mount             Socket_Mount on the declared mount face, axis = face normal (modules only)
M5 neighbour gap     module placed on / under / beside an A_Sleek Cube in the mount direction: 30 mm grid gap kept, no intersection
M6 silhouette        orthographic front / side / top masks of everything above the shared mount plinth (z > -0.185 m);
                     a model FAILS if ALL three views overlap another model by IoU > 0.80
M7 animation         modules with movable nodes carry an animation in the GLB; node names match the manifest

| model | M1 | M2 | M3 | M4 | M5 | M6 | M7 | tris LOD0 / LOD1 | kontakty | najbliższa sylwetka (min IoU z 3 rzutów) |
|---|---|---|---|---|---|---|---|---|---|---|
| R4X_Battery | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3396 / 724 | 2 | RepairPulse 0.61 |
| R4X_EnergyRegenerator | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3260 / 684 | 2 | ChargeCapacity 0.69 |
| R4X_DrivePower | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2840 / 584 | 3 | HullBooster 0.69 |
| R4X_RepairPulse | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2944 / 472 | 1 | DamageBooster 0.69 |
| R4X_DirectionalShield | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1988 / 364 | 2 | DoubleShotOverdrive 0.52 |
| R4X_PointDefense | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2052 / 524 | 1 | DamageBooster 0.60 |
| R4X_StructuralScanner | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2388 / 308 | 3 | Blink 0.56 |
| R4X_ScannerPulse | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2024 / 722 | 1 | DamageBooster 0.62 |
| R4X_Booster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1628 / 356 | 5 | HullBooster 0.79 |
| R4X_JumpDrive | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1616 / 528 | 1 | IndependentSteering 0.65 |
| R4X_Blink | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2864 / 456 | 1 | StructuralScanner 0.56 |
| R4X_Gyroscope | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2788 / 580 | 1 | OffensiveTurretDeployer 0.49 |
| R4X_MagneticGrapple | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2756 / 548 | 2 | RapidFire 0.70 |
| R4X_Suspension | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3764 / 1116 | 2 | IndependentSteering 0.65 |
| R4X_IndependentSteering | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3840 / 1136 | 3 | HullBooster 0.67 |
| R4X_HoverEngine | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1820 / 908 | 1 | PersonalShield 0.69 |
| R4X_HullBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3464 / 344 | 4 | Booster 0.79 |
| R4X_DamageBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2372 / 644 | 1 | RepairPulse 0.69 |
| R4X_ReloadBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3764 / 1236 | 1 | RepairPulse 0.67 |
| R4X_MobilityBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2420 / 468 | 1 | ModuleAmplifier 0.53 |
| R4X_RepairBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1960 / 840 | 1 | ChargeCapacity 0.45 |
| R4X_CoreBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2968 / 472 | 2 | HullBooster 0.57 |
| R4X_Overdrive | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2144 / 848 | 1 | DamageBooster 0.63 |
| R4X_RapidFire | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2036 / 724 | 2 | DirectionalEMP 0.76 |
| R4X_Stealth | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 556 / 348 | 1 | DeployableShieldGenerator 0.77 |
| R4X_DirectionalEMP | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2780 / 994 | 2 | RapidFire 0.76 |
| R4X_AreaEMP | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3616 / 990 | 1 | Suspension 0.53 |
| R4X_PersonalShield | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2328 / 842 | 1 | EMPTrapDeployer 0.74 |
| R4X_DeployableShieldGenerator | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3644 / 644 | 3 | Stealth 0.77 |
| R4X_DoubleShotPassive | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2384 / 720 | 2 | DeployableShieldGenerator 0.68 |
| R4X_DoubleShotOverdrive | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3880 / 1432 | 3 | PersonalShield 0.58 |
| R4X_RechargePulse | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1552 / 428 | 1 | StructuralScanner 0.43 |
| R4X_ChargeRegenerator | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2596 / 1104 | 1 | ChargeCapacity 0.69 |
| R4X_ChargeCapacity | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1736 / 712 | 1 | EnergyRegenerator 0.69 |
| R4X_SalvageOptimizer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 744 / 392 | 1 | DrivePower 0.66 |
| R4X_CargoUplink | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1632 / 400 | 1 | RepairPulse 0.61 |
| R4X_RewardReroll | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2504 / 1000 | 1 | OrbAmplifier 0.56 |
| R4X_WeaponDurabilityBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3744 / 648 | 1 | ModuleDurabilityBooster 0.65 |
| R4X_MovementDurabilityBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3940 / 860 | 1 | ModuleDurabilityBooster 0.57 |
| R4X_ModuleDurabilityBooster | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 4076 / 844 | 1 | WeaponDurabilityBooster 0.65 |
| R4X_OrbAmplifier | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1748 / 716 | 1 | ChargeCapacity 0.64 |
| R4X_ModuleAmplifier | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3140 / 484 | 3 | GroundDroneDeployer 0.57 |
| R4X_DeployableAmplifier | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2532 / 548 | 1 | StructuralScanner 0.46 |
| R4X_Radar | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1528 / 528 | 1 | PointDefense 0.47 |
| R4X_Minimap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1916 / 572 | 1 | OffensiveTurretDeployer 0.49 |
| R4X_WeakPointScanner | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2328 / 848 | 1 | PointDefense 0.51 |
| R4X_ExplosiveTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2956 / 1124 | 2 | DeployableShieldGenerator 0.72 |
| R4X_EMPTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3072 / 1560 | 1 | PersonalShield 0.74 |
| R4X_KnockbackTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2804 / 880 | 2 | Battery 0.49 |
| R4X_SnareTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 4952 / 3512 | 1 | EMPTrapDeployer 0.50 |
| R4X_SlowTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3052 / 1324 | 1 | ReloadBooster 0.58 |
| R4X_BurnTrapDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2912 / 1372 | 1 | Stealth 0.66 |
| R4D_ExplosiveTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1416 / 852 | 0 | SlowTrap 0.73 |
| R4D_EMPTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1596 / 836 | 0 | ExplosiveTrap 0.51 |
| R4D_KnockbackTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 660 / 452 | 0 | ExplosiveTrap 0.67 |
| R4D_SnareTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1280 / 1024 | 0 | KnockbackTrap 0.50 |
| R4D_SlowTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 788 / 580 | 0 | ExplosiveTrap 0.73 |
| R4D_BurnTrap | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1032 / 684 | 0 | DamageBooster 0.55 |
| R4X_OffensiveTurretDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3560 / 840 | 1 | ScannerPulse 0.54 |
| R4X_DefensiveTurretDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3372 / 620 | 1 | DirectionalShield 0.46 |
| R4X_RepairTurretDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 4020 / 796 | 1 | ReloadBooster 0.48 |
| R4X_SupportTurretDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 3128 / 920 | 1 | SalvageOptimizer 0.57 |
| R4X_FlyingDroneDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2072 / 1196 | 1 | PersonalShield 0.37 |
| R4X_GroundDroneDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2120 / 552 | 2 | PersonalShield 0.68 |
| R4X_RampDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2788 / 484 | 1 | Minimap 0.45 |
| R4X_WallDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 2508 / 396 | 3 | DrivePower 0.58 |
| R4X_JumpPadDeployer | PASS | PASS | PASS | PASS | PASS | PASS | PASS | 1724 / 696 | 1 | EMPTrapDeployer 0.58 |
