# B01 PACKAGE-AUTO-001 — standalone package verification and disposable-worktree import

**Status:** TOOL VALIDATED (offline), NOT A CODEX INTEGRATION OR NATIVE UNITY CERTIFICATION. **G2=HOLD**.

This is a Python 3.10+ standard-library-only, Windows/PowerShell compatible tool. It never imports into a normal game repository: `stage` and `rollback` accept only a **new detached Git worktree created and stamped by this tool**, with pinned HEAD. A dirty target aborts. There is no `--force` overwrite option. Real source and target worktrees remain untouched; `git worktree add --detach` updates worktree bookkeeping in Git only. Output is machine-readable JSON and non-zero exit on failure.

## Acquisition

Source package ZIPs are separate producer artifacts. Download MAPGEN R3.4 real **inner ZIP** `MAPGEN001_R3_4_FULL_SOURCE_AND_QA.zip` (not the GitHub Actions wrapper) from [run 40197716466, artifact 12600003307](https://github.com/C8LyNn/RoboRougelike/actions/runs/40197716466/artifacts/12600003307). The package hash is `57365e4f4f6f453f0d09366a53c6927ccf87e53df54b4d12bb086b7616f699cb`; exact **83,928 bytes**, **62 entries**, **61 registered SHA256 file hashes**, **22 unique Unity GUIDs**. This toolkit has separately rehashed actual archive bytes and does not claim Unity G1.

**Update 2026-10-25:** R3.4 **FAILED native compilation** in receiver Codex QA PR #236 (CS1061: Shader.GetTag). B01-20 published separate R3.4.1 [Draft PR #237](https://github.com/C8LyNn/RoboRougelike/pull/237) exact SHA `5be31039411f89f17654b1c332f8692883fa03cc`, correcting the PlayMode assertion to `Material.GetTag`. The independent frozen **new** archive [Actions 40213410197 artifact 12606462105](https://github.com/C8LyNn/RoboRougelike/actions/runs/40213410197/artifacts/12606462105) is **63 entries / 89,258 bytes / SHA256 `2337cb85c090f194a7e10474e73bf3b6aa69f86eeabc74b3204a59d3a60bc623`**, 62 registered file hashes and 22 GUIDs. Use **`configs/MAPGEN001_R341.json`** for this newest source revision, not the historical R3.4 config. **R3.4.1 native Unity G1 remains NOT_RUN, G2 HOLD.**

For CAMERA/BUILD/RAC use full **inner ZIPs** in [run 40183207415 artifact 12594982513](https://github.com/C8LyNn/RoboRougelike/actions/runs/40183207415/artifacts/12594982513), not only PR deltas. Config `configs/CAMERA001.json`, etc. embed independent 36-file SHA256/byte receipts from W9, because historical producer manifests store Git blobs/size rather than a complete SHA256 table. LEGENDARY001 current PR230 is a **.NET8 domain prototype**; its config intentionally blocks Unity `stage` until a separate vetted Unity adapter exists.

## Example PowerShell (entirely disposable; no live Codex modifications)

```powershell
$Root = "C:\Work\RoboRougelike"  # EXISTING local checkout; used READ ONLY except Git worktree metadata
$Target = "C:\Temp\B01-MAPGEN-AUDIT-$(Get-Date -Format yyyyMMddHHmmss)"  # MUST NOT EXIST
$Ref = "4c9236a4cdc27814ea2e7679872ec4a524150cc8"  # PUBLISHED OBJ210 COMPARISON ONLY, not current Codex ACK
$Py = "python"
$Tool = "Tools\B01\PACKAGE-AUTO-001\package_auto.py"
$Config = "Tools\B01\PACKAGE-AUTO-001\configs\MAPGEN001.json"
$Package = "C:\Downloads\MAPGEN001_R3_4_FULL_SOURCE_AND_QA.zip"
& $Py $Tool verify --config $Config --zip $Package
& $Py $Tool sandbox --repo $Root --target $Target --source-ref $Ref
& $Py $Tool preflight --config $Config --zip $Package --target $Target --expected-head $Ref --report C:\Temp\B01-MAPGEN-PREFLIGHT.json
# Only if preflight has dry_run_ok:true; still no integration in running game:
& $Py $Tool stage --config $Config --zip $Package --target $Target --expected-head $Ref
# The stage output includes the .json transaction journal path. Roll back using EXACT journal:
& $Py $Tool rollback --target $Target --journal "C:\Temp\B01-MAPGEN-AUDIT-...\.b01-package-auto\<transaction>.json"
# AFTER confirming copied QA logs are stored elsewhere, ordinary operator may remove disposable worktree:
# git -C $Root worktree remove --force $Target  # NEVER on active repo
```

Alternatively run `run-package-auto.ps1` with the same commands/options. Use `--fault-after 4` ONLY for negative QA injection inside a disposable test sandbox. The fault triggers automatic rollback, and a durable JSON journal supports manual retry after hard process interruption. `rollback` refuses to delete any file changed since staging.

## Validation policy

- Check entire ZIP SHA256, byte length, count, CRC, safe paths, no ZIP symlinks/duplicates and bounded total expansion; match all per-file SHA256 and byte lengths; validate SHA1 Git blobs when published.
- For Unity: `.meta` pairing for assets and package directories, unique GUIDs, parse asmdefs and package self-references, verify retained MAPGEN URP Resources shader plus runtime `Resources.Load<Shader>` use. All these are **static** checks, not graphics/shader rendering or PhysX.
- Check target exact Git HEAD/marker/clean status, Unity 6000.3.14f1 and required UPM packages. Traverse existing `.meta` to reject GUID collisions. A **different pre-existing shared parent** `.meta` is explicitly *preserved*, with source→destination GUID mapping recorded; only package-owned paths are staged, other paths forbidden. Any other path conflict aborts.
- Transactional `stage`: write intent to durable journal before each exclusive file creation, fsync, fail closed; on ordinary exception roll back newly created files and keep originals; on process kill, use rollback with the journal. Rollback verifies each staged SHA256 before any deletion.
- `preflight` and `verify` do not write to target. `stage` requires disposable sandbox; no direct live integration flag exists.

## Codex must still do manually

Codex/HQ must independently ACK the **actual live** host branch SHA and reserved path list; OBJ210 snapshot is only a historical comparison. Codex must supply small R3/R4/Pack run-selection + prefab/garage gameplay wiring, preserve host authoritative mechanics, and run native licensed Unity EditMode/PlayMode, Windows URP Player and end-to-end physics/visual tests. B01-60 independent native QA and B01-00 specific G2 are mandatory. CAMERA R1.1 functional QA #234 is **FAIL** even though compile and its 5+3 unit tests passed; MAPGEN R3.4 native is **NOT_RUN**. A source `PASS_STATIC` is never `READY_FOR_CODEX`.

## Recorded real test evidence

Tests run with **actual downloaded inner ZIP** for MAPGEN R3.4 and all three W9 package ZIPs; 21/21 Python stdlib unittest PASS with negative SHA, tampered file, missing ZIP entry, missing `.meta`, duplicate GUID, dirty/wrong target, non-owned paths, symlink/path traversal, injected partial stage, rollback integrity, version/URP dependency mismatch. Exactly this code is exercised; see `evidence/test-results.json`, `evidence/source-receipt.json`, and CI workflow. The GitHub CI downloads pinned MAPGEN producer artifact before running these tests; if the artifact later expires, run manually with archived expected SHA or re-publish a still-pinned full archive without changing expected bytes.
