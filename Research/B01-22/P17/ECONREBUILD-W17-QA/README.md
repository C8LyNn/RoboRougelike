# B01-22 — P17 W17 original frozen ECON #252 read-only QA

Source authority: [ECONREBUILD #252](https://github.com/C8LyNn/RoboRougelike/pull/252) at
`12572117a3dce369257feabc57021cde58023369`, actual immutable C#
Git blob `cac95b28647528a7a46cb818c6d78037dc3e80a2`.
Original .NET8 surrogate source from the SAME producer branch blob
`2189c750566d692361702268580a93d3b660536f`. This QA branch is
stacked **on but does not edit** producer #252. No source code copied into
QA project: the project compiles original producer file in place.

**New real regression (not existing original 25 tests):** Save a
valid but old empty checkpoint (wins=0); perform legitimate win
`win-0001`; the original source accepts restoring the old checkpoint
and then accepts the exact same `win-0001` again while its own host
has no outstanding reservation. Victory event history is downgraded.
Positive control: current reservation-containing checkpoint reloads
correctly with physical reservation and free-slot state unchanged.

**Semantics:** `QA_DETECTOR_PASS` means the weakness actually reproduced
on unchanged original C#. `SOURCE_RELEASE_RED` means source has no
anti-rollback/monotonic host generation defense; do not call this G0
PASS or production-ready. The test does NOT prove exploitation of the
actual game's host: canonical R3 integration, owner-save transaction
and durable generation/authority are pending. An authorized integration
must require the host and source state to commit in one transaction,
reject stale checkpoints and event replays across process restarts.

Only a new owner-authorized version can fix #252. Current frozen
#510 and #556, Codex, ECONLEGENDARY #263, wallet and game Assets
are untouched. Native Unity / PhysX NOT_RUN. No merge / no G2.

## PASS17 W18 — original two-process durable event-ID regression

The original linked frozen #252 C# now has two additional executable QA modes: make a real pre-win and post-win source checkpoint and serialize a physical authoritative host snapshot to a local test JSON file; execute a NEW OS process to reconstruct that host and service from those bytes. A current checkpoint must reject replay of its consumed victory ID; the older structurally valid checkpoint is then accepted and the same ID replayed. CI executes two separate dotnet runs and records both source-state and process-boundary evidence on Windows and Ubuntu. Detector PASS means original source RELEASE RED/HOLD, not a successful release; tested host is a labeled surrogate, not canonical Unity/R3 or PhysX. No modifications to frozen packages and no integration claim.

## Machine-readable producer/QA ownership

`manifest.json` declares the QA package ID/version, all five and only five owned paths, both frozen Git blob identities and the original producer SHA, actual two-OS executable cases, explicit release RED/NATIVE_NOT_RUN semantics and a non-destructive no-install rollback. CI validates the manifest before running the compiled source; the original producer blobs remain checked against on-disk and HEAD Git object identities. No QA package file is for installation into canonical Unity.
