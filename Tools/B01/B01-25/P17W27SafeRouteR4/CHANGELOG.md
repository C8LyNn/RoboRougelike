# B01-25 / SAFEROUTE changelog

R1 #745 (immutable): bounded 15×11 conservative anti-softlock grid BFS, Light vs Heavy, three moving physical gates, two active hazards, safe bypass.

R2 #763 (immutable): live player Rigidbody origin navigation, replan after crossing cells, fail closed on invalid player cell, always check actual gate collider.

R3 #789 @ `d4883cb3ee740cf5b3f0d847d149858ff09c848b` (immutable parent): real registered movable crate, physics overlap route occupancy, reject single-crate Heavy softlock, detect external crate grid-cell change, source tests 82/82 and Git QA 20/20 each OS, Unity native G1 NOT_RUN.

R4 W27 (this separately versioned Draft PR): second physical Rigidbody crate; union of registered real collider occupancy for proposed moves (fix "individually safe but pair unsafe"); actual subcell pose change replan; bounded broadphase overflow/unobserved-collider fail-closed; +12 executable C#9 regression cases, +5 authored Unity PlayMode tests (17 total). Own new versioned manifest, independent source+safe-migration QA/CI. Never coinstall old versions. No merge.
