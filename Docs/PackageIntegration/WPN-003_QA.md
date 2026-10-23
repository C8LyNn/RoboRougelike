# WPN-003 Beam Cutter integration QA

Source: verified kit WPN003BeamCutter, SHA256 `5186dbb7eebada6969f8b9f5fb9b853ae11eb9cdae06968bb05aa3e787242c2d`. Branch feature/rrl-wpn003-integration; base WPN002 checkpoint6a800e3 on frozen W5. Original dry-run/apply imported14 Assets files without overwrites. Existing GUIDs/scenes/packages unchanged.

The garage's physical gun binds the original BeamCutterModule to R3/P12/R6, with one controlled FixedUpdate clock, real nearest non-self ray queries, shared collider/HP/shield routing and immediate damage stop on release/death/detachment/transition. The source owns DPS integration and heat/lockout; the adapter never multiplies time twice. One child LineRenderer shows the actual original ray endpoint without a collider or physics writes. Player continuous-fire intent comes from the existing tank input; enemy intent respects its existing range, aim and friendly-fire checks.

Source defaults retained:16m,35DPS,heat capacity100,32heat/s,24cool/s,40% lockout release,energy cost0,recoil0. Heat is the consumed/limited source resource. Integration tuning:30kg,130HP,55scrap. Source ThermalState/Module gained a bounded restore/read API; original firing/thermal algorithms are unchanged. Canonical R3 heat/lockout survive capture, copy, salvage, paid repair and rebuild. Source Configure is not called per tick to erase heat.

Both new physical tests are API-driven, not native-input play: mounted original beam damages actual P12 HP at35DPS during actual game frames, stops on release, rebuild preserves heat, destruction makes source Inoperable, paid repair keeps identity/kind. A real4-enemy encounter spawned from the run controller's pristine scene template equips a beam gun and generates actual loot preserving heat60/locked state.

RED: missing catalog case. Additional RED exposed unreachable initial AI condition >=5 (Gauntlet maximum4); fixed to a distinct normal escort in4-enemy groups, alongside separate shield/rail/shotgun slots. Fixture correction: cloning an already-profiled active enemy is not the production spawn path; the test now uses the controller's original template and allows enemy Start before killing it. No production system was rewritten for the fixture. Final Edit30/30 and original Play17/17 PASS, native0. The earlier shared run's single failing new fixture was resolved and is included in the final green Beam run. Native Windows/full W6 regression remain pending wave closure.

**Current decision: READY in the W6 RC2 aggregate, with the documented Unity/UIA shutdown limitation.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](WPN-003_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

Draft PR #24: https://github.com/C8LyNn/RoboRougelike/pull/24; runtime SHA e5fb550732edbc3f9dd8ba2560f90d3718729010.


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.
