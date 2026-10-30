# B01-60 / PASS 17 WAVE 16 — Source identity AFTER Unity Editor import (QA-owned native gate R5)

**HQ:** [#589](https://github.com/C8LyNn/RoboRougelike/issues/589), governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). Only frozen actual gameplay Secure Reattach [#510 `d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510). Only frozen versioned binary Save format [#556 `33094f154c7b91119b36d58aab285e6e83c5de57`](https://github.com/C8LyNn/RoboRougelike/pull/556). B01-23 is selecting precisely ONE independently corrected Unity Save/Load bridge [#585](https://github.com/C8LyNn/RoboRougelike/pull/585) vs [#587](https://github.com/C8LyNn/RoboRougelike/pull/587); **this QA-only change does not select either or import mixed candidates**, and does not alter game/Save producer/source/Codex. #511/#494, old Save #544/#549/#554 remain HOLD.

This is a NEW version of the existing original [B01-60 native receiver #532](https://github.com/C8LyNn/RoboRougelike/pull/532), stacked on exact frozen [W15 B01-60 QA #581 `95843dc3c5f1d899091749cf7208bf43858abc6a`](https://github.com/C8LyNn/RoboRougelike/pull/581). Previous W13 #542 fixed foreign empty directory rollback, W14 #567 fixed aggregate-only XML G1 evidence, W15 #581 fixed both EditMode/PlayMode execution validation. **Those original branches and SHA remain unchanged.** This W16 changes only TWO OWN B01-60 files (existing `runner.py` and existing W15 synthetic regression helper **on this new branch**) and adds THREE W16 tools/README/manifest plus ONE W16 CI workflow; no Unity test code rewrite and no duplicate runtime runner.

## New distinct native evidence vulnerability

The existing source preflight pins exact original Git bytes and packages them into a disposable Unity project. But after actual Unity import/EditMode/PlayMode, before `native-verdict.json` says `G1=PASS`, previous native runner did **not** check whether Unity/import scripts modified the staged `Assets/**` content, added new `.cs/.asmdef/.meta` or rewrote original `Packages/manifest.json` / `packages-lock.json` / `ProjectSettings/ProjectVersion.txt`. Thus test success might refer to **different code than original frozen #510**, or contain unselected injected scripts. This is a **proven source-provenance gate defect**, not a confirmed malicious Unity import nor an actual Unity/PhysX finding.

W16 adds `require_native_source_integrity(project,combined)` to the **same existing runner** after EACH actual Unity process finishes and before accepting its NUnit XML/Editor.log or emitting a G1 verdict. It checks byte-for-byte all original+QA-owned `Assets/**`, all `Assets` directories including EMPTY ones, refuses symlinks and unknown paths, and checks original project's exact dependency/Unity-version files. Excludes Unity-generated `Library/Logs/Temp` caches from byte contract (never treats those as authoritative source). Strict conservatism: unexpected Unity source/package auto-upgrade -> HOLD, even if NUnit passed. Does not attest external binary identity; all source-only tests remain non-native.

## Actually executable genuine old-versus-new regression (NO native Unity)

```sh
python Tools/B01/B01-60/P17W16NativeSourceIntegrity/test_source_integrity.py
```

This executes the frozen W15 original `runner.py` `main()` from Git exact blob `27293bdbbb112c0fbe1256a9c9708e2e2289cc3b`, **the SAME Python control flow** that would invoke the real Unity Editor. For reproducibility/safety the *subprocess + NUnit XML + Editor.log are visibly SYNTHETIC STUBS, not Unity*, while staging physically creates a disposable project and mutations use real filesystem writes/unlinks/mkdir. Cases: modified original source bytes, deleted source, injected C# file, foreign empty Assets directory, injected asmdef, modified manifest, changed Unity ProjectVersion, and added foreign .meta. Old frozen W15 `main()` wrongly returns synthetic `G1=PASS` despite tampering (expected RED). W16 `main()` must fail-closed for every case before success receipt, then show a control-flow-only synthetic positive for an unchanged owned source tree. Only W16 receipt under `qa-w16-native-source-integrity-evidence` is kept; all fake-native verdicts reside in temporary and are deleted. **No native Unity/PhysX is executed or claimed**.

The Windows2025+Ubuntu24.04 workflow also reruns the original W15 six EditMode corruption gate, W14 exact NUnit case-identity gate, W13 physically safe directory rollback gate, and the same original native #532 `--mode native` with a deliberately nonexistent Editor to demand exact exit code 3 + **NO real Unity XML/Editor.log**. Every Actions artifact is explicitly SOURCE_ONLY/NOT_NATIVE. Previous W15 test helper gets *only* an improved physical temporary staging simulation on this new QA branch to permit this test; W15 frozen original stays immutable.

## Real native G1 blockers, host boundaries, release

On this B01-60 execution environment `Unity` Editor executable and valid Unity 6000.3.14f1 activation **not available**; thus `G1_NOT_RUN` for the original #532 2 EditMode and 14 PlayMode real tests and also any new B01-23 #585/#587 physical host native cases. The G1 runner remains:

```sh
python Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py --mode native --unity "/absolute/path/authorized/Unity" --out "/absolute/new/evidence"
```

The selected adapter's **source G0 and bridge migration** belong to B01-40/62; its independent transaction/ledger/ID regression belongs to B01-61; B01-23/62 own corrections for #572/#582/#588. B01-60 does not assert that #587 is selected, that its tests were executed or that the #510 private `Rig[]` / `AtomicDockLedger` is hydrated. Full canonical Unity Save/Load remains `HOLD` until an authorized new host boundary, real process Save→cold Load and actual Unity import, without altering frozen #510/#556.

**Gates:** `#510_G0_ACCEPTED_SOURCE_ONLY`; `#556_G0_ACCEPTED_SOURCE_ONLY`; `B01-60_W16_OWN_RECEIVER_SOURCE_QA` only after actual per-OS CI; `REAL_UNITY_G1_NOT_RUN`; `HOST_SAVELOAD_HOLD`; `G2_HOLD`; `CODEX_ACK_NONE`; `NO_MERGE`.

**Rollback:** reject/revert the new W16 QA-version only (one runner, one W15 test helper, three W16 new files, own workflow). The original W15 #581, W14 #567, W13 #542, original native test package #532, frozen owners #510/#556 remain byte-unchanged. **Receiver:** B01-60 authorized activated Unity host; B01-40/62 new selected host receiver/migration; B01-61 original source transaction QA; B01-23 owner correction; B01-00 sole Codex inbox #79.
