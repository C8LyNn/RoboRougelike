# B01-40 — PASS17/WAVE14 — One Save contract transport lock, original C# boundary RED and safe receiver migration

**This is B01-40 independent INTAKE/QA, not a third Save contract**, not a replacement #510 candidate and not a rewrite of another agent's producer. Under HQ #552 / governance #276:
- Frozen **only** Secure Reattach V2 [#510 @ `d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510). #511/#494 remain HOLD.
- Two competing SAVE schema producers [#544 @ `e77529262b8f03c6e9f0e25b642038c351872044`](https://github.com/C8LyNn/RoboRougelike/pull/544) (independent C# schema under Tools only, no Unity .asmdef) and [#549 @ `6da2c523e03766cd5a6deaed0cb3cccf8765354b`](https://github.com/C8LyNn/RoboRougelike/pull/549) (new importable Unity C# assembly, four GUIDs) **are not interchangeable**. Producer owner B01-23/B01-62 / HQ must select ONE immutable corrected version and resolve actual known API mutation RED before any receiver import.
- [#550](https://github.com/C8LyNn/RoboRougelike/pull/550) independently executed **original #549** and found `RunSnapshot.Parts` mutable array exposure and `ColdRestore.GetLedger` mutable original W06 ledger. **#550 does not certify or test #544**.
- This W14 QA specifically compiles/runs **original unchanged #544 StableIdSnapshotCodec.cs** and original immutable W06 `DebrisImpulseCore.cs`, #510 `AtomicDockLedger.cs` and `SecureRigSpec.cs` in a C#9/.NET8 executable (no Unity simulation). It tests **real post-validated snapshot array mutability AND external `PreparedRestore.StagedLedger` mutation**. Two original-source witnesses must both be observed for CI to pass. Here QA PASS means **release SECURITY RED reproduced**; it is NOT a pass of the Save contract.
- Original #544 and #549 physical package.py scripts independently executed on exact pinned original source SHAs, each actual Git file binary payload/manifest verified, and #549 original cross-OS physical SHA pinned `97656f6cb2a08820f8682079c9b896cd58ca9df0f94150ae9bdbf8e4b6a13eee`. They are only **transport byte-identity** checks, not an approved SAVE contract. The script deliberately exercises **seven** release admission negative choices including none, both simultaneous, each individually with known/pending mutation RED, missing files.
- Actual original [B01-40 safe wrapper #536 @ `77d75d73467d236f6e3404caa58271ffec9d45cb`](https://github.com/C8LyNn/RoboRougelike/pull/536) and actual original [B01-60 native-ready corrected receiver #542 @ `ed0234d8a3b1b3bc85f3d0a07bbe44b065c96508`](https://github.com/C8LyNn/RoboRougelike/pull/542) are imported **unchanged** and called directly in external disposable temp folders. Each must **refuse** rollback of an unowned nested empty directory and refuse foreign/modified source (one extra negative each); after removal, a clean positive staging rollback must succeed. The original frozen #510 dangerous `package.py::rollback` and original #532 destructive rollback are **never used for live receivers**. This test does not alter or rebase #536 or #542, and no files touched in Codex's actual current workspace.
- The original secure gameplay archive G0 [#521](https://github.com/C8LyNn/RoboRougelike/pull/521) still `ACCEPTED_SOURCE_ONLY`. **Provisional SAVE_SOURCE_PHYSICAL_G0** can only verify individual unchanged source archives; **SAVE_CONTRACT_ADMISSION=HOLD** (no selected corrected contract yet), **SAVE_LIVE_HOST=HOLD** (private #510 Rig/ledger inaccessible), **UNITY_G1=NOT_RUN**, **G2=HOLD**. Source #550/this W14 witness must be repaired by owning B01-23/62 under NEW version/SHA, then independent B01-61 QA; this W14 branch never repairs/edits their code.

## Reproduction

CI on Ubuntu 24.04 and Windows 2025 checks out **six pinned immutable trees** (ours, original #510, #544, #549, #536, #542). Requires Python 3.12, .NET SDK 8.0. In the common parent:

```bash
python own/Tools/B01/B01-40/P17W14SaveReceiverGate/intake.py \
  --game game --save544 save544 --save549 save549 \
  --wrapper536 wrapper536 --wrapper542 wrapper542 \
  --project own/Tools/B01/B01-40/P17W14SaveReceiverGate \
  --out dist

dotnet build own/Tools/B01/B01-40/P17W14SaveReceiverGate/Original544BoundaryQA.csproj -c Release --warnaserror

dotnet run --no-build -c Release --project own/Tools/B01/B01-40/P17W14SaveReceiverGate/Original544BoundaryQA.csproj
```

`intake.py` copies **actual original bytes** from exact `git show HEAD:path` of producer source to nontracked generated `Input` path before C# compilation. Its JSON evidence contains both actual original physical hashes, 7 expected release denials, actual #536/#542 rollback results. Native Unity test suite is **NOT** invoked here: B01-60 must run existing [#532](https://github.com/C8LyNn/RoboRougelike/pull/532) on legally activated real Unity 6000.3.14f1 with true NUnit XML/Editor.log or report `G1_NOT_RUN`. This program's physical rollback source test does not validate arbitrary malicious filesystem TOCTOU races. Wrapper #542 uses validated file+directory census followed by recursive delete, so this test proves isolated absence of alien directories at audit time, not race-free global deletion.

## Hard release/migration rule

**Deny both Save options** until B01-23/62 explicitly select and correct one immutable schema version, including API immutability and no ledger escape, and independent B01-61 exact-SHA C# GREEN. Do not silently pair new Save schema with #510's private live state; no authorized capture/rehydration/atomic scene swap currently exists. Even if corrected source Save G0 becomes accepted, actual game save load and migration are unverified. Never merge directly into Codex's canonical project; B01-00 decides #79 handoff, explicit Codex ACK still needed. **Source verification is not OFFERED/ACK/INTEGRATED.**
