@echo off
setlocal EnableExtensions EnableDelayedExpansion
title RogueMachine Pack 12 - Pipeline Safe Installer

set "SRC=%~dp0"
set "PROJECT="
if exist "%SRC%Assets" if exist "%SRC%ProjectSettings" set "PROJECT=%SRC%"
if not defined PROJECT if exist "%SRC%..\Assets" if exist "%SRC%..\ProjectSettings" set "PROJECT=%SRC%..\"
if not defined PROJECT if exist "%SRC%..\..\Assets" if exist "%SRC%..\..\ProjectSettings" set "PROJECT=%SRC%..\..\"

if not defined PROJECT (
  echo ERROR: Nie znaleziono glownego folderu projektu Unity.
  echo Wypakuj paczke do folderu projektu, Assets albo Assets\Pack.
  pause
  exit /b 1
)

for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"
set "ASSETS=%PROJECT%\Assets"
set "DEST=%ASSETS%\RogueMachine_Current"
set "STAGE=%TEMP%\RogueMachine_P12_%RANDOM%_%RANDOM%"
mkdir "%STAGE%" >nul 2>nul

echo [1/5] Staging Pack 12...
if not exist "%SRC%P12_PipelineSafeAutoCleanup.cs" (echo ERROR: Brakuje P12_PipelineSafeAutoCleanup.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeAutoCleanup.cs" "%STAGE%\P12_PipelineSafeAutoCleanup.cs" >nul
if not exist "%SRC%P12_PipelineSafeCamera.cs" (echo ERROR: Brakuje P12_PipelineSafeCamera.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeCamera.cs" "%STAGE%\P12_PipelineSafeCamera.cs" >nul
if not exist "%SRC%P12_PipelineSafeDamageable.cs" (echo ERROR: Brakuje P12_PipelineSafeDamageable.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeDamageable.cs" "%STAGE%\P12_PipelineSafeDamageable.cs" >nul
if not exist "%SRC%P12_PipelineSafeEnemyTurret.cs" (echo ERROR: Brakuje P12_PipelineSafeEnemyTurret.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeEnemyTurret.cs" "%STAGE%\P12_PipelineSafeEnemyTurret.cs" >nul
if not exist "%SRC%P12_PipelineSafeHUD.cs" (echo ERROR: Brakuje P12_PipelineSafeHUD.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeHUD.cs" "%STAGE%\P12_PipelineSafeHUD.cs" >nul
if not exist "%SRC%P12_PipelineSafeHierarchyValidator.cs" (echo ERROR: Brakuje P12_PipelineSafeHierarchyValidator.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeHierarchyValidator.cs" "%STAGE%\P12_PipelineSafeHierarchyValidator.cs" >nul
if not exist "%SRC%P12_PipelineSafePlayerBlock.cs" (echo ERROR: Brakuje P12_PipelineSafePlayerBlock.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafePlayerBlock.cs" "%STAGE%\P12_PipelineSafePlayerBlock.cs" >nul
if not exist "%SRC%P12_PipelineSafePlayerGun.cs" (echo ERROR: Brakuje P12_PipelineSafePlayerGun.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafePlayerGun.cs" "%STAGE%\P12_PipelineSafePlayerGun.cs" >nul
if not exist "%SRC%P12_PipelineSafeSceneBuilder.cs" (echo ERROR: Brakuje P12_PipelineSafeSceneBuilder.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeSceneBuilder.cs" "%STAGE%\P12_PipelineSafeSceneBuilder.cs" >nul
if not exist "%SRC%P12_PipelineSafeTankController.cs" (echo ERROR: Brakuje P12_PipelineSafeTankController.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeTankController.cs" "%STAGE%\P12_PipelineSafeTankController.cs" >nul
if not exist "%SRC%P12_PipelineSafeTargetBlock.cs" (echo ERROR: Brakuje P12_PipelineSafeTargetBlock.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeTargetBlock.cs" "%STAGE%\P12_PipelineSafeTargetBlock.cs" >nul
if not exist "%SRC%P12_PipelineSafeTracer.cs" (echo ERROR: Brakuje P12_PipelineSafeTracer.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeTracer.cs" "%STAGE%\P12_PipelineSafeTracer.cs" >nul
if not exist "%SRC%P12_PipelineSafeTurret.cs" (echo ERROR: Brakuje P12_PipelineSafeTurret.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeTurret.cs" "%STAGE%\P12_PipelineSafeTurret.cs" >nul
if not exist "%SRC%P12_PipelineSafeWeaponAnchor.cs" (echo ERROR: Brakuje P12_PipelineSafeWeaponAnchor.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeWeaponAnchor.cs" "%STAGE%\P12_PipelineSafeWeaponAnchor.cs" >nul
if not exist "%SRC%P12_PipelineSafeWheelVisual.cs" (echo ERROR: Brakuje P12_PipelineSafeWheelVisual.cs & pause & exit /b 2)
copy /Y "%SRC%P12_PipelineSafeWheelVisual.cs" "%STAGE%\P12_PipelineSafeWheelVisual.cs" >nul

echo [2/5] Removing Pack 04-12 source duplicates...
for %%P in (P04_*.cs P05_*.cs P06_*.cs P07_*.cs P08_*.cs P09_*.cs P10_*.cs P11_*.cs P12_*.cs) do (
  for /r "%ASSETS%" %%F in (%%P) do (
    if exist "%%~fF" (
      del /f /q "%%~fF" >nul 2>nul
      if exist "%%~fF.meta" del /f /q "%%~fF.meta" >nul 2>nul
    )
  )
)

echo [3/5] Removing generated material folders...
for %%D in (
  RogueMachineGenerated_P04_Trackframe
  RogueMachineGenerated_P05_BreachTest
  RogueMachineGenerated_P06_Sightline
  RogueMachineGenerated_P07_ReturnFire
  RogueMachineGenerated_P08_CleanBoot
  RogueMachineGenerated_P09_Groundlock
  RogueMachineGenerated_P10_StableRig
  RogueMachineGenerated_P11_OriginLock
  RogueMachineGenerated_P12_PipelineSafe
  RogueMachine_Current
) do (
  if exist "%ASSETS%\%%D" rmdir /s /q "%ASSETS%\%%D" >nul 2>nul
  if exist "%ASSETS%\%%D.meta" del /f /q "%ASSETS%\%%D.meta" >nul 2>nul
)

echo [4/5] Installing one clean Pack 12 set...
mkdir "%DEST%" >nul 2>nul
copy /Y "%STAGE%\P12_PipelineSafeAutoCleanup.cs" "%DEST%\P12_PipelineSafeAutoCleanup.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeAutoCleanup.cs
copy /Y "%STAGE%\P12_PipelineSafeCamera.cs" "%DEST%\P12_PipelineSafeCamera.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeCamera.cs
copy /Y "%STAGE%\P12_PipelineSafeDamageable.cs" "%DEST%\P12_PipelineSafeDamageable.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeDamageable.cs
copy /Y "%STAGE%\P12_PipelineSafeEnemyTurret.cs" "%DEST%\P12_PipelineSafeEnemyTurret.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeEnemyTurret.cs
copy /Y "%STAGE%\P12_PipelineSafeHUD.cs" "%DEST%\P12_PipelineSafeHUD.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeHUD.cs
copy /Y "%STAGE%\P12_PipelineSafeHierarchyValidator.cs" "%DEST%\P12_PipelineSafeHierarchyValidator.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeHierarchyValidator.cs
copy /Y "%STAGE%\P12_PipelineSafePlayerBlock.cs" "%DEST%\P12_PipelineSafePlayerBlock.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafePlayerBlock.cs
copy /Y "%STAGE%\P12_PipelineSafePlayerGun.cs" "%DEST%\P12_PipelineSafePlayerGun.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafePlayerGun.cs
copy /Y "%STAGE%\P12_PipelineSafeSceneBuilder.cs" "%DEST%\P12_PipelineSafeSceneBuilder.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeSceneBuilder.cs
copy /Y "%STAGE%\P12_PipelineSafeTankController.cs" "%DEST%\P12_PipelineSafeTankController.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeTankController.cs
copy /Y "%STAGE%\P12_PipelineSafeTargetBlock.cs" "%DEST%\P12_PipelineSafeTargetBlock.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeTargetBlock.cs
copy /Y "%STAGE%\P12_PipelineSafeTracer.cs" "%DEST%\P12_PipelineSafeTracer.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeTracer.cs
copy /Y "%STAGE%\P12_PipelineSafeTurret.cs" "%DEST%\P12_PipelineSafeTurret.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeTurret.cs
copy /Y "%STAGE%\P12_PipelineSafeWeaponAnchor.cs" "%DEST%\P12_PipelineSafeWeaponAnchor.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeWeaponAnchor.cs
copy /Y "%STAGE%\P12_PipelineSafeWheelVisual.cs" "%DEST%\P12_PipelineSafeWheelVisual.cs" >nul
echo COPY: Assets\RogueMachine_Current\P12_PipelineSafeWheelVisual.cs

echo [5/5] Cleaning staging...
rmdir /s /q "%STAGE%" >nul 2>nul

echo.
echo PACK 12 INSTALLED.
echo Otworz Unity i wybierz:
echo Tools ^> Rogue Machine ^> [12] Pipeline Safe ^> RESET SCENE
echo.
pause
