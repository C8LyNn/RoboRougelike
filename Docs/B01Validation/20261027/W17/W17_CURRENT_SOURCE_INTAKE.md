# B01-40 / W17 — real two-package receiver and disposable staging audit
**2026-10-27; read-only producer verification; no G2, no merge, no Codex source mutations.**

Authority: [HQ W17 #71](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6438657467), [exclusive owner #276](https://github.com/C8LyNn/RoboRougelike/issues/276), [ledger #79](https://github.com/C8LyNn/RoboRougelike/issues/79).

## Exact source results / no stale-SHA inheritance

| Package | Exact checked SHA | Original GitHub Actions producer archive | Original physical inner ZIP | Source-level result |
|---|---|---|---|---|
| PARTSIZE-001 R1; B01-23 | `be2be0e4802bc3f450179901f9ae91c6d84bcb5b` | [run 40484486467, artifact 12725235814](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484486467/artifacts/12725235814) | 41 entries / 112237 bytes / SHA256 `05b86bf90ba2b1a71ce32b081b91e2f24793ea81cb00f2025757c5c017d95f54` | **PASS original bytes 40/40, exact Git blobs 40/40, 17 unique Unity GUIDs, own three asmdefs, path/case safety** |
| CHL-003 R1; B01-20 | `30c0355a62764eb37798347612564ee51f3b36e1` **historical verified snapshot ONLY** | [run 40483539797, artifact 12723786717](https://github.com/C8LyNn/RoboRougelike/actions/runs/40483539797/artifacts/12723786717) | 53 entries / 57265 bytes / SHA256 `e4c950e7476d2fd93d7943de6f84a39eb22da5f02cb2cfcb518bb927a382b541` | **PASS original bytes 52/52, Git blobs 52/52 + verified exact tracked manifest blob, 23 unique Unity GUIDs, three asmdefs** |
| CHL-003 R1; B01-20 | `44d8453356d8fd7547d990a62eeadfc2efef037d` **later, unverified HEAD** | [run 40484994420](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484994420) | **NO valid newest-head ZIP** | **G0 SOURCE BLOCKED**: actual .NET C# core 24056/24056 PASS and Unity importer static PASS, but the real producer packaging step raised `RuntimeError: SHA256 manifest stale`; never reuse old CHL ZIP hash for this HEAD |

Actual original physical ZIP: CRC, outer/inner SHA256 and bytes, every registered original source SHA256/bytes, all .meta pairs/unique GUIDs (including owned parent folder), CHL asmdef refs, exact allowed producer paths and Win32 casefold/path checks. Git identity cross-check was against **independently read real full GitHub recursive Git Trees at the exact 40-hex SHA**, not a producer-declared `source_sha=null` as if it were frozen. Separate Git blob SHA-1 for each original committed file, canonical path/blob-pair vector independently SHA256 compared: PARTSIZE `9c49d4b11efb0e6f5229b6b78bd89f2ae1b51fed27174a50b2eba6cac20732ee` across 40 files; CHL `516ed07dbd34952af574f83935750d698478338477552370b9dcf8f32695b8bb` across 52. Original CHL tracked `Docs/B01/CHL003/manifest.json` Git blob `48e5434f79b19bab11c96ad09896b2eecb588669` additionally matched.

## Actual disposable Git stage→rollback; not a claim of live import

With original files extracted **unmodified** from the original producer ZIP, create an independent local Git repo with three protected sentinels (`Assets/Pack/OWNER_PROTECTED.txt`, `Assets/scene 1.unity`, `ProjectSettings/ProjectVersion.txt`); commit baseline, `git worktree add --detach`, place **original** source bytes only at original package paths, verify all staged SHA256/bytes, `git ls-files --others` exactly equal the expected paths, no tracked baseline edits; mutate one staged original file deliberately to prove fail-closed rollback; restore it; delete *only* byte-matching owned staged files; verify original HEAD, clean `git status --porcelain`, and sentinel SHA256 unchanged.

- PARTSIZE `be2be0e`: **41/41 original archive entries staged; rollback PASS; 3/3 protected sentinel checks PASS; edited-staged-file rollback rejection PASS; zero unowned tracked modifications.**
- CHL `30c0355`: **53/53 original archive entries staged; rollback PASS; 3/3 protected sentinel checks PASS; mutation rejection PASS; zero unowned tracked modifications.**
- Across these *specific* two source snapshots: **zero cross-package Unity GUID collisions**.
- This is a **DISPOSABLE SYNTHETIC TARGET**, not Codex current canonical game checkout, Unity import, licensed Editor, or execution of B01-40 [PACKAGE-AUTO-001 #238](https://github.com/C8LyNn/RoboRougelike/pull/238). `G0_TARGET_LIVE=NOT_VERIFIED`; exact private Codex HEAD/dirty/path ownership ACK unknown. The existing #284 receiver has seven documented outdated assumptions; these independent byte+Git checks do **not** claim that obsolete receiver `gate.py` accepts the two actual schemes. See [#284 review](https://github.com/C8LyNn/RoboRougelike/pull/284#issuecomment-6438330519).

## Gates / next single receiver action

**PARTSIZE:** `G0_ORIGINAL_SOURCE=PASS@be2be0e`, `G0_DISPOSABLE_STAGE_ROLLBACK=PASS`, `G0_CURRENT_PRIVATE_TARGET=NOT_VERIFIED`, `G1_UNITY=NOT_RUN`, `B01_60_NATIVE=NOT_RUN`, `G2=HOLD`, `OFFERED=NO`, `CODEX_CLAIMED=NO`. B01-23 owns independently verifying the newly revised external broadphase and typed sockets with *real PhysX missing-neighbor* negative; source checksum is not a safety/gameplay verdict. B01-40 should reconcile own #284 gate to actual protected paths and invoke existing #238 **on an authorized exactly pinned actual target** only after receiver ACK.

**CHL:** `G0_ORIGINAL_SOURCE=PASS_ONLY@30c0355`; **later `44d8453` G0=BLOCKED with manifest stale**. B01-20 exclusively repairs own producer manifest/package CI and supplies original current-head physical ZIP; B01-40 then independently repeat full-byte/source-tree/GUID/rollback checks. Six 3x Light/Heavy physical completions, contact/fall/retry/pause, 15 authored Edit+9 authored Play cannot be labeled actual native execution. `NATIVE_UNITY=NOT_RUN`, `G2=HOLD`.

No freeze, SOURCE_OFFERED, Codex ACK, target edits, Unity PASS or merge. This is a QA receipt for **specified original bytes/commits only**.