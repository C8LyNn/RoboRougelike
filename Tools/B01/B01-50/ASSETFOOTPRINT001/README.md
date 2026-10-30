# B01-50 — ASSETFOOTPRINT-001/R1 (PASS17/WAVE12)

## Actual feature
Read-only, streaming SHA256 inventory of **physical checked-out source files in `Assets/`**. Produces sorted source-byte totals by extension, the top 20 largest sources, SHA256-confirmed duplicate groups, and a strict two-snapshot diff (added/removed/content changed; same-size changes are recognized). Can fail CI on an explicit **source-byte** growth budget. Refuses path traversal in manifest input, symlinks, inconsistent/tampered summaries, duplicate paths, and overwriting any report. Never writes into or modifies `Assets/`.

**This is NOT a Unity build size, Player payload, GPU/CPU, frame time, VRAM, imported texture memory, Unity Addressable output, or native Unity/PhysX measurement.** `*.meta` excluded from source asset payload; LFS pointer checkout would produce pointer bytes, not real binary sizes. This package does not activate Unity and does not alter the existing CI-04 pipeline.

## Usage (Python 3.11+)
```bash
python3 Tools/B01/B01-50/ASSETFOOTPRINT001/assetfootprint.py scan --assets Assets --out /tmp/b01-50-source-snapshot.json
python3 Tools/B01/B01-50/ASSETFOOTPRINT001/assetfootprint.py compare --base old.json --candidate new.json --out /tmp/b01-50-delta.json --max-growth-bytes 1048576
python3 -m unittest discover -s Tools/B01/B01-50/ASSETFOOTPRINT001/tests -v
sha256sum -c Tools/B01/B01-50/ASSETFOOTPRINT001/MANIFEST.sha256
```
Exit codes: 0 success; 1 invalid/missing input/report; 2 source-byte growth above optional threshold (**report still saved**). Output is exclusive-create; choose a new filename per run. All input files are hashed in streaming 1 MiB chunks. GitHub Actions exercises original source against checked-out **real repository Assets**, publishes a full report and logs as run artifacts. The local `unittest` suite uses small synthetic test fixtures solely to validate the verifier/CLI and fail-closed conditions.

## Scope, ownership, gates
- `PACKAGE_ID=B01-50-ASSETFOOTPRINT-001`, `VERSION=R1`; producer B01-50.
- Only new paths `Tools/B01/B01-50/ASSETFOOTPRINT001/**`, `.github/workflows/b01-50-assetfootprint001.yml`.
- Never edits canonical Unity gameplay, #510/#511/#494 secure docking, the owner `B01-23`, frozen visual pool #512, or Codex files. No migration/merge or auto-install.
- G0 source checks are independent of G1; even successful source CI **does not** imply Unity EditMode/PlayMode, Player build or performance PASS.
- Intake: B01-40/B01-60 read-only review, B01-00 release policy, Codex integration only after explicit acknowledgment.
- Revert/rollback: delete only this new tool folder and dedicated workflow on disposable branch. Do not touch existing `Assets/`, CI-04 or scenes.

## Limitations
- Duplicate hashes indicate equal *source bytes*, not guaranteed removable redundant assets; GUID/import references may require both copies.
- Source-byte changes are not linear estimates of compiled build byte or runtime resource usage.
- `scan` refuses symlinked inputs instead of following them; a purposely malformed tree reports error and creates no receipt.
- For a stable comparison pin the exact same Git checkout scope and scanner version for both snapshots; the JSON currently records input classification but not Git SHA. The caller must record tested commit SHA beside the artifact.
