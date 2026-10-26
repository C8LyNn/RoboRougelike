# RC2-RELEASE-GATE-001 — B01-40 / W13

Executable, read-only, **fail-closed receiver**, not a second game integrator. Python 3.10+, standard library only. This gate never executes an EXE, writes to Unity, checks out/cherry-picks a commit or mutates any git worktree. Do not send actual game ZIP to public GitHub without owner approval.

## Actual producer source ZIP validation (works before Unity)

`python source_intake.py BUILD_R13 /path/to/Actions-BUILD-wrapper.zip` and `python source_intake.py MAPGENWORLD_R2 /path/to/Actions-WORLD-wrapper.zip`. Each also accepts the intact producer INNER ZIP from `actions/download-artifact`. Required exact sources: BUILD R1.3 `be54b4a33360cbd1923ae5dacbc92dff8eaf586b`; WORLD R2 `5ce9d806d7fc7eb0ab2ef7b586626404c346174f`. Entire ZIP SHA256+length, all 55+36 file records, asmdef/GUID/meta, trusted source head and upstream dependency checked. No native Unity is run. B01-23's new R1.4 supersedes R1.3 for actual functional fix as soon as frozen; rerun this gate for its **new** archive, not reusing old hashes.

## RC2 actual Windows retail ZIP (requires file on Windows PC)

The original RC2 release ZIP is **not available in this B01-40 session**. Do not mark it verified from the published JSON/ZIP digest. On an isolated local Windows copy with user permission:

```powershell
python Tools/B01/RC2-RELEASE-GATE-001/rc2_release_gate.py `
  --zip "C:\Users\kubap\Desktop\Prototypy\Releases\B01_UNIFIED_RC2_20261026\RoboRougelike_B01_UNIFIED_RC2_Windows_x64.zip" `
  --receipt Docs/B01_RC2/release-receipt.json `
  --native Docs/B01_RC2/Native/native-summary.json `
  --equivalence Docs/B01_RC2/production-equivalence.json `
  --report rc2-receiver-result.json
```

All `Docs/B01_RC2` inputs MUST come from frozen GitHub PR258 HEAD `4f39b4487744f81b3e0075dabd15b67df2f07e25`, not a regenerated working tree. `--repo PATH_TO_FULL_GIT_REPO` additionally compares pinned immutable *Git objects* across RC1, producer R2 & KEEP3, RC2 original build, Editor-only correction and final report. These commit objects must already exist in the local repository; missing objects cause a SOURCE gate failure, not download/checkout.

The ZIP validator reads all archived file payloads in memory-bounded chunks and checks *every* file against the published hash/size receipt (not just ZIP CRC): exact 38,523,382-byte ZIP + SHA256 `7d35cd1bc823499a8855258e24ed0a0ff1d47e49bd694adb94c031ef8c946de6`, paths safe on Windows, no duplicate case-insensitive names, no symlinks, no unexpected files, exact managed assembly SHA `9250338fa8ff71d153875cea35678f992d1c94461ef4a6c10bfedd978c465910`. It **does not extract or launch** any file. On missing ZIP output is `PENDING_BINARY_ACCESS`. If a provided ZIP has incorrect bytes/hash, exit code is 2.

`--native/--equivalence` enforces that the ORIGINAL full EditMode cycle was **1066/1068 + two failures**, targeted corrected tests were 2/2 in *later* Editor-only revision, and full Play was 424/424. It refuses invented all-green full EditMode. **Final full Edit on the corrected Editor revision is NOT_RUN**. Manual OS UI/gameplay remains NOT_RUN and `C0000005` not cleared. This receipt is external Codex evidence, **not** B01-60 independent G1.

## Integration boundary / rollback

No install occurs here; rollback is simply removal of your own output report. Do not alter the RC1/RC2 source or active game. The existing PACKAGE-AUTO-001 handles only isolated package staging/rollback after a separately recorded target ACK. Any new RC2 candidate must get its own SHA and a new independent receiver gate. **G0_TARGET LIVE PENDING, G1 B01-60 PENDING, G2 HOLD.**

Test: `python -m unittest discover -s tests -v`. CI actually downloads pinned BUILD and WORLD producer packages and executes all positive and malicious ZIP/receipt/native contradiction tests on Windows and Linux; no licensed Unity is requested.
