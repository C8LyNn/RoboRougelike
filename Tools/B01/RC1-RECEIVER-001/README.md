# B01-40 — RC1-RECEIVER-001 (read-only provenance gate)

**Scope:** independent reception of the published RC1 #248 source and CAMERA R1.2 #247, plus conservative classification of external native QA #249. It is NOT a game integrator or installer, does not require an activated Unity license, and cannot approve B01-00 G2.

## What is actually checked

- Downloads GitHub `git/trees?recursive=1` at **immutable** game `86eb7bb7`, baseline OBJ `4c9236a`, producer MAPGEN `5be3103` and MAPGENWORLD `de1ce56`. Requires the untruncated authoritative repository tree and direct equal Git blob content IDs on **all 58** package-owned files stated in RC1 source-integrity.json, not just the producer's textual assertion. Also requires six whole protected trees (Assets/R3, Assets/R6, Assets/Pack, scene, Packages, ProjectSettings) unchanged from OBJ. Records canonical `Assets/B01Packages.meta` Git blob identity. Does not confuse Git snapshot with private active Codex worktree, or infer native approval from source hashes.
- Downloads pinned CAMERA source artifact from exact producer Actions run `40226247928` / artifact `12610554389`. Verifies *physical inner* ZIP SHA256, size, CRC, exact path counts, producer 40/40 SHA256 and bytes, Git blob SHA1 on every file including .meta, 16 unique Unity GUID, three asmdefs, complete .meta pairs and owner scope. Parent `.meta` differs from MAPGEN/RC1: **preserve destination parent** if installing later; do not rewrite GUIDs or serialized references.
- Reads independently published native SUMMARY receipts at RC1 report head `5c29e8e` and CAMERA QA `08037f0`. Captures CAMERA Edit 5/5 + Play 13/13 and RC1 final visible tests as **external**. Crucially retains CAMERA **WindowsFinal exit2 / Light tracking FAIL** and prior RC1 hidden UI shutdown exit `-1073741819` (`C0000005`); neither can be promoted to whole-game PASS.
- Optional actual Windows ZIPs: **without bytes, marks `PENDING_BINARY_ACCESS`**. When owner supplies actual local Windows BASE/EXP ZIP, computes full-file SHA256+size, ZIP structure and every CRC; bad files fail closed. Revalidating public JSON cannot replace raw binary verification.

## Commands

```powershell
# Can run from any local checkout, does NOT write to game files.
python Tools/B01/RC1-RECEIVER-001/receiver_gate.py --camera-artifact C:\path\camera-producer-artifact.zip --live --json-out rc1-receipt.json

# Optional real Windows build files; no need to copy to game/Unity.
python Tools/B01/RC1-RECEIVER-001/receiver_gate.py --camera-artifact C:\path\camera-producer-artifact.zip --live --base-zip C:\path\RoboRougelike_B01_RC1_BASE_Windows_x64.zip --experimental-zip C:\path\RoboRougelike_B01_RC1_MAPGEN_EXPERIMENTAL_Windows_x64.zip --json-out rc1-release-receipt.json
```

Only the exact **Final** experimental ZIP 38,502,444 bytes, SHA256 `722be56b98db0abe1517ec1d018b6f4d6ecbf0a46ea172d198f8294a7edc8cca`, is valid; prior pre-guard candidates remain superseded. BASE 38,481,635 bytes SHA256 `457ec2020375e18c4599d832dc028eed7900f8cf5d603765c9e417fd80a8ca90`.

When running from this branch with CI, the Actions step reconstructs an outer ZIP from the **unaltered real source artifact** because `actions/download-artifact` extracts GitHub's transport wrapper by default. The inner full ZIP's recorded hash and every individual package byte remain authoritatively verified.

## Rollback and Codex remainder

This gate is **read-only**; it creates no changes requiring rollback. If a later G2-authorized installation uses the separately verified `PACKAGE-AUTO-001`, it must stage only in marked disposable worktree with pinned exact target HEAD, preserve pre-existing root `Assets/B01Packages.meta`, retain its transaction journal and use its rollback before touching any live checkout.

**Codex:** no second RC1 rebuild or 43 old MAPGEN tests requested. Only independent RC1 signed binaries/provenance to B01-40 once available, CAMERA aim-tracking defect triage under B01-00 ownership, then actual B01-60/manual human test before any G2. Future integration is intentionally not started by these instructions.

## Additional actual PACKAGE-AUTO-001 transaction case

The test suite imports **the original** `Tools/B01/PACKAGE-AUTO-001/package_auto.py` from upstream #238 (in the stacked branch), never copies/reimplements its transactional logic. Test 15 reads the actual 41-entry CAMERA R1.2 source ZIP, constructs a marked detached synthetic Unity/URP worktree with **MAPGEN's different existing parent GUID**, verifies `24` files are staged, restores all `24`, checks parent GUID preserved, injects an interrupted installation after `3` files and verifies that no CAMERA file remains. The target here is **synthetic**, not active Codex. The full suite has 15 positive/negative tests.
