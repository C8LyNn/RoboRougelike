# R4 backlog native verification — 2026-11-07

Tested code SHA: `a3d37d30f971c651d3a5915d66a48551dab023a5`, incorporating
the stacked drafts through #1025. Unity `6000.3.14f1`, Windows x64.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity project compilation | PASS | process exit 0; Compile.log SHA-256 `d6b4b5645be650f85d184e14d21c64f740dcd139a4870881cc6859c6e48835e5` |
| Targeted Unity EditMode | PASS | 25/25, 0 failures, 0 skipped; exit 0; XML SHA-256 `a4c6b691be50ccdea9e2ab058d5db11c887cae49592476f9d177e75528df3615` |
| Targeted Windows build | PASS | build exit 0; Unity reports 0 errors and 12 warnings |
| ZIP extraction | PASS | executable hash matches build output; Unity data, runtime and player DLL present |
| Extracted Windows Player API fixture | PASS | exit 0; JSON SHA-256 `f3b4b5ad9b5dbb77ff421adfdccd13de64bca512d5e39e19ef9cda190f6a5c85` |
| Human gameplay / visual acceptance | NOT RUN | batch fixture is API-driven |
| Full EditMode / standalone PlayMode regression | NOT RUN | targeted suite only |
| Interactive close after UI automation | NOT RUN | a prior automated interactive Player exit crashed; no cause established |

The targeted Unity result comprises `BacklogEnemyWheelTierTests` 3/3,
`R4EnemySawEmpArtTests` 4/4, `R4MountDefinitionAdapterTests` 6/6 and
`RRLW6EmpPhysicalTests` 12/12. The EditMode suite includes tests that enter
actual Play Mode; it is not a complete standalone PlayMode run. It verifies,
among other things, actual saw contact, EMP disruption, model switching,
salvage after expiry including T3 wheel/gun identity, and rotated socket
filtering in the staged adapter.

The Windows batch fixture entered combat and checked two enemies/eight physical
wheels, detached wheel art, fallback geometry, normal and elite weapon art,
projectile hits, shotgun, railgun, rocket, beam, saw and EMP. It uses API-driven
fixtures, including explicitly advanced weapon simulation; it is not a manual
playthrough. Player.log SHA-256 `d037a2e35de99cb158972743a7148c37e1f54ac3fd4aa388f5028ccf75036284`;
no exception/error/crash text was found in this batch log. Exit 0 here does
not establish a fix for the historical interactive automated-exit issue.

Artifact:
`C:\Users\kubap\Desktop\Prototypy\RoboRougelike_R4_EnemySawEmp_a3d37d30_Windows_x64.zip`
(43,544,455 bytes), SHA-256
`9a63e136ae58e56e46ab40b6d28570fc5e305a9c502efc4103176906695f8c1b`.
Extract the whole archive before running `RoboRougelike.exe`.

Local raw evidence lives in `Artifacts/Local/` under
`RC2-R4-Backlog-Resume-a3d37d30-20261107T211545Z`,
`R4-EnemySawEmp-Build-a3d37d30`, and `R4-EnemySawEmp-Player-a3d37d30`.
The unmodified [EditMode XML](QA/R4-backlog-20261107/EditMode.xml) and
[Windows Player result](QA/R4-backlog-20261107/WindowsPlayer.json) are included
in the report branch. Full Editor, build and Player logs remain local.

**Remaining blockers to shaped-block gameplay:** adapter not connected to the
canonical garage, physics, save/load or damage graph; four source socket
surface discrepancies require producer review, and four raw FBX orientation
checks previously failed. The 29 adapter definitions that remain available
are not thereby approved for gameplay. Automatic gun art has no source model.
No merge to `main` or `develop` was performed.
