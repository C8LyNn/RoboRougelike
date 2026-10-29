# B01-23 / PASS 17 WAVE03 — PARTSIZE001-P17-BRACE-DELIVERY-001 R1

**New working producer-side delivery functionality:** builds a physically downloadable, byte-for-byte reproducible ZIP of *exactly* the 4 original Unity TextAsset JSONs and 6 original Unity `.meta` files from [P17 Bracing #375](https://github.com/C8LyNn/RoboRougelike/pull/375). This is **not** another robot blueprint and does **not** change any #375 source; it closes a concrete CODEX INBOX #79 requirement: verifiable original artifact, SHA-256, read-only staging and reversible rollback. B01-40 still has sole authority to mark G0/OFFERED, B01-60 independent G1, B01-00 HQ handoff, and Codex actual CLAIM/INTEGRATE.

## Physical evidence from original CI

[Completed original-source Windows2025 + Ubuntu24.04 run #40906530192](https://github.com/C8LyNn/RoboRougelike/actions/runs/40906530192), exact code SHA `abb8d0e463a1903df13f1d085d43f05baf757934`:

- Both OS run **20/20 actual Python unittest cases PASS**: original Git blob preservation, manifest source pin, byte-identical rebuild, 10 allowed file scope, 11-member ZIP (10 Unity files + exact archive receipt), isolated staging/rollback, forbidden in-repository staging, existing staging protection, original source tamper rejection, original manifest tamper rejection, corrupt game JSON, fake receipt, extra Codex source, zip traversal, duplicated archive member, unmarked stage, nonempty stage, modified files preserved on refused rollback, unrelated files preserved on refused rollback, unsafe names, refusing archive overwrite.
- Both OS produce a **real binary `p17-bracing-original-unity-assets.zip` of 23,547 bytes**, SHA-256 **`c4f9b083d618013d1d6c4f8a888dcb57d0a4e59220d37acf3f45644c65953580`**. The ZIP contents are **byte-identical across Linux and Windows**, with deterministic member order, fixed ZIP timestamps/modes, ZIP_STORED, and original authoring bytes.
- Actual downloadable GitHub Actions artifact [Windows — artifact #12910483987](https://github.com/C8LyNn/RoboRougelike/actions/runs/40906530192/artifacts/12910483987) and [Ubuntu — artifact #12911702475](https://github.com/C8LyNn/RoboRougelike/actions/runs/40906530192/artifacts/12911702475). GitHub downloads an **outer Actions artifact wrapper**. Extract that wrapper to obtain the **inner 23,547-byte original Unity ZIP**; compare SHA-256 against the value above. Do not compare the outer wrapper's digest.
- Real CLI `init-stage → stage → rollback` executed in a temporary directory on both OS (actual files created, verified and removed); script refuses to write **anywhere inside its source repository**, so it cannot accidentally mutate Codex's active Unity worktree.

## Reproduce from the exact branch — Python3.11 stdlib, no packages

```bash
python Tools/B01/PARTSIZE001/P17Delivery/test_pack_bracing.py
python Tools/B01/PARTSIZE001/P17Delivery/pack_bracing.py build --repo . --zip p17-bracing-original-unity-assets.zip
python Tools/B01/PARTSIZE001/P17Delivery/pack_bracing.py verify --repo . --zip p17-bracing-original-unity-assets.zip
```

`build` requires original immutable #375 authoring source from this stacked parent, and verifies **all 10 original Git blob SHA-1 hashes**, plus the original `P17_W02_BRACING_MANIFEST.json` Git blob `02221b524d2c106e90d179f16c09dcfef8815c6d`. It does not trust names from a user-supplied manifest; ten exact `Assets/PARTSIZE001/Bracing*` paths are hardcoded and checked. `verify` always re-reads the current pinned producer source and rejects any altered ZIP entry, inserted path, duplicate entry, changed SHA, unexpected compression or member metadata.

For a **NEW, outside-the-repository empty directory** owned by the receiver:

```bash
python Tools/B01/PARTSIZE001/P17Delivery/pack_bracing.py init-stage --repo . --dest "/some/clean/external/p17-stage"
python Tools/B01/PARTSIZE001/P17Delivery/pack_bracing.py stage --repo . --zip p17-bracing-original-unity-assets.zip --dest "/some/clean/external/p17-stage"
# Inspect the exact preserved Assets/PARTSIZE001/Bracing/ hierarchy outside the game.
python Tools/B01/PARTSIZE001/P17Delivery/pack_bracing.py rollback --repo . --zip p17-bracing-original-unity-assets.zip --dest "/some/clean/external/p17-stage"
```

The script fails closed when receiver data was edited, unknown files appeared, the archive is altered, or a stage already existed. No force cleaning. **It does not install to the game**, does not bypass Unity's import, and cannot replace the Codex branch. A separate integration action requires explicit Codex scope ACK and B01-40/60 acceptance.

## Receiver and actual Codex integration decision

Dependencies: [original frozen PARTSIZE #286](https://github.com/C8LyNn/RoboRougelike/pull/286) → [3 base P17 rigs #348](https://github.com/C8LyNn/RoboRougelike/pull/348) → [weakpoints #361](https://github.com/C8LyNn/RoboRougelike/pull/361) → [original P17 bracing #375](https://github.com/C8LyNn/RoboRougelike/pull/375) exact `5666a39eabd4cea91b2b605a9eb99ea2f1e226e8`. **Producer #375 is not modified**. This new tool neither edits nor reads canonical Codex R3/Garage/Combat/Save; Codex's current PR #383 build/source must be identified by Codex at actual intake.

B01-40 acceptance request: download each original Actions artifact; extract inner ZIP; compute exact 23,547-byte SHA-256; verify pinned original source 10/10 and manifests, archive receipts and same SHA on Win/Ubuntu; independently validate that the staged hierarchy has only 4 JSON + 6 `.meta`; perform isolated stage/rollback; inspect no-conflict path list and confirm dependency order. Publish an independently authored G0 report with exact SHA. B01-60/61: independent read-only source QA; licensed Unity actual collider/typed sockets and 6 bracing source-goal scenarios must be run in a **different** native G1 environment. Do not report this pure archive CI as Unity or PhysX PASS.

**Copy-ready request to Codex, only AFTER B01-00 and G0/G1:**

> Intake optional PARTSIZE P17 Bracing from producer PR #375 immutable SHA `5666a39eabd4cea91b2b605a9eb99ea2f1e226e8`. Download the original ZIP artifact from Actions #40906530192 via the linked Windows/Ubuntu artifacts, extract the inner ZIP, verify SHA-256 `c4f9b083d618013d1d6c4f8a888dcb57d0a4e59220d37acf3f45644c65953580` and exact ten manifest entries. Do not edit or merge the producer PR. In a new explicitly approved isolated integration branch, after prerequisite P17 data imports, stage the 4 new Unity TextAsset JSON and 6 matching .meta from `Assets/PARTSIZE001/Bracing`. Allocate fresh live runtime IDs; do not treat authored localIds as persistent inventory GUIDs, and do not grant free equipment, alter current R3, weapon, garage or save logic without an approved Codex task. Run real Unity 6000.3.14f1 import, geometry/typed-socket/PhysX clearance and actual driving/firing under six part-loss cases; record Editor.log, XML, process exit, Windows Player/human smoke as applicable. If any part fails, preserve evidence, classify REWORK, and remove only newly staged Bracing files from that disposable integration branch. No main/develop merge without explicit owner approval.

### Gates and limitations

**B01-23 producer G0 checks PASS**, **B01-40 independently verified G0 PENDING**, **B01-60/61 independent source and native Unity G1 PENDING/NOT_RUN**, **G2=HOLD**, **CODEX_ACK=PENDING**, **INTEGRATED=NO**, **NO MERGE**. Logical part graph tests in original #375 are not proof of physical sockets. The new archive is safely transportable data; it does not itself make these robots playable.
