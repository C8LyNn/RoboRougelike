# CI41-TESTIMPACT-001/R2 — PASS17 WAVE09
Producer: B01-41 CI / TEST AUTOMATION. Immutable R1 remains in Draft PR #459 at 432acd34ca12378f512fa5e2384afbe1ff041b7b.
Branch: ci/b01-41-p17w09-testimpact-safety-r2.

## Original defect / R2 repair
Original R1 classifies changes to primary native Unity CI workflows as SOURCE_ONLY; changes to MOV C# .meta/GUID and MOV shader assets as TARGETED_PRECHECK. That is unsafe because Unity's native checks, importer references or shared GPU/runtime behavior may change. This new source-only R2 raises FULL_UNITY_SUITE_REQUIRED on all 4 canonical CI workflow paths, changed MOV .meta/.asmref/.rsp, non-C# MOV assets, and MOV runtime renames/deletions. Pure C# edits may still select 2 test assemblies for a precheck; a targeted precheck never grants native G1.

## Reproduction
- python3 -m unittest discover -s Tools/B01/CI41/P17W09 -p test_test_impact_plan.py -v
- git fetch --depth=1 origin refs/pull/459/head
- python3 Tools/B01/CI41/P17W09/proof_red_green.py
- python3 Tools/B01/CI41/P17W09/test_impact_plan.py --repo . --head-sha "$(git rev-parse HEAD)" --inventory-only --expect-module-count 9

The comparison script fetches exact original source Git commit and pins original source Git blob 463edd27d74d7dc0b178a2d651ff57a95c0ce89e. It executes 3 real Git change fixtures on unchanged original code vs corrected R2. Original frozen source and game Assets are NEVER edited.

## Scope, receiver and rollback
Only new Tools/B01/CI41/P17W09/** plus new .github/workflows/b01-41-p17w09-test-impact-r2.yml. Producer QA: source Git/Python on actual Windows and Ubuntu (source-only). Native Unity Editor/PlayMode/Player NOT_RUN, G1 HOLD. Independent B01-40 verify exact Git paths/blobs and G0, B01-60/61 independently rerun original RED / R2 GREEN. B01-00 controls any CODEX INBOX #79. No Codex ACK, no integration, no merge. Rollback: keep Draft PR unmerged or remove only R2 paths, never alter R1 frozen SHA.
