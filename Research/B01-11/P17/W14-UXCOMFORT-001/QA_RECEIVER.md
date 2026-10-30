# B01-11 P17/W14 UXCOMFORT-001/R1 receiver

**B01-40 G0:** independently verify frozen exact PR SHA, **8 payload SHA-256 + byte counts + Git blob SHA1** and **9 owned diff paths**, dedicated workflow and source/test/docs ownership only; zero `Assets/**`, camera/VFX/CHL/PARTSIZE/Secure #510 or other owner changes. Check no overlap with separate B01-12 A11Y-PREFS #479. Review deterministic time rolling 1000ms logic, cap, fail-closed constructors, run isolation, source provenance and discard-only rollback. Publish exact-SHA SOURCE_G0 outcome independently; producer self-hashes do not equal external acceptance.

**B01-60 source QA:** independently check out final exact SHA, run original `dotnet run --project Research/B01-11/P17/W14-UXCOMFORT-001/Tests/UXComfortTests.csproj -c Release` on available systems and challenge original 98 cases plus edge tests: two permitted within one second at 250ms gap, third static, 999/1000 window boundary, reduce motion switches mid-window, no flashes never remove Critical static indicator, all 7 effect kinds, flash-count <=4 after 5000 events, invalid input produces no mutation, `long.MaxValue`, run resets. Publish QA receipt, NOT a native Unity pass.

**Native Unity G1:** REQUIRES authorized licensed host and Codex-built adapter: real on-screen visual effect/rendering, URP/HDR luminosity, keyboard/controller preferences, pause/resume, human accessibility. Test **not run** by B01-11. No epilepsy-safety guarantee.

**Receiver:** B01-40 G0, B01-60 independent source QA, B01-00 future #79 OFFER only after acceptance; Codex must ACK. States: PRODUCER_SOURCE_PASS, G0_EXTERNAL_PENDING, QA_EXTERNAL_PENDING, G1_UNITY_NOT_RUN, CODEX_NOT_OFFERED, ACK_PENDING, NOT_INTEGRATED, G2_HOLD. Rollback discards own isolated Research files/workflow; no canonical changes.


## W15 / R2 exact-SHA independent receiving

**B01-40 source G0:** independently verify final R2 SHA, 9 source/test/docs/workflow payload SHA256/UTF-8 bytes/Git blob digests (plus manifest self-exclusion), 10 total own diff paths, no `Assets/**` or frozen #510/#556 modifications, prior immutable R1 SHA `866ab291924be2449fe8b295a1c1291259183608`, no Codex OFFER/ACK. Verify fail-closed input validation precedes zero handling and `Record` consumes source sequence/time exactly once. Record standalone source G0 or HOLD with exact evidence.

**B01-60 independent source QA:** check out exact final R2 SHA and execute `dotnet run --project Research/B01-11/P17/W14-UXCOMFORT-001/Tests/UXComfortTests.csproj -c Release --no-launch-profile`. Expect **116/116 executed original C# cases**, and independently verify RED 14 failures on test-only SHA against R1 if investigating reproduction. Challenge real/budget crossing at 249/250ms and 999/1000ms, intensity 0 flash+shake, 5000-event no-op flood, critical icon fallback and retained source replay protection. Record independent PASS/FAIL; producer CI and self-hash checks do not equal external QA.

**Native Unity G1 / actual visual safety:** requires licensed Unity 6000.3.14f1 and authorized Codex-owned real visual emitter. Effects output is a pure effect recommendation, not an actual installed camera or shader. Screen-level emitted flash and screen-reader UX, URP/HDR and human playtests remain NOT_RUN. B01-00 alone may offer source, Codex ACK/integration separate. No merge.
