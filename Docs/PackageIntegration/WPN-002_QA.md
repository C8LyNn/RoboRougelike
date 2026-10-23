# WPN-002 Railgun integration QA

Source: verified kit RoboRougelike_WPN002_Railgun_v1.zip, SHA256 `887f10cc5f7d7103e293cf49d88f0f1862d9201ef5381eb8edc4d02a1132b392`. Branch feature/rrl-wpn002-integration; base DEF001 checkpoint b685fae on frozen W5. Original dry-run/apply imported19 Assets source files, no overwrite. Original runtime algorithms unchanged.

The real garage gun binds original RailgunModule and its resource/sink contracts to canonical R3 ammo/readiness, shared device availability and the existing P17 projectile cap/encounter cleanup. Penetration is a swept kinematic Rigidbody mode owned by P17, with sorted hits,128-hit overflow fail-closed, owner/team/dead rejection and lifetime-wide logical P12-part deduplication. It uses original RailgunRules for travel and impact energy. No demo HP, projectile or vehicle solver enters gameplay. Unknown hard surfaces stop the shot. P12 exposes settled damage from its existing pre-HP filter, so shield absorption also consumes penetration energy and full interception stops the rail.

Original defaults:85m/s,120m range,.075m radius,100 initial energy,.4 loss/m,.65 damage/energy,75 damage/part cap,6 distinct parts,2 minimum remaining energy,2.4s cooldown,48Ns recoil at muzzle,inherited velocity1. Integration tuning: resistance clamp(physical MaxHp*.2,5,60),44kg,180HP,65scrap;12-round magazine, T reload2s. R3 identity/ammo/cooldown persist. A distinct normal escort in groups>=4 carries an actual railgun, fires through its original module and salvages actual ammo/condition.

RED:2 missing-catalog tests failed as expected. Final Edit filter Railgun45/45 PASS (43 original +2 integration); original Play22/22 PASS. Shared W6 regression8/8 PASS; overlapping cases are not counted again as unique. Physical test fired one actual P17 projectile, consumed12 to11 once, rejected same-frame retry, penetrated two P12 parts with decreasing damage and ignored a second collider on the first part; destroyed gun stopped immediately. Destroyed recovered rail retained ID/ammo4 through paid repair and DeepCopy.

Original source fixture defect: RecoilActsOppositeToMuzzleDirection asserted velocity immediately after AddForce, before PhysX simulation (observed0). It now explicitly advances one .02s physics step with try/finally restoration of the runner simulation mode; assertion and runtime recoil remain unchanged. Original Play previously21/22, corrected22/22. No test was removed or weakened. Native Windows and complete W6 campaign remain pending wave closure.

**Current decision: READY in the W6 RC2 aggregate, with the documented Unity/UIA shutdown limitation.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](WPN-002_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

Draft PR #23: https://github.com/C8LyNn/RoboRougelike/pull/23; runtime SHA d60d04596773068e08f8967e6bbe179929806be2.


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.
