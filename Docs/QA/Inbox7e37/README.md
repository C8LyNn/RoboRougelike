# Native regression triage — HOLD

Source HEAD 7e37e1e08c56ef567f7037cf3b998e620d2cb934 (test fixture changes only after Windows build bc6919ee). PR #937.

The full EditMode run at df294b8e was 1308 passed, 82 failed and 1 skipped; it was not rerun at this HEAD. Full PlayMode was 478/478 at df294b8e; it was not rerun at this HEAD.

InboxRemainingTriage1: 23 targeted cases, 5 passed, 18 failed. InboxDriveTriage2: 20 targeted cases, 15 passed, 5 failed. InboxDriveTriage3: 10 targeted cases, 5 passed, 5 failed. These reflect sequential working states, not independent full suites or 35 unique passes. XML and Editor logs include all failures. Exit codes are preserved. Paths and session metadata are redacted; original hashes are recorded.

Native passing cases include heavy spawn/ground contact, physically driven tracked Light/Standard/Heavy with decreasing speed as mass rises, omni without wheels, retaining garage configuration and heavy physical boss victory. The latter uses normal virtual input and actual AI; it is not a human playtest.

Remaining failures: suspension lift after switching from hover, firing clearance on the garage input fixture, maximum-size 180-part construction (145 assembled in one run), and six-stage loot count discrepancy. Another six-stage physical pilot had not finished successfully. One B01-24 fixture still needs the producer to update a private-method signature. These failures need independent reproduction and diagnosis; no blanket root cause or production fix is claimed.

The downloadable bc6919ee Windows build is unchanged by the subsequent test-only commit. Player probes and build evidence are in the sibling InboxBC69 directory. Save backup before manually saving: %USERPROFILE%\AppData\LocalLow\DefaultCompany\RoboRougelike\Run\checkpoint.json. Do not promote this candidate as fully verified or merge without authorization.