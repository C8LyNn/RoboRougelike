# B01-62 P17-W03 — CHECKPOINT-ORIGINAL-CSHARP-QA-001 / R1

## Independent source receiver, not a new save implementation

This package independently **compiles and executes unchanged, actual Codex C#** against Windows/Ubuntu physical temporary files and deterministically proves the exact historical reload regression at its prior commit. It is **only a QA receiver** under B01-62; canonical code belongs exclusively to Codex.

Original frozen source inputs:
- Codex PR #382 (historical before fix) commit a06dea84e11859a519a42f89c88b2dcdb055646f; original Assets/R3/R3ModelCheckpoint.cs Git blob 658b6377cf6f36496c1d555ff6b92447e58d23a9.
- Codex PR #383 (fixed gameplay commit) 203a0805251ed28293888e43f2d49f0df63767c6; original Assets/R3/R3ModelCheckpoint.cs Git blob 29a85ae3ff0e761538c1937bf901d9a5ad573620.
- Both source revisions contain identical original Assets/R1/R1RunCheckpoint.cs blob 6eb8682c14329088d5698fc369009fb86b99b426.

**No source file from Codex is copied into this PR or modified.** The prepare.py extractor reads original Git objects after checking their immutable blob SHAs. It preserves the complete original RunCheckpoint DTO and original R1RunCheckpointStorage static class (drops only unrelated Unity-heavy R1RunController after the class). It separately extracts exact original R3VehicleModel.Remaining and SavePart methods, without changing their bodies, and gives them a test-only static caller. Other huge Unity graph classes and the real model TryRestoreCheckpoint remain OUT OF SCOPE. Explicit .NET8 System.Text.Json fields-based JsonUtility shim and R3 part DTO shims permit source code execution but **are not real Unity serialization or native physics.**

## Actual executed test intent

15 tests from Program.cs:
- Real physical filesystem Write, Read, nested checkpoint field IDs, replacement and cleanup of temporary files.
- Actual original SHA-256 payload/JSON envelope check and negative tamper: payload, hash, valid hash for deliberately unsupported version, malformed JSON, empty/missing/oversized files.
- Actual original R3 SavePart expired inventory reload positive marker, remaining future cooldown, zero marker, source object nonmutation, DeviceReadyAt expiration and Infinity defense sentinel.
- Historical negative control must execute **actual old Codex original method** and return exact exit code **86** when ReloadReadyAt incorrectly collapses to zero; fixed same scenario must pass nonzero marker. This comparison is a semantic owner-original RED → GREEN test, not mutation invented by the receiver.

No code is silently fixed in source extraction. Unexpected changes to original source blobs fail closed before .NET runs.

## Reproduction

Requires Git, Python 3.11+, .NET SDK 8, access to repository and two original commits.

    git fetch origin 203a0805251ed28293888e43f2d49f0df63767c6
    git fetch origin a06dea84e11859a519a42f89c88b2dcdb055646f
    python Tools/B01/B01-62/P17W03CheckpointSourceQA/prepare.py --repo . --mode fixed --out isolated/fixed
    python Tools/B01/B01-62/P17W03CheckpointSourceQA/prepare.py --repo . --mode old --out isolated/old
    dotnet run --project isolated/fixed/Receiver.csproj -c Release
    dotnet run --project isolated/old/Receiver.csproj -c Release -- --historical-negative

Expected old code exit **86**, not zero; the workflow asserts that value and exact output. The producer CI automatically runs source-pinned exact commands on both Windows and Ubuntu, uploading logs. Never run original source extraction in Assets/** or overwrite game files.

## Independence and handoff

- Owned paths only Tools/B01/B01-62/P17W03CheckpointSourceQA/** and .github/workflows/b01-62-p17w03-checkpoint-original.yml.
- B01-40 to inspect source provenance and scope; B01-60 independent QA if assigned; B01-00 controls whether any results are forwarded via Codex Inbox #79.
- Codex is the sole owner of run save, JSON schema and R3 vehicle/inventory reload repair. This package is evidence for that existing fix, **not** a competing source patch and **not** independent native acceptance of every controller state transition.
- Rollback: delete only this isolated QA tool folder and scoped CI workflow. Do not delete user saves or edit any Codex commit.
- **G0:** .NET8 original C# excerpt execution is verifiable with exact pinned Git blobs. **G1 native Unity Edit/PlayMode:** NOT_RUN. **G2 integrated Player/manual GUI:** NOT_RUN. Full native EditMode suite for Codex #383 remains **FAIL 1327 PASS, 2 FAIL, 1 skipped** per Codex evidence and is not rewritten as GREEN because these 15 source tests pass. Codex ACK for THIS QA receipt PENDING.
