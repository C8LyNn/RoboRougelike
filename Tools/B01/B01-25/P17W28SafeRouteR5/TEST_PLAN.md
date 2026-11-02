# SAFEROUTE R5 / acceptance

AC-W28-01: registering independent world Rigidbody with real collider at physically observed obstacle cell (7,8) blocks Heavy gate-closed/hazard-active route, while Light may still pass; unregister restores route; no movement or mutation of registered authority. Source regression tests R5 external/detached and native test `W28RealDetachedModuleColliderBlocksHeavyWithoutAuthoritativeMutation`.

AC-W28-02: Rigidbody has child compound colliders: collider #1 at safe (13,8) and collider #2 at chokepoint (7,8). Both attached colliders must be registered and observed, and the child alone blocks Heavy. Source QA validates all-child loops, native test `W28CompoundDetachedPartRegistersBothPhysicalChildColliders`.

AC-W28-03: external physics motion, including Rigidbody position/rotation and child transform/scale, must trigger physical replan on next FixedUpdate and restore Heavy path when obstacle moves away. Native `W28ExternallyMovingDebrisCausesRealPhysicsResample`.

AC-W28-04: disabled, destroyed, inactive, missing or outside-world foreign registered Collider must fail closed (not ignored). Unregister of actually retired body re-evaluates routes. Native `W28DisabledRegisteredColliderFailsClosedUntilUnregistered`; missing/destroyed state native follow-on acceptance.

AC-W28-05: invalid registrations rejected (player, test crate, null, duplicate, trigger-only, >32 root bodies or >16 compound shapes), and original registered body is never moved/altered by route source.

AC-W28-06: R4 two-crate 8-state anti-softlock and 94 old tests still pass; exact SHA 60 Git source pins, 14 unique GUID, 3 unchanged asmdefs, 23 owned Unity source/meta files, original 24-member cross-OS source ZIP and actual stage/rollback. All PASS evidence must come from completed workflows and independent formal G0. No native Unity PASS without original Editor log/NUnit XML/exit.

Risk HIGH: missing one child collider falsely routes Heavy through an actively detached armored component. Owner B01-25 source mitigation: enumerate all attached nontrigger child colliders, require all physically observed, reject unknown. Native and game integration still unresolved.
