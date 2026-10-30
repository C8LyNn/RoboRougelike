# B01-40 / PASS 17 WAVE 24 — independent source transport G0, candidate-only

## Exact immutable inputs
- One currently **selected** W23 production candidate **#749**: `8634e71dfcf633323073fe584a6c0884f1ca5d23`
- **Unselected** W24 descendant public-Store hardening **#773**: `3aaaec4027ee246fbfb6f209179f0e9507363473`
- One original canonical OS owner #729: `b8ca29c7707cd6b4338d0b9772d74dee676d49d2`
- B01-23 new Unity physical replacement port **#772**: `ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1`
- B01-22 approved detached economy logic **#764**: `cc5d65f66ad7a764af6515c566e489b9fc3be82e`
- B01-23 selected R5 corrected ColdRestart source **#750**: `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb`
- Frozen #510 game `d94400d95d13db7f27e1533df05c9292d5380431` / #556 Save `33094f154c7b91119b36d58aab285e6e83c5de57`.

## Executable G0 scope

`intake.py` reads **original exact Git objects** of the eight named owners in separate checkouts. It verifies head SHA, exact owner-only additive diffs (new R5 fixture is the sole allowed modified existing source), immutable original API/codec/old Save Git blobs, candidate-only ancestry and no #753/#741 alternative lineage. It performs an original-byte **source-only** Unity Assets staging preview, rejects collisions by path and 32-character Unity GUID, checks 9 new physical-port + 4 economy meta GUIDs, validates serialized `m_Script` references and necessary asmdefs, then physically creates and safely rolls back a **new** disposable private staging tree.

Two negative rollback probes must **refuse** removal when an owned file has changed or a foreign empty folder was introduced. A separate neighboring sentinel file and empty directory are preserved. A receipt with original SHA256 per staged source path and pinned exact Git parents is uploaded by the two-OS CI. No existing Unity project, player Save or operating system lease is modified by the scratch gate.

**Gate meaning:** G0 is source origin / transport / candidate packet integrity only, never executable game integration. #773 source OS tests and hostile concurrence require independent B01-61/B01-41 semantic QA. An expected-green #773 does **not** mean that #749 is replaced: B01-00/B01-23 must explicitly select the **single** candidate. Do not co-install #749's old raw public Store with #773's replacement; #753/#741 remain ALT_HOLD. Do not flat-overlay different owners' `PACKAGE_MANIFEST.json`; the generated B01-40 manifest is namespaced.

## Remaining P0 blockers and honest release gates

- New #772 does **not** import private existing frozen #510 `Rig[]` nor claim a live Unity host integration. Its `Rigidbody`/ledger replacement needs authorized Unity-scene, assembly and selected-session binding; native physics retirement must be tested.
- Approved #764 `65%`/one-free-per-authentic-distinct-victory/one-shot-weak-loaner and nonfunctional-only deployment policy remains a detached reducer. It is **not** yet recorded atomically inside a versioned full Save envelope with world, wallet, proof IDs, next-win recovery and one physical owner.
- Corrected #750 R5 g4→g5 source fixture does **not** prove activated Unity 6000.3.14f1 Windows PlayMode.
- For honest-process crash/corruption, require original complete-run/generation/ID consistency, lost ACK quarantine, single physical `.owner`, no second `.lease`, physics roots retirement, migration and independent hostile real OS regression. Hostile on-disk whole-save replacement anti-tamper is deliberately outside owner-approved release scope.
- G1 = **NOT_RUN** until genuine user-consented Windows Unity Editor.log, NUnit XML and version+SHA are supplied. G2 = HOLD; Codex ACK = NONE; merge/release = NO.

## Lifecycle / handoff

`IMPLEMENTED=YES_QA_SOURCE | TESTED=CI_PENDING_UNTIL_LOGS | QA_ACCEPTED=NO_FORMAL_PRODUCT | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`.

Receivers: B01-00 #731 (selection/governance), B01-62 #773 (single-store fix), B01-23 #772/#750 (physical host/ColdRestart), B01-22 #764 (economy atomic schema), B01-61/41 (independent negative QA), B01-60 (native G1). This branch is disposable and touches **only B01-40-owned new files and CI**; close the Draft to roll back. No frozen sources, Codex environment, protected branch or owner computer is changed.
