# B01-23 — P17/W18 — selected #596 Host Slot Lease R3 (source-only)

PACKAGE B01-23-UNITY-SAVE-HOST-BRIDGE-001 v3.0.0, NEW VERSION; original frozen #596 exact efb2b06eafedab5469b57f163152d96aca3c5915 remains UNMODIFIED and authoritative until independent exact-R3 G0 decision. #604 = ALT_HOLD, never import. Frozen #510 d94400d95d13db7f27e1533df05c9292d5380431 and #556 33094f154c7b91119b36d58aab285e6e83c5de57 unchanged. No Codex edits and no merge.

## W18 evidence-driven issue
Independent B01-41 PR #631 genuine original #596 source execution in Actions #41102371048 Windows and Ubuntu reproduced 17 assertions each that four authored native PlayMode tests share one global checkpoint even while using run IDs 31, 41 and 9. This is source-derived P0 fixture contamination; actual native Unity not executed. B01-61 #629 originally failed at harness C# compiler warnings, so its candidate stale-authority RED is not claimed independently proven by those failed jobs.

## New W18 fix (not altering frozen #596)
New Core/HostSlotLease.cs + Unity .meta GUID: retained FileStream(FileShare.None) owner lock at exact checkpoint path+.ownerlock, plus static HashSet for intra-process exclusion. Host holds lease until OnDestroy, including while SetActive(false); never deletes the production lock path (avoids Unix lock inode split). Second same-slot host Awake cannot obtain lease and remains inert HostBusy. Unity host now exposes ConfigureCheckpointSlotBeforeAwake(slot) for an INACTIVE GameObject (1-48 ASCII alphanumerics, underscore or dash), scoped to a fixed Application.persistentDataPath subfolder. Historical default sidecar path preserved without cleanup. Every original native PlayMode case rewritten for GUID-separated slot, real Destroy/yield cold restart and original R2 monotonic generation 4->5; fifth native case checks simultaneous same-slot exclusion. All genuine native Unity tests AUTHORED, NOT_RUN.

Source C#9 tests directly compile original frozen #596 transaction core, #556 binary codec, frozen #510 identity, W06 consumed ledger, and new HostSlotLease.cs. Tests use real OS files, 32 concurrent managed contenders, second distinct CLR process denial then successful cold reopen, disabled-host lease and slot isolation. Source CI does not compile MonoBehaviour using UnityEngine, and does not prove Unity/PhysX G1. Strict Git source ZIP includes 26 owner-only files + manifest; 11 Unity .meta GUIDs, 3 asmdefs, immutable #596, #510, #556 and 5 original Git blob pins; safe scratch rollback rejects foreign EMPTY dirs, symlinks and changed files.

## Local and CI commands
dotnet run --project Tools/B01/B01-23/P17W15UnitySaveBridge001/HostBridgeTests.csproj -c Release
python -m unittest discover -s Tools/B01/B01-23/P17W15UnitySaveBridge001 -p test_package.py -v
python Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py build --archive dist/B0123_UnitySaveHostR3_Original.zip

## Independent gates and blockers
B01-61/41: independent exact new SHA host public route, 32-writer, stale ID and slot rollback. B01-62: original #556 ID/owner/run/generation, and receiver 34 total unique GUIDs (#510 14, #556 9, #596 10, R3 +1). B01-40: formal new source G0 on new exact SHA, safe #575/#536/#542 disposable physical migration; no shared / alien EMPTY dir removal. B01-60: real licensed Unity6000.3.14f1 EditMode+PlayMode XML/Editor.log and PhysX or NOT_RUN. B01-00 only future authorized Codex integration, no phantom ACK.
Current isolated host has its OWN original W06 ledger, cannot hydrate original #510 private Rig[] / AtomicDockLedger and cannot prove cross-file power-loss atomic Save+sidecar. Native G1 NOT_RUN; canonical GAME_SAVELOAD_HOLD; G2_HOLD; CODEX_ACK_NONE; no merge. R3 cannot be co-installed with frozen R2 #596 or ALT #604.
