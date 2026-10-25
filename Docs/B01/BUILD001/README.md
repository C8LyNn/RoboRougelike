# B01-BUILD-001 R1 — Typed Structural Sockets
Issue #140. Unity 6000.3.14f1 URP. Receiver: Codex **after** G0/G1/G2 and target scope ACK.

Isolated source: `Assets/B01Construction/BUILD001/**`, no dependencies on live R3/P12/R6 scripts or gameplay worktrees.

Actual C#:
- `BuildSocketGeometry`: per-part 0.55m logical grid cells and face patches with exact typed structural/axle/weapon-base/suspension-base; quarter-turn XYZ transforms, contact patch overlap area (not just face-centre flags).
- `BuildPlacementRules`: cell ownership, nonzero area, axle-only wheel, bottom-base gun, rotated 2x2x1 single part ID, fail-closed reasons, graph reachability after destruction; READ-ONLY module, not authoritative R3 save/HP.
- `BuildSocketRaycast`: accepts only a real RaycastHit within an authorized socket patch, rejects tire/gun art, no collider mutation.
- `Build001Demo.cs`, independent Unity scene `BUILD001_Demo.unity`: sample socket ghost and reasons.
- EditMode tests for positive/negative placement; PlayMode physics raycasts on Cube/Cylinder hitboxes.
  
**Proof state:** C# sources, tests, and scene COMMITTED; Unity compile, EditMode, PlayMode, manual UI NOT_RUN. This source package alone does not FIX game live wheel placement. Codex integrates after G2; until then do not claim the bug repaired in game.
