# B01-50 / PASS17 WAVE14 — PINNEDPLAYER-INTAKE-001 R1

This is a **read-only exact-source receiver** of the frozen W13 [PLAYERFOOTPRINT-001 #540](https://github.com/C8LyNn/RoboRougelike/pull/540), **not** a revision of its frozen code. Unlike W13, this W14 intake has genuinely executed the original frozen inspector against a 39,623,366-byte real historical Windows Unity Player ZIP. It does not execute that EXE or Unity Editor.

## Physical historical archive actually verified
- Original artifact: `RoboRougelike_CX_Windows_x64.zip` from [Codex #310](https://github.com/C8LyNn/RoboRougelike/pull/310), [Google Drive file](https://drive.google.com/file/d/1D8tCALdstlIYEivxFXdunrRHdALNBzN9/view?usp=drivesdk).
- Operator-reported game source for this historical build: `aa7a917f177a9b1f07203868fb7d1d871eb3ee46`; **not independently inferred from binary**.
- **Physical SHA256 verified** `a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442`, matches Codex #310.
- 214 original ZIP members, including 3 directory entries; **211 physically hashed source-executable/payload files**, **103,666,052 unpacked file bytes**.
- Original frozen #540 source Git blob: `f8acb5a17d1721aade5d78eb8a8ae33f7db8477d`, from frozen commit `d7278f224faecb2aa694bcb93139ef338171e885`. Local physical copy was independently matched with `git hash-object`. The original unmodified `inspect` executed with **exit 0** on this real ZIP.
- Original complete, deterministically formatted 211-file report: 45,590 UTF-8 bytes, SHA256 `59143ef02b0f011e85815761a82e2b7dc31d09e4a278a39e9674e68615ebfc0a`.
- Compact results are in [evidence/historical_player_receipt.json](evidence/historical_player_receipt.json). Full 211-file result may be independently regenerated from the linked historical ZIP with this exact source; neither the ZIP nor executable is committed to the repository.

## Reproduce (existing original source and actual historical ZIP needed)
Use Python 3.11+ and a *clean* Git checkout.
```sh
git fetch origin d7278f224faecb2aa694bcb93139ef338171e885
mkdir -p Tools/B01/B01-50/PINNEDPLAYERW14
git show d7278f224faecb2aa694bcb93139ef338171e885:Tools/B01/B01-50/PLAYERFOOTPRINT001/playerfootprint.py > Tools/B01/B01-50/PINNEDPLAYERW14/original_playerfootprint.py
git hash-object --no-filters Tools/B01/B01-50/PINNEDPLAYERW14/original_playerfootprint.py
python Tools/B01/B01-50/PINNEDPLAYERW14/verify_pinned_player.py \
  --source Tools/B01/B01-50/PINNEDPLAYERW14/original_playerfootprint.py \
  --archive /path/to/RoboRougelike_CX_Windows_x64.zip \
  --expected-sha256 a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442 \
  --out /tmp/w14-player-verified.json
python -m unittest discover -s Tools/B01/B01-50/PINNEDPLAYERW14/tests -v
```
The exact Git blob must print `f8acb5a17d1721aade5d78eb8a8ae33f7db8477d`. Do **not** add this temporary source to the PR: original code belongs only to #540. For current future builds use a separately verified expected archive hash, always report that historical comparison is not current release validation. With no ZIP, **real archive replay is NOT_RUN**, even when source CI is green.

## Boundaries / rollback
- Own paths only `Tools/B01/B01-50/PINNEDPLAYERW14/**` and the dedicated workflow. No existing `Assets/**`, `main`, `develop`, #510, #540, Codex or other owners modified.
- Original frozen #510 Secure Reattach V2 and save-state owner code untouched; #511/#494 HOLD. This package says nothing about SaveLoad acceptance or native Unity G1.
- Static provenance is **operator-declared**; hash confirms downloaded archive equals the #310 public SHA but does not prove any original source commit is encoded inside the executable.
- QA wrapper refuses wrong hash, modified original inspector, symlink archive, ZIP-slip, case collisions, corrupt ZIP and overwriting evidence.
- CI runs 10 tests against original source fetched by exact SHA; tests use **synthetic fixtures**, whereas the W14 historical archive receipt was **genuinely executed locally**, not in CI.
- Rollback removes exclusively this new folder and workflow on its separate branch. Never delete any arbitrary or shared empty folders. No merge, CODEX_ACK, G2 or integration.
- Independent receivers: B01-40 physical/G0; B01-60 QA source; B01-00 oversight. NATIVE_G1=NOT_RUN, G2=HOLD.
