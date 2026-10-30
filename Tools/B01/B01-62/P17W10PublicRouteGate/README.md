# B01-62 / PASS17 WAVE10 — PUBLIC-API-REATTACH-ROUTE-QA R1

## Independent source QA (not an implementation fix)

This B01-62-only QA delta is **stacked on the immutable #476 QA branch**, not any B01-23 or Codex source. It adds no Unity Assets and does not modify frozen producer files.

**Original source pins**: W07 #439 @ `cdcaea750fda1a9d7db6018e34970f4b9f206427`; W08 #455 @ `99dfbd25d471f9983f9472b12d3725f4c002f7e9`; W09 #471 @ `763748f1be0c61356fb4462144640bb359364226`. Five original C# files are verified against their Git blob IDs.

## Source finding: public reattach remains unguarded

Original W07 `DebrisRigPlayground.Reattach(int)` is **public**, performs no collision clearance checks, yet mutates Rigidbody state, part transform, `Detached` and the stable-ID ledger through `RearmAfterReattach`. W08's `TrySafeReattach` and W09's `TryDock` are guarded opt-in routes. W09 `Arm/Update` disable old GUI behaviours, but `MonoBehaviour.enabled = false` does not restrict other scripts from calling the original **public method**. The W09 installer only adds the new demo component. Thus W09's GUI hardening is not an API-level fail-closed security/physics boundary. No claim is made that its protected UI path is broken.

**Expected gate**: `API_BYPASS_HOLD`. Successful execution of source QA **reproduces a release-blocking finding**, not green end-to-end reattachment.

## New W10 contribution

`route_gate.py` checks five pinned original Git blobs; W07 body/identity mutation and legacy GUI call; W08 guard; W09 guarded entry, Inspect + full-layer PhysX calls, mask policy, UI disable and optional installer. Changes to any assumed source contract fail closed and require manual review. Reuses only the previously committed B01-62 #476 parser, without modifying its source.

`test_route_gate.py`: **21 adversarial scanner controls** including guard deletion, control after mutation, source or identity-ledger drift, missing PhysX penetration, narrowed layer mask, fake comments and strings, installer drift and forged blob. Unit tests use synthetic method-shape fixtures; the workflow **separately scans actual original Git source** and produces deterministic receipts on Ubuntu 24.04 and Windows 2025.

## Reproduce / rollback

With Python 3.12 and full Git source:

    git fetch --no-tags origin cdcaea750fda1a9d7db6018e34970f4b9f206427
    git fetch --no-tags origin 99dfbd25d471f9983f9472b12d3725f4c002f7e9
    git fetch --no-tags origin 763748f1be0c61356fb4462144640bb359364226
    python -m unittest discover -s Tools/B01/B01-62/P17W10PublicRouteGate -p test_route_gate.py -v
    mkdir -p qa-receipt
    python Tools/B01/B01-62/P17W10PublicRouteGate/route_gate.py --repo . --output qa-receipt/receipt.json

Receipt output must not already exist. Rollback: drop only the W10 QA folder and workflow from this isolated branch; leave frozen #476 and B01-23 untouched.

## Receiver and release gate

- **B01-23**: owner-controlled new-version guard at the actual public reattach mutation boundary, or an authoritative host contract covering **every** entry path; no edits to frozen W07/W08/W09 or Codex interfaces without explicit ownership transfer.
- **B01-60/61**: independent negative tests invoking the public method with an obstructed target and verifying zero body, parent, detached/ledger/save mutation on refusal; licensed Unity 6000.3.14f1 native G1 where possible.
- **B01-40**: verify SHA/source/manifest, scope and deterministic QA intake; **B01-00** controls #79 Codex queue.

**G0 SOURCE QA (upstream HOLD); G1 UNITY/PHYSX NOT_RUN; G2 HOLD; CODEX_ACK NO; INTEGRATED NO; NO MERGE.** This is source analysis, not a general Roslyn call graph or executed native PlayMode proof.
