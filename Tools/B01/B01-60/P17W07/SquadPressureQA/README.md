# B01-60 — PASS17 WAVE07 / independent SQUAD-PRESSURE QA

**Actual source under test:** frozen B01-24 [Draft PR #429](https://github.com/C8LyNn/RoboRougelike/pull/429) at commit `4d13f76e6c0c08033e305900daf61c0a7267a725`; unmodified original B01-24 C#9 `SquadPressure.cs` Git blob `3402fe61153e307ba164340956afd76aec6e8603` and original Codex #383 `Assets/R3/R3EnemyArchetype.cs` blob `66cd1ee734b129117b3753d1205469a0496cb673`. **This is a new independent executable QA fixture**, not an AI gameplay module, balance rewrite, second owner package, or reproduction of the owner's own 40 tests.

## Different, directly executed coverage

`IndependentQA.csproj` compiles *unchanged original* scheduler and original Codex R3 enum, **C#9 warning-as-error**. It calls their real `SquadPressureScheduler.Tick` from the separate independent `Program.cs` and uses externally validated lifecycle invariants:

- 128 seeded 1–4-member squads, **480 irregular ticks each = 61,440 real C# Tick transitions**, with changing target visibility, disabled capability, killed enemies, exact range boundaries and out-of-range retreat. Every pulse is compared against an identical original scheduler with **shuffled observation order** and independently checked for exclusive active slot, legal `Started/Active/Ended/Idle`, eligible selected actor, exact original R3 intent mapping, global window spacing, individual cooldown, valid forced end and scout flag TTL. Full cross-OS deterministic event digest emitted.
- Eleven malformed input types (clock/order, invalid/duplicate/foreign IDs, NaN, Infinity, negative/out-of-contract range, overflow and count) are submitted to only one twin scheduler, then both receive identical valid ticks: resulting pulses must match (no invalid input state mutation).
- 250 injected invalid ticks, each followed by legal ticks against a separate untouched reference, testing repeatable fail-closed state transactionality.
- Sparse long timeline jump, 64-bit tick top end and overflow prevention.
- Four-role fairness with one archetype transiently disarmed; isolated second squad never affected by the first squad's intentional LOS blackout.

The source-pin script verifies the 7 original source manifest Git object hashes; strict B01-60-owned paths only; unchanged Codex R3 source and original C# core source. Windows 2025 and Ubuntu 24.04 Actions logs are preserved. Tests run with **original untouched C#**; no JS/Python gameplay reimplementation, no production source modifications.

## Reproduction (from checkout of this QA branch)

```sh
python Tools/B01/B01-60/P17W07/SquadPressureQA/verify_frozen.py
dotnet build Tools/B01/B01-60/P17W07/SquadPressureQA/IndependentQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-60/P17W07/SquadPressureQA/IndependentQA.csproj
```

The workflow fetches immutable #429 and original Codex gameplay #383 SHA first. **Do not claim Unity native pass.** Source G0 means the original C# deterministic advisory scheduler meets tested invariants, not the mechanism is imported, host AI attacks issued, prefab velocity/LOS/target identity tested or Player built. Native Editor/PlayMode/PhysX = NOT_RUN. Producer's scout mark is generated at `Started`, before window completion; integration must decide whether interrupted/scout target marks should remain valid. This is a contract ambiguity, not proof of a runtime bug in single-target combat.

**Handoff:** B01-24 receives defects/QA verdict, B01-40 receives frozen SHA and independent evidence for source gate, B01-00 alone chooses any subsequent queue #79 offer (current form package already offered). Codex alone owns canonical R3 AI/LOS/tick adapter and integration. No merged PR. **Rollback** removes only these B01-60 test sources and workflow; producer #429 and Codex #383 remain completely unchanged.
