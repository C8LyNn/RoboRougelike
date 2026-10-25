# CAMERA001 changelog
R1 2026-10-25: isolated pure hull-aware camera solver; opt-in Camera001Rig using world collision spherecasts, smoothing and diagnostics; standalone demo scene with Light/Heavy and controls; authored EditMode/PlayMode NUnit suites; Unity .meta/asmdef, installation contract, receiver prompt and evidence ledger.
NO modification of production P12, physics R6, garage, AI, main or develop. Native Unity not yet executed.

## W12 CAMERA R1.2 — final-pose collision/aim fix
- CAMERA-01: apply final camera rotation to look at actual post-smoothed, post-world-collision camera position; flag and inspect ACTUAL own collider ray/hull, not just solver's desired pose. 26 native own-ray pitch-transition hits on prior R1.1 motivate this.
- CAMERA-02: blocked-pivot fallback keeps prior pose; cap continuous motion15m/s; physically spherecast previous→candidate, independently verify near-plane-enclosing radius overlap at final camera. Source R1.1 Tall corridor9.1732m observed fail. Explicit rebuild/reset still snaps.
- Demo Light/Heavy/Tall/Wide, C corridor, X detach with telemetry. New ten individually named PlayMode cases (eight rig×resolution, one Tall corridor, one detachment); preserve existing eight NUnit methods. No game input edit, no P12 integration, BUILD owned B01-23 untouched.
- Source CI real JS analytic geometry840 + static mutation contracts; actual Unity/native Windows QA pending. Details TEST_EVIDENCE_R12.md; G2 HOLD.
