# B01-61 P17 WAVE06 — Original Codex / Bracing Unity GUID & path collision G0

**Package:** `B01-61-P17-W06-CODEX-BRACING-COLLISION-GATE-001`, independent B01-61 QA-only asset import preflight for *existing* original B01-23 [#375](https://github.com/C8LyNn/RoboRougelike/pull/375) + [physical ZIP #391](https://github.com/C8LyNn/RoboRougelike/pull/391). This is **not another armoring mechanic, ZIP integrity test or Unity runtime patch**.

## Why a different test matters

Prior independent [B01-61 #396](https://github.com/C8LyNn/RoboRougelike/pull/396) proved all original ten JSON + .meta asset bytes, all 227 old-owner double losses and 64 extra brace-owner failures, plus staging/rollback. That original receiver **did not compare those six original new Unity GUIDs and their case-insensitive paths with the original existing Codex game asset tree**. This preflight closes exactly that G0 installation-risk gap.

**Immutable inputs:** actual Codex game [#383 original executable source commit `203a0805251ed28293888e43f2d49f0df63767c6`](https://github.com/C8LyNn/RoboRougelike/pull/383), real B01-23 reinforced original authoring [#375 `5666a39eabd4cea91b2b605a9eb99ea2f1e226e8`](https://github.com/C8LyNn/RoboRougelike/pull/375), original producer manifest object `02221b524d2c106e90d179f16c09dcfef8815c6d`. This is **not** current live Codex HEAD unless separately checked against latest; never claim approval for a different game commit. In particular do not cherry-pick #375 as if the original PARTSIZE/rig/weakpoint prerequisites had already been staged.

## Executed capability

`bracing_codex_preflight.py` reads full original Git trees with `git ls-tree -r -z`; reads the original Codex `Assets/**/*.meta` raw byte contents in one real `git cat-file --batch` process, not from text claims or inferred path names; checks exact original source manifest and all ten actual B01-23 original Git blob IDs; validates every original Codex and Bracing GUID strictly (one 32-hex GUID per metadata file), detects existing-game duplicate GUIDs, imported new-vs-original GUID collisions, casefold path collisions, exact collisions, malformed metadata, orphan asset-meta pairs and extra foreign files; records exact missing direct PARTSIZE original source dependencies.

The original Codex gameplay tree should contain 1,023 original Assets/** .meta files; the gate **fails** if the pinned commit produces a different count. This is meaningful **only for exactly `203a0805...`**. Count is not a global Unity invariant.

`test_bracing_codex_preflight.py` independently mutates synthetic input metadata to test **16** positive/negative cases including forged original GUID collision, direct path collision, casefold path collision, duplicate existing-game GUID, duplicated package GUID, invalid extra metadata GUID, malformed originals and unexpected additional content. The synthetic negatives **do not replace** the actual full original source tree run. Both Windows and Ubuntu CI execute these and the actual original-game Git object scan and archive original log + JSON.

**Run from a read-only clone:**

```sh
git fetch --no-tags origin 203a0805251ed28293888e43f2d49f0df63767c6
git fetch --no-tags origin 5666a39eabd4cea91b2b605a9eb99ea2f1e226e8
python -m unittest discover -s Tools/B01/B01-61-P17-W06 -p 'test_bracing_codex_preflight.py' -v
python Tools/B01/B01-61-P17-W06/bracing_codex_preflight.py --repo . --output codex-bracing-g0-preflight.json
```

Requires Git, Python 3.11+ stdlib; no private access tokens, Unity license or network **after** those exact input commit objects are locally available. Refuses to overwrite existing report file. The scan reads original game asset paths but only the original *.meta payloads, never binary game art textures. No writes to Codex or original B01-23 producer source.

**Verdict limits:** `ZERO_GUID_AND_PATH_COLLISIONS` proves only the tested **source identities and bytewise Unity .meta GUID/path uniqueness** against that exact game Git commit. It is not an Editor import; it does **not** prove Unity serializations resolve, P17 PARTSIZE dependencies are installed, prefabs exist, six authentic native prefab assignments are available, typed sockets/PhysX/collider/drive work, or the game launches. Missing prerequisite upstream source paths are explicitly a **HOLD** even if no GUID collides. `G1=NOT_RUN`, `CODEX_ACK=NOT_OBSERVED`, `G2=HOLD`.

**Receiver:** B01-40 — formal G0 source/import dependency and *latest* game tree collision acceptance. B01-60 — native Unity 6000.3.14f1 G1 on licensed isolated receiver with true prefab refs (original independent [#405](https://github.com/C8LyNn/RoboRougelike/pull/405) probe). B01-23 retains exclusive authorship of content. B01-00 owns #79 Codex queue; no implicit ACK. **Rollback:** drop the new B01-61 QA-only `Tools/B01/B01-61-P17-W06/` and one scoped workflow. No game source/consumer asset changes to revert, generated receipt stays outside the worktree or is discarded.

**Initial real test RED corrected:** First original CI found 1,023 .meta in the actual `Assets/**` tree, whereas an earlier aggregate whole-repository lookup found 1,024 (including one outside `Assets/**`). The first preflight deliberately failed its pinned-count guard on both OS. QA-only expected-count corrected to the exact scan scope; original gameplay and bracing assets unchanged. The corrected final-head workflow must be green before claiming this gate accepted.
