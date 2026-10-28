# B01-23 / PASS16 — 44-case *blank* native Unity receipt

**Scope:** original machine-readable acceptance **intake template**, not a Unity session, a real measurement, a gameplay source patch, G1/G2, or code integration.

The [committed 44-case JSON](../../../..//Docs/B01/PARTSIZE001/P16_BLANK_NATIVE_44.json) contains one record per 11 original PARTSIZE kinds × yaw 0°,90°,180°,270°. Every physical number, real socket/collider identity, native XML/log SHA256 and Editor exit code is **null**; every `native_status` is `NOT_RUN`. This remains true even when the generator's own source tests pass.

## Immutable provenance

- Exact frozen original [PARTSIZE producer #286](https://github.com/C8LyNn/RoboRougelike/pull/286) `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`.
- Original `Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs` Git blob `6153d1758e800ec5c4210323610b061c06e8ea85`; the generator physically executes `git rev-parse COMMIT:path` and fetches the source with `git show`; a changed original source hash fails.
- Exact existing independently B01-40 accepted native QA operator [#298](https://github.com/C8LyNn/RoboRougelike/pull/298) `26634727ab2323a982bce893421c88c330be46e8`.
- Original registered source experiment and 11-kind/4-yaw acceptance [W58 sheet](https://github.com/C8LyNn/RoboRougelike/blob/26634727ab2323a982bce893421c88c330be46e8/Tools/B01/PARTSIZE001/W26Receipt/W58_11KINDS_4YAW_NATIVE_QA.md). The P16 form adds **explicit native evidence fields** to those old `NOT_RUN` slots; it does **not** remeasure prior source AABBs.

## Reproduce (real original Git and Python, no Unity)

```bash
git fetch --no-tags origin be2be0e4802bc3f450179901f9ae91c6d84bcb5b
python Tools/B01/PARTSIZE001/P16NativeReceipt/generate_blank_native_44.py --repo . --check
python Tools/B01/PARTSIZE001/P16NativeReceipt/test_blank_native_44.py
```

The 17 tests execute the real generator/validator, and reject fake `PASS`, invented zero penetration, fabricated NUnit hash or Editor exit, duplicate/omitted/swapped 44-case identities, changed 11-kind catalog, wrong Unity version or source SHA, false G1 approval, extra field and altered source logical dimensions.

Workflow `.github/workflows/b01-23-pass16-native-44-blank.yml` runs the above against original source on Windows 2025 and Ubuntu 24.04. No Unity Editor is started. The source test validates *blank receipt integrity*, not a PhysX test.

## Procedure for authorized G1 operator — not yet performed

1. B01-00/HQ must explicitly authorize a **separate legally licensed Unity 6000.3.14f1 Windows host**, original source-pinned **detached registered disposable** worktree and original #298 operator. Codex's active worktree cannot be reused. First execute original #298 secure `-PreflightOnly`, and preserve evidence **outside ALL registered worktrees**.
2. B01-60 then runs the original 17 EditMode + 11 PlayMode authored tests from pinned source #286 and separate real physical tests for the 44 yaw poses. Measure original `Renderer.bounds`, `Collider.bounds`, actual contact and signed `Physics.ComputePenetration`, independent neighbours and *typed* mount socket owner/face; include joint/moving sweeps where relevant.
3. Keep raw untouched NUnit XML, original Editor.log, process exit codes, screenshots and SHA256 under independent external evidence root; record immutable original source SHA, actual scene, orientation, collider counts and fail causes.
4. **Make a NEW receipt COPY** for actual evidence, not overwrite or amend this immutable blank source template. Any completed row is only evidence for independent **B01-60 review**, not automatic `G1_PASS`. The original #298 `check_receipt.py` verifies structural stage evidence separately.
5. B01-40 verifies physical source/receipt identity and rollback. G2/Codex installation requires HQ authorization and explicit Codex claim. At present `G1=NOT_RUN` and `G2=HOLD`.

## Known limitations

- The blank form does not assert integration with current Codex game nor that the original Unity demo is the R3 assembled host.
- Eleven selected kinds remain logically 1×1×1 by original PARTSIZE catalog; actual wheel, gun and other payload colliders can exceed that space. **Never infer full hull or trusted sockets from those logical dimensions**.
- The baseline validator intentionally rejects *any* filled measurement. It is a tamper-proof **source-only blank template checker**, not a validator of real filled G1 evidence; preserve raw external measured records and use independent QA.
- No new Unity source package, prefab, `.meta` or asmdef was created. Existing producer #286 and independently accepted toolchain #298 were untouched.

**Owner:** B01-23. **Physical QA:** B01-60, **receipt intake:** B01-40, **canonical integration:** Codex. **CODEX queue:** existing CV-10, no duplicate. **Source status:** isolated QA intake prototype, NOT OFFERED/G2.
