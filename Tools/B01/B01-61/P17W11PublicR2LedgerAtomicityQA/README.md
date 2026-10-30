# B01-61 / PASS17 W11 — independent original-source W10 R2 reattach ledger transaction QA

**Package:** B01-61-W11-PUBLIC-R2-ORIGINAL-LEDGER-ATOMICITY-QA-001 v1.0.0
**Exact original target:** B01-23 [#494](https://github.com/C8LyNn/RoboRougelike/pull/494) @ `6efe45961ed0ffdf183e8016e350978b9f3dc51c`; frozen original W06 [#421](https://github.com/C8LyNn/RoboRougelike/pull/421) @ `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. No edits to either source or Codex. Distinct from B01-62 #486 (modeled V2), B01-61 #491 (V2 public route), #492 (old W09 external method).

**Critical negative transaction path on #494:** `DebrisRigPlayground.Reattach` now correctly checks *ALL-layer physical clearance before performing state changes*, which repairs the prior public bypass in its new version. However, after clearance passes, it still sets `linearVelocity=0`, `angularVelocity=0`, `isKinematic=true`, reparents the GameObject, sets local mount and `Detached=false`, **then** calls original W06 `DebrisRepulsionEmitter.RearmAfterReattach(ID)`. If the ledger has been cleared by its real public `ClearForNewRun()` during a detached state, `RearmAfterReattach` returns false and public `Reattach` throws **after all physical mutations**, leaving partial state. This is an explicit fault-injection scenario, not a claim of normal-frequency gameplay reproduction.

## REAL executable original C# evidence (not mocked gameplay class or ledger)
`LedgerAtomicityQA.csproj` compiles exact unchanged original #494 `DebrisRigPlayground.cs` **and** original W07 `RigDemoDefinition.cs` **and** real unchanged W06 `DebrisImpulseCore.cs` + `DebrisRepulsionEmitter.cs` as C#9/.NET8, not a reimplementation of their ledger/reattach logic. A QA-only `UnityBoundary.cs` simulates Transform, Rigidbody and Physics interfaces, and is **NOT native Unity PhysX**. The independent runner executes Light+Heavy positive baseline detach/rearm, then actual emitter `ClearForNewRun()` after detach, followed by real producer public `Reattach`. It records exception and before/after Rigidbody/parent/Detached/ledger and insists the original partial-state witness is observed.

From repo root, with .NET8 SDK, Git and Python 3.11:
```bash
python Tools/B01/B01-61/P17W11PublicR2LedgerAtomicityQA/source_gate.py
dotnet build Tools/B01/B01-61/P17W11PublicR2LedgerAtomicityQA/LedgerAtomicityQA.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W11PublicR2LedgerAtomicityQA/LedgerAtomicityQA.csproj -- --evidence
dotnet run --no-build -c Release --project Tools/B01/B01-61/P17W11PublicR2LedgerAtomicityQA/LedgerAtomicityQA.csproj -- --release
```
`--evidence` exits 0 only if both source-executed corruption witnesses exist, while `--release` **intentionally exits 3** to block unsafe G0. Both OS jobs preserve original logs and exit status. Green CI means *negative QA passes*, not safe reattach.

## Real Unity PlayMode negative tests (AUTHORED, NOT_RUN)
Under `Assets/B0161W11DockAtomicityQA/**`, two NUnit Unity tests instantiate the exact real W10 playground + original W06 emitter, detach Light / Heavy, call **real emitter `ClearForNewRun`**, then attempt `Reattach`. The acceptance assertion demands `false`, no exception, and **no mutation** of velocity, Rigidbody kinematic, parent, position, Detached and ID ledger. Current original source is expected RED natively; no licensed Unity6 Editor/PhysX/PlayMode was available here.

## Required correction and ownership
B01-23 alone selects ONE producer solution per [#500](https://github.com/C8LyNn/RoboRougelike/issues/500), publishes NEW version/SHA with a prevalidated ID/ledger reservation or rigorously exception-safe atomic rollback; original source versions stay immutable. Revisit every external/public alias, physical blocked/clear-path tests, rearm failure and repeat detach/rearm. B01-40 validates exact source/ZIP/GUID/asmdef G0. B01-60 runs true Unity6000.3.14f1 native G1; B01-00 controls #79. This PR does **not** claim which producer was selected, no CODEX_ACK, G1=NOT_RUN/G2=HOLD. **Rollback:** close unmerged Draft, remove only new B01-61 QA owned paths. No CodeX or producer changed.
