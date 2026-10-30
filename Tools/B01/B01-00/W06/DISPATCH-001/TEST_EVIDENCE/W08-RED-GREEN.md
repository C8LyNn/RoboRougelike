# W08 RED → GREEN / DISPATCH-001 v1.2.0

The original test suite gained **29 new** adversarial cross-platform portability tests in `test_portability.py` (original W06 22 + W07 14 + W08 29 = **65** cases). This tests real Python source only; no Unity claims.

## RED — original source with new tests
- Git commit `ce73208e7106e3ecdfbb8dbb5e64e4c179b1dcbf`, [run #41045653677](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045653677).
- Ubuntu 24.04: **65 total, 26 failures, 1 error**, overall job FAILED; Windows 2025 same **65 total, 26 failures, 1 error**, overall job FAILED. The remaining 38 tests passed.
- Illustrative detected defects: case-insensitive Codex reservation bypass, case-alias ownership overlap, duplicate mixed-case package/task/claim ID, nonportable Windows reserved devices (`CON`/`NUL`/`COM1`/`LPT9`), forbidden punctuation/control chars, trailing dot/space, Unicode normalization aliases.

## GREEN — corrected same test suite
- Git source fix commit `b6ec8a7303f954b21393ae59058e1f9e4b6bda52`, [run #41045838687](https://github.com/C8LyNn/RoboRougelike/actions/runs/41045838687).
- Windows 2025 job #124029583392: **65/65 PASS, 0 failed**, plan CLI success.
- Ubuntu 24.04 job #124029583914: **65/65 PASS, 0 failed**, plan CLI success.
- Specific patch logic: `NFKC + casefold` used for comparisons, literal path segment boundaries maintained; reject ambiguous NFKC representations, Windows reserved device stems/extensions, colon/pipe/angle brackets/quotes/control characters and trailing dot/space; case-insensitive duplicate task/package/claim IDs and stream names fail closed. No gameplay paths changed.
- Earlier temporary intermediate source commit `3c42b3c3ce1da6c09644d66a878da1a594c6b595` is **not the corrected code** and must not be staged or offered. The corrected file was rewritten from the previously tested W07 file, and structural source-count checks were applied.

## Gates
- Original-source Python tests: PASS 65/65 on each system.
- Independent B01-40/60 audit: PENDING; this log is producer evidence, not independent QA.
- Native Unity EditMode / PlayMode: NOT_RUN (this Python tool does not use Unity).
- No Codex ACK, no game integration, no branch merge.
