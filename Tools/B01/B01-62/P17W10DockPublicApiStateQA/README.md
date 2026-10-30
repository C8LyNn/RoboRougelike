# B01-62 / PASS17 WAVE10 — DOCK-PUBLIC-API-STATE-QA-001 R1

**Owned scope:** this directory and its dedicated Actions workflow only. Based on frozen B01-23 #471 exactly at `763748f1be0c61356fb4462144640bb359364226`; no B01-23 source, Codex, main/develop edits. Do not merge.

## Why this is different from #472 and #476

#472/#476 proved that original W07/W08 GUI bypassed W08 optional clearance. W10 checks the **new #471 W09 implementation itself**, including (a) both old behaviours disabled at arm and re-disabled in Update, (b) W09 all-layer + W08 double audit before demo reattach, (c) original public W07 `Reattach(int)` STILL callable outside that W09 GUI, (d) exactly when old W07 re-arms consumed stable part identity, and (e) fail-closed negative mutations of the new W09 host source. A positive CI test validates the **existence of the remaining external API blocker**, not gameplay safety.

## Reproduce

On the isolated W10 branch with Python 3.11+ and Git:

```bash
python -m unittest discover -s Tools/B01/B01-62/P17W10DockPublicApiStateQA -p 'test_gate.py' -v
mkdir -p /tmp/b0162-w10-receipt
python Tools/B01/B01-62/P17W10DockPublicApiStateQA/gate.py --repo . --receipt /tmp/b0162-w10-receipt/evidence.json
python Tools/B01/B01-62/P17W10DockPublicApiStateQA/gate.py --repo . --receipt /tmp/b0162-w10-receipt/release.json --release
```

The **release command MUST exit 3** for original #471; treat this as a real `HOLD_UNSAFE_PUBLIC_API` release verdict, not failed QA execution. Real original 6 Git blob identities are checked before any assertion. Evidence JSON is deterministic, write-once, UTF-8/LF. The GitHub workflow runs positive and 26 negative/positive tests on both Windows 2025 and Ubuntu 24.04 and uploads raw stdout and receipts.

## Exact original source invariants and receiver

W09 is a **source-level protected isolated demo UI**. A separate external script still can call the unmodified public W07 `DebrisRigPlayground.Reattach(int)`, which repositions Rigidbody and rearms the consumed ID without obtaining W09 `Inspect` clearance. W09 disarming the GUI does not imply that this public method became safe. W09 source `TryDock` does check permission first; it is **not accused of unguarded reattachment**.

**Required owner fix:** B01-23 must issue a **new immutable version**, without altering W06/W07/W08/W09 frozen commits, that enforces fail-closed actual collision/reach checks for **every entry point that can rearm the stable ID** (including public method or authoritative host adapter); enforce no state mutation for blocked, dead, kinematic or non-detached parts. Native Unity PhysX EditMode/PlayMode, script execution order and real persistent save/load are separately required. B01-62 #457 original ledger restart test remains evidence that there is no approved production save schema; W10 does not duplicate that persistence harness.

**Gate results:** independent W10 QA `SOURCE_EVIDENCE_PASS` if workflow green, `EXTERNAL_PUBLIC_API_HOLD` until producer fix, `NATIVE_UNITY_NOT_RUN`, `G1/G2_HOLD`, `CODEX_NOT_ACK`. Receiving roles: B01-23 source owner, B01-40 physical/GUID/asmdef gate, B01-60/61 independent licensed-native regression, B01-00 future #79 routing. No QA-only guard should be installed into live game.

**Rollback:** close draft and remove ONLY W10 QA paths from this branch. Original #421/#439/#455/#471 source is immutable. There is no Unity gameplay installation or save migration to undo.
