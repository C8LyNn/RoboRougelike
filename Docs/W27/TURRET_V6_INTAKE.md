# W27: turret v6 intake — native FBX axis mismatch

Date: 2026-11-05. Isolated QA project based on W27 `b54f569b7faec50df82211d3a146e6975d9e1774`; Unity 6000.3.14f1. Original user files were not edited, and the production prefab was not replaced.

- Source FBX SHA-256 `ec45cb86a9020861c2af0c3d7985af8df490e5314c279f5dfc328f3178d35686`; GLB `f744752600b58e75921196c4bed5fc0d22de62d203770fe7daf2305ab604aeec`.
- GLB structural inspection: 11,452 triangles, zero triangles under the selected numerical degeneracy threshold; hierarchy and six animation names exist. This does not validate collider geometry or gameplay.
- Native FBX inspection **PASS for import**, process exit 0; axis compatibility **FAIL**. With `bakeAxisConversion=true`, muzzle left/right positions are (-0.06, 0.135, 0.7) and (0.06, 0.135, 0.7), but both muzzle `Transform.forward` vectors are (0, -1, 0), while intended firing direction is +Z. Central muzzle position is (0, 0, 0.565); imported local yaw Y rotates in the wrong plane. Default conversion also misaligns positions/axes. Direct use in the game is unsafe.
- Isolated wrapper probe **NOT_RUN**: Unity repeatedly failed to connect to the local licensing service, so its owned editor process was stopped; exit -1. This is not evidence of adapter pass or failure. Full Player animation, recoil, aiming, collision/functional sweep and firing remain **NOT_RUN**.
- Evidence: `C:/Users/kubap/Desktop/Prototypy/RoboRougelike-W27-Saw/Artifacts/Local/W27/TurretV6/` (`unity-baked.txt`, `unity-default.txt`, `Editor.log`, `exit-code.txt`, `adapter-Editor.log`, `adapter-exit-code.txt`, `glb-inspection.txt`). Original FBX and QA scripts were preserved under `ImportedAsset`; no source file deleted.
- Producer handoff: https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6663283052 and https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6663283492. Require corrected export or independently verified rig adapter (both muzzles, central origin, yaw/pitch and recoil) plus native Player tests before installation.

## Save intake update

[#923](https://github.com/C8LyNn/RoboRougelike/pull/923) exact `e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b` has source-only G0 evidence from [#929](https://github.com/C8LyNn/RoboRougelike/pull/929). Independent [#927](https://github.com/C8LyNn/RoboRougelike/pull/927) reports a reproduced same-ID/HP `Core` versus `Armor` world/economy mismatch accepted on this exact source. Full game G0, owner schema/migration decision and original live R3 physical capture/restore binder remain open. Native Unity cold save/load is NOT_RUN. No Save code was copied into W27.

### Follow-up: recoil hierarchy and licensing retry

Static GLB channel inspection shows `Fire_L_Recoil` / `Fire_R_Recoil` animate the individual barrel translations. The two muzzle transforms and the flash transforms are siblings of those barrels under `R41_Barrels`; they do not inherit that translation. A fixed firing origin can be intentional, so visual and shooting alignment require an explicit contract and a native animation test rather than a presumed defect. Evidence: `glb-animation-targets.txt`; producer follow-up https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6663435924.

A single retry after the license client reappeared also failed to connect after 60 seconds (`adapter-retry-Editor.log`), and that owned editor was stopped (exit -1). The temporary copied asset and script were preserved under `RetryImportedAsset`; the user's original remained untouched. A further native attempt used access to the existing local licensing service with below-normal priority; see the result below.

### Follow-up: native transform adapter feasibility

A subsequent isolated Unity 6000.3.14f1 run with access to the local licensing IPC completed with exit 0: all 12 neutral-geometry combinations of yaw (0, 45, -90, 180 degrees) and pitch (0, -20, 10 degrees) matched left/right exported muzzle positions to the 0.000001 m reporting precision and expected wrapper forward to 0.000001 degree reporting precision. This validates only the test wrapper's transform geometry. The wrapper is not integrated; recoil playback, functional zones, weapon binding, colliders and Player firing remain NOT_RUN. Evidence: `adapter-probe.txt`, `adapter-native-Editor.log`, `adapter-native-exit-code.txt` and test-only script preserved in `NativeImportedAsset/Editor/W27TurretV6AdapterProbe.cs`. Handoff: https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6663489272.
