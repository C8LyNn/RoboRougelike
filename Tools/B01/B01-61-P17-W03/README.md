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
