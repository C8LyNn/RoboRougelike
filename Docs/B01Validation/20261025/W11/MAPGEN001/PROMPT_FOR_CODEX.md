# B01-40 W11 — MAPGEN-001 R3.3 / CODEX PHASE-0 DIAGNOSTICS ONLY

**DRAFT ONLY — DO NOT SEND AS GAME-INTEGRATION INSTRUCTION. NO G2.** This is an operator-ready read-only/isolated QA request that B01-00/HQ may consider; publication does not initiate a Codex run.

**Immutable source** complete stacked PR #99+#103+#211+#225 at `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`. Actual independent SHA256 checked: `MAPGEN001_R3_3_FULL_SOURCE_AND_QA.zip`, 56 files, 71,667 bytes, `384ef6909c1ebcb361c55a6b23b22eb105b2aaaed0270e229cbad37a250155a0`. B01-40 has rehashed all 55 registered file bytes; 20 Unity .meta GUIDs internal PASS. Reference published target PR210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`; **your actual live target SHA and unpublished worktree state MUST be ACKed separately**.

**CRITICAL OBSERVED RED:** Codex PR #232, QA HEAD `b80faa121e4022d772caa3b93d8b2a482d9a53af` on same R3.3 source: Unity compiles, EditMode 24/24 PASS, PlayMode 6 PASS + 12 FAIL, cube cases 12/12 FAIL; standalone URP Windows Player cannot find shader via `Shader.Find` despite Editor test PASS. B01-60 independent native verdict PENDING. **DO NOT import to active game, adapt host run or claim READY.**

## PHASE 0 ONLY — inspect/verify in isolated disposable QA

1. Print true target branch, HEAD, dirty/unpushed paths and reserved R3/R6/P12/OBJ/camera/other agent changes. Clarify whether `Assets/B01Packages.meta` exists and its GUID. It has different GUIDs in MAPGEN `b01a0001000000000000000000000001` and CAMERA `b01c0000000000000000000000000001`; never overwrite existing shared metadata.
2. Read this W11 `INSTALL_FOR_CODEX.md` and the exact PR #225 manifest. If any source SHA/ZIP/header changed since audit, return mismatch and STOP.
3. On a fresh QA-only checkout with licensed Unity6000.3.14f1 URP, independently replay real C# compile, EditMode, PlayMode 12 separate cube trajectories and invalid geometry, capturing NUnit XML, raw Editor.log, process codes and seed-level failures. No old R3.2 G1 inheritance. Never weaken finish criteria to turn RED into apparent green.
4. In a separate minimal Player/URP build prove a serialized retained URP shader/material is present **before** running the generator. Record GraphicsSettings, Player shader availability, actual renderer shader names, visible nonmagenta 720/1080 captures, exceptions and cache lifecycle. Do not alter production GraphicsSettings globally without approved source proposal.
5. If source R3.4 fixes become published by B01-20, only test a **new frozen exact SHA**, include 12 real R6WheelContacts/OBJ210 Light+Heavy mounted vehicles with seed7/31/83 × both recipes and 7 Heavy historical cases; API-guided and manual runs must be labelled separately. B01-60 must independently attest QA.
6. Return `MAPGEN_RECEIVER_PRECHECK`: SOURCE_SHA, QA_BRANCH_SHA, ACTUAL_TARGET_SHA, paths, shared GUID resolution, raw native test evidence, real drives per case, shader build evidence, blocking bugs, docs/rollback and **G0_TARGET=PENDING** until live scope ACK. Do not approve B01 gates yourself.

**STOP at PHASE 0. PHASE 1 HOST INTEGRATION IS NOT AUTHORIZED.** Explicit B01-00/HQ G2 and independent B01-60 PASS will be needed after all confirmed source failures have been repaired and re-tested. No main/develop merge, live Codex worktree modifications, release, GPT OSS duplicate send or auto-created adapter branch.
