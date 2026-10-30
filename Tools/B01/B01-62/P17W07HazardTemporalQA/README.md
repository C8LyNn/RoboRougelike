# B01-62 / PASS17 WAVE07 — HAZARD-TEMPORAL-ROUTE-QA-001 R1

**Independent cross-package DATA INTEGRITY receiver** for actual producer B01-25 HAZARDPULSE-001 vR1, [Draft #430](https://github.com/C8LyNn/RoboRougelike/pull/430) exact commit 6095577ece27320b1a861a738cc23ba42d6793a6. No changes to the original owner, Codex or Unity Assets.

## The new failure mode being checked (not covered by the owner's existing suite)
Existing #430 publisher proves C# warning/active/recovery behavior and static (non-timed) MapDocument BFS. **That does not prove a traversable no-hit path under clock-alternating hazards.** This receiver builds a *time-expanded graph*, with 0.25-second substeps, waiting and four cardinal moves, and reconstructs a full source-grid route from actual original map spawn to finish while avoiding each author's original pulse radius throughout every transition. Replays the whole route and emits a physical JSON with waypoints, phase ticks, moves, waits, source SHA identity and exact limits. A denied path fails closed, not "PASS because two circles don't overlap".

**Critical experimental assumption**: a hypothetical point player moves 1 grid cell (4m) per second in straight cardinal segments, can stop instantly, and waits 1s per optional WAIT step. Source-grid obstacles kinds 1 (Wall), 6/7/8 (props/pillars/barriers) are conservatively treated as blocking at their CELL CENTERS; kinds 4/5 are start/finish markers not blockers. This is *not* actual robot acceleration, Light/Heavy dimensions, 3D colliders, PhysX, Unity or PlayMode. Route is a **conceptual proof of a possible schedule**, not a claim that the whole game is playable. This receiver does not import or build new gameplay.

## Exact original inputs, no fake duplicated map geometry
- B01-25 original package source commit 6095577ece27320b1a861a738cc23ba42d6793a6; original profiles.json Git blob c0b03adee398ed12fa535c85ca2506d090d40cca and original PulseCore.cs blob a7a2c69c80d43bf027f2b00896df72f892fa1a17. Both read via git show and git rev-parse, bytes tested against actual Git blob hash.
- CLEARANCELADDER original #359 map commit ad52a604ffa4f143d98e607049d6f79e0c7e580f, blob 53eb696950121465befea2855eda4d7268750016, 415 original map items.
- SLALOM original #346 map commit f565e250659692541d672cdab108c4cfb8cfdd31, blob 013f5fe2058e76233f580a439e8402c0fe09f124, 360 original map items.

The tool fails closed if source Git identities, map byte hashes, item counts, map version, profile-to-map cell centers, hazard phases/offsets or unsupported multi-cell geometry drift. All map/program bytes are only read; no write occurs inside producer trees.

## Real test plan
- 17 independently authored Python stdlib unittest cases including original two-map 0-hit route reconstruction and deterministic receipt.
- Source SHA pin, corrupted original raw bytes, duplicate zone ID, world coordinate mismatch, zone placed on original blocker, modified active duration, nonalternating offsets, changed level or wrong linked original map PR, original spawn enclosed after synthetic mutation, unmodeled multiscale part, unsupported original kind, wrong meters-per-cell, and stagger safety.
- Windows2025 and Ubuntu24.04 each execute original 16 tests and original receiver against exact Git original files; emits a physical route.json and log.
- Third job compares Windows and Ubuntu route.json bytes with cmp and records SHA-256.
- Expected route proof must be measured from CI and not fabricated in advance.

## Reproduce (read-only)
Requirements: clone of repo, Git access to three original commits, Python 3.12; NO Unity or external Python packages.

    git fetch --no-tags origin 6095577ece27320b1a861a738cc23ba42d6793a6
    git fetch --no-tags origin ad52a604ffa4f143d98e607049d6f79e0c7e580f
    git fetch --no-tags origin f565e250659692541d672cdab108c4cfb8cfdd31
    python -m unittest discover -s Tools/B01/B01-62/P17W07HazardTemporalQA -p test_receiver.py -v
    mkdir -p /tmp/b01-62-hazard-qa
    python Tools/B01/B01-62/P17W07HazardTemporalQA/receiver.py --repo . --out /tmp/b01-62-hazard-qa/route.json

Receiver refuses preexisting output path, never changes the original maps or producer files. Delete only self-created /tmp receiver output for rollback. On Windows use an equivalent new disposable output folder.

## Independent handoff and boundaries
B01-62 owns only Tools/B01/B01-62/P17W07HazardTemporalQA/** and .github/workflows/b01-62-p17w07-hazard-temporal.yml. B01-40 independently checks provenance, original intended 4 hazard placements and manifest; B01-60 checks source-based route proof and validates actual Rigidbody/vehicle physics in licensed Unity when possible. B01-00 decides subsequent #79 Codex queue; this QA PR does not claim CodeX ACK. Do not replace Codex canonical map/world/game systems, hazard implementation or run saves.

**G0 original source DATA QA** is measurable; **Unity G1/G2 / physical Light+Heavy travel** = NOT_RUN. If a route cannot be found using this abstract mobility model, it means the stricter safety case fails (not necessarily real game is softlocked). If a route exists it is NOT proof a real 2m-wide heavy rig can drive it without damage. The producer's physics integration remains Codex-owned and ACK_PENDING. No merge.

Rollback: close this Draft, remove only QA-owned files/isolated outputs; original source and user saves never change.
