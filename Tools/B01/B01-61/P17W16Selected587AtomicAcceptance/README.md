# B01-61 / PASS 17 WAVE 16 — single selected B01-23 #587 candidate atomic release gate

Assignment [#589](https://github.com/C8LyNn/RoboRougelike/issues/589); B01-61 independent original C#9/.NET8 source QA. **Selected producer lineage preference: B01-23 #587; #585 and B01-62 #577/#584 are NOT canonical alternatives.** This QA branch is stacked on **exact frozen W15 #587 candidate** `ad7904eca571c742257551fa8103f266fcb9b752`. Its original source `Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs` has Git blob `16a7d7043a2ecb59a41bbfb063170235872fd7fc`, Unity-facing `IsolatedTwinRigSaveHost.cs` original blob `ff5da20e3c98ab3b540f37fb5652666c47228ed3`. Frozen ONLY schema [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) `33094f154c7b91119b36d58aab285e6e83c5de57` original C# blob `b52e511c370f5fca56c74029050844dc8cbb7f9a`, W06 original blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5`, #510 immutable gameplay commit `d94400d95d13db7f27e1533df05c9292d5380431` and actual `SecureRigSpec.cs` are used **unmodified**. No ownership violations or merge.

## What is independently new vs W15 QA #572 / #588

An original, independently-authored `AtomicAcceptance.csproj` compiles **real, unmodified B01-23 #587 coordinator C#** directly with **real selected #556 and original W06 + #510 catalog**. The `IWorldFactory/IPreparedWorld` adversarial physics boundary only is a fault-injectable test double. This is a tightly scoped SOURCE/API test, not a Unity/PhysX native test or rewrite of the production transaction implementation.

**15 new executable cases** verify actual Light/Heavy consumed stable IDs, original binary Save roundtrip, accepted higher generation and retirement, two activation-time divergence attacks (**pose drift** and **stable-ID drift**), activation exception and compensating restore of old world, failed rollback cleanup and sticky quarantine, checksum-recomputed foreign ID, wrong run, stale external min-generation, two-stage idle callback race, reentrant load denial and identical-generation replay. Two **explicit RED** witnesses show (a) different consumed-ID world accepted with *same run+generation*, retiring prior body; (b) a failed rollback `Deactivate()` makes `Quarantined` true but the method **returns `HostException`** instead of `RollbackQuarantine`. Existing `IsolatedTwinRigSaveHost` *does* check `Quarantined` at callsite, but generic `AtomicWorldSwap.TryLoad` consumers reading only the public result code are exposed to misleading recoverability. This is an API/transaction contract risk, not a claim that the Unity wrapper forgot the flag.

**Critical separation:** 15/15 **QA witness assertions PASS** means the two historical W15 defects are accurately *reproduced*, NOT that the candidate is release-safe. A distinct `--release` invocation MUST exit **3** and print `RELEASE_VERDICT=HOLD_P0_RED` while any issue persists; CI asserts expected exit code rather than hiding it as GREEN. If a new versioned B01-23 source fixes both, independent QA must be rerun on **that exact new SHA** with the negative witnesses converted into positive source release gates, all original Git blob pins updated explicitly, AND separate G1 native evidence required. Do not force-push a frozen producer commit or silently bless a new HEAD.

This code uses no new Save schema, no serialized sidecar, no reflection, no private #510 rig/AtomicDockLedger field writes. Even perfect SOURCE tests here do **not** supply a durable independently persisted anti-replay watermark, an authorized #510 private ledger hydration boundary, or original live Codex scene integration. `#510/#556_SOURCE_G0_ONLY; #587_HOST_SOURCE_RELEASE=HOLD; UNITY_G1=NOT_RUN; G2=HOLD; CODEX_ACK=NONE; NO_MERGE`.

## Reproduce

```sh
dotnet run --project Tools/B01/B01-61/P17W16Selected587AtomicAcceptance/AtomicAcceptance.csproj -c Release -- --release
# expected exit 3, two explicit W15 P0 RED witnesses, full original source logs
```

Use the dedicated two-platform GitHub Actions workflow for exact ref/blobs, owned-path manifest and both test logs. Receiver: B01-23 (new, separately versioned exact producer correction), B01-62 (independent physical/ID QA), B01-40 (source/ZIP receiver), B01-60 (activated Unity 6000.3.14f1 G1 or NOT_RUN), B01-00 formal selection. #577/ #584 alternative paths not accepted as canonical.

`EXPECTED-RED ≠ RELEASE PASS`.
