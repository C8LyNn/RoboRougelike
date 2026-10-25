# OBJ-001 playable reactor defense

Owner request: implement an actual optional reactor-defense activity, reusing PR14; include PR125 physics, no main/develop merge or MAPGEN99/103. Execute autonomously after audit, not a plan-only handoff. Unity6000.3.14f1 remains fixed.

## Audited sources / isolation
- Base PR125 OPEN/DRAFT HEAD916654634d1bfdc2bbc14e64b9eeac856ede1fa8, latest gameplay523046f and corrected Editor fixtures54f0192. W8 and shared branches unchanged. This is an explicit stacked dependency, not an assertion that B01-60 accepted PHY-QA-001. Its audit records independent native acceptance pending.
- PR14 OPEN/DRAFT headcdc36875efe4b5661a7c41c0b7b60d0010850e2b: standalone ReactorDefenseObjective HP/time/state/events, demo and11 EditMode/3 PlayMode, not host integration. Reuse original files/GUIDs; quantify original retained lines/hashes at closure.
- DEF PR22/26 are directional shield and point defense, not reactor activities. Branch/title/source searches found no host OBJ integration. Model M05-011 is visual only. MAPGEN and model branches are excluded.
- New checkout C:/Users/kubap/Desktop/Prototypy/RoboRougelike-OBJ001, feature/rrl-obj001-integration; old W6-W9 checkout stays untouched. Native worktree tool returned Not a git repository for this projectless chat; native git fallback follows the existing sibling checkout layout. No active Unity process at audit.

## Chosen approach
Extend the existing isolated objective with optional external integrity authority and explicit completion, preserving standalone timed behavior/tests. The game binds existing P12PipelineSafeDamageable as the sole HP/damage authority. An R1 partial controller owns waves and transitions; existing encounter spawner, P12 AI, projectile/weapon pipeline, R6 body/contact/Core recovery, salvage ledger, reward settlement and garage remain authoritative. No new HP/AI/economy solver.

Alternative separate demo scene would not deliver the requested game loop. Replacing Gauntlet with defense would remove existing activity. Use an opt-in encounter in the existing expedition instead.

## First playable defaults (implementation choices)
- Three waves of2,2,3 existing enemies;3 simulation seconds between waves. Reactor600HP, ordinary existing arena, no new map generator/models. Reward uses the current stage's authored scrap and existing1-of-3 bonus/up-to-3 salvage settlement, once after the final wave.
- Menu button opens a real preparation garage with the chosen starter and0 starting scrap, then defense. Normal expedition garage can choose defense for its next slot. Sandbox600-scrap workshop cannot enter a reward-bearing defense, preventing budget laundering.
- Enemies use existing aim/movement/fire. Retarget between reactor and player through a small existing-controller setter and generic damageable fallback; threat near player can draw fire, with both attack targets represented in each wave.
- Victory requires every wave cleared and a live reactor/player. Reactor or player defeat follows existing Lost screen; simultaneous physics-step fatal reactor damage takes precedence over last-enemy clear. Pause freezes waves/time. Restart rebuilds the scene and removes objective, enemies, shots and activity state.
- HUD shows reactor HP, wave/3, alive enemies and intermission; label defense explicitly. Ordinary six-stage route, garage/range and non-defense encounters keep their existing behavior.

## Evidence / boundaries
Use actual Unity RED/GREEN tests, original package tests, physical projectile damage and actual AI target changes, lifecycle/reward/garage/restart/pause/race tests, regression and full EditMode/PlayMode when available. Build retail+QA Windows and distinguish native controlled API fixtures, ordinary-input pilot/UI and human tests. Known UIA-attached C0000005 is not declared fixed. Preserve W7/W8/B01 release archives. Final Draft PR targets PR125 branch; publish source SHA, package reuse, actual PASS/FAIL/NOT_RUN, instructions, build and limits. No shared merge, external coordination messages or next package work.
