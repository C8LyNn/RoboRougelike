# B01-50 / PASS 17 WAVE 15 — ZIPREADBENCH-001 R1

## One NEW profiling function, not a duplicate source/ZIP size audit

`zipreadbench.py` measures observed **wall-clock read + decompression + streaming SHA256 time per ZIP entry** for a *real Windows Unity Player archive*. It reports each entry's repeated observations (median & nearest-rank p95), slowest 15 entries, and total full-archive round wall times. Unlike prior B01-50 packages, **#540 validates ZIP size/integrity, #568 uses #540 to verify a real historical ZIP; this package adds actual measurements of read/decompression latency by physical entry**.

The benchmark refuses to start until:
1. The original UNCHANGED `#540@d7278f224faecb2aa694bcb93139ef338171e885` inspector source matches Git blob `f8acb5a17d1721aade5d78eb8a8ae33f7db8477d`.
2. Input archive matches user-declared SHA256.
3. The unchanged original `inspect` validates all real ZIP bytes, file layout, CRC, names, hashes and uncompressed budgets.

Each measured pass rehashes all inflated entries and compares with the original snapshot. Archive SHA is checked again after the final pass. No extracted files, Player execution, changes to ZIP, Unity project or shared directories. Existing evidence files are never overwritten.

## Actual historical build benchmark (not fabricated)
- Real historical `RoboRougelike_CX_Windows_x64.zip` from [Codex #310](https://github.com/C8LyNn/RoboRougelike/pull/310), physical SHA256 `a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442` verified.
- Linux / Python 3.13.5, local runner, three repeated read+decompress+SHA rounds on **211 real files**, **103,666,052 unpacked bytes**. Round wall times `470,499,781 ns`, `452,657,694 ns`, `437,785,404 ns`. Median **452.658 ms**.
- Slowest individual observed median: `UnityPlayer.dll` (36,340,136 bytes), **168.570 ms** including its SHA and Python overhead. See [compact 3-round evidence](evidence/historical_zip_readbench.json).
- Full unabridged local per-entry receipt: 211 entries, SHA256 `51375f2a37647d6e8783a35e0ad12292b1a199333a00bbde9cc822936ae74bfc`. The complete real archive/EXE is **not** committed to this PR; receiver can recreate receipt with the same source and archive. Observed wall times WILL differ between machines and even repeated runs.

**Interpretation limit:** this is **not Unity Player startup**, game scene load, FPS, CPU/GPU Profiler or native PhysX. Wall clock includes OS cache, ZIP access, Python overhead, decompression, SHA256 and I/O. n=3 per entry is descriptive, not statistically reliable p95; no regression threshold is inferred. No game performance improvement or release readiness asserted.

## Reproduce (Python 3.11+)
Fetch exactly frozen original inspector source into temporary untracked own path; NEVER modify original PR #540:
```bash
git fetch --no-tags origin d7278f224faecb2aa694bcb93139ef338171e885
git show d7278f224faecb2aa694bcb93139ef338171e885:Tools/B01/B01-50/PLAYERFOOTPRINT001/playerfootprint.py > Tools/B01/B01-50/ZIPREADBENCH001/original_playerfootprint.py
git hash-object --no-filters Tools/B01/B01-50/ZIPREADBENCH001/original_playerfootprint.py
python3 Tools/B01/B01-50/ZIPREADBENCH001/zipreadbench.py \
  --frozen-source Tools/B01/B01-50/ZIPREADBENCH001/original_playerfootprint.py \
  --archive /absolute/path/RoboRougelike_CX_Windows_x64.zip \
  --expected-sha256 a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442 \
  --repeats 3 \
  --out /tmp/b0150-new-benchmark.json
python3 -m unittest discover -s Tools/B01/B01-50/ZIPREADBENCH001/tests -v
```
Source Git blob must print `f8acb5a17d1721aade5d78eb8a8ae33f7db8477d`. Only the original W13 source is temp-fetched; it MUST NOT be committed as part of this PR. Different build archives require their own independently verified SHA and matching provenance. `repeats` must be 2–8. Exit code 0 means only ZIP-read-bench succeeded; 1 means blocked/failed.

## Ownership / rollback / receiving QA
- `PACKAGE_ID=B01-50-ZIPREADBENCH-001`, `VERSION=R1`, producer B01-50. Exclusive NEW scope: `Tools/B01/B01-50/ZIPREADBENCH001/**` and `.github/workflows/b01-50-w15-zipreadbench.yml`.
- Source files / workflow are pinned in `MANIFEST-GIT-BLOBS.tsv`; CI checks actual blob hashes on Windows and Ubuntu, fetches original #540 at exact SHA and executes 11 original positive/negative tests with **synthetic ZIP fixtures**. Real historical ZIP benchmark was executed on local physical input, NOT present in CI.
- All canonical `Assets/**`, Codex game and save/ID code, #510, #556, #540, #568 and `main/develop` remain untouched.
- Rollback = delete only this package directory and its dedicated workflow on disposable branch; does not remove foreign files, folders or Unity state.
- Receiver B01-40 for independent G0 source/provenance; B01-60 independent QA, B01-00 governance. `NATIVE_UNITY_G1=NOT_RUN`, `LIVE_SAVELOAD=NOT_TESTED`, `G2=HOLD`, `CODEX_ACK=NONE`, no merge.
