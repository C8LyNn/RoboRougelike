# CAMERA001 current limitations
Bounds are conservative world-aligned Collider.bounds, not exact oriented meshes/vehicle real muzzle. Extra framing can occur with rotated/large vehicles; FitSaturated explicitly flags impossible fit under max zoom.
Solver aims above and ahead of vehicle silhouette, not at an enemy's worldspace hit point. Codex must preserve target acquisition and weapon-projectile ray as source of truth.
World spherecast ignores own vehicle root and triggers, not other scene debris/occlusion semantics. WorldOverlapAtAim flags blocked origins but does not guarantee a safe fallback in dense concave geometry.
Never run this rig and P12 camera at once on same Camera. Only Codex may change production ownership after G2/scope ACK.
No direct tests of 720p/1080p native gameplay, full garage/combat, actual P12 mechanics or full camera input; standalone demo only.
Current active Codex local camera task unknown; PR125 report only declared no camera fix at its snapshot. Unity NOT_RUN, G1 BLOCKED, G2 HOLD.
