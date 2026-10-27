# R1 validation plan and evidence standards

Producer CI requires: dotnet8 compiles real `PartSizeContract.cs`+`PartSizeBaselineCatalog.cs`, rotates 2x1x2 footprints in XYZ with negative and >360° turns, preserves unique cells and 0.55m metre conversion, rejects invalid NaN/Inf/0/negative, overlong hull, bad input and false physical measurement; plus reproducible source ZIP, CRC, SHA256, GUID/.meta/in-scope checks and negative archive tamper tests. These are **real source/contract tests**, not Unity physics.

Native Unity6000.3.14f1 operator (currently **NOT_RUN**) must independently record:
- EditMode `PartSize001EditModeTests`: **17 test cases**, including 7 parameterized quarter turns and 10 standalone contract/physical-negative method cases. Assert actual NUnit XML / Editor.log / process exit and no fake skipped/missing cases.
- PlayMode `PartSize001PlayModeTests`: **8 tests**, actual `BoxCollider`, `Physics.SyncTransforms`, `Physics.ComputePenetration`, valid adjacent legacy cubes, positive rotated wide, excessive hull, penetration, missing-full-neighbor inventory, decorative physical omission, physically measured preview demo.
- Demo: standalone prefab in disposable blank URP scene; **2 valid +1 rejected** green/green/red before any R3 integration. Screenshot actual Play 720p/1080p, camera and colliders visible, no magenta or renderer errors. If visual fails, native FAIL, not source PASS.
- Integration compatibility (later, only after permitted by HQ): actual existing saved 1x1x1 and 2x1x2 R3 owner IDs, 180-cell cap and six mounting faces remain unchanged; rotated wide 0.55m ownership against real collider; movement/weapon parts cannot be certified until their *actual* sweeps are measured in native Unity.
- 2 independent witness layers: B01-40 inspect real ZIP/source/rollback and B01-60 execute independent native matching SHA. Codex integration G2 only after exact authorization.

**Known potential weaknesses to test explicitly:** non-convex mesh colliders fail closed, 0.005m penetration tolerance, root scale≠1 rejected, world-unrotated coordinate frame assumption, overlapping two parts without complete full external broadphase denied, and inconsistent Unity Quaternion.Euler(ZXY) versus portable pure C# rotations (XYZ mixed orientations).
