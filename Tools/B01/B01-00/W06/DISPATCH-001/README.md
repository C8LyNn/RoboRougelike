# B01-00 DISPATCH-001 v1.1.0 / PASS17 WAVE07

An executable offline scheduler for B01 swarm task assignment. It detects overlapping repository paths, enforces frozen ownership, prevents duplicate package IDs, checks dependencies, blocks Codex-reserved scopes, limits active gameplay production streams, and creates atomic local claim files. Python 3.10+ standard library only. No Unity or game source is modified.

## Use

From this directory:

    python dispatch.py --snapshot snapshot_wave06.example.json --claims-dir local-claims plan
    python dispatch.py --snapshot snapshot_wave06.example.json --claims-dir local-claims claim W06-DISPATCH
    python dispatch.py --snapshot snapshot_wave06.example.json --claims-dir local-claims freeze W06-DISPATCH --expected-sha SHA256_FROM_ACTIVE_CLAIM
    python dispatch.py --snapshot snapshot_wave06.example.json --claims-dir local-claims release W06-DISPATCH --expected-sha SHA256_FROM_CLAIM
    python -m unittest discover -s . -p 'test_*.py' -v

The sample snapshot is NOT a live GitHub/Codex ownership ledger or an authorization to perform those tasks. B01-00 must populate a fresh snapshot based on verified GitHub state before actual assignments. No ACK, game integration or automated GitHub writes are inferred. The example contains two allowed streams (PARTSIZE, CHL), a safe support task, two rejected production candidates, and a conflicting support candidate.

Scope entries are literal repository-relative directory prefixes (never wildcard globs). Unknown Codex scope requires an explicit conservative reserved list. Selection is deterministic: priority descending, task ID ascending; earlier selected scopes count against later candidates. A task can be claimed only when NEW, its dependencies are DONE, the stream is approved and within the cap, and no live claim overlaps. Claims are serialized by an exclusive local directory guard and written with O_EXCL, fsync and file SHA256. The guard must be investigated if stale; it is never automatically removed.

## Safe rollback and limitations

The plan command does not write any file. To undo an ACTIVE claim, confirm ownership and run release with the exact SHA256 printed by claim. Modified files are not deleted. A matching hash never grants permission to release FROZEN or CODEX_CLAIMED claims. Freeze atomically changes an ACTIVE claim into FROZEN, issues a NEW SHA256 receipt and intentionally has no matching unfreeze or ownership-transfer operation. A real explicit Codex ACK or RETURNED_FOR_REWORK must be processed through owner governance, not simulated locally. The tool does not edit main/develop, canonical Assets, Unity, GitHub issues or external branches. Discard this isolated directory to uninstall. This cannot protect multiple workers using separate claims directories, inspect Codex unpushed changes, or substitute for B01-40/60 independent QA. Any network/shared-filesystem semantics require separate validation.

## Evidence / receiver

Historical W06 TEST_EVIDENCE/unittest.txt records 22 executed local unit tests, with zero failures and exit 0. W07 added 14 actual freeze/rollback regressions in test_freeze.py. See exact-head GitHub Actions for results of the expanded 36-case suite; the historical 22-case log is NOT evidence for new tests. The attached TEST_EVIDENCE/example-plan.json is the actual dry-run CLI output. The W07 revision manifest lists exact new source/evidence SHA256 and byte lengths; the PR HEAD is the authoritative commit SHA (not circularly embedded). The optional CI workflow runs tests on Windows and Ubuntu when triggered. Native Unity tests: NOT_RUN. Independent B01-40 and B01-60 QA: PENDING. CODEX ACK: not required for isolated planning tool; any gameplay handoff still follows #79 and #276.

Refs: https://github.com/C8LyNn/RoboRougelike/issues/416 , https://github.com/C8LyNn/RoboRougelike/issues/276 , https://github.com/C8LyNn/RoboRougelike/issues/79 .
