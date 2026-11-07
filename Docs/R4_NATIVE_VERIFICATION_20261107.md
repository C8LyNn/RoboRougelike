# R4 native verification — 2026-11-07

Target project: isolated `RoboRougelike-PlayQA-97f8`, Unity 6000.3.14f1. Source under native tests: `a6c75fbb4837c8871ef09c03408a7d511a7b969f`. Windows Player was the separately built and extracted `6711673fa389d2a2aa1b8ab101ff6d9cab366135`; it does not contain the later, inactive R4 block prototype. No new block shape is available for player use yet.

| Check | Result | Evidence |
| --- | --- | --- |
| Unity compilation | PASS, exit 0 | `Artifacts/Local/RC2-R4-Native-a6c75fbb-20261107T165258Z/Compile.log` |
| Catalog, bundle, mesh factory/cache, staged collider and definition adapter, targeted EditMode | PASS, 9/9, exit 0 | same directory, `EditMode.xml` |
| Existing BUILD001 physical hit/penetration, targeted PlayMode | PASS, 6/6, exit 0 | `Artifacts/Local/RC2-R4-Physical-a6c75fbb-20261107T165342Z/PlayMode.xml` |
| Imported R4 weapons/mobility art, targeted EditMode | FAIL, 107/111, exit 2 | `Artifacts/Local/RC2-R4-Art-a6c75fbb-20261107T165415Z/EditMode.xml` |
| Windows Player visual smoke, code `6711673f` | PARTIAL: menu, garage, test range, one shot and pause observed. On exit, log records a UnityPlayer crash. Exit code was not retained. | `C:/Users/kubap/Desktop/Prototypy/R4-visible-qa-6711673f-player.log` |
| Full gameplay, new shape physics, persistence, 24 rotations and stress | NOT RUN | Staged shapes remain disconnected. |

The four failed art tests examine raw imported Basic weapon yaw/muzzle and Basic/Wheel socket axes relative to their internal FBX roots. Model prefab-space socket checks and many runtime art tests passed, but these failures are real and are not a general approval of all orientations. Runtime adapters must not be confused with correction of the raw FBX coordinate system. More visual/orientation validation is required.

The initial stage test on `9ebfb7c24acb3cf989116affcf889a462c7e72b6` failed 1/8: it expected Unity's `attachedRigidbody` on a disabled collider. A separate actual Unity diagnostic (`00425c29e5f9cda0372eb9bfccb6fb5e5baa60dc`, 1/1 PASS) showed a disabled child collider returns null there, while the parent Rigidbody is present and the association returns when enabled. The corrected test keeps the initial disabled-state assertion, checks the actual parent, temporarily enables each staged collider to verify its actual Rigidbody, then disables it again. No production staging behavior was changed to obtain the passing result.

The Player log confirms `Crash!!!` in UnityPlayer after the automated UI session. The visible window did close, but this is not a clean-shutdown PASS. Earlier manual confirmations concern other builds and do not prove the cause or clean exit here.

Offline `GeometrySmoke` also compiles the project's actual `BuildSocketGeometry.cs` and adapter against identity-only Unity stand-ins: 30 structural definitions and 23 module-support definitions validated, with a partial Wedge rejected for a wheel and a full Cube accepted. Nonidentity poses deliberately throw in the stand-in; no rotation, Unity PhysX or production placement claim follows from this smoke test.
