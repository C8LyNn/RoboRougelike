# RC1 reproducible checks

Run from the repository root with Unity6000.3.14f1 installed at its standard Windows path. These are receiver tools for this pinned RC1 cycle.

- `Run-RC1Native.ps1 -Variant Experimental -Filter 'MapgenRC1IntegrationTests;B01.MAPGEN001.Tests;B01.MAPGENWORLD001.Tests' -FilterEditOnly`: compile, release+QA builds, complete35package/host Edit and full423Play at86eb7bb. Omit the filter for the larger legacy Edit suite. BASE requires a separate checkout at exact4c9236a.
- `Run-RC1Player.ps1`: actual native process exit, source and launch receipts; `-Source` is mandatory. Interactive monitoring never closes the app automatically. The separate QA binary accepts `-rc1-native-output <path>` or `-obj001-native-output <path>`; retail does not contain the new fixture.
- `verify_rc1_sources.py`: original producer bytes, ancestry, GUIDs and protected paths; not gameplay certification.
- `Rollback-RC1.ps1 -Repo <checkout> -Target <NEW directory>`: creates an isolated rollback branch/worktree, reverts five RC1 game commits, validates Assets/Packages/ProjectSettings against exact BASE. Never overwrites the original checkout. Actually tested; no new Unity run claimed after rollback.
- `package_player.py`: complete Player ZIP, CRC and byte-for-byte extracted-file check; refuses existing output. The final experimental archive is in Releases/B01_RC1_20261025/Final.
- `collect_rc1.py --require-complete` and `write_final_report.py`: task-specific evidence collection/report reproduction, requiring the original local run/Intake/Release paths. Preserve earlier binary audits across later builds.
- `stage_world.py`: receiver journal import for B01-21's documented manifest format unsupported by PACKAGE-AUTO; exact ZIP/source pin and owned paths only. Original toolkit at402863b and producer verifier were used without modification.

Successful standalone checks are not extra NUnit tests or human victories. Full pre-guard1055/423 at8ac3a81, final35/423 at86eb7bb, shutdown failures and unperformed gates are explicitly separate in the report. No merge/G2 command is provided.
