# CAMERA R1.2 native QA — authorized scope

Producer: `48581e49a878eca376674551017532bf17fd35b2`, full #134 + #214 + #247.
Receiver base: OBJ #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`, includes PHY #125.
Fresh sibling worktree, not either RC1 checkout. App-native worktree creation unavailable for this projectless Desktop task (previously returned NotGitRepository); manual git fallback.

1. Preserve RC1 HEAD and archive hashes; inspect active processes/worktrees.
2. Verify complete physical ZIP, 40 manifest entries, exact Git blobs, meta/GUID/dependencies.
3. Freeze import; run one real Unity compile, original EditMode 5 and PlayMode 13; save actual exits/XML/logs.
4. Copy receiver-owned R1.1 reproduction harness into this sandbox only. Measure final-camera actual hull rays, final aim, near-plane overlap, ordinary translation cap, pitch transition, yaw, zoom, corridor, actual part damage/detachment. Eight real R3/R6 rig/resolution cases in Windows.
5. Probe canonical aim/fire and InputSystem LMB/RMB through a separate QA fixture, label API/input injection separately from human play. Preserve any failures without modifying producer or assertions.
6. Publish one Draft QA PR, Issue #66 and B01-00 #71, then stop. No integration, merge, G2 or next package.
