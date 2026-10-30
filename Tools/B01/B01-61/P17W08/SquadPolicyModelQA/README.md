# B01-61 — PASS 17 WAVE 08 — Variable-policy model QA for SQUAD-PRESSURE #429

**Package:** `B01-61-SQUAD-PRESSURE-VARIABLE-POLICY-QA-001 v1.0.0`.
**Target:** immutable original B01-24 [#429](https://github.com/C8LyNn/RoboRougelike/pull/429) at `4d13f76e6c0c08033e305900daf61c0a7267a725`, original `SquadPressure.cs` Git blob `3402fe61153e307ba164340956afd76aec6e8603` and original Codex R3 archetype Git blob `66cd1ee734b129117b3753d1205469a0496cb673`.

## Why this is different from existing QA #434

[#434](https://github.com/C8LyNn/RoboRougelike/pull/434) performs state invariants and randomized original-source sequences under `SquadPressurePolicy.Standard()`. This suite executes an **independently written reference model under 96 distinct, legal NON-DEFAULT policies** (variable window durations, global gaps, mark TTL, cooldown arrays, four role ranges). It compares every pulse field from the original compiled B01-24 scheduler against the external oracle under dynamic alive/LOS/ability/range and mixed frame partitions. That catches policy-dependent regressions rather than only standard-policy invariants.

- 96 `SquadPressurePolicy` configurations × 512 calls to **actual original C#9 code** = 49,152 exact model comparisons;
- role arrays / non-default cooldowns / boundary distances and rotated observation order;
- negative checks of malformed policies, policy arrays defensive-copy, NaN and duplicate-tick rejection without state mutation;
- deterministic event-trace SHA256 for cross-platform comparison (log emitted by runner).
- Separate Git object and PR-diff gate checks untouched two original C# blobs and EXACTLY six QA-only files.

**This is pure-source QA; it does not assert native Unity/PhysX/Player success.** No producer code, shared gameplay, canonical Codex assets, CHL, PARTSIZE or Model Lab content is changed.

## Reproduction

From the repository root on the isolated QA branch:

```sh
python3 Tools/B01/B01-61/P17W08/SquadPolicyModelQA/source_gate.py
dotnet build Tools/B01/B01-61/P17W08/SquadPolicyModelQA/ModelQA.csproj --configuration Release --warnaserror
dotnet run --no-build --configuration Release --project Tools/B01/B01-61/P17W08/SquadPolicyModelQA/ModelQA.csproj
```

The GitHub Actions workflow runs both Ubuntu 24.04 and Windows 2025 and stores raw C# stdout and stderr. `SOURCE_QA PASS` is conditional on the actual exact-HEAD CI finishing successfully; CI does not imply native G1.

**Acceptance receiver:** B01-40 for read-only G0/source pin and artifact validation; B01-24 for model findings; B01-60 for second-line independent review; B01-00 for queue/claim records. Native G1 remains assigned to authorized Unity/PhysX host. Codex ACK is not presumed.

**Rollback:** close Draft PR without merging and remove only `qa/b01-61-p17w08-squad-policy-model`. Original #429 and its frozen branch remain untouched.
