# B01-60 — PASS17 / W27: native four-case physical R2 receiver

**Gameplay outcome:** Verify that an interrupted world switch preserves the **current** velocity of a moving robot's Rigidbody (not the stale Save initial velocity). Tests target actual B01-23 author [#792](https://github.com/C8LyNn/RoboRougelike/pull/792) with four authored Unity PlayMode cases: original #510 physical AtomicDockLedger/Rigidbody, new live linear+angular velocity rollback, corrupted #556 snapshot no body/ID mutation, and blocked stage without physics quiescence.

**Source:** W27 only new B01-60 paths. Direct immutable parent [#783](https://github.com/C8LyNn/RoboRougelike/pull/783) exact 494e1056366c9004deefd7e4a73ad07b0a62a137. Frozen original #772 ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1. Owner-authored corrected #792 16cb2a7b5158605b15191b6074f9e392e0d1ebd3. Runner selects only two UPDATED original Unity C# blobs and pins all other original B01-23 Unity file content to immutable #772. Reuses unchanged original B01-60 #718 hardened ZIP/scratch/Unity/NUnit Editor.log/process return verifier. No changes to owner source, Codex W26 worktree, protected refs, Save or installed game.

## Windows operator (manual consent; no unattended Unity)
Requires trusted local W27 checkout with #792 original Git commit object already present, Git, Python3, Windows PowerShell 5.1/7. Safe SOURCE preflight never starts Unity:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W27Physical792Native\Invoke-B0160W27Physical.ps1 -RepoPath .
~~~

After independent B01-40/41/61 **G0 acceptance for exact #792**, and legitimate locally activated Unity Hub Editor 6000.3.14f1, owner may execute actual native:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W27Physical792Native\Invoke-B0160W27Physical.ps1 -RepoPath . -Run -SourceG0Approved
~~~

Prompts owner to type B0160 W27 PHYSICS NATIVE. Four individual original #792 PlayMode tests + four QA-only private save-scope NUnit guard tests; each case in distinct GUID-nonce scratch Unity project. If truly run, retain 8 XML NUnit files, 8 Editor.log, 8 process exit logs and exact SHA manifest locally. SHA256 self-receipt is unsigned, independent raw review mandatory. Never upload personal scratch paths without redaction. No auto-fetch, SSH, admin, uploads, merge, user-game install or foreign file cleanup.

## Source CI

~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W27Physical792Native -p test_rig_r2_native.py -v
~~~

Runs source-only original author Git SHA & 4-physical-project stage/rollback contracts on Windows and Ubuntu, plus actual Windows PowerShell preflight and blocking unsafe scratch, missing independent G0, missing Editor. This is NOT native Unity and NOT a synthetic UnityEngine shim test; the source is pinned to the real two author-edited #792 C# files. Actual native/editor testing needs owner PC and external evidence.

**Separate selected HQ #800 sources:** #787 one real OS Save owner, #797 one full Save with economy, #792 corrected physical R2, #780 V7 economic state, #750 ColdRestart R5. W27 does NOT install/co-run these five in a live Unity scene (composite G0 HOLD, live G1 NOT_RUN). Codex's scope ACK [#79](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6588669295) reserves Unity binder/garage/weapon and no W27 overlap is permitted. Do NOT resend old packages to #79. Prior R5 #750 seven-case route is separate [#760](https://github.com/C8LyNn/RoboRougelike/pull/760).

**Risk HIGH:** interrupted world replacement could destroy current velocity, physical part state or Save identities. Native pass requires genuine current body linear/angular velocity, old physical root/consumed ID retention after failed competing stage. SOURCE PASS never promotes a playable game. Six gates: IMPLEMENTED source; TESTED source (once actual CI observed); independent QA pending; READY/INTEGRATED/RELEASED no; G0 pending, G1 NOT_RUN, G2 HOLD. Receiver B01-40/41/61 and B01-00, original producer B01-23. Rollback discard only new six W27 files/branch, preserve Codex and owner data.
