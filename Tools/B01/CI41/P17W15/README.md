# CI41-SELECTED-SAVE-LINEAGE-FENCE-001/R1 — PASS17/WAVE15

Role B01-41 CI/CD & Test Automation. Dispatch: https://github.com/C8LyNn/RoboRougelike/issues/571 ; governance: https://github.com/C8LyNn/RoboRougelike/issues/276

## New executable functionality
lineage_fence.py reads actual original Git commit objects #510 → historical #549 → selected #556, exact SHA only (no moving branch authority).
GAME #510@d94400d95d13db7f27e1533df05c9292d5380431 is IMMUTABLE. SAVE #556@33094f154c7b91119b36d58aab285e6e83c5de57 is the ONE selected schema.
Historical parent #549@6da2c523e03766cd5a6deaed0cb3cccf8765354b is a Git ancestry dependency only, not a separately imported schema; alternatives #544, #554, #511, #494 HOLD.
Enforces exactly 16 NEW added selected Save-owned paths between frozen #510 and #556, no deleted/modified gameplay, no Codex or alternative scope, symlink, mode or submodule surprises.
Verifies 4 original Save Unity meta GUIDs distinct from ALL original existing Asset GUIDs, file and folder meta pairs, isolated W06-only asmdef, and frozen contract_selection.json with no fake Unity/ACK/host acceptance.
Receipt pins every original 16 Git blob SHA plus closure fingerprint; outputs LIVE_SAVELOAD=UNVERIFIED_HOLD / UNITY_G1=NOT_RUN / G2=HOLD.

This is NOT a ZIP parity tool (W14), GitHub Actions execution attestation (W13), single #510 candidate plan intake (W12), original C# regression suite, formal G0, native Unity or a host Save/Load adapter.

## Reproduce
git fetch origin +refs/pull/510/head:refs/remotes/origin/ci41-ro510 +refs/pull/549/head:refs/remotes/origin/ci41-ro549 +refs/pull/556/head:refs/remotes/origin/ci41-ro556
python3 Tools/B01/CI41/P17W15/lineage_fence.py --repo . --output /tmp/ci41-w15-lineage.json
python3 -m unittest discover -s Tools/B01/CI41/P17W15 -p test_lineage_fence.py -v

Tests initialize real disposable Git repos and real commit objects; mutate original file structure, parent lineage, Unity GUID/asmdef/selection and game paths. Dual OS GitHub Actions validates the live original #510 #549 #556 SHA refs and their physical Git source trees.

## Ownership / rollback / receivers
Only Tools/B01/CI41/P17W15/** and dedicated .github/workflows/b01-41-p17w15-save-lineage.yml new files on own isolated Draft branch base main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1. No Assets edit, source overwrite, Codex write or merge. Previous frozen B01-41 PR #564 remains unchanged.
Rollback: leave own Draft unmerged or delete only own W15 files/receipt; nothing was imported/staged in the game.
Receivers: B01-23/62 new host adapter and true atomic consumed-ID hydration; B01-61 independent exact adapter QA; B01-40/62 migration and safe rollback; B01-60 actual licensed Unity or NOT_RUN; B01-00 exclusively coordinates Codex. Save source G0 #560 accepted source only; HOST_SAVELOAD HOLD, G1 NOT_RUN, G2 HOLD, ACK absent.
