# B01-60 W15 — RC2 unfiltered vs targeted EditMode evidence gate

QA-ONLY; NO NEW UNITY RUN / G2 HOLD.

Pinned GitHub inputs: RC2 #258 report HEAD 4f39b4487744f81b3e0075dabd15b67df2f07e25.
Full original Unity source 46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f: native EditMode exit 2, original NUnit XML 1066 Passed, 2 Failed, 1068 total, no filter.
Changed Editor-test source f6184d2254c0ff4c59bcd1e9f21db6a134a54097: native targeted exit 0, original NUnit XML 2 Passed/2, exactly the same two full-case names.
Producer production-equivalence.json declares only two changed Editor fixture source files; this gate checks claim consistency but does not independently binary-prove assembly equivalence.

Independent B01-60 inspected the original raw 1068-case XML and 2-case XML, not just the summary.
Consequence: full unfiltered EditMode on the final Editor-test SHA is NOT_RUN. Neither split-SHA combined 1068/1068 PASS nor final G1 or G2 can be inferred.

Run from an isolated local copy of five ORIGINAL files taken at report HEAD:
python rc2_edit_retest_gate.py --full-xml ORIGINAL/EditMode.xml --full-process ORIGINAL/EditMode-process.json --retest-xml RETEST/EditMode.xml --retest-process RETEST/EditMode-process.json --equivalence production-equivalence.json --out evidence-verdict.json

Original repository paths:
- Docs/B01_RC2/Native/RC2-FinalRegression-20261026T115307Z/EditMode.xml
- Docs/B01_RC2/Native/RC2-FinalRegression-20261026T115307Z/EditMode-process.json
- Docs/B01_RC2/Native/RC2-LegacyFixturesRecheck-20261026T123558Z/EditMode.xml
- Docs/B01_RC2/Native/RC2-LegacyFixturesRecheck-20261026T123558Z/EditMode-process.json
- Docs/B01_RC2/production-equivalence.json

Result PASS_SPLIT_SHA_RECEIPT_CONSISTENCY means only receipt consistency; result full_editmode_final_sha=NOT_RUN remains immutable. Rejected: tampered NUnit counts, omitted/duplicated cases, mismatched retest names, wrong native exit/source, unfiltered run claim with test filter, unapproved production changes, false assembly identity. Returns nonzero on invalid evidence.

Portable synthetic tests: python -m unittest -v test_rc2_edit_retest_gate.py
Independent genuine Unity 6000.3.14f1 compile/Edit/Play/human/Windows startup: NOT_RUN (licensed runner unavailable).
Codex next QA-only action: an unfiltered EditMode suite on final Editor-fixture source SHA in a fresh licensed Unity checkout; retain original XML, Editor.log, exit and SHA; no production merge, no G2.
