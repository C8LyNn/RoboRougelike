# B01-60 W16 independent QA preflight (CHL-003 + PARTSIZE-001)

QA-ONLY support tooling, **not** a third gameplay package. Source ownership: B01-60 only. This cannot authenticate the Unity process which produced an input: even if input is consistent, the verdict is `CONSISTENT_UNVERIFIED_INPUT`, independent native Unity `NOT_RUN`, G2 `HOLD`. No edits to game, prefabs, producer paths, or Codex worktrees.

Governance: Issue #276; producer issues CHL #91 and PARTSIZE #141. Existing PARTSIZE design PR #150 lists 14 proposed kinds, but its Unity Collider/mesh measurements are null. Thus it is NOT input for native positive acceptance. The CHL R1 gameplay PR/immutable SHA has not yet been published at this point.

## Commands

Run in Tools/B01/QA-W16-ACCEPTANCE:

    python -m unittest -v test_qa_w16.py
    python verify_manifest.py
    python qa_w16.py <producer_observation.json>

Exit 0 validates *shape and internal invariants of self-reported data only*, not authenticity. Exit 2 = FAIL_CLOSED. Origin must be SYNTHETIC_TEST with native_status NOT_RUN or EXTERNAL_PRODUCER with native_status EXTERNAL_UNVERIFIED. The SHA256-like fields are syntax checked but not yet verified against original files. Untrusted self-declared Unity.PhysX strings are not proof of actual physics.

## CHL-003 event evidence contract

Schema b01-60-w16-qa-input-v1, package_id CHL-003, version R1, exact 40-hex source_sha, course with seed and 3–5 ordered checkpoint IDs. Two physically different Rigidbody rigs: mass_kg and width_m must differ; at least 3 distinct trial replicates per rig and same seed. Each run needs finite positive displacement, fixed physics steps, wheel contact and control samples, a telemetry SHA256-shaped digest, and events [begin, ordered checkpoints, finish] with the same actor ID. Wrong actors, skipped/duplicated checkpoints, early/duplicate finish, NaN, no physical inputs/contacts all fail closed.

This does NOT cover or certify the native fall/retry/pause/manual/URP/GUI requirements. They still require actual real Unity PlayMode/native logs and independent B01-60 reproduction on the same frozen producer SHA. A self-written JSON cannot certify a single drive.

## PARTSIZE-001 geometric observation contract

Grid 0.55m, each part with integer logical cells, unique part ID, physical_owner_id, mesh SHA256-shaped digest and FOUR yaw 0/90/180/270 measured collider AABBs. Data gate checks finite positive dimensions, 90° X/Z swap, 0°/180° and 90°/270° consistency, nominal footprint and overlap depth. Conservative data-only tolerance 0.025m and overlap screen 0.005m ARE NOT approved production collider rules. Legitimate protruding function envelopes require separate explicit physical definition and approval; do not change geometry to satisfy this QA-only heuristic. Actual Unity Collider measurements, 14 proposed kinds, different joint types, mounted-socket collisions and save migration are NOT_RUN / NOT_VERIFIED.

## Rollback and ownership

Delete this QA-only directory and its dedicated workflow on its own isolated branch. No Unity Assets, global ProjectSettings, Codex integration, producer ownership or source SHA changes. Separate G0 physical artifact and native G1 remain mandatory. B01-60 will reject self-reported positive telemetry that cannot be tied to actual immutable source, process exit, NUnit XML, Editor.log, raw sample and controlled Windows/Unity execution.
