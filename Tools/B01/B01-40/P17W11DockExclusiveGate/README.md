# B01-40 — PASS17 WAVE11 / exclusive reattach transaction intake

## Goal and owner boundary

This is a **new, independent, executable cross-producer integration compatibility gate**, not another physics policy implementation and not gameplay installation. It **does not choose on B01-23's behalf**, change W06/W07/W08/W09, overwrite the two producer branches, merge into main, or modify Codex-owned Assets.

The two *different alternative* producer packages are currently still:
- **#488 Secure V2** @ `33a32b8bd9ab40467ab1a17b5684926a37dbd4d3` — 27 owner-only new paths, replacement-only scene; physical original archive `e7ce7d735d81ceaf02d4eba9672d22555330806e7abdd0074be106faa1d6b068`, 72,216 bytes and 28 ZIP entries.
- **#494 Public API Guard R2** @ `6efe45961ed0ffdf183e8016e350978b9f3dc51c` — exactly 10 changes relative to immutable #471, including changed legacy W07 C# class under an explicitly new revision; original ZIP `55acf5f3176856f64ca66ba4449b165f528e8be2215d29a20618d7b3d38c1b80`, 15,948 bytes and 4 entries.
- Original frozen W07 `cdcaea750fda1a9d7db6018e34970f4b9f206427`, W09 `763748f1be0c61356fb4462144640bb359364226`, W06 `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. B01-62 independent exact-source #486 @ `24733ec01a345382beaca2b59170d51c85391573`, 9 original C# ledger/transaction tests and 14 new V2 source tests; QA green **for detecting the source defect**, NOT owner safety pass. B01-61 #491 original V2 public route tests green **for routing**, not atomicity. Legacy #472/#476 remain RED historical evidence.

## Actual tested invariants

1. Exact immutable original Git producer HEAD for each variant, W06/W07/W09, original W06 C# Git blob and legacy W07 code SHA.
2. Exclusive scope of each producer version. No silent change to frozen W06; no unexpected paths/unauthorized gameplay edits.
3. Original producer **unchanged `package.py` actually runs**, producing genuine deterministic ZIP on Ubuntu and Windows; exact immutable physical SHA, bytes, member counts, per-source original Git bytes/SHA256; no fabricated source archive.
4. V2: every exposed `Reattach`, `TrySafeReattach`, `ReattachFromGui` aliases actual owner GuardedDockCommand, new GUI uses the same route, original Unity source calls all-layer overlap and penetration, state mutator remains private.
5. #494: direct public W07 `Reattach(int)` validates all-layer original Physics before mutations; original W07 public bypass still confirmed on frozen old SHA.
6. **Decisive independent transaction invariant:** examine true method bodies, not just filewide string occurrences. Both source variants still perform Rigidbody/Transform/Detached changes **before** `RearmAfterReattach`. Original W06 ledger's separate `ClearForNewRun()` erases the ID, so rearm can fail after mutating state. No original-version rollback is present. This is a SOURCE FAIL, even if V2 routing or #494 original managed Unity stub compilation passed.
7. Eight independent mutation/self-check assertions verify the inspector rejects GUI route bypass, public API bypass, physics-gate removal, layer-mask regression, and reproduces both original commit-before-rearm patterns.
8. **Three deliberate release admission negatives** run on both OS: `--choice v2 --release`, `--choice guard --release`, `--choice both --release` must exit **3 RED**, while read-only evidence mode reports success only for *correctly detecting the HOLD*. A CI green for this harness **does not** mean the game or either producer is safe.

## How to execute

Place six exact independent Git checkouts alongside each other: `gate/` (this B01-40 QA), `v2/`, `guard/`, `legacy/`, `w06/`, `w09/`. In the common parent directory:

```bash
python gate/Tools/B01/B01-40/P17W11DockExclusiveGate/gate.py \
 --v2 v2 --guard guard --legacy legacy --w06 w06 --w09 w09 \
 --choice none --out dist
```

For actual release denial proof, append `--choice v2 --release` (or `guard`, or `both`), expect **exit code 3**. The fixed SHA source and original archive binary checks require original Git checkouts. GitHub Actions runs both Ubuntu 24.04 and Windows 2025, publishes machine-readable exact SHA/manifest/scope/risk evidence and original logs. Native Unity is **NOT_RUN**.

## Acceptance and repair contract to B01-23

**Choose precisely one producer path**, then publish a **new immutable version/SHA** that: (a) denies every public and GUI entrance if world-space collision, identity/ledger, geometry, or reach invalid; (b) guarantees that a missing/cleared consumed ID causes no physical Rigidbody/Transform/detachment/ledger partial mutation or a genuinely verified full rollback; (c) has original *executed* C# positive/negative test for `Detach → ClearForNewRun while detached → each API → no mutation`; (d) explicitly forbids coexistence with the competing alternative/legacy W07 receiver path; (e) includes exact scoped manifest, physical ZIP, source owner GUID/asmdef and byte-strict staging/rollback. A source preflight must not create a check-then-rearm race; a one-shot authoritative commit or robust rollback is needed.

After B01-23 version+SHA and independent B01-60/61/62 same-source retest, B01-40 must **create a new exact-SHA receiver**, not rewrite frozen producer #488/#494, and rerun independent source/GUID/asmdef/physical ZIP/receiver migration. **G0_TRANSPORT_ACCEPTED does not imply G0_SAFETY_ACCEPTED or native G1**. Unity 6000.3.14f1 / Rigidbody/PhysX/PlayMode/Player tests **NOT_RUN** here, G2 HOLD, CODEX_ACK NONE, NOT_INTEGRATED. No authorized merge or offer.

Rollback: this QA stays wholly under `Tools/B01/B01-40/P17W11DockExclusiveGate/**` and own workflow; runtime inputs are read-only checkouts and original ZIPs built in ephemeral folders; abandoning only drops this Draft PR. A later integration installer must be new and refuse existing target paths/unknown SHA; old W09 producer packager's original W07 SHA mismatch is an **expected fail-closed compatibility warning**, not a reason to weaken its frozen provenance check. B01-00 owns #500/#79 queue decisions.
