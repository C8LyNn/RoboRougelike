# Independent acceptance — B01-40 G0 / B01-60 source QA

**B01-40 G0:**
1. Verify exact Draft PR head and all SHA256/byte counts/Git blob hashes in `manifest.json`; inspect only W07 own `Research/B01-11/P17/W07-UXFEED-001/**` and dedicated workflow; no `Assets/**` or core Codex/CHL/PARTSIZE conflicts.
2. Confirm independence from UXHELP #394, UXFOCUS #424, hit VFX and score/reward/economy packages; no copy into game before B01-00/CodeX claim.
3. Inspect reproducible .NET8 source test, dependency-free source and rollback instructions.

**B01-60 independent QA:**
1. Checkout EXACT package SHA with independent evidence, run original 87-case C# test, capture independent CI/job/log and source SHA.
2. Inspect coalescing only for consecutive same actor and eligible kind, exact 750ms inclusive; priority critical in burst and critical-only; 3000/6500 TTL; history retention and bound.
3. Validate stale epoch and input order rejection; no gameplay mutation, no reward computation or arbitrary UI markup injection.
4. Confirm this is **not** a real Unity HUD or native GUI test. Future G1 requires an authorized Codex adapter and actual Unity Player evidence.

QA requests and producer CI are not an independent G0/G1 verdict. Do not set CODEX_CLAIMED or INTEGRATED until explicit receipts.

## W08 R2 mandatory independent receiver regression
Prior R1 is superseded by R2 for producer source acceptance (historic commit remains immutable). **B01-40** independently verify NEW exact R2 SHA, 9 payload hash/bytes/Git pins, no changed paths outside own source+dedicated workflow, bounded critical queue and rollback. **B01-60** independently rerun compiled original **95** cases and stress 32+ ordinary events within 6500 ms after part/gun loss; verify critical still visible while `History().Count` stays <=32, latest limited critical slots replace earliest, expiry/new-run reset, and normal-only flood never fabricates critical alerts. [W08 RED/GREEN](EVIDENCE/W08_RED_GREEN.md). Producer's GREEN is NOT independent QA; native Unity/Player remains NOT_RUN.

## W09 R3 source QA receiver — new lifecycle in same PR #438
**B01-40:** independently read final R3 source SHA and manifest including new W09 evidence; check code changes only in producer-owned source/test/doc + isolated workflow and proper bounded critical buffer. Check no Codex-owned UI input is modified, no merging or falsely claimed Unity gate.
**B01-60:** independently compile/run 112 original C# tests on EXACT final SHA. Verify `AcknowledgeCritical` accepts only a current unexpired **same-object** critical ticket, not a data-equal forged ticket; an older ticket never clears the newer same-subject alert; unaffected history, source input order, TTL, FIFO/slot bound and new-run isolation. Test null/normal/expired/stale tickets, concurrent source events, and false repeats. No actual player/UI input acknowledgments are implemented in Unity, so native G1 and human QA = NOT_RUN.
