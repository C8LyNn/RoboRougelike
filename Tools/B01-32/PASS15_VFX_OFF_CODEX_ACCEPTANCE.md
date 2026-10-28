# B01-32 — PASS 15 · VFX001 pooled / R6 legacy muzzle · exact Codex acceptance

**State:** QA ONLY / READY_FOR_CODEX_NATIVE_TEST; **NOT_RUN in Unity**. No production fixes, no merge, no third producer package. Codex is sole owner of original game integration and any fix.

**Original canonical baseline:** [Codex Draft #310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`:
- `Assets/R6/Vfx001R6LiveBridge.cs` Git blob `e909f13fb95b2e8a6bb4324eebc0f51943874ae4`, `SetVisualEffectsEnabled(false)` calls **pool.ClearAll** only; `OnDisable` clears **pool and R6CombatFeedback.ClearTransientVisuals**.
- `Assets/R6/R6CombatFeedback.cs` Git blob `90be5192a9fffb5bf53d598cc6d2a556be4a5715`, `Muzzle(Transform,R4WeaponKind)` creates independent transient primitive sphere `Błysk trafienia` when damageable parent supplies a Renderer material.
- `Assets/RoboRougelikePackages/VFX001/Runtime/Common/VfxPoolManager.cs` blob `9a328f10c99e963d36f1dca7a06835d60290f262` controls **VFX001 pooled actors**, independent of muzzle spheres.
- `Assets/RoboRougelikeMechanics/WPN003BeamCutter/Runtime/BeamCutterModule.cs` blob `4e510de15ce33183afd6464425f5001f43c45dc7`: independent `LineRenderer`, `OnDisable=>HideBeam`; **not exercised by this fixture**. Beam visibility policy still requires HQ choice.

## Executable original UnityTest (QA only)

`Assets/RoboRougelikePackages/VFX001/Tests/PlayMode/B0132P15MuzzlePoolSeparationTests.cs`
(with its stable `.cs.meta`) is in **existing B01-32 isolated QA branch** and references only `RoboRougelike.VFX001.Runtime` through the original unchanged PlayMode asmdef. All R6 integration and P12 types are located at runtime by exact `FullName` reflection; missing original APIs **fail closed** instead of emulating them.

1. **DEFAULT diagnostic/teardown** — `PooledOff_CurrentSourceStillAllowsLegacyMuzzle_ThenBridgeDisableClearsBoth`: instantiate real original Vfx001R6LiveBridge, obtain its original child pool, inject an actual VFX001 Hit, create an original P12 damageable cube with material and invoke real R6CombatFeedback.Muzzle using `Automatic`. Preconditions: pool.ActiveCount=1, R6 legacy private effects queue Count >0. **Current-source expected**: `SetVisualEffectsEnabled(false)` makes pool count 0 while standalone legacy muzzle count stays >0. Then seed both channels again, disable bridge; **PASS** only if both counts reach 0. This is a **current-pool-only scope witness**, not global OFF approval.
2. **[Explicit] policy-gated contract** — `GlobalVfxOff_MustAlsoClearLegacyMuzzle_OnlyIfApprovedByHQ`: explicitly select **only after HQ authorizes `VFX_OFF=ALL_VISUALS_INCLUDE_BEAM` or `DECORATIVE_ONLY_KEEP_BEAM_TELEGRAPH`**. Given legacy muzzle precondition >0, **PASS** only if VFX OFF clears both existing pool and legacy queue immediately. This is expected **RED** on #310 and must not be mistakenly counted as a generic PASS.

**Important:** both scenarios directly call `VfxPoolManager.Emit` to isolate pooled actors because a scene-less test host has no R1 combat controller. These test cleanup/source ownership semantics, **not** in-encounter hit routing, live muzzle flash appearance in a rendered Player, or damage. A full-scene 720p/1080p smoke witness belongs to existing CV-06; camera-loss trigger D04 is separate.

## Implementable Codex fix-contract — only after HQ VFX_OFF decision

- **POOL_ONLY (existing semantics):** preserve existing pool clear and legacy/Beam independence, but UI label must say only VFX001 pooled effects are disabled. Default diagnostic remains GREEN; no production effect suppression is assumed.
- **DECORATIVE_ONLY_KEEP_BEAM_TELEGRAPH (B01-32 recommendation, NOT approved):** OFF gates all *new* VFX001 pooled events and **legacy Muzzle()**, clears both queues immediately; **retain** Beam line as an essential continuous damage-path telegraph. Bridge teardown must still clear pool + muzzle, Beam module `OnDisable` hides beam.
- **ALL_VISUALS_INCLUDE_BEAM (NOT approved):** same pooled + muzzle OFF behavior; suppress/hide Beam `LineRenderer` while preserving real Beam damage/physics and separate readable firing state. Do not alter turret targeting, beam DPS, heat/jam, damage sinks or core destruction accounting.

Codex must fix in its own integrated production source only; do **not** cherry-pick these QA files as a game feature. Before/after values for pool.ActiveCount and legacy queue Count are required; native particle and renderer visibility must be verified in original 720p/1080p Player separately. Existing original C# shim [PASS14 Actions #40654018931](https://github.com/C8LyNn/RoboRougelike/actions/runs/40654018931) already reproduces the *distinct* disabled-`VfxPoolManager.Emit` source-RED; do not rerun same unchanged source to inflate test counts.

### Codex engine test receipt

1. **CV-00 prerequisite:** confirm actual licensed Unity 6000.3.14f1 availability + OS, source/build SHA, isolated safe worktree. No assumption that Codex possesses Unity.
2. Import QA-only fixture **only** as a temporary separate test input, with no canonical gameplay edits or asmdef changes. Run PlayMode NUnit by method name; explicitly include policy test only if HQ answered. Capture original `Editor.log`, actual NUnit XML selected/executed/passed/failed/skipped counts and process exit.
3. Check positive and negative transitions plus full-scene muzzle/Beam teardown in 720p/1080p. Legacy spheres must not become invisible by merely hiding their Renderer while retaining active objects after teardown.
4. **PASS** only when test conditions match the **approved** policy on a new Codex fix SHA, damage remains identical, and B01-60 independent native QA accepts actual artifacts. Source-only/static verification is not Unity G1.

**Next owner:** HQ determines VFX OFF semantics → Codex implements as sole owner → Codex runs authentic CV-06 only if environment actually available → B01-60 reviews independent evidence → B01-32 differential source acceptance. No G0/G1/G2/release/merge claim.
