# MAPGEN R3.2 native QA scope

User dispatch HQ-B01-20261025-CODEX-MAPGEN-NATIVE-QA authorizes native source testing now, no integration/merge/release. Target is current OBJ2104c9236a4cdc27814ea2e7679872ec4a524150cc8 with PHY1259166546. Preserve all existing checkouts and archived builds. New qa/rrl-mapgen-r32-native checkout only; never copy Library or import CAMERA/BUILD/RAC/model/loaner changes.

Source immutable003bced54480d509ffc0d49d26daf87160a8d609, full MAPGEN subtree from stacked99+103+211, accompanying metas/docs/tools and original SHA256 manifest. Unity6000.3.14f1/packages unchanged. Compile first, original Edit/Play including regenerate/invalid/edge cases; if source compilation fails preserve reproduction, subsequent tests NOT_RUN, return producer correction to93/79.

After compilation, native PhysX trials must use real R3 Light/Heavy assemblies, canonical R6WheelContacts and existing Rigidbody/drive. API-controlled R6 force steering is allowed diagnostic control, not human play; no cube-only evidence, no teleport traversal or replacement solver. Minimum12rows: Arena/Course × Light/Heavy ×7/31/83. Replay Heavy course normal2/7/31 and hard0/2/7/19 with exact configs; reuse identical rows only if configurations match. Record full collider envelope/mass/COM, contacts, displacement, collisions, stall/flip/ramp/seam/spawn/finish. Keep actor/source identities and failure rows, no silent regeneration/reroll for success. Capture720/1080 frames and retain original logs/XML/exits.

Local QA does not certify independent B01-60/G2. No package runtime source fix or game integration: source defects go to B01-00/B01-20 with exact source/config/repro. Draft QA PR/report and Issues93/79 publication are explicitly authorized.
