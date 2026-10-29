# B01-13 PASS17 / WAVE03 — AIM-VAULT-001 R2

## Executable capability

A **private local evidence lifecycle tool** for images/receipts made by [AIM-OBS001 #349](https://github.com/C8LyNn/RoboRougelike/pull/349), including [AIM-DELTA001/R2 #363](https://github.com/C8LyNn/RoboRougelike/pull/363) private reports referencing two different sessions. The tool **does not** annotate screenshots, calculate gameplay measurements, implement an additional HUD, or alter any Unity gameplay or project files. It provides a missing facility in the existing B01-13 study workflow: source-pinned cryptographic checks at intake, an index of **exact local files**, byte-integrity verification, and a **durable withdrawal tombstone that also unlinks registered cross-session derived reports**.

- Owner: `B01-13 HUMAN PLAYTESTING`. Package: `AIM-VAULT-001/R1`. Scope: `Research/B01-13/P17C/**`, brand-new branch stacked only on frozen #349. No writes to source #349, #363, `Assets/**`, Codex, CHL, PARTSIZE, main or develop.
- Original verifier dependency: frozen `Research/B01-13/P17/verify_aim_receipt.py` Git blob **`70f93467fef1d5782ec4e5cbd6a8a98af6fd0b00`**, SHA-256 **`8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4`**. It is **not copied or edited** in this package; a different verifier fails closed.
- Python 3.11+, Pillow required. No network calls. Native Unity, Windows GUI and consent validation not implemented. `HUMAN_APPROVED` files are refused by default and require explicit `--allow-human-data`, but even that **cannot authenticate HQ permission**; never run human sessions before HQ GATE D and actual participant consent.

## Commands (operator private machine; avoid repository folders)

```bash
# 1. Create an isolated, new private root OUTSIDE all Git worktrees.
python3 Research/B01-13/P17C/aim_evidence_vault.py init --root /private/AIM_VAULT

# 2. Privately place exactly 5 already captured physical files in each subfolder:
# /private/AIM_VAULT/sessions/S01/receipt.json + four images named in receipt;
# /private/AIM_VAULT/sessions/S02/receipt.json + four images named in receipt.
# The tool DOES NOT copy or collect images from the game.
python3 Research/B01-13/P17C/aim_evidence_vault.py seal \
  --root /private/AIM_VAULT --sid S01 \
  --receiver Research/B01-13/P17/verify_aim_receipt.py \
  --game-sha YOUR_EXACT_40_HEX_SHA --zip-sha256 YOUR_EXACT_64_HEX_PLAYER_ZIP_SHA

# Repeat seal with S02 and its own exact build+ZIP SHA.
# Optionally generate the actual private before/after report using original #363,
# saving output as /private/AIM_VAULT/derived/REPORT01.html.
python3 Research/B01-13/P17C/aim_evidence_vault.py attach-report \
  --root /private/AIM_VAULT --report REPORT01.html \
  --related-session S01 --related-session S02

# 3. Check every exact registered file, verify the original screenshot receipt
# against the unmodified #349 receiver, and reject unregistered files.
python3 Research/B01-13/P17C/aim_evidence_vault.py verify \
  --root /private/AIM_VAULT --receiver Research/B01-13/P17/verify_aim_receipt.py

# 4. Only after a real withdrawal request from the proper participant/operator:
python3 Research/B01-13/P17C/aim_evidence_vault.py withdraw \
  --root /private/AIM_VAULT --sid S01 --confirm-local-delete
```

**Withdrawal semantics:** a durable tombstone is written **before** unlinking tracked local files. Any interrupted deletion leaves `verify` **RED** until deletion resumes; repeat the same `withdraw` to recover. If S01 participated in `REPORT01.html` alongside S02, the **entire shared private HTML is unlinked**, while S02's unrelated original screenshots/receipt remain. Reusing S01 after withdrawal is rejected. Foreign/untracked file in the session blocks automatic deletion rather than removing arbitrary data. Symlinks, unsafe paths, extra screenshots, malformed index and repository roots are refused. **This is unlinking, not secure erasure**: external screenshots, uploaded files, other exports, sync copies, downloaded reports, backups and retention schedules remain outside this tool and require the actual operator's manual/policy handling. For Windows, private folder ACLs are an **operator responsibility**; POSIX root `0700` checks do not prove NTFS ACL privacy.

**Safety:** do not put real participant data, raw screenshots, private generated HTML or session indexes in GitHub, ChatGPT, CI artifacts or this repository. No real participant was used to test this code. Tested fixture modes were `SYNTHETIC` with deliberately bogus example hashes and `HUMAN_APPROVED` with explicitly fake witness IDs to demonstrate the gate **does not establish consent**. Do not infer a completed playtest or game QA from structural verification.

## Actual source tests

From a checkout containing the **unchanged frozen #349 parent** and this new source:

```bash
python3 -m pip install pillow
python3 -m py_compile Research/B01-13/P17C/aim_evidence_vault.py Research/B01-13/P17C/test_aim_evidence_vault.py
python3 Research/B01-13/P17C/test_aim_evidence_vault.py
```

The test suite uses actual original #349 verifier, generates physical PNG/receipt bytes, runs the real original **#363 R2 `aim_delta.py`** for an end-to-end registered generated HTML, and verifies withdrawal removes shared derived content but retains the unaffected session. The test expects the original R2 `Research/B01-13/P17B/aim_delta.py` present in checkout (without editing it). 33 source tests incl. negatives, tampering, crash/tombstone recovery, malicious index traversal, hard rejection of missing consent flag. **Linux Python + Pillow tested; Windows native NOT_RUN; Unity native NOT_RUN.** Original parent test 59/59 and R2 test 47/47 were rerun separately; they are **not** new AIM-VAULT feature tests.

## B01-40 / B01-60 / Codex handoff

- B01-40: verify only new `P17C/**` changes, parent branch/source pin and manifest; confirm no canonical game asset or other-producer edit. Source install only; no Unity integration expected.
- B01-60: independently rerun 33 source tests on this exact SHA with original #349 and R2 #363. Test crash recovery, untracked-file negative, consent disclaimer and **Windows NTFS ACL behavior** if a licensed machine is available. This producer PASS is *not* independent QA.
- B01-00: decide whether this operator tool belongs in existing CODEX INBOX #79. `READY_TO_QA != CODEX_ACK != GAME_INTEGRATED`.
- Codex: optional utility for an **authorized human-playtest operator** after real build/ZIP pin, explicit study consent, and private storage. No gameplay import; do not cherry-pick research code into `Assets`. Rollback: do not merge Draft PR, discard new own `P17C` files and remove optional private vault only under the operator's actual data-retention policy.

## PASS17 WAVE03 R2 — verified privacy preflight repair

**Real owner defect at R1:** `initialize()` checked only that the requested root was empty; it created `sessions/`, `derived/` and `.aim-vault.json` even **inside a Git worktree**, while `private_root()` subsequently rejected that location. An ancestor symlink could also redirect a new private vault to a different physical path. This is unsafe for private study evidence.

**R2 fix:** shared `checked_location()` runs **before any mkdir/chmod/index write** in `init` and for every operation through `private_root()`. Refuses a Git worktree ancestor (`.git` file or directory) or symlink ancestor, retains valid new external directory behavior. This is a scoped research-only correctness fix; no native Windows ACL/junction guarantee.

**New executable fail-closed regression:** `python3 Research/B01-13/P17C/test_private_root_location.py` (Python stdlib, no real people/screenshots, no Unity). Four cases: reject Git worktree **before creating any files**, reject symlink ancestor on init, reject symlink ancestor on existing vault, and accept normal external vault with POSIX 0700. Initial bounded exact-logic preflight reproduction: prior check **incorrectly ALLOWED** Git/symlink roots; patched excerpt **4/4 PASS** on Linux, confirmed by direct execution. The **entire unmodified final R2 Python source file** was reconstructed byte-for-byte from GitHub and verified as Git blob `7937b0a37e7c3038d571860e15d0a40710331948` (12,803 bytes; SHA256 `4273a7ad73046a62c5ebba33a3342b980d7f823a09708d289cad21e98802f615`). The **exact Git-identical published regression test** blob `664efd3cb8f52fbd31165ee8422f2f9222b8bf3b` was executed against that complete original file on Linux Python 3.13.5: **4/4 PASS, exit 0**. See `QA_R2_LOCATION.log` for unedited terminal output. This is actual same-source execution of the NEW R2 regressions, **not** an independently executed full R1/R2 33-test suite or independent QA. The preexisting `QA_execution.json` / `QA_execution.log` are preserved as historical **R1 33/33**, not relabelled as R2. Independent full-source tests on final exact R2 SHA remain required before CODEX handoff.

## PASS17 WAVE03 R2 — full exact-source Windows/Linux acceptance

After the first 4/4 source checks, executed the **complete 33-case original vault test suite** on the final corrected owner code, using exact original frozen #349 verifier and now-repaired own AIM-DELTA/R3 producer source (#363). Windows real CI initially **RED** on original #363 R2 `WinError32` temp rename, then on unsupported Windows Python 3.11 `os.fchmod`, plus R2 test's default `cp1252` decoding of UTF-8 private HTML. These were **repaired in the owning B01-13 code**, not bypassed or hidden.

[Final GitHub Actions #40913275888](https://github.com/C8LyNn/RoboRougelike/actions/runs/40913275888) **4/4 jobs SUCCESS** (Ubuntu/Windows × 3.11/3.13), source Git-blob pinned before every execution: #349 receiver `70f93467fef1d5782ec4e5cbd6a8a98af6fd0b00`, #363 R3 `8825e21d73fc9e735c17f9d2a05254a5bac9b810`, original AIM-VAULT source `7937b0a37e7c3038d571860e15d0a40710331948`, and current R2 original test `81e190f4568d98bf0ab06065b8ea8a42d07e158f`. Per job **33/33 vault cases**, **6/6 upstream R3 publication tests**, and **4 location tests** (Windows 2 PASS/2 SKIP by explicit native junction/ACL exclusion). **Total 168 PASS + 4 SKIP**, not 172 PASS. Physical images and derived HTML are SYNTHETIC. Historical `QA_execution.json`/`QA_execution.log` remain labeled R1; actual new evidence is in linked original public CI job logs. No Unity/native Player, true participant consent, Windows ACL approval, independent receiver acceptance or Codex ACK.
