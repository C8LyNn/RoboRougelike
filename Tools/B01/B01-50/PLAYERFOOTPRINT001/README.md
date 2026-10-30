# B01-50 — PLAYERFOOTPRINT-001/R1 (PASS17/WAVE13)

## What actually runs
Python 3.11+ **read-only post-build audit** of a Windows x64 Unity Player ZIP. It verifies the real ZIP's physical SHA256, CRC by completely reading each file, per-file SHA256, Windows-safe member paths, consistent names (case-fold collisions), symlink/special entry rejection, unpacked size and compression ratio limits, and the expected Windows Unity structure: RoboRougelike.exe, UnityPlayer.dll, RoboRougelike_Data/globalgamemanagers. It performs a **basic MZ header check only**, not PE verification or native execution. It can compare two verified audit JSON snapshots and fail on an unpacked-byte growth budget. Outputs cannot overwrite preexisting paths.

This is DIFFERENT from W12 ASSETFOOTPRINT #523, which inventories checked-out source Assets, not built Player payloads. No existing source, game, save, prefab, VFX, CI-04 or Unity build configuration is changed.

## Commands
    python Tools/B01/B01-50/PLAYERFOOTPRINT001/playerfootprint.py inspect --zip /path/to/real-windows-player.zip --out /tmp/receipt-a.json
    python Tools/B01/B01-50/PLAYERFOOTPRINT001/playerfootprint.py compare --before /tmp/receipt-a.json --after /tmp/receipt-b.json --out /tmp/delta.json --max-uncompressed-growth 1048576
    python -m unittest discover -s Tools/B01/B01-50/PLAYERFOOTPRINT001/tests -v

Exit status: 0 = audit/compare success; 1 = refusal, invalid ZIP or data, collision or unsafe structure; 2 = genuine uncompressed growth beyond user-given budget (diff JSON written). Do not feed untrusted user-controlled ZIPs without system memory/file size controls: maximum unpacked bytes, per-entry count and compression ratio are checked, but Python itself and header/central-directory parsing still consume resources.

## Native and evidence boundary
- Tests generate **synthetic ZIP fixtures**, including mock MZ stub bytes for file layout, and intentionally malicious ZIPs for negative controls. They do NOT produce a runnable Unity Player or assert frame time/build quality.
- To actually benchmark a build, pass a genuinely produced, pinned Windows Unity Player ZIP and record the job SHA/Unity version/platform alongside the JSON. Inspect that artifact, THEN compare identical build configurations. A source-only pass is NOT native Player build PASS, Edit/Play/PhysX PASS, or SAVE/LOAD acceptance.
- Default scan size caps: 100,000 files, 8 GiB total decompressed bytes, 4 GiB per entry, 1000:1 compression ratio. Explicit CLI custom total/count budgets may be stricter.
- The archive itself is only read; no extraction, mutation, install, migration, canonical Unity integration or silent cleanup occurs.
- No automatic deletions inferred from equal bytes or unexpectedly large files. ZIP contents are not executed.

## Ownership, rollback and receiver
PACKAGE_ID=B01-50-PLAYERFOOTPRINT-001; VERSION=R1; owner B01-50.
New and exclusive scope: Tools/B01/B01-50/PLAYERFOOTPRINT001/** and .github/workflows/b01-50-playerfootprint001.yml only.
- B01-23 #510@d94400d95d13db7f27e1533df05c9292d5380431 is untouched; #511/#494 HOLD. No Codex/gameplay, CHL/PARTSIZE or existing performance tool modifications.
- Rollback: remove only new owned tool folder/workflow on this disposable branch; does not touch Unity game assets or foreign directories.
- Receive/review: B01-40 for G0 physical/ownership review, B01-60 independent source QA, B01-00 ownership policy; Unity G1 NOT_RUN unless external authorized host yields original XML/logs, G2/INTEGRATED HOLD.
- CI runs the exact published Python and its tests on two OS, pins original Git blob IDs and publishes test/provenance logs. It cannot fake a Windows build.
