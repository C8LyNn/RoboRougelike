# MAPGEN-001 R3.4 — W12 RED / SOURCE FIX / NATIVE RETEST PENDING

## Proven immutable native RED (external Codex, not B01-60 execution)
- Source R3.3 PR #225: `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`.
- Native QA PR #232 exact `b80faa121e4022d772caa3b93d8b2a482d9a53af`, logs/XML in `Docs/MAPGEN001R33NativeQA/**`.
- Unity 6000.3.14f1 compilation PASS exit0, EditMode 24/24 PASS exit0.
- PlayMode 6 PASS / 12 FAIL exit2. Arena/Course × seeds 7/31/83 × Light/Heavy **each** stalls before first waypoint after 900 FixedUpdates. Horizontal displacement approximately 0.55–0.56m and terminal speed 0.003m/s. First/last positions and per-case XML are in QA #232.
- Separate *minimal* standalone Windows URP Player built successfully but generating **both recipes** threw `MAPGEN001_URP_SHADER_NOT_AVAILABLE`. Editor `Shader.Find` worked, which does not guarantee build inclusion. No valid Player map rendering/screenshot was produced.
- Actual R3.3 wheeled R6 12-case matrix NOT_RUN. Earlier R3.2 mounted-wheel trials 12/12 base + 7/7 historical Heavy PASS apply only to R3.2.

## ROOT_CAUSE classification / confidence
1. **PROVEN design flaw in material dependency:** R3.3 `GetGeneratedBlockMaterial` only calls `Shader.Find("Universal Render Pipeline/Lit")` and fallback Simple Lit. A Player may strip shaders not referenced by serialized assets; minimal Player failed those lookups. This is a producer package-retention defect, not generator geometry.
2. **CUBE HARNESS high-probability contact/control defect, not proved physics-root cause:** same ~0.56m stall for all 12 independent configurations despite different recipe/seeds and correct safe route geometry. Cube uses its *own* primitive BoxCollider, default PhysicsMaterial/friction, gravity, yaw frozen X/Z, 7m/s² acceleration and 0.35 damping. Which contact impulse physically counteracts longitudinal force was **not logged** in R3.3. No evidence justifies altering planner, collision seams, R6 physics or increasing timeout. New R3.4 adds a zero-friction disposable cube collider material and independent flat control, plus measured floor ray counts, planar peak velocity, nearest goal distance and heading. This isolates test rig traction while retaining actual real Rigidbody mass/gravity/AddForce/torque/colliders; no teleport/transform-driven progress. **Real native confirmation pending.**

## R3.4 actual SOURCE FIX in isolated Draft PR #235
- `Assets/B01Packages/MAPGEN001/Runtime/MapGenBuilder.cs`: remove runtime shader name lookups, load `Resources/MAPGEN001_BlocksURP.shader` from the package and cache shared runtime Material, fail closed if missing/unsupported; original cleanup preserved.
- `Assets/B01Packages/MAPGEN001/Resources/**`: URP-tagged lightweight HLSL opaque shader and stable Unity .meta folder/shader GUIDs included in source/ZIP. Resources ensures build inclusion even when no scene material references it; does not edit GraphicsSettings.
- `Tests/PlayMode/MapGenPhysicalTraversalTests.cs`: explicit low-friction disposable contact material for **CUBE TEST ONLY**, 12 named test rows retained, a flat floor actuator test and more physics/contact telemetry. The actual generator floor material and planner remain unchanged.
- `Tests/PlayMode/MapGenPlayModeTests.cs`: check loaded package retained shader + `UniversalPipeline` tag; assertion updated from URP built-in name.
- `Tools/B01/MAPGEN001/check_w12_contract.py`: source-only assertions; .NET source geometry CI continues independently. Static results do not prove native Unity/PhysX or GPU rendering.

## NATIVE RETEST matrix required after immutable R3.4 source freeze (not performed by producer)
Unity6000.3.14f1 licensed and physical disposable Windows environment; record exact source SHA/target commit and actual logs, process exits, NUnit XML:
- Compile and entire EditMode suite (>=24) and PlayMode (>=19: previous 18 + flat motor case).
- **All 12 independently reportable CUBE** named trials, no early-abort hiding later cases. Record angle, force, groundedSamples, maximum planar speed/displacement, goal progress; preserve FAIL if no goal. Flat plane control must show displacement >4m.
- A/B/C discrimination: unmodified R3.3 cube on map; R3.4 cube on simple flat; R3.4 same cube on generated map. If flat control fails, audit Rigidbody execution; if flat succeeds but mapped fails, inspect actual floor contacts and collisions; zero-friction does not certify R6.
- Minimal Windows x64 URP Player with **no test-only material override**, import complete package Resources shader as tracked; both Arena and Course generate and appear correctly colored (not magenta), render screenshots 1280×720 + 1920×1080, verify material/cache lifetime during 3x Regenerate/Clear/Destroy. Record shader path, renderer actual shader, screenshots and process/log.
- Actual R3/R6 PR125 + OBJ210 reference Light/Heavy, 3 seeds ×2 recipes =12 real robot drives with wheel counts, speed, contacts/seams and finish checks; these cannot be replaced by cube test, and were NOT_RUN on R3.4.
- B01-60 independent G1 and B01-40 target/GUID/SHA verification then B01-00 G2 decision. G2 HOLD, NO MERGE, NO CODEX INTEGRATION.

## Honest test ledger
R3.4 physical Unity compile/EditMode/PlayMode/Windows Player and real R6 driving: **NOT_RUN by B01-20**. Headless planner C# with Unity math surrogate and static source contracts run only if an actual linked CI run confirms them. Source SHA256/ZIP must be verified and reported separately; neither a prepared test nor CI source compile is native Unity.
