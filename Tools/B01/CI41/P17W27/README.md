# B01-41 / PASS17-WAVE27 — CI41-W27-COMPOSITE-SOURCE-CLOSURE-001/R1

**Goal:** ACTUAL immutable original-source composite ZIP, physical SHA256 manifest, safe scratch preflight and dependency audit for #787 #797 #792 #780 #750. Not a Save implementation, Unity build, Codex merge, or approved install.

## EXACT SELECTED/EXCLUDED LINEAGE
- #787 f5db40bf293fe9582ec7c6fcb3515aeff6fcf937 R3 strict Save = AUDIT_ONLY, not installed as second raw writer.
- #797 2b7983f0f9e09dd1d56906b379a3f7af7b02f21a whole world+V7 Save = ONE active writer; include EXACTLY two runtime C# source files from W25 Tools, not full branch/history (older V1/V6 files).
- #792 16cb2a7b5158605b15191b6074f9e392e0d1ebd3 physical Unity source = full Assets/B0123CanonicalRigPortW24 tree, includes prior #772 with R2 rollback velocity changes.
- #780 b4df10a7ad7e6c758f8331013f6b216b3a3689a3 ECON V7 = full exact Assets/B01Packages/ECONAPPROVED007 tree including 5 fresh GUIDs, asmdef.
- #750 60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb = one EXACT corrected R5 ColdRestart PlayMode fixture overlaying original #690 file, not another Save.
- Immutable original W06 #421, secure #510, codec #556, R5 host #690, canonical API V3 #729 = frozen Assets prefixes imported read-only; no changes.

## REAL DELIVERABLE
source_packet.py directly executes original git ls-tree/git show at all immutable SHA. It creates one ORIGINAL Git-byte SOURCE/ archive plus per-path SHA256, Git blob SHA, byte length, GUID-map and asmdef-reference manifest, measured complete ZIP SHA, safe INSTALL_FOR_CODEX/TEST_PLAN/KNOWN_LIMITATIONS/QA_B01_60 notes. It cannot install anything; output directory must not exist, even EMPTY, and its parent cannot be a symlink. The output is disposable, outside the active Codex worktree or Unity project.

Tests: 18 adversarial Python QA checks; physical source Git SHAs (not mocks); provenance, source collisions, .meta GUID duplicates, Unity asmdef references, correct one approved R5 fixture replacement, safe symlink and foreign EMPTY/dirty path, deterministic archive bytes. Real Windows2025+Ubuntu24 original CI logs and ZIP artifacts are required; no false native Unity G1.

## GATES
Source provenance PASS does not authorize Unity import: #797 is under Tools with NO Unity runtime asmdef or executable binder to #792. #792 owns a NEW real Unity Rig[] rather than importing frozen #510 private Rig[], which still needs actual game-facing binder/scene migration. #750 real Unity NUnit not executed. Codex W26 owns its own active branch and reserved gameplay paths. B01-40/61/60 independent review required before B01-00 considers handoff, and CODEX ACK is not implied.

Do NOT coinstall raw #787 Save with #797 whole-game Store, older #749/#773/#793/V6, or import whole source branches. Old Save migration backup-first, restore-tested, explicit consent only; do not run original #510 destructive rollback in shared trees.

Reproduce on independently owned checkout with full Git fetch of all source_plan.json commits: python -m unittest discover -s Tools/B01/CI41/P17W27 -p test_source_packet.py -v ; python Tools/B01/CI41/P17W27/source_packet.py --out /tmp/ci41-w27-owned-new-unique

Six stages: IMPLEMENTED=YES; TESTED=CI_PENDING; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. SOURCE_TRANSPORT_G0=PENDING; GAME_INSTALL_G0=HOLD; NATIVE_UNITY_G1=NOT_RUN; G2=HOLD; CODEX_ACK=NONE. Receivers B01-40 /61 /60 /00. No merge. Rollback discard only this branch or the fresh UUID-owned test folder, NEVER user player data.
