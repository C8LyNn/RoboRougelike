# Codex/B01-60 — minimum receiver action after HQ scheduling (NOT SENT / QA-ONLY)

Maintain RC2 #258 production `46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f`, report `4f39b4487744f81b3e0075dabd15b67df2f07e25` and full operator provenance. **No integration or G2 requested here.**

1. In a read-only Windows local session, run `rc2_release_gate.py --zip <exact RC2 existing ZIP> --receipt <PR258 frozen release-receipt.json> --native <original Native/native-summary.json> --equivalence <production-equivalence.json> --report rc2-receiver-result.json`. Share result plus independent tool/script Git SHA with B01-40. No rebuild/overwrite/execution required. If archive not available, report PENDING_BINARY_ACCESS.
2. If HQ wants final single-snapshot full EditMode evidence, create an independent detached Unity6000.3.14f1 QA checkout at **editor fixture SHA** `f6184d2254c0ff4c59bcd1e9f21db6a134a54097`, rerun complete unfiltered EditMode suite and retain raw NUnit XML/Editor.log/native process exit. The previous 2/2 fix does not certify unfiltered 1068/1068.
3. Separate manual Windows UI/keyboard/mouse test and UI client shutdown (known C0000005) against the same physical EXE; preserve BASE comparison and logs. Do not claim `G1_INDEPENDENT` from agent B01-40 scripts.
4. B01-23 owns BUILD contact-area mathematical fix; await new R1.4 exact SHA and native acceptance. B01-20 owns CAMERA differential; B01-60 owns independent QA. Do not edit their protected files.

G2 HOLD until B01-00 explicitly approves each package.
