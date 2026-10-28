# B01-41 / PASS16 / CI41-GITSCOPE-001 — executable CI-only receiver

## What is actually delivered

- `git_verified_required_gate.py` — validates two **exact 40-character Git commit IDs**, requires ancestor relationship, reads full original `git diff --no-renames --name-only -z`, rejects empty/unavailable history or a forged claimed file list; passes the verified paths to the existing W78 native-status aggregate.
- `test_git_verified_required_gate.py` — executes original pinned pre-fix W78 Python from **original Git bytes** to reproduce false QA-only classification, then executes new exact-Git gate against **actual Codex `231005c6... → a2ccd055...` commits** and validates six fail-closed/positive cases on Windows and Ubuntu.
- Existing `W78/required_aggregate.py` CLI was **patched in place**, not left as a bypass: it now REQUIRES `--repo --base --head`, checks source identity, and resolves file changes from the same original Git commit diff. Old `--input` alone fails. The underlying `evaluate()` remains a policy-only function, not an independent source provenance authority.

**This is CI-only tooling; never Unity G1, G2, gameplay code, merge, integration or an authenticated GitHub required check.** The external HQ/CI owner must authorize adoption into shared workflows, and trusted base/head SHA **MUST come from the actual GitHub PR/event**, not from a submitter choosing a convenient ancestry interval.

## Safe invocation from checked out original Git repository

```bash
# Example pinned original Codex pair; fetched as actual original commits:
python Tools/B01/CI41/W82/git_verified_required_gate.py \
  --repo . \
  --base 231005c6d0351945a8ee6684f3856af118f0e94f \
  --head a2ccd055070a963805cba0779b79f18489f4794f
# Expected: exit 2, NATIVE_REQUIRED, G1 NOT_RUN.
```

To supply a claimed file-list, use `--claim-paths-json /path/to/list.json`; its complete path set must **exactly** equal the computed original Git diff. Any mismatch rejects the request. `--observed-json` only accepts untrusted CI metadata and NEVER certifies original Editor.log/XML; native-relevant scope remains blocking.

For compatible historical entry point:
```bash
python Tools/B01/CI41/W78/required_aggregate.py \
  --repo . --base <TRUSTED_BASE_SHA> --head <TRUSTED_HEAD_SHA> \
  --input <UNTRUSTED_OBSERVED_JSON>
# Legacy --input alone rejects (exit 2). No fake native positive can return 0.
```

## Repro / results / QA handoff

1. Check original source blob IDs and exact unchanged Codex commit history from `manifest.json`.
2. Run both actual GitHub Actions on this QA branch: `b01-41-w82-exact-git-required-check.yml` (6 tests per OS) and `b01-41-w78-required-check-aggregate.yml` (8 tests per OS). Both require actual original Codex commit objects. Any missing object, wrong source blob or failed negative makes CI RED.
3. Observe explicit prior `RED` from immutable old source: a user-supplied QA-only path set falsely returned a nonblocking result on a Codex game SHA. Observe new `GREEN`: original full Git diff has `Assets/**`, so native gate blocks; a false QA-only list is rejected.
4. Independent B01-40 or B01-60 reviewer may re-run on a changed exact SHA; independent execution has **not** been claimed here. Handoff to Codex only as optional CI helper; Codex gameplay integration remains exclusive. No new CHL/PARTSIZE or gameplay producer package.

**Known limits:** no test has actually started Unity or Windows Player; no ability to verify source digest of an externally uploaded archive without real raw bytes; no new GitHub branch protection checks; never treat a status=success or forged log digest as Unity test evidence. A future integration should bind base/head to immutable GitHub event and perform independent original artifact verification before claiming any native success.
