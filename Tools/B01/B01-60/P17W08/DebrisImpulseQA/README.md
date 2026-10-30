# B01-60 — PASS17 WAVE08 — independent mathematical invariants of original debris mechanics

Frozen immutable producer: [B01-23 Draft PR #421](https://github.com/C8LyNn/RoboRougelike/pull/421) exact SHA `cb2b3a76e84266e2730fffc4ab048c1039f0652f`, original source Git blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`. **This QA does not rewrite, copy, or repackage the producer C#**. The independent project compiles the **real unchanged original** `Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs` with a completely separate C#9 test oracle in this folder.

## Distinct physical-rule verification (not #443 emitter stubs / not original 33 author tests)

The 50,000 deterministic detached part events each calculate an **independent exact Newton-second analytical model** `mass * clamp(desired radial speed − actual radial velocity, 0, max added speed) * normalized outward direction`, compare X/Y/Z, radial and tangential deltas, impulse cap, and final radial speed. Each event additionally tests that absolute world translation leaves the impulse invariant, and that a 90-degree global yaw rotates the output correctly; duplicates are refused and 390+ randomly spaced explicit rearm operations reproduce the same impulse. Distinct from author 33 unit cases, these are more than 50,000 original C# calls and many independently checked invariants.

Additional adversarial tests: 2,000 invalid API values (NaN/Infinity/negative/out-of-contract positions, mass, speed) must not consume identities, followed immediately by a successful actual valid call with **the same ID**; 64 coincident-centre IDs cover all eight deterministic fallback XZ directions; already-fast or zero impulse still consumes one-shot until explicit reattach, and new-run reset restores; extreme but supported positions/mass/speeds yield capped finite output. Actual original 13-entry producer source-member Git blobs are independently checked against immutable exact SHA; no producer/Codex touched. Two GitHub-hosted operating systems compile/run unchanged C#9 using .NET8, with reproducible original stdout receipts and a normalized SHA256 event digest.

Reproduce after clone/checkout of **this own branch** with .NET8, Python3 and Git:

```bash
python Tools/B01/B01-60/P17W08/DebrisImpulseQA/verify_frozen.py
dotnet build Tools/B01/B01-60/P17W08/DebrisImpulseQA/IndependentQA.csproj -c Release --warnaserror
dotnet run -c Release --no-build --project Tools/B01/B01-60/P17W08/DebrisImpulseQA/IndependentQA.csproj
```

**Status:** independent pure C# source QA (G0), **NOT native Unity**. No licensed Unity runtime/real Rigidbody/PhysX, detached part prefab collisions, HP/garage/save/Player, or human gameplay run. Original adapter is separately covered by [B01-61 #443](https://github.com/C8LyNn/RoboRougelike/pull/443), which uses a declared synthetic UnityEngine stub; it is not real native Physics. **Do not say G1 PASS or integrated**.

Recipient: B01-23 original source owner for actual defects; B01-40 physical/GUID/assembly gate; B01-00 triage and exclusively Codex #79 offer if authorized. Rollback deletes only B01-60 own QA files/workflow; never touch original producer `Assets/**` or Codex canonical game code, never merge.

## Rzeczywiste wykonane testy (zamiast deklaracji)

[**Exact code test run #41045871841 — COMPLETED SUCCESS Windows2025 + Ubuntu24.04**](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045871841), test source commit `15ba847231d68598bf3a2ecf47c47c1de4fc63dc`. Oba systemy skompilowały **oryginalny, niezmieniony** `DebrisImpulseCore.cs` w C#9 `--warnaserror`, potwierdziły 13/13 oryginalnych członków źródła Git oraz wykonanie pięciu niezależnych grup `5 PASS / 0 FAIL`. Każdy: **50 000 rzeczywistych decyzji oryginalnego C#**, 26 979 niezerowych impulsów + 23 021 impulsów zerowych, **50 000 odrzuconych dubli**, **2 000 błędnych wejść** bez utraty identyfikatora, 394 poprawne rearmy, 8 fallback kierunków, transformacja translacyjna i obrót yaw z weryfikacją własności matematycznych. Identyczny SHA256 śladu na obu OS: `5b33f65e80cf471370008380ab78e47f1c81be0e465ac4b889f92ca6b7e4c571`.

Dowody rzeczywistych uruchomień: [Windows #12971494182](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045871841/artifacts/12971494182) · [Ubuntu #12971613739](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045871841/artifacts/12971613739). Wszystkie testy są **SOURCE-ONLY, nie Unity/G1**. G0 matematyczne QA B01-60 PASS dla #421 exact source. Formalny B01-40 binarny odbiór rzeczywistej paczki/GUID/asmdef i native Unity PhysX nadal osobne.
