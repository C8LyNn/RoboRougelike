# CONCEPT-MOBILITY-001 — B01-31 / W17

**State:** OPTION_SET_READY / HQ_DECISION_PENDING / DESIGN RESEARCH ONLY  
**Authority:** [B01-00 W17](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6438657467), [CONCEPT LAB #287](https://github.com/C8LyNn/RoboRougelike/issues/287), [ownership #276](https://github.com/C8LyNn/RoboRougelike/issues/276).
**Files:** `comparison.svg` (one aligned comparison), `A-wheel.svg`, `B-track.svg`, `C-omni.svg`, `metadata.json`.

## Functional brief (not final gameplay design)

**Question:** how can a mobility part advertise its *method of movement* instantly to a player who assembles modular vehicles? The concept must be distinguishable when tiny, partially hidden and viewed at 3/4 camera angle. Compare three original forms without implying that their mechanisms already work in RoboRougelike. Each drawing includes the same small cube labelled 0.55 m as a **grid reference**, not a measured footprint. All body dimensions are illustrative; the cube is not an authoritative socket or collision enclosure.

| Concept | Visual language / what function it communicates | Two primary risks |
| --- | --- | --- |
| **CM001-A-WHEEL** | Four exposed round tires, accessible struts and an open structural saddle. Signals elementary contact-based locomotion, module repairability and compactness. | Wheel travel could occlude module mounts; steering/suspension articulation and collision swept-volume UNKNOWN. |
| **CM001-B-TRACK** | Two continuous reinforced elongated belt loops around a protected lower hull. Communicates a broad contact surface, heavily armored mobility, and a substantially wider mechanical envelope. | Increased collision/footprint and side overhang; steering radius/contact fidelity and snagging UNKNOWN. |
| **CM001-C-OMNI** | Floating-centered tapered hull with four independent luminous ring pods. Communicates omni-directional/experimental mobility and separate actuators, **not validated hovering physics**. | Requires a new movement/contact contract; pod clearance, camera visibility and actual ground interaction UNKNOWN. |

## Common design constraints (intent only)

- A/B/C use comparable 3/4 camera and a 0.55 m reference cube.
- Their mechanisms are intentionally **different shape families**, not a shared skin or color swap.
- Each must expose a central hypothetical module-body region; **no actual socket geometry, typed mount, pivot or physical attachment is asserted**.
- Clear read at 128 px thumbnail is an acceptance hypothesis, not a pass.
- PARTSIZE #286 actual validation/typed mounts/physics broadphase override all silhouettes. Avoid external collision claims and any third package.
- No resemblance intended to a specific commercial game asset; no external graphics imported.

## Visual review plan for B01-00 + B01-11/12

1. Present three images side-by-side without explanatory labels (or crop text); request blind interpretation: `wheel`, `tracked`, `omni/unknown` and confidence 1–5. **Threshold proposal:** ≥4/5 correct wheel and tracked, ≥3/5 identify C as explicitly *unconventional*.
2. Show at **128 px and 64 px** approximate in-game thumbnail sizes; flag silhouette ambiguity and interference between chassis outline and locomotion outline.
3. Ask participants to point where they would expect attachable surface and what can collide; count any falsely perceived `verified mount` as a **research risk**.
4. Compare icon readability in B01-12's throwaway garage UX HTML concept, if authorized. No false "real PARTSIZE feedback" claims.
5. B01-00 chooses `SELECT A` / `SELECT B` / `SELECT C` / `MIX A+B` / `REVISE` / `PARKED`. **No answer = no approval**.

## Handoff and gates

Only concept options are complete. **B01-23** measures physical sockets/bounds after HQ selection; **B01-10** specifies gameplay implications after selection per #287; **B01-30** can consider a 3D blockout only with separate HQ authorization and available source-production slot. Codex receives **no Unity asset** or integration work from this research PR.

- Source images: original vector SVG only, editable, no 3D exports.
- Prototype physical tests: NOT_RUN. Unity/PhysX: NOT_RUN. Garage/UI playtest: NOT_RUN.
- Physical geometry dimensions, pivot/attachment envelopes, mass, hitboxes: UNKNOWN.
- This research branch is **not** a third Unity runtime package, not Model Lab M01–M05 and not G2.
- Reuse/provenance/licensing: original source shapes in SVG, procedural human-directed vector art with no third-party source art. Repo-wide licensing remains authoritative.

**Next receiver action:** HQ selects MIX/SELECT/REVISE based on this contact sheet, then B01-10/B01-23/B01-30 can draft the approved design and verified tech-art measurements.
