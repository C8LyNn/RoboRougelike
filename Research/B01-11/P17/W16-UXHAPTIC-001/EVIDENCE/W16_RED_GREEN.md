# B01-11 P17/W16 UXHAPTIC-001/R1 — observed RED→GREEN

## Original feature baseline
First version source commit `79209c251c441fca63b3efc9672cede4c5e3df7e` compiled and ran original .NET8 direct linked C# source across Ubuntu and Windows. Actual [Actions #41096654228](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096654228) **111/111 PASS per OS**. The baseline handled priority/preemption, feedback mute, directional motor hints, anti-spam, immutable decisions, run ID and source sequence validation.

## Defect: stale prior-run critical incorrectly asked for new visual warning
`HapticGate.Record` ignores `cue.RunId < _runId` as intended, but original `HapticDecision` previously set `RequireVisibleCriticalAlert` solely by kind. Thus a **delayed old-run CriticalDamage**, correctly ignored for motors, still requested an on-screen critical warning for the new run. This is a deterministic logic bug at source; it was not seen in a real Unity HUD.

**Test-first RED** commit `31f0ca7a6937fcc7c3a37d29f63058b48117e502` added 3 original C# tests while original runtime file remained unchanged. [Actions #41096911640](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096911640) on Ubuntu **112 PASS/2 FAIL**, Windows **112 PASS/2 FAIL**; source logs independently read. The two failing cases were stale CriticalDamage before and after new-run reset. A third valid-current-run/disabled Critical test already passed.

**Real source fix** commit `b9974591f5360831526e0364d42b0f7fc66b4956` binds mandatory visual flag to `Kind==CriticalDamage && Outcome!=IgnoredOldRun`, with valid current-run critical still requiring visuals regardless of motor mute. [GREEN #41097067292](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097067292) **114 PASS/0 FAIL Ubuntu +114 PASS/0 FAIL Windows** from original production C# source, not mocks. A separate final packaged SHA cross-platform test follows.

## Gates and rollback
No canonical Unity game/controller/motor integration, no frozen #510/#556 source, no Codex assets, no merge. Producer source GREEN, B01-40 external exact-SHA G0 pending, B01-60 independent original source QA pending, Unity G1 NOT_RUN, B01-00 controlled OFFER pending, Codex ACK pending. Rollback drops producer-owned isolated Research subtree and dedicated workflow.
