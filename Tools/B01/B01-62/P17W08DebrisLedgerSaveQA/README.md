# B01-62 P17 W08 — DEBRIS-LEDGER-SAVE-QA-001/R1

Independent real source-level DATA INTEGRITY/SAVE-RELOAD QA for original B01-23 DEBRIS-REPULSE #421. Source HEAD: cb2b3a76e84266e2730fffc4ab048c1039f0652f. Original DebrisImpulseCore.cs Git blob: fa1aa5e531927cbe73cf5fdfe161b4a981316db5.

## Real save/load boundary

The unchanged original DebrisImpulseLedger uses an in-memory private HashSet of consumed stable-part IDs. It exposes TryDetach/RearmAfterReattach/ClearForNewRun/Contains/Count, but no direct save/load API. A *fresh original ledger* after an artificial restart accepts a previously detached ID and can repeat its impulse. Actual native Unity run reloading was NOT executed; no original save adapter is integrated.

The independent QA executable runs the unchanged original C#9 ledger and implements an isolated RECEIVER-ONLY disposable file proof: record IDs after successful original TryDetach calls, save canonical sorted positive long IDs and source/run/version fields to a new physical JSON file with SHA256 (corruption detection, NOT authentication), validate file invariants before any replay, and reconstruct consumed IDs in a new unchanged original DebrisImpulseLedger using original TryDetach called with zero desired speed and zero max added speed. Then duplicates remain rejected, new IDs are allowed, and reattach/new-run reset semantics are checked.

This receiver prototype is NOT a game patch or installable production save system. No original game/producer source is mutated, and no genuine user save is read or touched.

## Executable independent testing

Python prepare.py reads the original immutable Git object, fails closed if the Git blob drifts, and copies only that UNMODIFIED core to a fresh disposable .NET 8 compilation project. C#9 warnings-as-errors build.

Program.cs runs 28 real source/physical disk checks per OS: original impulse magnitude, within-run one-shot, additional ID, invalid ID, stable sort, create-new disk save, physical SHA envelope, explicit original fresh-ledger duplicate-impulse observation, safe zero-force replay on new original ledger, restored duplicate IDs rejected, fresh ID allowed, rearm/re-detach and new run clear. Negatives reject SHA corruption, wrong run/source/version, duplicate/zero/unsorted IDs, oversized list, missing payload, missing input, and overwrite of existing receipt. Two further tests cover empty valid receipt and deterministic canonical JSON.

CI Windows 2025 and Ubuntu 24.04 each execute original source and publish actual source outputs and logs; a third CI job uses byte-for-byte comparison on receipt.json and qa-result.json across OS.

## Runbook

Requires Git, Python3.12 and .NET SDK8. All paths below must be FRESH scratch dirs, never an existing real save or the repository.

    git fetch --no-tags origin cb2b3a76e84266e2730fffc4ab048c1039f0652f
    python Tools/B01/B01-62/P17W08DebrisLedgerSaveQA/prepare.py --repo . --out /tmp/b0162-pr421-original
    dotnet build /tmp/b0162-pr421-original/DebrisLedgerSaveQA.csproj -c Release -warnaserror
    dotnet run --no-build --project /tmp/b0162-pr421-original/DebrisLedgerSaveQA.csproj -c Release -- --sandbox /tmp/b0162-pr421-new-fixture

Windows uses equivalent new absolute directories under its temporary folder. The C# executable refuses existing sandbox paths. Restore validation uses full payload verification before reconstructing any consumed IDs.

## Limitations and handoff

A SHA256 checksum only protects against accidental corruption, not an attacker who modifies and re-signs untrusted JSON; this receiver is not an authoritative anti-cheat mechanism. The original Unity emitter, Physics/Rigidbody, real actual game save, domain reload, Player, EditMode/PlayMode and native Unity tests are NOT_RUN.

B01-40: independent source/manifest/hash/scope and artifacts. B01-60/61: independently rerun C# source persistence proof and review native behavior on an authorized Unity host if available. B01-23: original producer owns any proposed interface or versioned rework. Codex: exclusive canonical gameplay/save integration, after B01-00 authorized #79 intake. No Codex ACK, merge or native G1/G2 claim.

Rollback: close unmerged Draft PR and remove only B01-62-owned test folder/workflow and disposable test data you created; never delete user save files. This scope is distinct from B01-61 #443's original emitter/Unity-shim event tests and producer #421's core/ZIP checks.
