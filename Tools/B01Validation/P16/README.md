# B01-40 PASS16 — current Codex + independent CHL/PARTSIZE source intake

**Package:** `B01-40-P16-INTEGRATION-ADMISSION-001` — QA-only, **not** a gameplay source package or an approved integration.

## Primary executable source admission

Exact original sources: Codex PR #310 `589b60a912096d1e9917cb0bbac73a53e7543b4f`; frozen B01-20 CHL #285 `c7b2a234a286089383503da41883d624fe650551`; frozen B01-23 PARTSIZE #286 `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`; repaired B01-25 maps `691c8cb6b0be086d455e530f2531de7f3753a0ba`.

On a **disposable** checked-out B01-40 QA branch in a working directory with original Git objects:

```bash
git fetch --no-tags --depth=1 origin 589b60a912096d1e9917cb0bbac73a53e7543b4f
git fetch --no-tags --depth=1 origin c7b2a234a286089383503da41883d624fe650551
git fetch --no-tags --depth=1 origin be2be0e4802bc3f450179901f9ae91c6d84bcb5b
git fetch --no-tags --depth=1 origin 691c8cb6b0be086d455e530f2531de7f3753a0ba
python Tools/B01Validation/P16/test_current_codex_packages.py -v
python Tools/B01Validation/P16/admit_current_codex_packages.py --repo .
```

The gate actually reads SHA-pinned **original Git blobs** and rejects source-asset path collisions including Windows-case aliases, UUID/.meta collisions, missing owner assembly references or duplicate names, changed exact repaired owner map copies, invalid v2 schema/corners, changed current Codex 2000-per-layer and `MapStorage` explicit-overwrite contracts. It produces a machine-readable receipt. An optional `--out <new-file.json>` creates one receipt **outside** the source checkout; refuses overwrite. Test runner has source-based positives and labelled in-memory negative mutations. It is **not** a Unity compile/PhysX test and does **not** merge/modify canonical `Assets/**`.

## Separate Codex original native evidence intake

```bash
python Tools/B01Validation/P16/test_codex_native_evidence.py -v
python Tools/B01Validation/P16/verify_codex_native_evidence.py --repo .
```

Both read original raw `Docs/CXFinal/Evidence/` XML/process Git objects from Codex #310. They preserve **full EditMode 1288/1303 PASS / 15 FAIL / exit 2**, independent 15/15 targeted retest PASS, **full PlayMode 474/474 PASS**, with matching source SHA and process exit. This is **original evidence analysis, not a second Unity execution**. **No overall EditMode PASS** may be inferred until Codex actually reruns its complete test suite on the newer test-only commit. Read the genuine raw NUnit log and receipt, and keep the previous fail record.

## Codex handoff and independent gate

- **Owner/branch:** B01-40 only, existing Draft [#284](https://github.com/C8LyNn/RoboRougelike/pull/284), no producer edits and no unauthorized merge.
- **Codex inbox:** [#79](https://github.com/C8LyNn/RoboRougelike/issues/79); Codex exclusively owns actual integrated game and **must separately acknowledge any intake**. No new CV duplicate.
- **Independent QA:** B01-60 may consume these receipts as **source-only**, separately execute any licensed G1 following HQ authorization; HQ alone owns G2.
- **Known limits:** this test does not prove actual Unity importer/compiler/scene dependencies, physical wheel bounds, real six CHL traversals, 11×4 PART colliders, camera GUI, current Codex full suite regression, nor release/Player compatibility.

Owner SHA changes require **new exact source and receiver**. A green CI result here is `SOURCE_G0_COMPATIBILITY` only, not G1/G2/OFFERED.
