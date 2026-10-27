## W17 required physical negatives — native Unity NOT_RUN
- EditMode E16–E18: real-source coordinate model accepts Light+Heavy full-hull forward crossings but rejects reverse, backside orientation, only-wheel edge grazing, before-center wheel hits, non-finite heights/coordinates, and off-aperture body.
- PlayMode P10–P12: real scene created by original bootstrap: airborne non-kinematic Rigidbody must NOT gain horizontal or steering velocity from held input, reverse CP0 crossing leaves checkpoint count zero, wheel-only grazing of CP0 leaves zero despite actual real OnTriggerStay, and real wheel ground rays allow body to accelerate by AddForce.
- Headless source checks include 6400 deterministic width/offset cases plus source mutation sentinels for bypass of forward-only and airborne thrust guards; these are NOT native Unity/PhysX.
- Full same-course 3x Light+3x Heavy P07 remains mandatory and native NOT_RUN; if traffic fails physical barrier/ramp/bridge, record FAIL, do not label acceptance.


# CHL-003 R1 acceptance and reproducible tests

## Executable right now without licensed Unity
`dotnet run --project Tools/B01/CHL003/Headless/CHL003.Headless.csproj --configuration Release`
compiles and runs EXACT `Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003CourseCore.cs` .NET 8 (no surrogate). Covers actor/gate/finish identity, order, events once, wrong actor, multi-collider semantics at state-machine boundary, retry three times, invalid config/tick, time limit, pause, 800 seeded mixed 3/4/5-gate cases.
`python Tools/B01/CHL003/source_gate.py` checks serialized scene importer GUID, meta pairs and no duplicate GUID, assembly refs/test methods, real physics shape/trigger source, negative deleted-actor-check and scene-GUID mutations.
`python Tools/B01/CHL003/package_gate.py --verify-zip` after immutable manifest receipt verifies per-file SHA256/binary ZIP. These tests **cannot prove** Unity6 physx/ramp traversal or native player operation.

## Licensed native Unity mandatory (not executed by producer)
Open isolated demo in Unity6000.3.14f1 (URP), record actual compile Editor.log/exit and Test Framework:
- EditMode18 exact E01–E15, including single win/early finish/out of order/multi collider/foreign actor/retry/pause/fall/time limit/bad input.
- PlayMode12 exact P01–P09 real nonkinematic Rigidbody passes four ordered physical triggers and finish, physical obstacle solidity via Physics.ComputePenetration, actual fall/retry, Unity pause time, physically different rig dimensions/mass, foreign actor and three retries.
- Manual: 2 profiles Light/Heavy (real masses/collider footprints), 3 completed crossing attempts each, HUD and controls both input backends, at least 3 failed falls+retries, off-course/pause, no leaks. Record seed or static scene checksum, elapsed, checkpoint counts, fall/timeouts, bumper/wheel contact, screenshot/video, XML and actual process exits, all on pinned exact commit.
- If a test is unsupported or fails: actual FAIL/NOT_RUN, no masking, report to B01-20 before source freeze. No G1 without independent B01-60.

## New native acceptance tests (authored, NOT_RUN)

`E11–E13`: both CP-last/Finish orders at one tick, early Finish, overlap-loss, pause and Retry; `E14–E15`: independently recount actual contact metrics and reset Heavy. `P07`: instantiate the **actual production CHL demo bootstrap** in Unity6, drive Light and Heavy three attempts each on identical generated geometry via Rigidbody `AddForce`, verify gate4/finish and original collision/ramp/bridge samples. `P08`: move actual finish trigger over CP4 and verify Unity `PhysicsStep` of Completed is strictly later than CP4; one completion only. `P09`: pause must freeze Rigidbody and clock, actual fall must fail, Retry must clear references/counters and never inherit stale win. These require real Unity/PhysX and are NOT_RUN by producer. No direct method injection is a replacement for collision-trigger tests.

## Negative integration checks
Compare base `main` exact source SHA and isolated CHL branch; changed paths must be solely package + docs/tools/dedicated CI, no forbidden host world/garage/weapon/financial/ProjectSettings source. B01-40 proves target-root GUID policy and rollback in disposable checkout. No main merge/G2.
