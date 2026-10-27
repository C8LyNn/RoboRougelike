# CHL-003 R1 acceptance and reproducible tests

## Executable right now without licensed Unity
`dotnet run --project Tools/B01/CHL003/Headless/CHL003.Headless.csproj --configuration Release`
compiles and runs EXACT `Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseCore.cs` .NET 8 (no surrogate). Covers actor/gate/finish identity, order, events once, wrong actor, multi-collider semantics at state-machine boundary, retry three times, invalid config/tick, time limit, pause, 800 seeded mixed 3/4/5-gate cases.
`python Tools/B01/CHL003/source_gate.py` checks serialized scene importer GUID, meta pairs and no duplicate GUID, assembly refs/test methods, real physics shape/trigger source, negative deleted-actor-check and scene-GUID mutations.
`python Tools/B01/CHL003/package_gate.py --verify-zip` after immutable manifest receipt verifies per-file SHA256/binary ZIP. These tests **cannot prove** Unity6 physx/ramp traversal or native player operation.

## Licensed native Unity mandatory (not executed by producer)
Open isolated demo in Unity6000.3.14f1 (URP), record actual compile Editor.log/exit and Test Framework:
- EditMode10 exact E01–E10, including single win/early finish/out of order/multi collider/foreign actor/retry/pause/fall/time limit/bad input.
- PlayMode6 exact P01–P06 real nonkinematic Rigidbody passes four ordered physical triggers and finish, physical obstacle solidity via Physics.ComputePenetration, actual fall/retry, Unity pause time, physically different rig dimensions/mass, foreign actor and three retries.
- Manual: 2 profiles Light/Heavy (real masses/collider footprints), 3 completed crossing attempts each, HUD and controls both input backends, at least 3 failed falls+retries, off-course/pause, no leaks. Record seed or static scene checksum, elapsed, checkpoint counts, fall/timeouts, bumper/wheel contact, screenshot/video, XML and actual process exits, all on pinned exact commit.
- If a test is unsupported or fails: actual FAIL/NOT_RUN, no masking, report to B01-20 before source freeze. No G1 without independent B01-60.

## Negative integration checks
Compare base `main` exact source SHA and isolated CHL branch; changed paths must be solely package + docs/tools/dedicated CI, no forbidden host world/garage/weapon/financial/ProjectSettings source. B01-40 proves target-root GUID policy and rollback in disposable checkout. No main merge/G2.
