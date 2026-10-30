# B01-11 — P17 / W15 — UXCOMFORT-001 R2 zero-stimulus RED→GREEN

## Root cause in actual R1
At immutable R1 source `866ab291924be2449fe8b295a1c1291259183608` the original `VisualStimulusGate.Record` handled a flash of intensity=0 as a timed emission: it added `_nowMs` to `_flashTimes` and returned `TimedEffect` / `ReducedEffect`. This artificial event could block a later positive-strength flash despite no light-producing input. Zero-amplitude collision/part detach similarly generated `TimedEffect` camera shake. Deterministic cue-level issue, **not** proof of actual Unity game visual emissions.

## Test-first RED
`a06d5e0b59a9fb24c38de5b54ed63bef74717fd9`: added 18 `W15` direct tests to same actual original source file under the same dedicated .NET8 project, runtime source **unchanged**. [GitHub source CI #41091279821](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091279821) on Ubuntu **102 PASS /14 FAIL**, Windows **102 PASS /14 FAIL**, job logs inspected. The 14 actual fail cases include zero flash static fallback, quota/cooldown budget, follow-up nonzero protection, reduced motion zero, zero shake animation, 5000 zero cue flood, and real-zero-real ordering; 4 negative/baseline checks were already passing.

## Actual fixed source GREEN
`2a2bd304454efc3cc3bb825e5617b669e29b59f5`: original pure C# source adds zero-intensity branch **after** authoritative validation/time/sequence update, **before** flash budget or shake scheduling. Preserves type-specific indicator and returns `StaticIndicator` with 0 emitted strength; no flash timestamp and no timed shake. Future positive-strength effects are no longer artificially suppressed. [Actual original-source CI #41091389393](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091389393) Ubuntu **116/116 PASS**, Windows **116/116 PASS**, 0 FAIL. Original 98 tests preserved.

## Scope, limits, receivers
R2 is only a source-level correction within original producer-owned UXCOMFORT research path; historical R1 SHA immutable, no Codex, Secure #510, Save #556, Unity scene or VFX changed. Final package documentation/manifest SHA must have independent source CI rereun, exact hash verification, B01-40 external SOURCE G0 and B01-60 external original-source QA. Native Unity Editor/PlayMode/Player and real rendering G1 **NOT_RUN**; CODEX_NOT_OFFERED / ACK_PENDING / NOT_INTEGRATED / NO_MERGE.
