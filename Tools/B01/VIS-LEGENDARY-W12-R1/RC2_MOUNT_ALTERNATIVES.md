# W14 / RC2 — measured root-anchored alternative envelopes (NOT gameplay authorization)

**Owner:** B01-30; **Design authority:** B01-10/B01-00, BUILD physics/socket owner B01-23. Existing original model outputs unchanged. New executable source: `tools/mount_envelope_candidates.py`, 22 positive/negative tests from genuine W13 package. No Codex-game changes.

### Immutable source dimensions, local axes

- Original M01-018 real animated GLB SHA256 `795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd`, at 121 samples per clip: X[-.586,.586], Y[-.0055,.907], Z[-1.745,1.927]m. Visual `MOUNT_TOP_SOCKET` at root, muzzle nominal +Z. **Current** 3×2×2 at 0.55m grid FAIL Z by2.572m.
- Original M04-024 real animated GLB SHA256 `c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8`, combined 4-gimbal clip: X[-.78878,.78878], Y[-.889306,-.005], Z[-.78878,.78878]m. Visual `ROOT_UNDERSIDE_MOUNT_Z0`, exhaust -Y. **Current** 3×3×2 FAIL Z by0.47756m.

### Actual minimum viable visual-bounding alternatives (conditional, no mesh rescale)

For the calculation **X = W, Y = H, Z = D**; grid=0.55m; 5cm experimental per-face slack, without model scale or axis remap. Proposed virtual footprint *placement* to retain visual pivot:
- **M01: 3×2×8 cells** (1.65×1.10×4.40m), Z-centered on visual root and Y bottom-anchored at [-0.10, +1.00]m. All six sides pass 5cm *sampled-vertex* review margin; geometric root is NOT a typed BUILD socket. Alternative **3×2×7** would reduce occupied cells but needs an **explicit 0.10m virtual depth-center offset** relative to pivot; this must be approved and physically connected by BUILD owner before considering it.
- **M04: 4×2×4 cells** (2.20×1.10×2.20m), X/Z centered at original root, Y top-anchored [-1.00,+0.10]m. All six side clearances exceed 5cm *sampled-vertex* diagnostic margin. The current 3×3×2 and alternative 3×2×3 **FAIL** the margin; a 90° reorientation of root/exhaust is NOT an authorized shortcut.

These are **minimum volumes under the explicitly proposed root-alignment policy**, not a validated physics/collider/socket implementation. *Spacing the artificial box around a top/bottom root by ±0.10m is a design proposal*, not the current garage cell occupancy contract. There are no registered BUILD socket types or approved masses from this calculation. The tool reports `approvedToMount=false` for ALL alternatives. The real usable envelope could be larger after hull/host transforms and swept triangle collisions.

### Single native receiver request (after owner decision only)

In a disposable Unity 6000.3.14f1 URP import (never the active Codex game), check the approved candidate pivot/offset against the actual BUILD R1.4 typed anchor plane and union collision geometry, 0/25/50/75/100% and intermediate M04 gimbal and M01 counterweight positions, including host non-unit scale. Collect Physics.ComputePenetration results + 720p/1080p front/side/top screenshot for low/large chassis and actual anchor connector. **Until this: UNITY_EDITOR/URP/PHYSX=NOT_RUN and G2 HOLD.** Do not prescribe change to actual BUILD or game controller without independent evidence.
