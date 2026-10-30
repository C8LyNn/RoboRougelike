# B01-00 W09 — RECEIPT-LATCH-001 producer evidence (RED→GREEN)

This is an executed **offline Python** source package; no Unity native test or independent QA claim is made. Only named exact GitHub Actions source runs are counted.

## RED 1 — actual GitHub URL shapes

- PR #468 source `d0b48ea6f984f65e978b03af8d1d71edf0a08c71`.
- [Actions #41053421464](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053421464): Windows 2025 `Ran 40 tests`, `FAILED (failures=3, errors=29)`; Ubuntu 24.04 `Ran 40 tests`, same `3 FAIL, 29 ERROR`.
- Genuine defect: the original parser rejected even valid `https://` URLs due to a global `//` check, and rejected real `#issuecomment-ID` comment anchors.
- Corrected in `da955cfa0d861f143987b6a6136f57b42bafd4f6` without modifying unrelated source.

## GREEN 1 — corrected URL parser

- [Actions #41053561030](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053561030): Windows 2025 40/40 PASS, Ubuntu 24.04 40/40 PASS.

## RED 2 — independent receipt replay

- New independent 4-test source `e738393b892aae8feddb45ab2830ff508355190e`.
- [Actions #41053683730](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053683730): Windows 2025 and Ubuntu 24.04 `Ran 44 tests`, `FAILED (failures=2)` on each.
- Actual defects: the same GitHub comment could be counted for *both* independent QA roles, and an `OFFERED` comment could be reused to simulate a Codex ACK.

## GREEN 2 — independently distinct receipt URLs

- Corrected source SHA `8d8e7bea50d7ce7f5d26e86ac9f38fd9de064157`; exact test-fixture correction SHA `d40870a4b654d6d746a5c18c5a10350f755fa62e`.
- [Actions #41053802245](https://github.com/C8LyNn/RoboRougelike/actions/runs/41053802245): Windows 2025 job #124056150387 **44/44 PASS**, Ubuntu 24.04 job #124056149965 **44/44 PASS**.
- Additional independent confirmation of final manifest/README commit is to be obtained via the exact final HEAD run; do not infer success from this earlier SHA if HEAD changes.

## Quality boundaries

- The tool checks only **internal consistency of supplied JSON**. It does *not* fetch/authenticate any GitHub issue comment, actor, commit, or job. A SHA-shaped identifier or correctly formatted URL alone is not proof of an actual event.
- No actual Codex offer or ACK is sent. No imported game package, Unity editor, Player or native test is executed.
- B01-40 and B01-60 independent QA on this new producer source are **PENDING**. `PRODUCER_SOURCE_TESTS_PASS` is not `SOURCE_G0_INDEPENDENT_PASS` and never `G2/CODEX_READY`.
