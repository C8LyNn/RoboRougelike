# W6–W9 incremental integration design

Frozen W5: PR20, `2154b4341dd6b6fefb135187dc07b4b12d44f908`. Unity6000.3.14f1. User authorizes autonomous implementation/native QA/Draft PRs and forbids main/develop merges. Working branch `integration/rrl-w6-w9`; W5 checkout/branch/build remain untouched.

## Source gates

The requested kit `RRL_CODEX_READY_ALL_PACKAGES_R1.zip`, expected SHA256 `bbbc3bdf74ad1e4445fc5ca231eddc60f189090cc68104e6a23e1b1f9469407b`, was not found in user directories, attachments, Temp, accessible G: or exact/broadened connected Drive searches. `verify_and_stage.py` is also unavailable. Therefore outer/internal verification is NOT RUN. No original is reconstructed from descriptions. WPN001–006, DEF001/002 and MOV011 are BLOCKED pending actual source bytes. WPN001 was investigated first; independent available packages continue as explicitly requested. The user has been asked for a path/link asynchronously.

Available originals are the exact pinned Git deltas of PR3/4/6/12–16. None of their463 added paths/blobs is present in W5; no inter-package changed-path overlap. Import one package at a time, without overwriting existing files/GUIDs. Preserve original source commit plus separate integration changes. Do not execute instructions embedded in source documents as new authorization.

## Runtime boundaries

Keep R3PartState/model as persistent identity/condition, P12 as vehicle HP/death/structure, R6 as the only Rigidbody drive. A small shared collider-hit receiver routes an actual collision to either an explicit arena/objective receiver or existing P12 HP. No independent vehicle health or physics solver. Settled damage/shot events feed VFX; VFX do not move bodies or edit collision/damage results.

COV owns its original cover durability, not a duplicate P12 pool. Only actual shell collision invokes it. HAZ requests resolve one authoritative attached vehicle part per original target/cadence; telegraph precedes damage. Covers/hazards are additions to existing arena layouts, excluding boundary walls and vehicle spawns. Existing six-stage route/progression/rewards remain authoritative.

COM and SEN share a physical-device availability/activation/HUD boundary, but preserve distinct semantics: COM reports visible enemy structural HP through LOS/cone; SEN reports marked nearby enemy/salvage/objective/hazard signals, without revealing structural HP through walls. New scanner devices reuse four-cell XYZ/ports/one owner and existing inventory/repair/salvage. No per-frame scene searches. Defaults/profiles come from the pinned source, not invented package parameters.

Race, reactor defense and MAPR6 road driving enter separate menu/workshop activities using the actual R3/R6 vehicle, owned runtime roots and explicit return/restart. They do not replace Gauntlet arenas or alter saved workshop/campaign state. RAC advances its original clock once; OBJ uses its original integrity as the reactor's only pool and real enemy hit routing. MAPR6 is tested for actual road/junction driving/collision before any campaign replacement (replacement is not required in this iteration).

## Branches, evidence and gates

Each implemented mechanic receives a frozen `feature/rrl-<id>-integration` checkpoint branch, its own report and a Draft PR stacked on the preceding frozen feature checkpoint. The aggregate branch advances only with completed mechanic commits. A final aggregate Draft PR targets frozen W5. This keeps each mechanic diff reviewable without merging any shared branch or moving W5.

Run meaningful targeted Edit/Play tests after each package, compile all imported code, and native Windows checks on each completed wave. Full regression only at wave closure, not after documentation or tiny changes. Package a candidate containing only verified functionality; blocked mechanics stay absent. Label API fixtures, input-pilot gameplay and human UI evidence separately. Preserve known W5 UIA shutdown limitation; clean no-client controls rather than claiming it fixed.
