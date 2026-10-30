# B01-50 — LIGHTBUDGET-001/R2 | PASS17/WAVE17

## One actual R1 defect repair, not an additional similar budget
Historical immutable R1 [Draft #600](https://github.com/C8LyNn/RoboRougelike/pull/600) `68befcd02b9812ea1d001c868a7151944e3d1b16` captures original Light.enabled at Awake but later iterates the **same mutable serialized LightSlot[]** in LateUpdate and OnDisable. If the source array is shortened, LightSlot is set null or an entry points to a different Light, R1 can throw before restoration, leave the original Light incorrectly disabled, or turn on a foreign Light that never opted in. Mutating Importance post-Awake can also rerank the owned lights without validation.

**R2 changes just that ownership boundary**, and keeps the R1 pure selector and demo production files byte-identical (original Git blob proof in manifest). After complete validation, R2 snapshots owned Light references and priorities into its own arrays, and captures original enabled bits. Every subsequent frame and restore/disable/destroy uses **only pinned owned arrays**, never re-reads mutated serialized LightSlot[] or its changed Priority/Light. This is NOT a global Unity scene transaction: do not present it as atomic world commit.

## Files / no-coinstall policy
R2 exclusively NEW owned paths `Tools/B01/B01-50/LIGHTBUDGET001R2/**` plus dedicated `.github/workflows/b01-50-w17-lightbudget-r2.yml`. R1 source [#600](https://github.com/C8LyNn/RoboRougelike/pull/600) is unchanged and remains frozen; old #510 and #556 frozen; 0 canonical Unity Assets, Codex gameplay, other package paths or shared workflow modified. R2 is a **replacement candidate for R1**, NEVER load/import both R1 and R2 in the same Unity assembly because they deliberately expose identical C# types. For a disposable Unity 6000.3.14f1 URP host, remove only the verified previously manually imported R1 source+meta pairs first (after safety review), then copy three R2 .cs+.meta pairs with the new stable GUIDs into a new isolated folder. Never remove foreign directories, unknown files, saved scenes/prefabs or shared/occupied targets. Rollback is removal of ONLY the newly copied R2 script+meta files, restoring the exact pre-R2 host from its immutable backup. B01-40 must authorize any actual physical migration.

This remains an **optional cosmetics-only** Light.enabled scheduler. Do NOT opt-in gameplay-relevant, warning, combat, puzzle, primary illumination or navigation lighting. Light.enabled is exclusively controlled by the adapter during operation, and native Unity fault handling/visual readability still requires actual Editor QA.

## Truly executable adversarial RED→GREEN source tests
`tests/LIGHTBUDGET.AdapterRegression.csproj` links the **real production Unity adapter source**, not a rewritten validator. It compiles adapter methods with an explicitly named minimal **surrogate Unity API** only for isolated source-state tests. Tests use reflection solely in their fixture to invoke actual private Awake, LateUpdate, OnDisable and OnDestroy (never reflective access to the canonical game's private #510 ledger). The eight scenarios include normal cap/restore, null slot after Awake, foreign replacement, shortened serialized array, priority mutation, baseline-off restoration, idempotent disable/destroy and forbidden post-Awake reconfiguration.

- To prove authentic historical RED, CI fetches exact immutable original R1 adapter by `git show 68befcd0...:` and checks original adapter Git blob `ef5487d400da0a3372ecd157d00f3c9545390c23`. It executes **the same eight tests** against R1 and REQUIRES exactly 4 PASS / 4 FAIL (exit=1). This RED is a **deliberately reproduced source flaw**, NOT a passing R1 release gate.
- CI then builds identical test project against the **new R2 real adapter** and REQUIRES 8 PASS / 0 FAIL (exit=0). Separately, untouched original production selector carries forward 42/42 .NET 8 tests. R2 must pass both Windows 2025 and Ubuntu 24.04.
- Each source/evidence/workflow blob pinned in `MANIFEST-GIT-BLOBS.tsv`. Three NEW .meta GUIDs checked for uniqueness and collisions against both canonical Assets and R1 old metas.

**G1 honesty:** surrogate API tests are NOT real Unity engine execution, not live scenes, PhysX or GPU/FPS. CI-02/03 may report missing Unity activation; if no licensed host tests run, G1_NOT_RUN. No frame rate improvement claimed. B01-60 independent source QA and Unity tests remain pending; G0 B01-40 pending, G2 HOLD; no merge/ACK.

## Reproduce
Run in a throwaway checkout of this Draft R2:
```bash
git fetch --no-tags origin 68befcd02b9812ea1d001c868a7151944e3d1b16
git show 68befcd02b9812ea1d001c868a7151944e3d1b16:Tools/B01/B01-50/LIGHTBUDGET001/Unity/B0150OptionalCosmeticLightBudget.cs > Tools/B01/B01-50/LIGHTBUDGET001R2/tests/original_r1_adapter.cs
git hash-object --no-filters Tools/B01/B01-50/LIGHTBUDGET001R2/tests/original_r1_adapter.cs
# Expected: ef5487d400da0a3372ecd157d00f3c9545390c23
AdapterSource=original_r1_adapter.cs dotnet run -c Release --project Tools/B01/B01-50/LIGHTBUDGET001R2/tests/LIGHTBUDGET.AdapterRegression.csproj -- --receipt /tmp/r1-expected-red.json
# Intentionally exit 1, 4 of 8 FAIL
dotnet clean -c Release Tools/B01/B01-50/LIGHTBUDGET001R2/tests/LIGHTBUDGET.AdapterRegression.csproj
dotnet run -c Release --project Tools/B01/B01-50/LIGHTBUDGET001R2/tests/LIGHTBUDGET.AdapterRegression.csproj -- --receipt /tmp/r2-expected-green.json
# Exit 0 / 8 of 8 PASS
dotnet run -c Release --project Tools/B01/B01-50/LIGHTBUDGET001R2/tests/LIGHTBUDGET.Tests.csproj -- --receipt /tmp/r2-original-policy.json
# Exit 0 / 42 of 42 PASS
```

**Receiver:** B01-40 source/GUID/host rollback gate; B01-60 independent source & native Unity QA; B01-00 governance. No Codex ACK or canonical installation.
