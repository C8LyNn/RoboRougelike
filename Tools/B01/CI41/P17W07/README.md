# CI41-UPMLOCK-001 v1.0.0 — Unity Package Manager source lock closure gate

**Owner:** B01-41 CI/CD & TEST AUTOMATION. **PASS17 WAVE07**. This is independent of the frozen asmdef graph gate in Draft #431 and native EditMode replay in Draft #409. The functional source gate verifies package-manifest/lock consistency; it does not edit Unity package files, canonical Codex code, CHL, PARTSIZE or shared CI02/03.

## Executable function

`upm_lock_gate.py` uses `git ls-tree`/`git cat-file` against **one exact lowercase 40-character committed source SHA**, not an uncommitted working tree or mocked Unity resolver. It reads `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt` directly from original immutable Git objects. It rejects missing/non-text sources and JSON duplicate keys; validates direct package versions and depth=0, all locked transitive package existence, shortest graph depths, orphan lock entries, invalid lock structure, malformed source, and insecure registry URLs. Its deterministic machine-readable JSON includes every source error and warning. Return code 0 means `PASS_SOURCE_UPM_LOCK`; 2 means `FAIL_SOURCE_UPM_LOCK` or invalid source.

**Important version semantics:** UPM transitive `dependencies` values can be minimum requested versions while `packages-lock.json` records the resolved version. A requested `1.1.33` can resolve to `1.6.0`. The tool emits `UPM_TRANSITIVE_REQUEST_DIFFERS_FROM_RESOLVED` as a **warning**, never treats an ordinary resolver upgrade as a broken lock. External unmodeled package origins such as remote Git packages are **fail-closed pending QA review**, rather than guessed to be safe.

## Authentic RoboRougelike source snapshot

Git main SHA `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`, Unity Editor `6000.3.14f1`. Original project files: manifest Git blob `d1dacf230ed7e1e148062d0441ec430a867ae0a7`, lock Git blob `6b8a670c36a9a9a821d9f1e7f78bfe3272274fbc`, ProjectVersion blob `88b74ddff7374ebb3d54bab5fd066fc78f20ad07`.

Real source graph: **45 directly declared packages, 57 locked total, 12 transitive-only lock entries**. All 45 direct version/depth pins match. All 57 entries are reachable at the original locked graph shortest depth, and all referenced packages exist. There are 15 source warnings for legitimate transitive requested/resolved version differences; these are *not* native Unity package-resolution tests. If package contents change, recheck with the new exact SHA before marking source compatibility.

## Run on the exact original source

Use a fresh checkout or a source utilities copy, replacing `YOUR_EXACT_COMMIT_SHA` with the actual source SHA:

```sh
python3 Tools/B01/CI41/P17W07/upm_lock_gate.py --repo . --source-sha YOUR_EXACT_COMMIT_SHA --expected-direct 45 --expected-resolved 57 --out upm-original.json
python3 -m unittest discover -s Tools/B01/CI41/P17W07 -p test_upm_lock_gate.py -v
```

The two `--expected-*` count pins are original-project-specific regression guards and must be updated only after owner review of a real package change; they are not universal Unity limits. On Windows use Python in place of python3 as installed. Run the same tool on any future independently reviewed branch SHA; the tool makes no changes to `Packages/**`.

## Actual producer testing versus native testing

`test_upm_lock_gate.py` contains **26 tests using real `git init`/`commit`/`ls-tree`/`cat-file` subprocesses**, plus real standalone CLI invocations. Original cases include correct 2-direct/4-lock closure, version drift, missing direct/transitive entries, orphan entries, depth drift and shorter paths, HTTP/credential-bearing registries, unsupported origins, bad types/JSON duplicate keys, exact SHA pin, uncommitted worktree tampering, missing lock, actual 0/2 CLI exits and cycles. The targeted malformed dependency case initially found an exception and was fixed before final packaging. Both Ubuntu 24.04 and Windows 2025 GitHub Actions run these original tests and a second **real current-repo scan** of 45/57 original project entries. Each OS uploads actual process logs and scan JSON.

**NOT RUN:** Unity Editor, Unity Package Manager resolver activation/install, full Unity compile, EditMode, PlayMode, Windows Player. `PASS_SOURCE_UPM_LOCK` asserts only committed manifest/lock *structural* consistency, NOT safe installation, license availability or G1. The script neither contacts package servers nor makes build/Unity availability claims.

## Independent QA and rollback

**B01-40**: verify exact immutable PR/source SHA, Git blob hashes, five-file scope, Windows/Ubuntu original real receipts, manifest correctness and rollback. **B01-60/61**: independently reproduce 26 source tests and compare 45/57 source scan; optionally perform authorized native Unity package restoration and compile later (only with suitable license and isolated checkout). **B01-00** alone can route optional CI tool to CODEX INBOX #79 after source QA. Codex is currently busy; no claimed ACK/integration. Prior frozen #431 and #409 are unchanged.

**Rollback:** leave the isolated Draft PR unmerged or remove only this optional CI tool under `Tools/B01/CI41/P17W07/**` and its own workflow. No asset changes, no binary dependencies, no migrations.

**Status:** PRODUCER_SOURCE_TESTS / INDEPENDENT_G0_PENDING / UNITY_RESOLVER_NOT_RUN / G1_NOT_RUN / CODEX_ACK_PENDING / NOT_INTEGRATED.
