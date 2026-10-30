# B01-13 PASS17 WAVE23 — ECON-SAFETY-PLAYTEST-PROTOCOL-001/R1

Owner B01-13 Human Playtesting | HQ #731 | stacked Draft against the existing B01-13 W22 #722 branch. No other source was changed.

## New functional work
playtest_protocol.mjs imports and DIRECTLY RUNS the actual immutable W22 Research/B01-13/P17T/recovery_routes.mjs (commit 5eb72088015062962ba8e9ac60ae684c8fba4bdb; exact original blob d40993ae55940477a8086a403dede831d6419d62). This is NOT a clone of the W22 solver or any real ECON game authority. It adds explicit synthetic post-combat coreAlive and nextChallengeAvailable gates absent from W22. Dead Core / no next eligible encounter veto normal battle and any proposed emergency victory, even a loaner, WITHOUT issuing a fake victory ID, rewards or repair. Both facts are authored fixture assumptions, never an authenticated Unity state or player observation.

The code derives 32 cases (LIGHT/HEAVY x provisional one free per victory/run x STRICT/no loaner or ONE_UNAPPROVED_LOANER x four Core/next-challenge combinations), then makes **four balanced eight-task cohorts** with deterministic rotation. Each cohort uses all eight base robot/cadence/aid combinations and exactly two of each Core/next-challenge state; across four cohorts each of the 32 unique cases is covered once. It creates actual file outputs:
- w23-cohort-N-protocol.md — eight player-facing structured tasks, distinct OBSERVER ONLY answer key, consent and participant privacy instructions
- w23-cohort-N-responses.csv — 8 empty observer rows for normal-start choice, expected recovery, confidence 1-5 and comprehension 1-5, no name/email/username/IP or other participant-identifying columns, no actual observations
- w23-cohort-N-key.json — exact machine-readable synthetic source-derived scenario and assessment; original W22 route replay validated, with explicit 0 real human sessions, NO native Unity, and NOT APPROVED emergency loaner

## Reproduce locally
Requires Node.js 20+. Choose a **private already-existing writable directory OUTSIDE any Git checkout**. Example (replace path with an actual local scratch directory):

    node Research/B01-13/P17U/playtest_protocol.mjs --cohort 0 /private/empty-playtest-directory

Allowed cohorts are 0,1,2,3. Generates 3 files with exclusive creation (O_EXCL; on supported filesystems 0600). Preflights all three output names before writing; refuses overwrite, Git worktrees, bad parameters; no network, telemetry, user identifiers or Git side effects. On an unexpected mid-write failure, previously created owned outputs may require operator inspection; the producer NEVER deletes or overwrites foreign data. The dedicated GitHub Actions job renders all four cohorts into separate isolated runner-temp subdirectories, producing 12 real files and uploads ZIP artifacts after exact-source tests.

## Future REAL human protocol — NOT executed
Obtain explicit informed consent and make stopping optional. Do NOT record real name, IP, email, audio, live game account IDs or sensitive notes in the CSV. Human-playtest operator shows only the eight cards, not answer key. Ask participants: Is NORMAL next-challenge start possible? Is there a valid recovery route or only an unapproved hypothetical one? Rate confidence and readability 1-5. The observer independently validates the game's actual Core, deployability, challenge eligibility, pending 0HP and future true combat wins, using native licensed Unity only after owner approval. Avoid presenting synthetic model outputs as human reactions or actual Unity playability. It remains possible that the real game treats essential parts differently.

The synthetic suite deliberately distinguishes CORE_LOST, NO_NEXT_CHALLENGE, ESSENTIAL_0HP, CAN_START_NOW, plus conservative recovery classes. The loaner is never approved by this tool; one-per-victory versus one-per-run and sample 100 Scrap/70 quote remain hypothetical, not accepted final economy.

## Source boundary and testing
This W23 candidate is a STACKED PR based ON the original unmerged W22 #722 branch, not on main. New PR diff must contain EXACTLY 6 new owner-only paths: Research/B01-13/P17U/playtest_protocol.mjs, test_playtest_protocol.mjs, README.md, verify_package.py, manifest.json, and .github/workflows/b01-13-p17w23-econ-human-protocol.yml. Manifest pins 5 new original source/test/docs/workflow Git blobs AND exact 1 inherited immutable W22 source blob; no source edits to dependency. Do not merge W23 without separately accepting W22, source/G0 review and correct retargeting.

Original frozen gameplay #510 d94400d95d13db7f27e1533df05c9292d5380431, Save #556 33094f154c7b91119b36d58aab285e6e83c5de57, historic host #596 efb2b06eafedab5469b57f163152d96aca3c5915, R4 #647 09c1809b73324a6d31c13e5d0143de62ac1e3ca6 and original ECON #252 12572117a3dce369257feabc57021cde58023369 all remain untouched. Selected R5 #690, V3 Save API #729, session #712/#669, ECON V3 #694/#716 are separate producer packages NOT imported/copied. All rejected #719/#693/#682/#695/#652/#604/#644 variants ALT_HOLD. No real SaveLoad, no direct Unity, no Codex ACK, no source overwrites.

Dedicated GitHub Actions source matrix Windows2025/Ubuntu24.04 x Node20/22 first verifies six physical actual Git blob SHA1/bytes and SHA256, then executes original immutable W22 58-test real Node suite plus 52 authored original W23 protocol tests in each OS, then physically creates 4 x 3 reports and uploads artifacts. Node unit tests include all 32 synthetic scenario combinations, balanced cohort coverage, invalid Core/next, unapproved loaner rejection, original W22 route replay, exact JSON/CSV, CLI subprocess, no-overwrite and Git output protection. Source-only green is not independent game/engine security G0 or real Unity native G1. Unity tests and genuine human study NOT_RUN; actual participants 0; G2 HOLD.

B01-10/B01-22 receive Core+challenge prerequisite/UX taxonomy; B01-40 receives exact-source G0 and non-destructive rollback; B01-60 receives operator protocol and later independently consented actual game/player testing; B01-00/#154 must approve any emergency policy. Roll back by abandoning isolated Draft and inspection of only operator-owned disposable exports; do not delete dependency W22, real game or user data. Six lifecycle stages separately tracked.
