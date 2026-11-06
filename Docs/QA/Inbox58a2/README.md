# Windows candidate 58a22184 and targeted tests — HOLD

Windows build source `58a22184662bc1829007481b61a84b1d450276ae`; Draft #937. Downloadable ZIP 39,663,834 bytes; local SHA256 `f010342fc1100e328bd044a7ebc55f76cc1278ebcfadb5aaa706ade581ae8360`. Build exit 0; extracted executable matches build. Player weapons/energy, API-controlled combat/rewards and cold save/load in separate processes exit 0. Three actual force-driven R3/R6 Player probes Light/Standard/Heavy exit 0 with contacts 4/6/8. Activities selected/settled through API, not human play or a full physical run. Offscreen images exclude IMGUI.

Focused.xml: 5/5 real Unity EditMode PASS on source working tree immediately before commit 58a22184, including both physical gun projectiles, six types of terrain for three starters, hover-to-wheels actual lift and keyboard controls. BeforeBalance.xml documents 4/5 before centering the guns, including repeatable 54.9-degree Standard peak on steps; afterward peak was 9.72 degrees with the same 45-degree limit. EarlierSuspension.xml records 21/23 tests on an earlier working state; one stale mass fixture and one terrain failure existed then. Do not add counts from overlapping runs. Fixture-results.xml: 19/19 PASS after additional test-only adjustments later committed as 4ab28e59; not a final full-suite result.

Full PlayMode 478/478 PASS is from prior 7e37e1e0 (see sibling Inbox7e37), not 58a22184 or 4ab28e59. The complete EditMode suite is not yet PASS. Several older fixtures, one B01-24 fixture, a 180-part construction scenario, and a full six-encounter physical pilot need further investigation. Human testing is NOT_RUN. No G1 or release approval.

Original hashes are recorded; logs redact local home paths and Unity session metadata. Screenshots are diagnostic render textures, not UI captures.
Later fixture review found that the MOVSeriesDriveTests helper directly placed gyro and booster on the Standard starter gun cells. Its apparent hover PASS is therefore not sufficient evidence for a valid Standard construction; the separate actual-garage hover test passed, and the invalid fixture is being corrected and retested.

Full PlayMode on exact commit 0738ac37c8a7251b6b03a7bcb8a0c9224d1b8491: 478/478 PASS, exit 0. GarageRange: 11/11 PASS on working state subsequently committed as 0738, including maximum 180-part physical garage build, real projectile, workshop repair/remount and damaged suspension support checks. Later commits may only update test fixtures; verify full suites at final SHA.

DriveFixture: 34/34 PASS on working state committed as fdf290a406cd93adfea1e3d73667f03d7352c9d7. Includes corrected non-overlapping drive/device assemblies, hover, tracks, special movement, camera and physical weapons. This retests the invalid Standard fixture described above. Full suites at fdf are running.

Full PlayMode on exact fdf290a406cd93adfea1e3d73667f03d7352c9d7: 478 passed, 0 failed, 0 skipped, exit 0. Full EditMode at this SHA is running; no final result yet.

An experimental virtual-WASD six-fight pilot was attempted in a separate worktree; Unity failed licensing initialization and crashed before reaching the test, exit -536870911, no XML. Status NOT_RUN, no game result. Pilot changes remain unverified and are not in fdf.


Full EditMode at exact fdf290a406cd93adfea1e3d73667f03d7352c9d7: 1386 PASS / 6 FAIL / 1 SKIP, exit 2. Full XML, sanitized compressed Editor log and original hashes included. Failures: B01-24 stale reflection signature; keyboard-device firing fixture; stationary automatic six-fight pilot lost all wheels at fight 4; enemy boundary movement; barrier contact; and W8 wheel fallback. The skipped case depends on an unresolved cover policy. Investigation and focused retests continue.

MovingPilot: 1/1 PASS, exit 0, all six fights including final with active AI and real projectiles. Source was fdf plus the included test-only patch: virtual WASD and directly positioned aim camera, not human gameplay or actual mouse movement. Rewards/garage actions use APIs; no enemy damage/HP APIs. The unchanged fdf full-suite stationary pilot still failed in fight 4. Do not conflate these results.

