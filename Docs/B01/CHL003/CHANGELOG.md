# CHL-003 R1
- First isolated challenge package: pure source-tested fixed-count ordered checkpoint logic, exact-actor/duplicate guard, retry/cancel/pause/time failure and events.
- Unity Rigidbody-backed collision validator with OnTriggerEnter, physical low barrier+ramp+bridge, two different demo motor rigs and camera/HUD.
- Fifteen authored NUnit EditMode + nine authored Unity PlayMode tests; exact core C# source compiled/executed in GitHub dotnet8 CI.
- W16 acceptance hardening: co-located CP4+Finish fixed-step arbiter with actual sustained PhysX overlap; real pause/velocity restoration, separate trial metrics/Csv log (contacts/ramp/bridge/height/speed/falls), live rig-switch cancel, wider physically viable bridge and 3x per-rig native PlayMode fixture. Source-only executable RED→FIX→RETEST; native Unity still NOT_RUN.
- Original stable .meta scripts/scene/folder/asmdefs. Reproducible full ZIP and receipt to be pinned by source workflow. Native G1 and Codex integration NOT_RUN; no host changes.
