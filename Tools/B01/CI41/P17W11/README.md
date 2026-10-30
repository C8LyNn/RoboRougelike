# CI41-EXCLUSIVE-STAGE-001/R1 — B01-41 P17/WAVE11

Ownership: B01-41 CI / test automation. Dispatch [#500](https://github.com/C8LyNn/RoboRougelike/issues/500), governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## One functional result — actual safe staging outside canonical game
The executable `exclusive_stage.py` reads **original Git objects** for exact B01-23 candidate `#488@33a32b8bd9ab40467ab1a17b5684926a37dbd4d3` (secure-v2) or `#494@6efe45961ed0ffdf183e8016e350978b9f3dc51c` (public-guard). It requires the operator to explicitly choose exactly one. Neither choice is endorsed by this CI tool.

It checks actual candidate and target ancestry, selected original Unity Assets tree, forbidden competing source layout, exact required Unity source and NUnit files, 1:1 asset and folder .meta metadata, unique 32-digit Unity GUID within new assets and against the original target Git tree, safe Git object/path modes, exact Git blob SHA and SHA256. A structurally sound selected candidate is then **physically copied into a new scratch directory outside the checkout** with a reproducible JSON file/blobs receipt. It will not overwrite even an empty pre-existing target directory or any source/game paths. This is an operational source staging gate, not source security proof.

**Both source variants are treated as `security_transaction=UNVERIFIED_HOLD`, `release_gate=HOLD`, `native_unity=NOT_RUN` even when staging succeeds.** In particular original #488 transaction issue [#486](https://github.com/C8LyNn/RoboRougelike/pull/486) is not repaired here. B01-23 must select and repair one variant; B01-40/60/61/62 independently validate public API / transaction / PhysX. Staging alone is never an ACK or integration.

## Example read-only stage
```bash
python3 Tools/B01/CI41/P17W11/exclusive_stage.py --repo . --target-sha 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1 --producer-sha 33a32b8bd9ab40467ab1a17b5684926a37dbd4d3 --variant secure-v2 --stage-dir /tmp/B01-41-one-variant-only --out /tmp/B01-41-plan.json
python3 -m unittest discover -s Tools/B01/CI41/P17W11 -p test_exclusive_stage.py -v
```
The exact source commit needs to be locally fetched first. Both variants may be examined in **separate** scratch staging directories; never combine or install both in the game.

## Provenance and rollback
Isolated producer scope: `Tools/B01/CI41/P17W11/**` plus `.github/workflows/b01-41-p17w11-exclusive-stage.yml`, no game `Assets/**` modified. Physical stage rollback is simply delete **only the newly created scratch directory** after matching its source receipt. No automated production deletion, game install, merge, cherry-pick or Codex source mutation. If producer HEAD changes, this frozen R1 pin rejects it; new version/independent G0 required.

Receiver: B01-40 independently validate exact source ZIP/GUID/integration and source G0; B01-60/61/62 transaction and legacy route regression, G1 only licensed Unity; B01-00 only owner of #79 queue and final selection. `CODEX_SENT=NO / ACK=NO / INTEGRATED=NO`.
