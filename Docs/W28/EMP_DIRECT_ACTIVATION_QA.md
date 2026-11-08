# PASS17 W28 — EMP direct module bypass guard

**Base exact W27 #824 HEAD:** e03d36f2b86f8c73a067e574b67237a8921f622a. The pre-existing original WPN006 radial weapon remains radial, with its own vehicle ignored for ray occlusion, friendly/ownership filtering, physical walls blocking target receivers, cooldown even when a valid pulse misses every enemy, and one energy debit only on a real pulse. This PR DOES NOT apply projectile path or projectile ammo rules to EMP.

**Actual code change:** EmpEmitter now offers a live authoritative activation predicate that is checked on every Ready/TryPulse (not merely the RRLEmpBinding.Update), BEFORE OverlapSphereNonAlloc, energy or cooldown. RRLEmpBinding installs a predicate that checks current RRLDeviceBinding.Available (Core/live body/part/EMP disruption/combat) and attachment. A destroyed or detached module cannot fire by direct EmpEmitter.TryPulse. No separate Save writer, no new scene, no modified original economy.

**Authored existing native physics test:** original EmpEmitterPhysicsTests gains Pulse_DirectModuleCallRechecksGateBeforeEnergyAndCooldown asserting no energy/cooldown/receiver impact when denied and real pulse after re-enabled. Existing physical wall/self/team/radius tests must still pass.

**Status:** source physically implemented on isolated PR; original Unity6000.3.14f1 compile NOT_RUN, original EMP PlayMode NOT_RUN, Windows Player NOT_RUN. Producer regression and independent QA pending. No merge/release, one direct guard, no new EMP implementation or competing energy source.
