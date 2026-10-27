# B01-21 / N29-21 — Trackmania-like future course editor DATA CONTRACT (RESEARCH ONLY)

**TASK_ID** `N29-B01-21-FUTURE-TRACK-EDITOR-CONTRACT-20261027` | **OWNER** B01-21 | **STATE** `RESEARCH_DRAFT` pending HQ/Design approval | **QA_KIND** `SOURCE_GROUNDED_SPECIFICATION` | **IMPLEMENTATION** `NOT_AUTHORIZED / NOT_RUN`.

References: [B01-00 dependency queue #297](https://github.com/C8LyNn/RoboRougelike/issues/297) N29-21, exclusive producers policy [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), actual existing B01-20 CHL #285 original producer **`077e692785aff2990a2d72a80fd1db62ac57b0e8`**. Our executed source-only 20-gate-grid / 23-test proof [B01-21 PR #295](https://github.com/C8LyNn/RoboRougelike/pull/295), N27 [README](README.md). No third Unity runtime pack; no new generator, UI code, authority/adapter, scene or build feature; no Codex/producer file changes.

## Problem and contract boundary
A future editor must turn **authored course data** into an **inspectable physical course and gate order**, while never confusing a geometrically plausible static configuration with PhysX travelability. Existing CHL runtime scene is an intentionally handcrafted source-generated obstacle demo, NOT an editor nor an authorized canonical R3/R6 track system. This document describes a proposed interchange format, downstream validation obligations and reversible editor operations, not acceptance or implementation.

An editor may visualize `SOURCE_VALID` (unit/construction rules) but may not label a course `PHYSICS_VALID` or `COMPLETE` until **real same-version Unity** traces satisfy independent QA. Proposal avoids using CHL runtime classes as serialization authority; do not make `CHL003CourseController` an editor API. If adopted later, an explicit B01-00 release slot, B01-10 functional signoff, Codex domain ACK and owners must allocate new work.

## Proposed versioned, source-data-only shape (example, NOT canonical game save)

```json
{
  "schemaVersion": "track-definition/0.1-research",
  "trackId": "example-isolated-chl-snapshot",
  "units": "meters",
  "coordinateFrame": {"up": "+Y", "forward": "+Z", "right": "+X", "handedness": "Unity"},
  "origin": [0.0, 0.0, 0.0],
  "declaredSource": {
    "repository": "C8LyNn/RoboRougelike",
    "commit": "077e692785aff2990a2d72a80fd1db62ac57b0e8",
    "sourceKind": "CHL003_DEMO_AUTHORING",
    "nativeValidation": null
  },
  "segments": [
    {"id":"s-ground","role":"drivable","shape":"box","center":[0,-0.35,4],"size":[5.4,0.7,21],"eulerPitchDegrees":0},
    {"id":"s-barrier","role":"obstacle","shape":"box","center":[0,0.13,8.7],"size":[3.8,0.26,0.45],"eulerPitchDegrees":0},
    {"id":"s-ramp","role":"drivable","shape":"box","center":[0,0.54,16],"size":[3.7,0.55,7],"eulerPitchDegrees":-12},
    {"id":"s-bridge","role":"drivable","shape":"box","center":[0,1.38,24.5],"size":[3.7,0.6,10],"eulerPitchDegrees":0},
    {"id":"s-final","role":"drivable","shape":"box","center":[0,1.38,34.5],"size":[4.2,0.6,11],"eulerPitchDegrees":0}
  ],
  "gates": [
    {"id":"cp0","sequence":0,"kind":"checkpoint","center":[0,2.25,5],"boxSize":[3.7,4.5,0.16]},
    {"id":"cp1","sequence":1,"kind":"checkpoint","center":[0,2.25,11.5],"boxSize":[3.7,4.5,0.16]},
    {"id":"cp2","sequence":2,"kind":"checkpoint","center":[0,2.25,21],"boxSize":[3.7,4.5,0.16]},
    {"id":"cp3","sequence":3,"kind":"checkpoint","center":[0,2.25,29],"boxSize":[3.7,4.5,0.16]},
    {"id":"finish","sequence":4,"kind":"finish","center":[0,2.25,38],"boxSize":[3.7,4.5,0.16]}
  ],
  "clearancePolicy": {
    "sourceMinimumSideMeters": 0.25,
    "forwardCrossingRequired": true,
    "worldColliderEnvelopeRequired": true,
    "realPhysicsRequiredForApproval": true
  },
  "acceptance": {"sourceGeometry": "NOT_RUN", "nativeLightRuns": null,
                 "nativeHeavyRuns": null, "independentQa": null, "g2": null}
}
```

This **example** transcribes actual original latest CHL source literals; it deliberately does NOT claim that the example is a serializable Unity map, runnable scene, physically traversable or approved. `null` means absence of evidence, NEVER PASS. Shape encodes world BoxCollider authored geometry, but actual eventual renderer, wheel colliders, materials, spline, suspension and Physics Material are unknown (no invented defaults). A source checksum/provenance attaches to input code version, not to any native physics trial.

## Required invariant and source-only negative cases

| Contract / gate | Source-only rejection | Native receiver |
|---|---|---|
| Identity and version | nonunique segment/gate IDs, unknown schema, wrong source SHA, nonfinite/NaN, missing units | source+scene exact SHA |
| Dimensions | zero/negative size, inverted axis, unbounded grid, source marker/render assumed collidable | actual Collider & mesh bounds |
| Track continuity | ramp top-end gap/rise, overhang, unsupported driving segment, missing exit area become **review findings**, not automatic native approval | full wheel contact and suspension at seam |
| CP order | duplicate/missing sequence, out-of-order CP, finish before final CP, unexpected co-located finish | actual ordered CP transition and same-step arbiter |
| Gate clearance | compare all rigid hull+wheel envelope at meaningful yaw and offsets; reject if width < actor envelope + 2×review margin | actual `Collider.bounds`, `OnTriggerStay` and forward center crossing |
| End runway | finish may be topologically on last segment but max-speed/yaw front overhang crosses segment end before deferred completion | finish tick, forward hull, wheel support and resulting state |
| Authority | `SOURCE_VALID` does not authorize native `PHYSICS_VALID`, progression/rewards/saves | B01-60 independent Unity runs and B01-00 explicit gate |

Important measured constraints from N27 exact input (all **source projections**, not native): ramp top exit (Z≈19.3663,Y≈1.5367) to bridge (Z=19.5,Y=1.68), gap≈.13366m and height delta≈.14332m; bridge→final overlap0.50m; Heavy source forward hull at FINISH Z38 to final end Z40 leaves 0.30m yaw0 / ~0.143m yaw8; Light yaw0 leaves ~0.84m. These must become explicit editor *risk diagnostics* and independent replay coordinates, never an invented `PASS`.

## Minimal authoring operation / reversible state machine (proposal)

`CreateDraft -> EditSegment/EditObstacle/EditGate -> ValidateSource -> SaveDraft -> RequestNativeQA -> [NativeResultAttached / Rework] -> IndependentQA -> HQDecision`.

Every edit is deterministic, undoable and references stable GUIDs/IDs; segment/gate coordinates are not primary scene object instance IDs. Undo restores **both** geometry and gates, preserving ordering and complete source snapshot hash. A change to a segment/gate invalidates **only proofs at older geometry revision**; never retain a green native badge after SHA changes. Preview may approximate clearance but must disclose UNKNOWN PhysX and wheel contact. A map import that lacks proofs stays `DRAFT`; do not auto-wire to R3 saves, rewards, matchmaking or Codex current host.

## Proof receipt contract for later integration (not actual current data)

**One receipt PER real independent run**, and each receipt must be bound to exact `trackSourceHash`, exact built Unity scene/import revision, vehicle physics version/rig profile, fixed timestep, input profile, attempt index and producer source Git SHA. Native fields: raw `Editor.log` and NUnit XML, process exit, exact run start/end, each gate center-plane before/after crossing tick, actor root X/Y/Z/rot, real `Collider.bounds` and active supporting wheel contact set with normals, speed, ramp contact, bridge contact, fall/retry, completion and finish arbitration timestamp. **At least three ORIGINAL complete Light runs and three ORIGINAL complete Heavy runs** and negative retry/wrong-order tests, not 6 synthetic/mocked rows. Independently verify by B01-60 and if relevant B01-10 scoring parser. No license, no source-identical native data: `BLOCKED_BY NATIVE_RUNNER`.

## Decision points, owners, actual authorization
- B01-10: defines acceptable physics gameplay difficulty/track building budget and if strict 0.25m/corridor is a player-facing restriction; cannot derive from source geometry alone.
- B01-12/B01-11: user research on warning readability and undo/redo behavior, not real editor implementation.
- B01-20: sole current CHL-003 runtime owner; nothing in this proposal authorizes changing B01-20 source or adding a new editor to CHL.
- B01-23: PARTSIZE shape/sockets separately owned; future track element attachment cannot reuse typed socket authority without owner design approval.
- B01-40/60: future G0/G1 intake and independent native evidence, exact hash.
- B01-00: concept choose/hold and eventually a *separate* authorized future slot; G2 and Codex integration never implied by this document.

**NEXT_UNBLOCKS:** HQ/B01-10 can discuss a scoped future editor design without guessing data fields; a later UI study can critique these design diagnostics; **no Unity implementation task is currently unlocked**. **BLOCKED_BY native evidence for physical acceptance** and **future HQ explicit implementation slot**. N28-21 still waits for true CHL C3 3+3 telemetry. Rollback: remove only this research Markdown file from the B01-21-owned QA branch.
