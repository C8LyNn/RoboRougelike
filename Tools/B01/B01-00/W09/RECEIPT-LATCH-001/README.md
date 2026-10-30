# B01-00 RECEIPT-LATCH-001 v1.0.0 / PASS17 W09

Offline Python 3.11 standard-library executable for B01-00 coordinating multi-agent package receipts. It **does not validate Github provenance online**; it checks whether the *supplied* immutable snapshot is internally consistent before B01-40/B01-60 and the authorized operator verify the referenced original links/source. Source-ready is a **candidate for external review**, never formal G0, G1, Codex ACK or integration.

## Working commands

```sh
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p 'test_*.py' -v
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/receipt_latch.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/example_hold.json
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/receipt_latch.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/example_hold.json --require-source-ready
```

The last command deliberately returns exit **2** because actual B01-40 and B01-60 independent reviews for the example remain PENDING. The example is a time-scoped factual W08 snapshot of [Draft PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427) with exact producer SHA and original CI, **not an authorization to offer the package to Codex**.

### Exact input contract

The JSON requires package ID/version, original producer and immutable lowercase 40-character SHA, exact PR ID/URL, original producer CI PASS with positive test count/zero failures, independent B01-40 intake and B01-60 QA receipts both bound to same source SHA and original comment URLs, explicit native-test requirement, optional native EditMode/PlayMode executed PASS on the same SHA, and separately ordered immutable OFFERED → explicit CODEX ACK → integration records. It will never equate sent, ACK, native PASS, source PASS or integration automatically.

The tool rejects inconsistent paths, roles, source hashes, missing test counts, invalid receipts, unavailable native gates, stage jumps, and overwritten output. Its `--output` mode atomically? **No:** write-once `open("x")` to a new file; this is local user-controlled storage, not a transactional, signed receipt. It never touches Github, Unity, main, develop, Codex code, or B01-40/60 authored artifacts. An output report includes input SHA-256 for evidence correlation.

### Rollback and limitations

Installation: copy only this isolated directory and run Python 3.11. Rollback: delete the copied tool and any self-generated non-canonical report files; **never** remove frozen handoff or QA receipts. No external writes or permanent side effects. GitHub links and actor names can be forged in local JSON; a result indicating consistent declared receipts is only advisory until reviewers verify the real GitHub objects/commits and provenance. Do not use this as an automated release gate without integrating authenticated source verification and authorized sign-off. Native Unity tests are NOT_RUN for this Python tool.

Receiver: **B01-40** checks source/GitHub receipts, **B01-60** independently reproduces tests and negative cases, **B01-00** uses decision output to avoid premature source offer. Formal source freeze/offer only by authorized independent process under [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), not by this CLI.
