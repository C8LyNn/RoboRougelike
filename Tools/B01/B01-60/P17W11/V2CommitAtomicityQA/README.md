# B01-60 — PASS17 / WAVE11 — genuine original V2 commit transaction atomicity QA

**Task [#500](https://github.com/C8LyNn/RoboRougelike/issues/500)**, owner immutability [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). Source under test **B01-23 original, unchanged [#488 @ `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3`](https://github.com/C8LyNn/RoboRougelike/pull/488)** and original independent [B01-62 #486](https://github.com/C8LyNn/RoboRougelike/pull/486). **This is an independent B01-60 QA-only receiver, not a V2 production fix or a second V2 variant.**

## Specific difference from existing QA #491 / #486

- [B01-61 #491](https://github.com/C8LyNn/RoboRougelike/pull/491) independently validates all three public `GuardedDockCommand` aliases, but they may call a private mutator which is not transaction-atomic.
- [B01-62 #486](https://github.com/C8LyNn/RoboRougelike/pull/486) executes actual original W06 ledger + V2 portable public policy and uses an explicitly *modeled* physical partial-state witness.
- **THIS NEW QA actually extracts the ENTIRE `private bool CommitDock(int index)` method from its exact raw original producer Git blob, then compiles and invokes that unmodified actual owner method body** alongside unmodified real W06 `DebrisImpulseCore.cs` and original V2 `SecureDockPolicy.cs` under C#9. A QA-owned double represents only the Unity Rigidbody, Transform, parent and velocity interfaces, so the owner-written *actual mutation statements* execute for real against measurable QA state. This is **not** native Unity or real PhysX. Method copied to ephemeral generated file only, erased after test; no owner file is written.

## Execution

From the isolated PR branch root, with Git, Python 3.11 and .NET8:

```sh
python Tools/B01/B01-60/P17W11/V2CommitAtomicityQA/gate.py
```

The gate pins exact unchanged owner V2 class Git blob `491c89a18827bb027854bba5e6be6630ce4947b6`, source V2 policy blob `cc6611343d585fd0f16ff048f8ecdb0e7b1be30c`, **original W06 real ledger** blob `fa1aa5e531927cbe73cf5fdfe161b4a981316db5` and emitter blob `2b3a8465c550bd49a5c98be5a8064a4ce0947f4b`; verifies 27 original producer paths and exactly six new QA-only paths; outputs real compiler and C# test logs plus exact original extracted `CommitDock` method SHA256 receipt. Tests three distinct public aliases separately and compiles the genuine producer method:

- Positive: original `TryDetach` records part ID, actual original `RearmAfterReattach` consumes it, actual compiled method commits.
- Inspection denied: unchanged body velocity, kinetic flag, parent, local mount and original W06 consumed ID.
- **Missing ledger ID:** original actual `ClearForNewRun` resets ledger while part stays detached, allowed inspected route enters actual `CommitDock`, **original source throws only AFTER all six physical field mutations**. Expected baseline QA result is intentionally **ORIGINAL SOURCE TRANSACTION RED**, not a valid accept. Three aliases are included.
- Invalid index: refuse without mutation.

Successful CI means the **known original RED was genuinely reproduced**; do not mistake it for a GREEN producer. After B01-23 issues exactly one new immutable corrected version, this red fixture should be re-run as a GREEN **reject-no-mutation** assertion on new source, separately from native Unity licensed PlayMode. This specific baseline fixture is designed for frozen #488 and must refuse a changed method until the owner/receiver explicitly revises the expected test.

## Release gates / rollback

**Global SAFE_REATTACH = HOLD** while either old W07 public route remains in a shipped scene or original V2 commit can leave a partial transaction. #494 is a competing owner path; **DO NOT install both**. B01-23 and B01-00 alone choose the supported correction, freeze new SHA, and arrange exclusive host migration. B01-40 independent ZIP/GUID/asmdef receipt, B01-60/61/62 independent source QA, real **Unity 6000.3.14f1 / PhysX / Rigidbody / PlayMode / Player G1 NOT_RUN** until licensed host; G2 HOLD / Codex ACK pending / not integrated / no merge.

Rollback: close Draft and remove only the six own B01-60 QA files/unique workflow; do not change frozen upstream #488 or #421, Codex, or canonical Unity.
