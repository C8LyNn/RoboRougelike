# B01-13 | SAVELOAD-RECOVERY-METRICS-001/R1 | PASS17 / WAVE15

**Owner:** B01-13 HUMAN PLAYTESTING. **Dispatch:** #571, governance #276.

## Actual function

An offline Node20+ **structured observation timeline evaluator** for the usability of **recovering after a save/reload with detached or reattached parts and a denied dock attempt**. This answers a different question from frozen earlier B01-13 packages: #531 counterbalances Light/Heavy collision trial ordering; #539 compares manually observed module state; #569 pins real screenshots and markers. This package instead computes **time-to-notice, time-to-first-action, observed completion time, retries, assistance, ambiguous/absent feedback, recovery/abandon/stop counts and medians by scenario**. No changes to their frozen bytes. This is a usability-research instrument, not another Save/Load implementation, QA harness for #556, or visual screenshot tool.

**Selected dependencies are only citations/pins, never linked game source:**
- Secure Reattach #510 immutable SHA d94400d95d13db7f27e1533df05c9292d5380431.
- Sole Save/Load contract #556 immutable SHA 33094f154c7b91119b36d58aab285e6e83c5de57.
- #544/#549/#554, #511/#494 are HOLD; no code mixing, no game-host mutation.

### Technical offline CLI

Run outside repositories in a private directory, after obtaining an authorized **real Player EXE** whose physical bytes the tool hashes (this SHA256 is only the EXE, **not** a whole verified build or source-to-build attestation).

    node Research/B01-13/P17M/recovery_timeline.mjs template --player /private/Game.exe --trial T0001 --scenario DETACHED_AT_SAVE --output /private/t0001.json

The generated template is `filled:false` and **cannot be scored**. Only an operator who actually witnessed a procedure may fill it. Allowed scenarios:
- `DETACHED_AT_SAVE`: an observed detached part is present when Save is invoked.
- `REATTACHED_AT_SAVE`: part reattached immediately before Save.
- `DOCK_DENIED`: a rejected docking attempt is made visible.

Event schema is **only** `{"at_ms":0,"event":"CUE","value":"NONE"}`. Required initial event `CUE` at 0 ms, chronological event order, final `END` with value `RECOVERED`, `ABANDONED` or `STOPPED`. Accepted intermediate event types: `NOTICE` (only once), `ACTION` (retries counted), `FEEDBACK` (CLEAR / AMBIGUOUS / ABSENT, only after some ACTION), `HELP` (operator assistance). All other values must equal NONE. At least one ACTION and one FEEDBACK are required for an operator-reported RECOVERED. Timecodes are integer ms 0..600000, 128 events max. Terminal END is last. An observed RECOVERED is a **subjective operator transcription**, never proof that the real save or part GUID survived.

After recording events, set `filled:true` and privately run:

    node Research/B01-13/P17M/recovery_timeline.mjs score --input /private/t0001.json --output /private/t0001_metrics.json

To aggregate multiple private trial files, create one `batch.json` with exactly keys `schema` and `trials`:

    {"schema":"B01-13-RECOVERY-BATCH/R1","trials":[<fully populated technical trial records>]}

Then:

    node Research/B01-13/P17M/recovery_timeline.mjs batch --input /private/batch.json --output /private/summary.json
    node --test Research/B01-13/P17M/test_recovery_timeline.mjs
    python3 Research/B01-13/P17M/verify_package.py

This refuses duplicate trial IDs and mixed EXE SHA256, reports per-scenario and overall medians and count metrics; default `independent_within_target` only when operator-reported recovered within 30 seconds **without HELP**. These are *descriptive observations*, not a statistically justified usability acceptance gate. Zero participant/human verification and G1 NOT_RUN are hardcoded. Not a native PhysX test.

## Privacy and provenance

Strict schemas disallow free text, participant names, usernames, interviews, audio/video and incidental PII. IDs are nonidentifying `T0001`–`T9999` only; sessions must be tracked outside source control in accordance with an authorized consent/data-retention process. Private output uses O_EXCL, fsync and 0600 where supported, rejects Git ancestor. No network calls. File size <=512KiB. Windows symlink/junction and ACL hardening not independently proved. Actual study requires opt-in/consent, authorized native player build, verified build provenance and real observation; no actual human participation was conducted by this package. Synthetic fixture tests only. **Source CI passing cannot verify human or Unity behavior**.

## Evidence, rollback, QA handoff

Owned paths: `Research/B01-13/P17M/**` + dedicated `.github/workflows/b01-13-p17w15-recovery-timeline.yml`. No `Assets/**`, no main/develop, no Codex, #510, #556, #531/#539/#569, other-owner edits. Manifest pins original source/doc/workflow Git blobs + byte lengths; CI runs original executable test file on Ubuntu24.04/Windows2025 × Node20/22, includes negative corruption/security trials and real temporary files/subprocess cases. Exact-head actual results must be read from Actions, not inferred from written tests.

Receiver **B01-40**: independent G0 scope/manifest/frozen SHA, **B01-60**: independent original source rerun, **B01-00**: choose whether to include in an authorized human playtest with consent. All Source_G0 independent PENDING, Unity G1 NOT_RUN, G2 HOLD, Codex ACK NONE, Save/Load live bridge NOT_VERIFIED. This does not resolve engineering host bridge work of B01-23/62, QA B01-61 or migration B01-40. Rollback = discard isolated Draft unmerged. Purge actual private operator datasets only using approved retention SOP.
