# B01-13 — PASS 15 / W74–W77: offline participant-export intake

**Practical output:** executable source-to-evidence check and **persistent withdrawal protection for already exported CSV**, which the in-browser scorecard cannot alter after download. Research-only; **not game code**. This is distinct from PASS14 `validate_player_receipts.py`, which receives **Windows Player** reports and must not be confused with **participant-study** exports.

## Exact original source and allowed scope

- Original owned form (unmodified): `Research/B01-13/W58/HPT13-scoring.html`, **Git blob** `d9ebb3dfc54f8e3e7e2992949b1852275ffef2a0`.
- Related game source (source ID only): Codex #310 `231005c6d0351945a8ee6684f3856af118f0e94f`. Historical Windows Player build source `7299dcc5fd63ac042a0ffcd97b69d02fb8744649`; declared ZIP SHA256 `ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d`. No independent real Player run, Unity activation, ZIP preflight or genuine participant consent follows from these identifiers.
- Operator files all live under a **private, isolated directory**, not this public repository. Only code/template and **synthetic logs** are public.
- No third gameplay package, Codex integration, G1/G2 or actual human recruitment was authorized here.

## Input files and commands

The operator creates a private folder `/private/HPT13-study` (example path; Windows uses a normal absolute directory) with:

1. `results.csv` — **actual** CSV exported from the original Chromium/regular-browser scorecard. It may contain 5, 10, 15… rows (exactly five task rows per `pid+sid`).
2. `intake.json` — copy from `intake.template.json`, matching actual `mode`, `build_sha`, `zip_sha256`, `form_html_git_blob`, and **each** `pid/sid`. It is an **operator attestation**, not cryptographic proof of informed consent. For genuinely approved human runs, `mode=HUMAN_APPROVED` and independent private `hq_gate_reference`, `host_preflight_reference`, `facilitator_id`, and each session's `consent_state=APPROVED` plus `consent_reference` must identify independently stored, authentic approval/consent evidence. Never include participant name/email/recordings in these files.
3. `withdrawals.json` — produced automatically upon first withdrawal. A persistent **tombstone** stops an old CSV containing a withdrawn `pid` from passing even if it is later reimported. **Do not delete this ledger** while holding any copy of study CSV.

```bash
# In the dedicated B01-13 research branch checkout:
python3 Research/B01-13/W74/hpt13_intake.py verify \
  --root /private/HPT13-study \
  --source-html Research/B01-13/W58/HPT13-scoring.html

# For participant P01's withdrawal, explicitly approve rewriting ONLY the LOCAL CSV:
python3 Research/B01-13/W74/hpt13_intake.py revoke \
  --root /private/HPT13-study \
  --source-html Research/B01-13/W58/HPT13-scoring.html \
  --pid P01 --confirm-local-export-rewrite

# Verify clean post-withdrawal evidence:
python3 Research/B01-13/W74/hpt13_intake.py verify \
  --root /private/HPT13-study \
  --source-html Research/B01-13/W58/HPT13-scoring.html
```

Exit `0`: structurally accepted local inputs / local rewrite. Exit `2`: **FAIL_CLOSED**; no successful participant study can be inferred. `verify` returns `STRUCTURAL_PASS_NOT_HUMAN_OR_GAMEPLAY_PROOF` even for correctly formed `HUMAN_APPROVED` because references cannot establish their authenticity. It strictly checks original HTML Git blob, product/ZIP SHA fields, CSV 16-column schema, 5 ordered unique tasks per session, allowed metrics, single mode/version, operator ledger, and whether any participant is withdrawn. The original browser consent checkboxes are **not** independent proof.

`revoke` writes a durable pseudonym tombstone **before** editing `results.csv`; then atomically replaces only the local CSV and removes the matching pseudonym's entries from `intake.json`. If interrupted between writes, the tombstone causes the next `verify` to fail closed; running `revoke` again completes the operation. Repeating is idempotent. Remaining session IDs and rows are retained. If all participants withdraw, the CSV retains just its header; `verify` reports zero active sessions, **not** a completed human study.

**Privacy limitation:** Atomic rewrite does **NOT** guarantee secure erasure of prior disk blocks, temporary data, backups, emails, shared folders, cloud sync, copies or previous attachments. The operator must locate and remove those separately under the HQ-approved data policy. The public GitHub issues must contain **no human participant data or consent forms**. Never use this tool to recruit, validate a genuine consent signature, or activate a Unity workstation.

## Executed original browser → file → local revocation proof

Run from the checkout:

```bash
python3 Research/B01-13/W74/test_e2e_browser.py
```

Requires Python 3.11+, `playwright` and headless Chromium. Checks **original** unchanged HTML source bytes at the pinned Git blob (not a JS mirror), launches real Chromium with **synthetic P01/P02**, saves five task rows for each, downloads actual CSV through browser UI, verifies source-to-evidence, triggers actual HTML `Wycofaj dane uczestnika`, confirms old CSV **still contains P01 (RED)**, records durable withdrawal, tests local CLI rewrite, and rechecks only P02 remains (GREEN). Negative checks: replay of withdrawn CSV, tampered HTML, wrong game SHA, fake external HQ refs, synthetic/HUMAN mode mixing, malformed CSV, aborted operator approval and malformed input. All human-consent flags in this automated suite are **synthetic**; no human participants involved. Native Player and Unity tests **NOT_RUN**.

`E2E_GREEN.log` is a synthetic test log, not gameplay evidence.
