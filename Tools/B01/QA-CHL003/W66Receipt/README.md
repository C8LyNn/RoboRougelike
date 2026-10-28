# B01-20 CHL-003 W66 — versioned QA-only operator

**NOT Unity G1.** This is a newly reconstructed, source-pinned successor to the inaccessible W30 QA-only ZIP, **not** a byte-identical edit of historical script SHA256 `47625bf0092f0e9b6c4fa4ad79f2db746a12ad8e7c34272c37bf48ba47093f0c`. It does **not** change CHL game-source PR #285. Independent B01-40 validation of this QA script remains required.

Immutable producer SOURCE SHA: `c7b2a234a286089383503da41883d624fe650551` (Draft PR #285); verified independently by B01-40 G0 on source-only 61-member ZIP SHA256 `6fe99717b8a4ed3e9ee7b80ea0dabfad968a5768c55e7b0062e260186e5e27bb` (not the QA-only script ZIP). Unity required: `6000.3.14f1`.

This script hard-pins all **20 EditMode E01–E20 and 15 PlayMode P01–P15 exact names, classes, suites** by original authored C# and validates original manifests. E20 native wheel provenance and P15 actual PhysX, P07 six continuous Light3+Heavy3 are required **only if Unity is really executed**. Test CLI uses `-batchmode -runTests` (never `-quit`). It rejects wrong SHA, ordinary clones/attached worktrees, dirty/untracked/ignored project source, evidence inside source, wrong NUnit name/class/assembly, bool `false` exit code and missing/duplicate/zero-contact native trials. This is a validation contract, **not a security boundary against malicious HQ operators**.

## Reproducible source-only checks (do not require Unity)

```powershell
python test_chl003_w66_operator.py
python chl003_w66_operator.py --preflight-only --selftest
```

All test XML and trial CSV fixtures used in selftest are explicitly **synthetic**; the detached linked-Git-worktree fixtures are **real Git**. Test group is separate from producer's native Unity tests.

## Licensed operator instructions (HQ decision required first)

Download the two Python files from this exact QA-only branch into a **separate tools folder outside producer checkout**. Have B01-40 check original SHA/hash and selftests. Create a new registered detached, single-use linked worktree at the exact above CHL producer SHA, isolated from Codex. Create an evidence location outside that worktree (must not exist before executing). Do not copy scripts into the pristine source root.

```powershell
$src='C:\QA\CHL003-PR285-DETACHED'
$out='C:\QA-Output\CHL003-W66-Unique'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
python chl003_w66_operator.py --preflight-only --project $src --evidence $out

# The following command is FOR AUTHORIZED LICENSED HQ-ASSIGNED OPERATOR ONLY:
# python chl003_w66_operator.py --execute-native --project $src --evidence $out --unity $unity --hq-authorization-reference 'https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-<real-approved-comment-id>'

# For independently obtained raw evidence from same trusted operator:
# python chl003_w66_operator.py --verify-only --evidence $out
```

**G1 is not granted automatically.** The script, despite successful receipt validation, emits `SOURCE_RECEIPT_VALIDATED_REQUIRES_INDEPENDENT_B01_60`; B01-60 independently checks full Unity log, human/physical contact, original XML and raw CSV plus rollback and machine provenance. It does not inspect the original event CSV beyond mandatory six P07 summaries; this remains B01-60's responsibility. Any skipped, invalid, missing or duplicate result fails closed. HQ approval and legal license are outside technical selftests.

## Exact manual six-run physical thresholds

Every pair (`LIGHT`, attempt 1..3, actual mass 270kg) and (`HEAVY`, attempt 1..3, actual mass 1150kg) must pass: physically original course, `Completed`, 4 checkpoints in order, one completion, no fall, >4 physics samples, >35m uninterrupted physical movement, >0 collision/contact/ramp/bridge points, Heavy collider width > Light, real enabled solid ramp/bridge/barrier (barrier collision itself is optional if legitimately avoided). Additional E20/P08/P09/P14/P15 negatives are required by their native NUnit case names. No G2, no OFFERED, no CODEX_ACK or merge without B01-40 + B01-60 + HQ approval.
## PASS 15 — Git worktree containment and isolated CI source scope

The native operator must keep its output **disjoint from every registered Git worktree** of the producer's repository (primary checkout plus all linked detached siblings), not just the tested CHL checkout. `safe_output` resolves canonical paths, reads the real `git worktree list --porcelain` inventory and rejects evidence equal to, inside or ancestral to ANY registered root. An unrelated external folder with a similar lexical prefix remains legal. This is tested with **two real detached linked Git worktrees**, not a mocked list.

This QA-only source is deliberately outside `Tools/B01/CHL003/**`: the frozen CHL R1 `package_gate.py` packages every tracked path in that directory and rejects extra unmanifested files. The QA-only folder **must never be added to CHL R1's producer manifest or ZIP**, and producer #285 must remain unchanged. The original 20 EditMode/15 PlayMode and six Light/Heavy native trials remain **NOT_RUN** until separately licensed Unity and independent G1.
