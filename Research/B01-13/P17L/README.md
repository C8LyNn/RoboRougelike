# B01-13 SAVE-SCREENSHOT-WITNESS-001/R1 — PASS17 WAVE14

**Role:** B01-13 HUMAN PLAYTESTING. Dispatch #552, governance #276.

## Purpose and novelty

Previous frozen [W13 Draft #539](https://github.com/C8LyNn/RoboRougelike/pull/539) compares operator-reported visual states before Save and after Load. Those observations were **not cryptographically bound to screenshot files**. This W14 tool physically checks *two actual PNG binaries*, binds each to a SHA256 and annotated image dimensions, verifies every visually reported module has a uniquely identified in-bounds image marker for the corresponding stage, then **calls the exact unchanged W13 original evaluate()**. This is screenshot-evidence provenance, not another Save/Load source contract, native gameplay test, combat screenshot/reticle annotation, or W12 Light/Heavy scheduling tool.

- Immutable upstream W13 #539 source commit: aee4315304ca3e2fe3e155242d7d181260089efc
- Actual upstream W13 save_visual_pairs.mjs original Git blob: 94c621f54fffd7e1325700b88b441652755c0feb
- Only selected Secure Reattach V2 #510: d94400d95d13db7f27e1533df05c9292d5380431
- #511, #494: ALTERNATIVE_HOLD. Source #510 is never modified.
- #544/#549 Save contract choice, #550 original independent security defects, #536/#542 rollback: engineering owners only B01-23/40/61/62. This tool does **not** fix them.

## Usage on an operator-owned private machine

Prerequisites: Node 20+, Python 3.11+ for manifest QA, Git with the original immutable W13 source SHA. Collect before.png and after.png in a private directory **outside Git**. Accepted PNG: 8-bit RGB/RGBA, noninterlaced, <=8192 pixels per edge, <=16 million pixels, <=24 MB physical file; CRC and lossless zlib pixel payload checked, with limited ancillary chunks (sRGB, gAMA, pHYs). Unsupported PNGs fail closed and should be converted in private, rehashed, without uploading. The tool does not classify pixels, prove that a marked pixel contains a module, or prove the PNG was taken from Unity.

Make a W13-filled private pair.json using the original W13 tool, with BOTH snapshots before save / after load. No personal names/free-text.

Create marks.json with EXACTLY one marker per (stage, visual_token) already appearing in W13's respective snapshot. Example:

    {"schema":"B01-13-PNG-RELOAD-MARKERS/R1","observations":[
      {"stage":"BEFORE_SAVE","visual_token":"V01","x":120,"y":90},
      {"stage":"AFTER_LOAD","visual_token":"V01","x":124,"y":95}
    ]}

Only existing visual tokens, valid PNG pixel coordinates and exact stages are accepted. The annotation is an operator claim, never automatic computer vision.

Fetch original W13 file byte-exact from frozen Git commit (example in a checkout containing the commit):

    git show aee4315304ca3e2fe3e155242d7d181260089efc:Research/B01-13/P17K/save_visual_pairs.mjs > /private/PinnedW13.mjs

Then run:

    node Research/B01-13/P17L/screenshot_witness.mjs --pair /private/pair.json --marks /private/marks.json --before /private/before.png --after /private/after.png --upstream /private/PinnedW13.mjs --output /private/receipt.json

Output: original W13 visual_status and visual_diff, image SHA256/size/dimensions, nonidentifying marker coordinates, same_image_sha flag, upstream exact Git blob. Output **does not include screenshot pixels, people, personal identifiers or game Save data**.

## Tests, limits, ownership, rollback

Real original Node tests: physical synthetic RGB PNG chunks, CRC, zlib inflation, dimensions, tamper/truncation, marker negatives, source-blob tamper, actual pinned W13 evaluation and child-process CLI on real temporary files; Windows2025/Ubuntu24.04 × Node20/22 via dedicated Actions. Original source/package file SHA256, Git blob, and sizes are verified in workflow. Synthetic PNG test fixtures are NOT actual Unity Player captures or human participants.

Hardcoded output gates: source_to_player=UNVERIFIED, human_verified=0, native_unity=NOT_RUN, save_id_verified=false. **Matching screenshots or marker coverage do not prove stable consumed IDs, save schema security, genuine Unity sessions, PhysX or actual usability.** Actual human research requires HQ approval, participant consent and privacy SOP, licensed Player, real source-to-build attestation. Do not commit real screenshots or identifiable evidence. No network client; private files use exclusive create and fsync. Windows ACL/junction handling needs separate host validation.

Exclusive owner scope: Research/B01-13/P17L/** plus dedicated .github/workflows/b01-13-w14-screenshot-witness.yml. No Assets/**, canonical Codex, #510, prior frozen B01-13 sources or other-owner source edits. **B01-40** verifies manifest/scope/rollback G0, **B01-60** independently reproduces source QA, **B01-00** decides any later approved human use. No offer/ACK/integration/merge. Rollback = abandon isolated unmerged Draft branch; delete private local receipts/screenshots only under the host's retention/withdrawal procedure. SOURCE_G0 pending independent, native UNITY_G1 NOT_RUN, G2 HOLD.
