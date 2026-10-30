# B01-11 / PASS17 WAVE22 — FRAME-SAFETY-001/R1

## Original new functionality
Stand-alone, executable Polish screen-space 3D **synthetic** framing calculator for a modular robot. This is separate from W21 #701 CAMERA-VISIBILITY-LAB-001, which studies projected obstacle occlusion in a top-down geometry model. W22 checks whether the 8 corners of each **active** core, left weapon, right weapon and other part fit inside a normalized HUD-safe rectangle after perspective projection. It exposes separate part-level fit, near-plane clipped parts and a bounded 1×–6× dolly camera suggestion minimizing unnecessary distance. Both weapons can be independently disabled; horizontal and portrait viewport aspect ratio and vertical FOV can be changed in the functional offline canvas.

This source is NOT a native Unity camera, renderer, collider, Save/Load, FOV/camera-collision acceptance or empirical human gameplay result. It does NOT measure real rotated meshes, occluders, VFX, clipping of UI, enemy visibility, camera smoothing, comfort or FPS. A green synthetic fit indicator is never game acceptance.

## Original source and test execution
Open `index.html` locally: the UI has real controls for wide/portrait viewport, left/right weapons, FOV, reset and applying the pure candidate camera. Use keyboard-native buttons and inspect text labels independent of colors.

Run from repository root:

```
node --check Research/B01-11/P17/W22-FRAME-SAFETY-001/frame.js
node --test Research/B01-11/P17/W22-FRAME-SAFETY-001/test_frame.cjs
```

The original JS tests exercise perspective basis (including overhead), two independent weapons and missing-part logic, near clipping, safe bounds, malformed/hostile geometry, deterministic solver and 400 generated cases, manifest SHA256 and HTML/workflow source. An own two-OS GitHub Actions workflow executes the exact Git head source. `manifest.json` pins SHA-256 and byte size for 5 companion files (manifest excluded to avoid self-hash).

## UX test contract (research hypotheses; not measured)
- Primary robot core should remain visible and **both** assigned weapons should fit in the playable safe frame in representative encounters. Detached modules must not force excess zoom.
- A HUD-safe framing warning should identify the exact off-screen module; avoid relying on color alone.
- A suggested dolly must be treated as advisory until native camera collision / smoothing, target aim and actual Unity FOV are verified.
- For portrait/mobile: no unsafe automatic zoom if the selected safe rectangle cannot be fitted by a bounded dolly; instead propose an explicit HUD-layout alternative.

## Project authority and receivers
Exactly 5 original `Research/B01-11/P17/W22-FRAME-SAFETY-001/**` files plus one own workflow; no existing files overwritten. Main/develop, original frozen #510/#556/#596/#642/#647, selected R5 #690, selected API #691, #669/#712, ALT_HOLD #604/#644/#652/#682/#693/#695, Codex and all `Assets/**` unchanged. No merges, no Unity install, no Windows owner PC access.

- B01-20 and B01-12: use as a design study for genuine Unity camera and UI only; no authoritative game wiring without approval.
- B01-13: develop later actual player comprehension study (not performed).
- B01-40: independent source G0/manifest/path verification.
- B01-60: actual Unity/PhysX G1 only with authorized licensed local runner; presently NOT_RUN.
- B01-00: parallel producer handoff and possible eventual Codex integration with authentic ACK.

Lifecycle fields: IMPLEMENTED=YES; TESTED=SOURCE only if original CI success; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; CANONICAL_LIVE_SAVE=NOT_APPLICABLE; G2=HOLD; CODEX_ACK=NONE; INTEGRATOR_FEEDBACK=NOT_MEASURED.

Safe rollback: close the distinct Draft PR; if staged only in a disposable receiver, remove strictly this own new subtree and own workflow after verifying SHA and absence of foreign data. Do not recursively clean shared folders.
