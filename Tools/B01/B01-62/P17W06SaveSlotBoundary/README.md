# B01-62 P17-W06 — SAVE-SLOT-BOUNDARY-001 / R1

Independent ORIGINAL Codex C# save-path boundary and disposable canary QA.

## Scope and purpose
Codex #382/#383 reported an earlier test-generated checkpoint accidentally written to the game's real persistent path when a test path was lost across Unity domain reload. This packet executes the unchanged original R1RunCheckpointStorage.PathName, Write and Read from commit 203a0805251ed28293888e43f2d49f0df63767c6 (Git blob 6eb8682c14329088d5698fc369009fb86b99b426). It reproduces the save-path fallback by deliberate C# static reset, then physically demonstrates the overwrite against an isolated disposable canary, restoring that canary byte-for-byte. It does not prove that Unity will repeat the reload.

Unlike previous B01-62 #395, which tested checksum, payload and reload semantics, this package verifies the authority boundary between test slot and production-equivalent slot. It does not create a competing save implementation or modify Codex source.

## Source and security
prepare.py fails closed unless the original Git blob is unchanged, extracts the exact original storage class and DTO without rewriting method bodies, excludes the unrelated Unity-heavy controller from independent compilation and supplies explicit .NET8 UnityEngine.Application and JsonUtility SHIMS. Unity conditional compilation includes UNITY_EDITOR and B01_UNIFIED_RC2_ACCEPTANCE so the original test-path and batch-path getter logic executes unchanged.

All writes happen only in a **new nonexisting disposable sandbox**. The original path Application.persistentDataPath is simulated as SANDBOX/host-data, not an actual user folder. Each test creates a canary only inside SANDBOX/host-data/Run/checkpoint.json. Executable refuses an existing sandbox root before writing. It never inspects or modifies any real user checkpoint.

## Four executed operating-system modes, 19 assertions per OS
- editor, 10: explicit original TestPath honors separate QA slot; original file Write/Read succeeds; canary initially unchanged; simulated static TestPath null and empty string fall back to production-equivalent path; direct protected, relative and whitespace paths rejected by receiver guard; original unguarded source physically overwrites ONLY disposable canary after reset; exact canary restored. The receiver source QA calls this a confirmed simulation of a security boundary hazard, NOT a native Unity test or gameplay fix.
- batch-safe, 4: original command-line -rrl-run-save-qa-path override works with actual original Write/Read and protected canary remains intact.
- batch-missing, 2: missing override falls back to production-equivalent path, and receiver refuses it.
- batch-unsafe, 3: explicit CLI override selecting production-equivalent path is refused by receiver; canary intact.

Windows and Ubuntu execute all 19 checks and output four actual receipt.json files. A separate CI job compares their bytes across OS.

## Exact commands
Requires Git, Python 3.12, .NET 8, an original Codex Git object and four separate NEW scratch directories.

    git fetch --no-tags origin 203a0805251ed28293888e43f2d49f0df63767c6
    python Tools/B01/B01-62/P17W06SaveSlotBoundary/prepare.py --repo . --out /tmp/b0162-orig
    dotnet run --project /tmp/b0162-orig/SlotBoundary.csproj -c Release -- --mode editor --sandbox /tmp/b0162-editor
    dotnet run --project /tmp/b0162-orig/SlotBoundary.csproj -c Release -- --mode batch-safe --sandbox /tmp/b0162-batch-safe -rrl-run-save-qa-path /tmp/b0162-batch-safe/qa/safe.json
    dotnet run --project /tmp/b0162-orig/SlotBoundary.csproj -c Release -- --mode batch-missing --sandbox /tmp/b0162-batch-missing
    dotnet run --project /tmp/b0162-orig/SlotBoundary.csproj -c Release -- --mode batch-unsafe --sandbox /tmp/b0162-batch-unsafe -rrl-run-save-qa-path /tmp/b0162-batch-unsafe/host-data/Run/checkpoint.json

NEVER pass any existing directory or real user save path. Never use the actual game persistentDataPath as a fixture. Windows uses equivalent disposable absolute paths.

## Acceptance and rollback
G0 original .NET8 source-level source canary test evidence; not native Unity G1, not Player G2. No real Unity domain reload, actual Unity JsonUtility, save UI or Player was executed. The receiver guard is an isolated independent QA policy, not a Codex production patch. For Codex: consider a fail-closed explicit per-test slot lease and verify TestPath after each domain lifecycle transition in a legally runnable disposable Unity host. Original Write has no test/prod guard, so losing TestPath can point to real persistent slot; do not assume the historical incident is fixed merely by this source test.

B01-40 to inspect immutable SHA and scope, B01-60 to independently run original C# path scenarios, B01-00 to select Codex #79 routing. CODEX ACK pending. Only owner-approved merges. Rollback means close Draft and delete only owned QA paths and your disposable sandboxes. Do not delete user saves.
