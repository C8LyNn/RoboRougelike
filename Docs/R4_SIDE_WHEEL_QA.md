# Side mounted wheel visual — QA

Source: PR #1018, commit `fb6910c29acd07989f1ad2a36b8ec5709ebfc9ce`, `R4M_WheelM_Side_Offroad.fbx`, SHA-256 `8473be4d43262a104cc27d6d5fe334eb9b5922ad5b937033a9b902d21dbed41c`. Imported Unity 6000.3.14f1, with Socket_Mount, Rotor, GroundProbe and axle inspected in native importer.

Player, enemy and garage ghost reuse existing wheel physics/HP/colliders. The model is mirrored for the left side and its rotor aligned to the existing axle. This introduces one visual type; the remaining variants are not yet selectable or assigned distinct performance.

Native EditMode at `f4ca27413710b2eefa5bc79030070dd454a03380`: 20/20 passed, exit 0, including actual wheel propulsion, steering attachment, damage, detachment, ghost and enemy fallback. Local evidence: `Artifacts/Local/RC2-SideWheel-All-f4ca2741-20261107T224125Z/`.
Windows Player visual assessment: NOT RUN. Manual checking is needed for rotated custom mounts and terrain. The model selection currently infers left/right from the grid X coordinate; arbitrary build orientations are not yet verified.
