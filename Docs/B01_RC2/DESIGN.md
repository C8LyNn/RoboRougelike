# B01 unified RC2 — implementation decisions

Authority: user dispatch 2026-10-26, archived as USER_DISPATCH.md. One canonical application from PR248, not older BASE. The user explicitly authorizes experimental integration despite producer G2 HOLD; no technical approval round is required. Formal independent gates remain ungranted.

Base HEAD5c29e8e28e7b34fedf0c992fca49451828b02314 contains game86eb7bb77e6bfc7787f781ea1ccda547b8a4f643 byte-identically. Work only in integration/b01-unified-rc2 at RoboRougelike-B01-Unified-RC2. Preserve all other worktrees/builds. Unity6000.3.14f1, URP17.3.0, existing packages unchanged.

Use the existing R1RunController/progress, R3VehicleModel.Parts+Inventory, R3/R6 actors, original HP/projectiles, R4 reward transaction and existing wallet. Every enabled mode enters through the existing menu/preparation garage and exits through canonical result/rewards/garage. Producer demos and alternate vehicle solvers are not game entries.

Update MAPGENWORLD in place only after full-source/GUID/hash gates; no duplicate MAPGEN R341. BUILD adds an extra fail-closed physical validation layer before canonical garage mutations, including direct controller calls; it must not invent a second grid or inventory. RAC uses the supplied checkpoint observer with exactly one clock and actual R6, and settles through existing run progression. CAMERA is last among spatial systems and remains disabled unless the aim observation is explained by matched evidence. REWARDKEEP3 may wrap existing reward settlement once; immutable enemy equipment IDs must be canonical before physical construction, not remapped at claim. LEGENDARY physical definitions/abilities and ECON rebuild costs/atomic durable hooks are not invented.

Approach: incremental gated adapters on one integration branch. New source files may be staged for native tests before activation; an unsafe/incomplete integration step is reverted alone and excluded from final Assets/build. Source surrogate CI is input evidence, never a native result. Maintain a package status ledger and native receipts per checkpoint.

One retail Windows x64 output and ZIP, same gameplay scene and activity selector. Internal finite acceptance hooks/builds are diagnostics only, never additional delivered games. Final ZIP must be fully extracted and byte-verified and its exact EXE launched. Existing C0000005 automation shutdown remains unresolved unless this task proves otherwise.
