# WPN-005 Rotary Saw integration QA

Source: verified kit WPN005RotarySaw SHA256 `240afef467981b1b051c241212aa39392c1e97b806915468900dd58dbee2efe8`. Feature feature/rrl-wpn005-integration; base DEF002 checkpointca1fac2 on frozen W5. Original dry-run/apply imported16 Assets files without overwrites. Existing GUIDs/scenes/packages unchanged.

Original RotarySawModule/Motor/ContactMath run on an actual R3 gun with a single wrapper-controlled source physics step. The source owns contact-disc geometry, stable logical-part deduplication, RPM spin-up, thermal lockout, hard-material batch rejection and jam. The adapter routes accepted contact damage through the existing P12/shield resolver and never invents a second vehicle HP or body solver. Shared fire intent starts/stops cutting; death/detach/paused combat gate it. A visual-only cylinder rotor spins under the source blade transform. The same shared R41 geometry renders the garage preview and actual blade, preserving the .9m world radius/.14m half-thickness even under the .52-scale gun anchor; it adds no second collision body.

Original defaults retained:radius.9m,half-thickness.14m,hub.06m,axleRight,64-overlap fail-closed,maximum2800RPM,minimum450RPM,acceleration3500RPM/s,deceleration2200RPM/s,full-speed52DPS,max resistance3,heat capacity100,heat25/s at full RPM,cooling24/s,restart40%,jam1.2s/20heat penalty,energy0,reaction force0 (original bounded reaction implementation remains). Integration resistance: existing P12 MaxHp/200 clamped.5..4; explicit damage receivers2; unknown solid surfaces4 (jam). Chassis tuning:25kg,130HP,50scrap.

Canonical thermal state and jam deadline survive capture/DeepCopy/rebuild/paid repair and complete-state enemy salvage. Rebuilt stationary motors restart at0RPM, retaining heat/lockout/jam. Source gained bounded motor restore and opt-out automatic simulation/public Advance, without changing cutting/thermal/jam algorithms. A distinct normal escort in four-enemy Wasteland encounters uses the real saw; its existing R6 movement switches to1.6m preferred distance, minimum3.5m/s and.1 strafe, rather than firing from the old12m standoff. Crossfire/default arenas retain the beam escort.

RED2/2 missing catalog/AI. Additional RED caught an empty garage preview without the blade; a single shared rotor factory fixed it. First AI fixture moved but did not spin within1s because a backwards-facing turret legitimately required more than1s to turn180deg; the scoped melee test now starts already facing the target. No source targeting logic was weakened.

Final Edit filter Saw32/32 (29 original+3 integration), original Play18/18, all exits0. Final shared RRLW6 regression18/18 includes paid repair assertions. Real tests verify one damage event for a two-collider P12 part, positive actual HP loss after original spin-up, hard-wall contact rejects the whole batch and jams, heat/jam survive rebuild, destruction stops operation, paid repair retains identity/thermal/deadline, reachable AI closes distance and spins, and garage blade dimensions/no enabled collider. These are API-driven physics checks; native Windows/full W6 gates remain pending.

**Current decision: READY in the W6 RC2 aggregate, with the documented Unity/UIA shutdown limitation.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](WPN-005_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

Draft PR #27: https://github.com/C8LyNn/RoboRougelike/pull/27; runtime SHA a631f272ca1593690a3cc3689a3e4353d7d28f62.


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.
