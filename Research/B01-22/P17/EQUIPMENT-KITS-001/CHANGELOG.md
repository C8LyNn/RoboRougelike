# CHANGELOG

## 1.0.0 / PASS17
- Added 10 actual, distinct equipment kit datasets based on existing R3/R4 item identities and source prices.
- Added deterministic offline validator and quote preview, 26 executable tests, source manifest/rollback docs.
- No change to canonical Unity, MODEL_JOB, Codex branch or game reward loop.

## 1.0.1 / PASS17 recovery
- Dodano walidację cennika i enumów z rzeczywistych blobów oryginalnego Codex R3/R4, 7 negatywnych mutacji i CI dwusystemowe.
- Skorygowano manifest: Node 26/26 PASS na każdym OS w v1.0.0, Unity nadal NOT_RUN.
- Rewizja v1.0.0 zachowana pod poprzednim immutable SHA.

## 1.0.2 / PASS17 WAVE02
- Added actual manifest-hashed safe data staging CLI, 10 flat import-ready equipment offers, no canonical game changes.
- Added rollback transaction receipt and 18 source unit tests including tamper/no-clobber/symlink checks.
- Preserved old v1.0.0/v1.0.1 commits, retained dedicated Windows+Ubuntu workflow.
