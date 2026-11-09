# Zaplanowana regresja Unity — 9 listopada 2026

Kod testowany: `b560d710bacd9926222f1f2997bb41ac0c913107` (Draft PR #1151), Unity 6000.3.14f1 Windows, batchmode/nographics. Repozytorium było czyste przed uruchomieniem; automatycznie zserializowany przez Unity plik TimeManager przywrócono do stanu HEAD po testach.

- **EditMode: FAIL** — 1931 przypadków, 1894 PASS, 36 FAIL, 1 SKIP, exit 2; 08:01:27–10:28:18 czasu polskiego.
- **PlayMode: PASS** — 478/478, 0 FAIL, 0 SKIP, exit 0; 10:28:18–10:31:10 czasu polskiego.
- Kompilacja Windows Playera, rzeczywisty Alt+F4/X, ręczny gameplay i ocena obrazu: **NOT RUN**. Nie tworzono buildu; istniejące starsze buildy nie potwierdzają testowanego SHA.
- Podczas analizy wyników nie rozstrzygnięto dla wszystkich błędów, czy dotyczą aktualnej gry, nieaktualnych oczekiwań testu czy środowiska. Żaden nie jest automatycznie naprawiony ani unieważniony. Pełna regresja EditMode nie przeszła.
- NUnit XML, oba pełne Editor.log i JSON-y procesów pozostają lokalnie w `Artifacts/Local/ScheduledFullRegression-b560d710-20261109/`. W testach map używane są losowe nazwy plików i sprzątane pliki testowe; testy checkpointu posiadają osobne ścieżki. Unity Test Framework zapisuje także własne `TestResults.xml` w katalogu danych aplikacji.
- SHA-256 XML: EditMode `e7f82434aff9e2759ecebf90fe77d9d0f39c97e48a9531f717cc7daee1d62f50`; PlayMode `2e65af7cde67ebcac1ae8ad7247e67a464316ea2768774b6d21e3b5ed257ba2b`.

## Nieudane przypadki EditMode
- `B0124EnemyAiNegativeEditModeTests.VariantWeaponUpdate_InvalidatesInactiveCachedTarget`
- `BacklogEnemyWeaponTierTests.RealEnemyShotgunUsesTierForEnergyDamageDurabilityAndSalvage`
- `FullMergeBuildTests.DirectMountCannotMoveInventoryOntoGunSupport`
- `FullMergeBuildTests.ForeignPhysicalObstacleRejectsGridValidPlacementWithoutSpending`
- `FullMergeBuildTests.StructuralFaceRemainsClickableAfterSuccessfulGarageRebuild`
- `GarageEditFreedomTests.DestroyedGunOrOnlyOneWheelSideCannotDeploy`
- `MOV009IntegrationTests.GarageRebuildAndRangeReturnPreserveElevatedThreeDimensionalPose`
- `MOVSeriesGarageTests.AllOriginalPartsSettingsInventoryAndResourcesSurviveRangeDamageResetAndReturn`
- `MOVSeriesGarageTests.PublicActionsRejectGarageTransitionPauseAndDeadDevicesWithoutResettingCooldown`
- `MOVSeriesSpecialTests.EmitterDeathRepairAndSecondEmitterCannotResetVehicleCooldown`
- `R1PlayIntegrationTests.KeyboardUpgradeChoiceWaitsForNewVehicleInitialization`
- `R1PlayIntegrationTests.PaidGarageRestoresConstructionAndKeepsEarnedDamageBonus`
- `R1PlayIntegrationTests.SixEncountersCanBeWonWithActualPlayerGunAndPhysicalHits`
- `R3GaragePlayTests.AllThreeVehiclesMoveFireTakeDamageAndReset`
- `R3GaragePlayTests.DisarmedEnemyRetreatsWhenBothBoundaryTangentsAreBlocked`
- `R4CombatTests.AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage`
- `R4CombatTests.RecoveredGunPhysicallyHitsNextEnemyForEveryStarterAndSurvivesWholeRun`
- `R4ModelIntakeTests.AllFortyNineFbxModelsArePresentAndImported`
- `R4ModelIntakeTests.BasicWeaponHasForwardMuzzleAndSeparateAimNodes`
- `R4ModelIntakeTests.BasicYawUsesTheUnityVerticalAxis`
- `R4ModelIntakeTests.ImportedSocketMatchesUnityMountingGrid("Assets/Art/R4Intake/Weapons/R4W_Basic/R4W_Basic.fbx","R4W_Basic_ROOT","Socket_Mount_Bottom",-0.26f)`
- `R4ModelIntakeTests.ImportedSocketMatchesUnityMountingGrid("Assets/Art/R4Intake/Movement/R4M_Wheel/R4M_Wheel.fbx","R4M_Wheel_ROOT","Socket_Mount",0.26f)`
- `R4ModelIntakeTests.MountSocketPositionInPrefabSpaceMatchesGrid("Assets/Art/R4Intake/Movement/R4M_WheelL_Side_Offroad/R4M_WheelL_Side_Offroad.fbx")`
- `R4ModelIntakeTests.MountSocketPositionInPrefabSpaceMatchesGrid("Assets/Art/R4Intake/Movement/R4M_WheelM_Side_Offroad/R4M_WheelM_Side_Offroad.fbx")`
- `R4ModelIntakeTests.MountSocketPositionInPrefabSpaceMatchesGrid("Assets/Art/R4Intake/Movement/R4M_WheelM_Side_Slick/R4M_WheelM_Side_Slick.fbx")`
- `R4ModelIntakeTests.MountSocketPositionInPrefabSpaceMatchesGrid("Assets/Art/R4Intake/Movement/R4M_WheelXL_Side_Monster/R4M_WheelXL_Side_Monster.fbx")`
- `R4ModelIntakeTests.MountSocketPositionInPrefabSpaceMatchesGrid("Assets/Art/R4Intake/Weapons/R41Automatic/R41_Turret_v6.1.fbx")`
- `R5ArenaTests.SolidLayoutsHaveSafeSpawnsAndAiTraversesIndustrialCover`
- `R6DrivingTests.PhysicalMassSetsAccelerationAndBrakingWhileReverseRemainsUsable`
- `RewardRC2IntegrationTests.LiveVictoryUsesOneKeep3WindowAndOriginalAtomicSettlement`
- `RRLW2ModelTests.DestroyedInventoryCannotMountUntilPaidRebuildAndCannotChargeTwice`
- `RRLW6SawPhysicalTests.ActualSawCutsOneLogicalPartJamsOnHardWallAndPreservesHeat`
- `RRLW7CoverTests.ActualPlayerProjectileDamagesOnlyContactedCover`
- `RRLW8SteeringTests.TwelveWheelLayoutAndAlternateDriveKeepFallbackWithoutCompetingSolver`
- `W27GarageOccupiedZonesTests.OccupiedGridIsVisibleAndTracksActualGarageConstructionWithoutColliders`
- `W27GarageOccupiedZonesTests.WheelGhostDistinguishesItsProtrudingGeometryDuringRotation`

## Pominięte

- `B0124EnemyAiNegativeEditModeTests.SolidOpaqueCover_SuppressesShot_ThenRecovers_ConditionalPolicy`: Enable only after HQ/B01-10 approves opaque solid cover suppressing enemy shots.
