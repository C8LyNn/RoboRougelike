# B01-60 / PASS17 W05 — actual independent acceptance of B01-22 EQUIPMENT-KITS v1.0.3

**Purpose:** Independent source receiver for newly fixed **real content** [B01-22 #411](https://github.com/C8LyNn/RoboRougelike/pull/411) at `a3dd45a4c1f222ec5cc387302eafe9104f91ab52`. Unlike previous research/creator tools, this is **read-only game-source acceptance**: no additional kit, economy design, garage adapter, copied Codex file, or game code change. Branch is stacked on the producer only to test its **exact unchanged source**. All PR diff files are B01-60-owned test/provenance paths.

## Distinct independent tests

- Execute actual original producer's unchanged `kit_core.test.mjs` (26 Node22 cases), `test_stage_kit_payload.py` (18 Python3.11 cases), `verify_host_source.py --selftest` (seven synthetic original R3/R4 price/enum mutations) **on both Windows2025 and Ubuntu24.04**. The source gate is the exact Codex gameplay `203a0805251ed28293888e43f2d49f0df63767c6`.
- **New independent oracle** `independent_acceptance.py` validates the full **original 13-file SHA256/bytes manifest** directly from Git objects, strict new producer seven-path scope, and byte-equal data for original frozen #333 10 kit BOMs, 17 part and 9 weapon reference prices. *Important:* the old #333 **package revision** was v1.0.2, while its `catalog.version` field was `1.0.0`; new #411 has package revision and catalog version `1.0.3`.
- Byte-for-byte original Codex R3 difference must be exactly **one 8-byte addition `partial `**, no other changes. Confirm exact latest R3 and R4 Git blobs from the Codex game and original new producer catalog.
- Actually run original producer's **unchanged staging API** in a fresh disposable directory: three real output files including 10 actual `offers.json` derived from original catalog, independently calculate **each original kit's total, quantities, line prices and references**, enforce no native game transaction, SHA receipt. Deliberately corrupt offer and receipt, inject foreign file and attempt no-clobber; every destructive rollback must **refuse** and preserve evidence. Restore and safely rollback then repeat to verify deterministic output. This is not another *producer* staging CLI.
- Capture machine-readable JSON independent receipt alongside true original author test logs in dedicated Actions artifact per OS; no fabricated native Unity results.

## Reproduce from the dedicated QA branch

Requires Python 3.11, Node 22 and Git. The original Codex/older owner Git objects must be available; the workflow fetches each immutable SHA.

```sh
node --test Research/B01-22/P17/EQUIPMENT-KITS-001/kit_core.test.mjs
python -m unittest discover -s Research/B01-22/P17/EQUIPMENT-KITS-001 -p test_stage_kit_payload.py -v
python Research/B01-22/P17/EQUIPMENT-KITS-001/verify_host_source.py --current-ref 203a0805251ed28293888e43f2d49f0df63767c6 --selftest
python Tools/B01/B01-60-P17W05-Equipment/independent_acceptance.py /tmp/b0160-equipment-source-qa.json
```

## Gate and handoff

**SOURCE_G0** is independent read-only data/price/rollback acceptance if final exact-head dual-OS CI green. **Not** `NATIVE_UNITY_G1`, `INTEGRATED`, `READY_FOR_CODEX`, `CODEX_SENT`, `CODEX_ACK` or a code/wallet transaction. Native Unity Editor/Play/Player is **NOT_RUN** due unverified authorized licensed host. B01-40 owns full source/G0 intake; B01-00 alone chooses a package for Codex inbox #79 and authorizes `OFFERED/SENT`. Codex alone adds optional garage stock/transaction adapter, runs real Unity and returns integration report. Frozen historical #333 remains unaccepted against latest R3; any approval here applies only to exact new #411 SHA.

**Rollback:** remove only this B01-60 Tools directory and its dedicated workflow. All original B01-22 files and Codex engine sources remain unchanged; no merge to main/develop.
