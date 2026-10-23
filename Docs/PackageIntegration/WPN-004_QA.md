# WPN-004 Rocket Launcher integration QA

Source: verified kit WPN004RocketLauncher SHA256 `d03102b35ea7ed08e93387ee866689fa7cd897145d16438633f328c2eec25179`. Feature feature/rrl-wpn004-integration, base WPN003 checkpoint27de4a6 on frozen W5. Original dry-run/apply imported18 Assets files without overwrites.

The original launcher/projectile/blast resolver are bound to actual R3 guns and canonical ammo/cooldown. A narrow per-P12-part IRocketDamageReceiver forwards the original once-per-logical-part blast to the same HP/shield router and applies the already-budgeted source impulse once to its captured Rigidbody. No demo HP or second vehicle solver is used. All physical P12 parts acquire the receiver at runtime; scene/prefab/GUID bytes are preserved. Original source swept collision, launch clearance, line-of-sight, owner exclusion, bounded body impulse, guidance/default unguided behavior and interception HP remain authoritative. Runtime owner/velocity getters allow the next DEF002 adapter to observe this exact projectile.

Defaults retained:18 initial/34 maximum m/s,20m/s2 acceleration,5s lifetime,.12m collision radius,60 central damage,3.2m blast radius,15% edge damage,12Ns maximum total impulse/body,1.7s cooldown,1 ammo,0 energy,5Ns recoil,0 guidance,12 rocket HP,interception without explosion,no owner damage. Existing run damage/cadence modifiers snapshot into the source config once per shot. Integration tuning:38kg,160HP,60scrap,10-round magazine (matching optional source magazine),T reload2.2s,32 live-rocket cap. A late normal escort (health multiplier>=1.2) uses an actual rocket launcher instead of its shotgun; other slots retain shield/rail/beam, bosses unchanged. Captured ammo/condition enter real salvage.

Expected RED:2 missing-catalog failures. Initial source compile failed because Demo.Editor referenced sample types in Assembly-CSharp. Added a sample runtime asmdef under UNITY_EDITOR and its missing editor reference, excluding samples from retail. No sample enters production. The original launcher now explicitly activates a successfully initialized clone, supporting the adapter's inactive runtime template; its source flight/blast algorithms are unchanged. The physical test verifies the clone is actually active and reaches a real target.

Final Edit39/39 (37 original plus2 integration) and original Play17/17 PASS, native0. Shared W6 regression12/12 PASS. Actual mounted rocket consumes10 to9 once, rejects cooldown retry, applies at most60 damage once to a two-collider P12 part and less to a nearby exposed part, excludes owner, and stops new fire immediately on gun death. Transition cleanup removes live initialized rockets. Destroyed recovered launcher keeps stable ID and empty ammo through paid repair and DeepCopy. These API-driven physics cases are separate from native-input play. Native Windows/full-wave W6 gates remain pending.

**Decision: INTEGRATED, targeted tests PASS; BLOCKED pending W6 Windows/native and full wave regression.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](WPN-004_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

Draft PR #25: https://github.com/C8LyNn/RoboRougelike/pull/25; runtime SHA e71f561e09527ae491c05272a317b152e7d8162e.
