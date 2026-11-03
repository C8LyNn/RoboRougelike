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
