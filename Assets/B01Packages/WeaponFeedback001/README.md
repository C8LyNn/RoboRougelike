# B01-32 WEAPON-FEEDBACK-001 | P17 W27 R1

Isolated opt-in Unity6 visual effects for confirmed shot, confirmed impact and weapon damage appearance. Strictly no calls to canonical shooting, aiming, health/armor reducer, ammo, economy, or Save. The authoritative weapon system explicitly calls NotifyShot(), NotifyImpact(position,normal), SetWeaponDamageVisual(fraction). Visuals must never create gameplay events or damage.

Concrete visuals: muzzle particle and brief light flash, local visual-only recoil pivot with exponential recovery, bounded impact particle instances and cleanup, optional damage smoke emission. Uses URP compatible built-in ParticleSystem/Light types without shader or material dependency.

To produce real prefabs and demo scene, run Unity Editor menu B01 > Weapon Feedback 001 > Generate Prefabs And Demo Scene. Draft source includes Editor generator, but generated .prefab and .unity assets **not present until Editor executes**. Demo gun is visually isolated; no weapon firing binding is supplied until Codex accepts a validated handoff. This respects Codex reserved weapon clearance and fire authority.

3 real native PlayMode UnityTest cases authored: shot flash/recoil recovery, impact particle, bounded visual damage state; Native G1 NOT_RUN and independent G0 PENDING. A source file is not proof of working Unity runtime. Next B01-32 owner should capture gameplay recordings and budget particle allocation; B01-40/61 GUID/ABI QA, B01-60 native G1.

Package may be removed ONLY from exclusive B01Packages/WeaponFeedback001 folder in a disposable staging checkout. Do not overwrite main/develop, Codex worktree, protected visuals or real weapon logic.

## P17 W30 receiver (stacked on #874)
The Editor generator now creates a **populated** isolated demo scene with an instance of the generated prefab, camera, and key light. The prefab includes a connected damage-smoke ParticleSystem (initially disabled), flash light, muzzle particles, recoil-only visual pivot and impact-particle reference. No authoritative shooting logic is attached.

**Operator steps (Unity 6000.3.14f1):** activate the matching isolated branch; in the Unity Editor invoke `B01 > Weapon Feedback 001 > Generate Prefabs And Demo Scene`; verify both resulting prefab and .unity scene are present and serialized with unique GUID .meta files; open the scene and inspect the links via Inspector; run all `WeaponFeedbackVisualTests` PlayMode tests including `RapidBurst_ReusesBoundedImpactPool` and `DamageSmoke_EmitsOnlyWhileDamaged`; run Windows x64 build and a real extracted Player test only after integrating into authorized gameplay. Generator saves files in the checkout; G0 must review generated assets before reuse.

**Limitations:** demonstration does not itself fire shots or drive effects. NotifyShot(), NotifyImpact() and SetWeaponDamageVisual() must be invoked after validated authoritative gameplay events; neither Source PASS nor generated demo constitute gameplay integration. Native editor, Player and performance tests are NOT_RUN in this source-only authoring environment. Do not include the demo scene in production build scenes without explicit approval.

## P17 W31 corrective receipt (same #878; no new prototype)
- Impact FX and muzzle particle generator now set `playOnAwake=false`. Runtime `Awake/BindVisuals` additionally clear unconfirmed muzzle emission, and `NotifyShot/NotifyImpact` ignore calls while the VFX component is disabled.
- Deactivation turns off muzzle light, stops muzzle/smoke/impact emitters, restores the visual pivot, and **retains** the impact pool across enable/disable; `OnDestroy` releases detached impact GameObjects. Damage fraction can update while disabled without leaking smoke, and resumes visually on enable.
- Unity Editor menu generation is additive, saves a populated new scene, restores the previously active scene, and refuses existing prefab/scene assets or their .meta paths. It does **not** modify active gameplay scenes. A partial generation failure may leave newly created assets; operator must inspect them, not automatically overwrite/delete.
- Two additional authored PlayMode `UnityTest` cases: no startup muzzle light/particles before confirmed shot; disable/re-enable smoke, flash, hit pool and blocked notifications with bounded pool reuse. Together with W27–W30 cases, suite has **7 authored cases**. All 7 require actual Unity 6000.3.14f1 execution; native result remains **NOT_RUN** until real XML/Editor.log is attached.
- Run the isolated Editor generator **only if the three output paths are absent**; verify saved scene, serialized prefab references, no accidental scene changes, PlayMode tests (including enable/disable), render/capture in actual Windows Player, profiler allocations & particle budgets under burst, and perform strict B01-40/61 G0 before intake by #873/#877 owner.
- Do not wire to authoritative fire/damage before native G1. No Windows ZIP, Unity Editor.log, NUnit XML, or Player.log was produced by this GitHub-only source iteration.
