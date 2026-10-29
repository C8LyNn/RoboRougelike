# B01-61 / PASS 17 WAVE 03 — BRACING-ACCEPTANCE-001

Independent **game-content receiver** for B01-23 original three braced robot layouts (producer frozen [#375](https://github.com/C8LyNn/RoboRougelike/pull/375), actual export [#391](https://github.com/C8LyNn/RoboRougelike/pull/391)). This is **not** another rig variant, physics system, or claim that the original Codex game imported bracing.

## Real executable and verified scope

[`bracing_cross_receiver.py`](bracing_cross_receiver.py) fetches/compares 18 Git-blob-exact original producer/source files across original base rigs [#348](https://github.com/C8LyNn/RoboRougelike/pull/348), one-part history [#361](https://github.com/C8LyNn/RoboRougelike/pull/361), reinforced rigs [#375](https://github.com/C8LyNn/RoboRougelike/pull/375), and read-only frozen original PARTSIZE contract. It independently verifies that all old owner properties survive unchanged and only five new `Armor` owners are installed; 6 new Unity `.meta` GUIDs are distinct, each source cell is unique, all mounted modules are core-connected and original author intentions correspond to six original weakpoint rescues.

**New coverage NOT present in the bracing owner's source-only tests:**
- Re-derive 38/38 historical one-owner outcomes from the original 38-case weakpoint data.
- For **all 227 same pairwise original owners removed** from both base and reinforced variants, require no newly disconnected original owner, no loss of previously available connected guns and no regression in selected drive operability; record actual baseline and reinforced affected IDs and count improved cases.
- Enumerate **all 64 additional two-owner-loss scenarios involving five new brace Armor owners**, producing 291 total reinforced two-loss combinations with original IDs. This does NOT count a new Armor member as a pre-existing original survivor.
- **Six independent adversarial negative cases** corrupt native physical status, original owner data, source cell ownership, kit geometry/provenance, a rescue target and brace connectivity. Each altered original input must be rejected.
- Publish actual deterministically generated `bracing_cross_acceptance.json` + raw OS console logs as downloadable Windows and Ubuntu workflow artifacts, including generated SHA256.

**Trust boundary:** This independent Python oracle operates on immutable original *source data* and checks original C# source method/constant signatures. It does **not** compile/run C# by itself. The original B01-23 producer compiled its own unchanged original PARTSIZE/Bracing C# in [Actions #40758643339](https://github.com/C8LyNn/RoboRougelike/actions/runs/40758643339), but that is **producer** evidence, not an independent Unity execution. The previous B01-61 QA [#373](https://github.com/C8LyNn/RoboRougelike/pull/373) independently compiled original C# for **the unreinforced rigs**; do not conflate the two checks. `UNITY_NATIVE=NOT_RUN`, `PHYSX/COLLIDERS/REAL_VEHICLE=NOT_RUN`, `PLAYER=NOT_RUN`, `G1/G2=HOLD`, `CODEX_ACK=PENDING`.

## Reproduce without touching the game

Requires Git, Python 3.11+ standard library. No third-party packages or Unity licensing to run source QA.

```sh
git fetch --no-tags origin 0978497561c8b6400490235d3e5a401ac32bea2d
git fetch --no-tags origin 098ccdbe8f129253fa4ada52318436b7cc2b4dc4
git fetch --no-tags origin 5666a39eabd4cea91b2b605a9eb99ea2f1e226e8
git fetch --no-tags origin be2be0e4802bc3f450179901f9ae91c6d84bcb5b
python Tools/B01/B01-61-P17-W03/bracing_cross_receiver.py --repo . --output bracing_cross_acceptance.json
```

Exit 0: all original producer SHA blobs, own manifest self-hash, input geometry, historical case parity, 227 original-pair comparisons, 64 extra-armor pair cases, six author mitigations, and six negative mutants verified. Exit 2: any mismatch, input absence or regression. File bytes are never rewritten under original `Assets/**` nor in Codex's active worktree.

## Receiver and integration

B01-40: verify actual all 18 Git source object identities plus published ZIP archive of producer [#391](https://github.com/C8LyNn/RoboRougelike/pull/391) and this independent JSON result. B01-60: independently reproduce original source and assess specific native Unity Collider/typed socket/real detach/weapon/drive issues with licensed Unity 6000.3.14f1. Codex can use `bracing_cross_acceptance.json` to select a small high-value set of **physical** part destruction cases when B01-00 authorizes handoff through [#79](https://github.com/C8LyNn/RoboRougelike/issues/79). This QA tool does **not** stage or install the braced prefabs or original game content. No direct Codex ACK, no automatic G1 acceptance.

**Own paths only:** `Tools/B01/B01-61-P17-W03/**` and `.github/workflows/b01-61-pass17-wave03-bracing.yml`. **Rollback:** delete only the own QA tool directory and own workflow; remove generated output from recipient test workdir. Do not alter or force-update original #348/#361/#375/#391 or Codex game code.


## PASS17 WAVE04 — ACTUAL B01-23 PRODUCER ZIP G0 BINARY RECEIPT

This W04 change **extends this exact existing QA Draft**, not the producer content or new gameplay prototypes. Separate [`archive_receipt.py`](archive_receipt.py) downloads the **actual published producer GitHub Actions artifact bytes** (not rebuilding a ZIP from assumed source):

- [B01-23 original Windows producer artifact #12910483987](https://github.com/C8LyNn/RoboRougelike/actions/runs/40906530192/artifacts/12910483987).
- [B01-23 original Ubuntu producer artifact #12911702475](https://github.com/C8LyNn/RoboRougelike/actions/runs/40906530192/artifacts/12911702475).

The GitHub archive is an outer wrapper containing one original **inner ZIP** `p17-bracing-original-unity-assets.zip`. Verify inner archive **23547 bytes** and **SHA256 `c4f9b083d618013d1d6c4f8a888dcb57d0a4e59220d37acf3f45644c65953580`**, original producer pinned #375 exact 4 JSON + 6 Unity `.meta` payloads, 11/11 ZIP members including one original embedded receipt, all 10 file Git blobs and SHA-256 sizes, metadata, 6 GUIDs and immutable manifests. This receiver independently rejects six adversarial in-memory corrupted archives (tampered original bytes, tampered asset, forged receipt, parent traversal, unexpected Codex source member, duplicate member). It physically stages all 10 original bytes in a disposable temporary external directory and refuses rollback if any staged payload is tampered; after restoring the deliberate fixture, it rolls back all owned files. No repo/Unity `Assets/**` files are touched.

**Real observed test:** [GitHub Actions #40966463944](https://github.com/C8LyNn/RoboRougelike/actions/runs/40966463944) **SUCCESS** both jobs. Both original producer artifact IDs were independently downloaded and verified from an Ubuntu hosted Actions test runner (matrix `source=windows/ubuntu` indicates original artifact provenance, **not a claim of a Windows test runner**). Outputs/logs are uploaded with the run. The first attempt [#40966114197](https://github.com/C8LyNn/RoboRougelike/actions/runs/40966114197) failed with HTTP 401 after GitHub redirected to a signed off-host artifact URL. The *receiver-only* Python HTTP redirect policy was fixed to remove GitHub bearer credentials when redirecting across hosts, eliminating inappropriate Authorization forwarding; original B01-23 source and archived bytes were not changed. The fixed exact original receiver source is pinned in the adjacent `manifest.json`.

**Run via GitHub Actions** (recommended): the new isolated [`b01-61-pass17-wave04-real-zip.yml`](../../../.github/workflows/b01-61-pass17-wave04-real-zip.yml) uses Actions read-only permission and the job-scoped GitHub token, fetches original two authoring SHA commits and verifies the two remote original artifact IDs separately. Local runner usage requires an authorized GitHub Actions-compatible `GITHUB_TOKEN` token with **actions:read**; do not expose the token in logs:

```sh
git fetch --no-tags origin 5666a39eabd4cea91b2b605a9eb99ea2f1e226e8
git fetch --no-tags origin 8fc0f99ef7bf6c39c21609616b7e852538d2de12
python Tools/B01/B01-61-P17-W03/archive_receipt.py --repo . --artifact windows --output isolated-windows-receipt.json
python Tools/B01/B01-61-P17-W03/archive_receipt.py --repo . --artifact ubuntu --output isolated-ubuntu-receipt.json
```

**Handoff:** B01-61 accepts the physical original **binary ZIP and content/source identity at source/G0 evidence level**, independently of B01-23 producer tests. B01-40 still owns formal G0 acceptance, placement/scope and current-project GUID collision/merge integration queue. B01-60 and Codex independently require a genuine licensed Unity 6000.3.14f1 host and six real prefab references before physical Collider/sockets tests (see producer [#405](https://github.com/C8LyNn/RoboRougelike/pull/405)); `NATIVE_UNITY/PHYSX=NOT_RUN`. The repo's global CI-02/CI-03 jobs at the W04 SHA failed the **activation credentials preflight**, before launching Unity; they are **BLOCKED, not gameplay FAIL or PASS**. Do not create/edit Unity license secrets from this QA tool. B01-00 is the exclusive owner of the Codex #79 offer and explicit ACK. This is no automatic game integration. Rollback remains removing the QA-only owned folder/workflow and discarding generated receipts; original producer code and player data remain untouched.
