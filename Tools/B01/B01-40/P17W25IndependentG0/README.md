# B01-40 — P17/W25 independent G0 — #773 + #772 + #750

**Owner:** B01-40. **Source selection is NOT mine.** This is a new-only isolated QA package based on immutable `main` commit `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`, without merging any production source.

## Exact tested input

- #773 @ `3aaaec4027ee246fbfb6f209179f0e9507363473` — selected-#749-only proposed correction of publicly callable raw Save path.
- #772 @ `ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1` — new physical Rig/Rigidbody/AtomicDockLedger implementation (not wired into old private #510 scene).
- #750 @ `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb` — corrected R5 Unity ColdRestart NUnit source fixture, g4→saved g5.
- Original #729 + #556/W06 + #510 as pinned in manifest. #753/#741 are ALT_HOLD, never combined with #773.

## Execution / precise evidence

`intake.py` fetches exact original commits; checks each ancestry and intended changed-file scope, validates 9 new #772 Unity .meta GUIDs and two asmdefs against old source packages, verifies fixture generation 5 and the physical ABI, and performs a safe isolated filesystem stage/rollback while preserving alien scratch files and empty directories.

`prepare_runtime.py` copies **original exact Git-source bytes** into a fresh disposable .NET8/C#9 directory. `CaseAliasNegative.cs` independently invokes #773 raw public `TryCreateNew` with the genuine #729 OS lease. On Ubuntu it attempts a *different physical directory that only differs from the canonical path in letter case*. The call **must** refuse it before creating an alternate Save; on Windows the normal foreign-sibling denial remains tested and case alias is not claimed as a separate OS path. This is an adversarial QA check, not the producer's own source suite.

**Important:** expected-source-test RED means G0 product HOLD. Do not change the test to invert RED into PASS, bypass the source code or promote #773 on failure. A source GREEN would still be SOURCE_TRANSPORT_ONLY, never native Unity/physically integrated gameplay.

## Scope / blockers

- R5 fixture source is not actual native Unity PlayMode; no Editor.log/NUnit3 XML means G1=NOT_RUN.
- #772 creates a separate real physical Rig[] world but does **not** import existing frozen #510 private Rig[] or connect to a single #773 MonoBehaviour session.
- One atomic envelope has no authenticated persistent wallet/victory/one-use loaner schema yet.
- External malicious disk backup rollback is outside approved release threat model; honest crashes and API safety remain in scope.
- No changes to main/develop, frozen source, Codex or user PC; no merge/release. B01-23/B01-62 own any corrected version, B01-00 makes final exclusive selection, B01-60 runs native G1 only on owner opt-in.

**Six stages before actual CI:** IMPLEMENTED=QA_ONLY; TESTED=PENDING_REAL_CI; QA_ACCEPTED=NO_PRODUCT; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO.
