# B01-23 / W74 — PARTSIZE QA receiver freeze and safe disposal

This is a **frozen QA receiver**, not a merge, an authorized Unity launch, or a G1/G2 approval.

| Frozen item | Exact immutable Git SHA |
|---|---|
| PARTSIZE producer #286 | `be2be0e4802bc3f450179901f9ae91c6d84bcb5b` |
| PARTSIZE QA receiver #298 | `26634727ab2323a982bce893421c88c330be46e8` |
| Original QA PowerShell operator blob | `bd7bccd2a949d91b1901e3ec01ab93c62874eef4` |
| Original NUnit receipt parser blob | `b4ca19bd1b9142a364dbed2fb6bb2dd30f2b53ac` |
| Original producer physical inner ZIP SHA256 | `05b86bf90ba2b1a71ce32b081b91e2f24793ea81cb00f2025757c5c017d95f54` |

**Machine-readable manifest:** [W74_RECEIVER_FREEZE.json](W74_RECEIVER_FREEZE.json) pins **10 exact QA source files and 40 exact producer files** by original Git blob SHA-1; source trees were read directly at the exact commit SHAs. The accepted #298 branch HEAD is **not** moved by this document. This separate documentation branch starts at exact QA HEAD and adds only these freeze/rollback files.

## Independent verified source evidence (not native)

- B01-40 independently ran the **corrected original parser** NEG01–03, original C# source intact: [Actions #40653960667](https://github.com/C8LyNn/RoboRougelike/actions/runs/40653960667), **10/10 each Windows and Ubuntu**.
- B01-40 independently ran the **corrected original linked-worktree PowerShell preflight**: [Actions #40654528336](https://github.com/C8LyNn/RoboRougelike/actions/runs/40654528336), **6/6 each OS** (sibling root, sibling Assets, ancestor and positive controls). [Original independent receipt](https://github.com/C8LyNn/RoboRougelike/pull/298#issuecomment-6463941312).
- B01-23 exact-head source workflow [#40653865774](https://github.com/C8LyNn/RoboRougelike/actions/runs/40653865774): **20/20 real Git/PowerShell operator preflight, 16/16 parser, and 328/328 extracted immutable producer C# assertions per OS**, source-only.
- B01-40 independently executed original producer **disposable stage + rollback** with **41/41 PASS**, full 40 source blob verification and three sentinel file controls: [source receipt](https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6438945311). This is historical source-G0 rollback validation, **not a new PASS15 run nor proof of target-native rollback**.

**Native remains NOT_RUN:** PARTSIZE 17 Edit / 11 Play and **44/44 11-kind×4-yaw** physical/render/sockets; HQ licensed independent Windows Unity 6000.3.14f1 and B01-60 G1 still required. Already queued conditionally as **CV-10** in [Codex validation queue](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223). No duplicate CV task.

## Safe disposal of an EXACT disposable QA Git worktree

**This only removes a temporary, producer-pinned linked QA worktree after evidence preservation.** It does **NOT** rollback an imported Unity target, Codex gameplay changes, the producer branch, or any other worktree. An uncertain or modified checkout must be retained for investigation. NEVER use `git clean`, `git reset --hard`, `git worktree remove --force`, arbitrary recursive deletion, or remove either original code branch.

1. Confirm that the actual Unity session, if any, ended. Preserve the original external `receipt.json`, both untouched XML and Editor logs and SHA256; the evidence directory must be wholly separate from **every** registered worktree. Do not use the second CHL/PART source tree as evidence. Keep evidence even when native tests fail.
2. Work from a **separate approved QA Git repository**, not the active Codex worktree. Enter exact paths for the worktree and independent QA repository:

```powershell
$qaRepo = 'C:\QA\QARepository'             # independent QA Git primary worktree
$disposable = 'C:\QA\PartsizeDisposable'  # disposable linked PARTSIZE tree, not Codex
$expected = 'be2be0e4802bc3f450179901f9ae91c6d84bcb5b'

if (!(Test-Path -LiteralPath (Join-Path $disposable '.git') -PathType Leaf)) {
    throw 'REFUSE_NON_LINKED_WORKTREE'
}
if (!(Test-Path -LiteralPath (Join-Path $disposable '.b01-60-disposable-qa') -PathType Leaf)) {
    throw 'REFUSE_MISSING_DISPOSABLE_MARKER'
}
$actual = (& git -C $disposable rev-parse HEAD | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $actual -ne $expected) { throw 'REFUSE_SOURCE_SHA_DRIFT' }
$branch = (& git -C $disposable symbolic-ref --quiet HEAD 2>$null | Out-String).Trim()
if ($LASTEXITCODE -eq 0 -or $branch) { throw 'REFUSE_ATTACHED_BRANCH' }

# Read and inspect the ORIGINAL registered worktree registry and exact path.
& git -C $qaRepo worktree list --porcelain
if ($LASTEXITCODE -ne 0) { throw 'REFUSE_MISSING_REGISTRY' }

# Review BOTH path and detached/HEAD lines. Do not proceed if this is primary,
# Codex, a sibling, unregistered, or if ANY worktree/path identity is ambiguous.
& git -C $disposable status --porcelain=v1 --untracked-files=all
if ($LASTEXITCODE -ne 0) { throw 'REFUSE_STATUS_FAILURE' }
```

3. **Manual fail-closed operator gate before deletion:** inspect the registry output and status above, verify that ONLY the two exact disposable-marker files and known Unity cache folders are disposable; preserve any unknown, tracked, ignored or untracked inputs by STOPPING (do not auto clean). Confirm the evidence store and protected source are elsewhere; obtain the independent operator's approval to dispose. If G1 investigation is still open, keep the original worktree.
4. Once approved, copy/preserve the original two marker files separately as part of the evidence record. Remove only `.b01-60-disposable-qa` and `.b01-60-disposable-qa.used` **by exact file path**, *after* the above checks. Use **non-force** `git -C $qaRepo worktree remove $disposable`; if Git refuses because caches or untracked files remain, **STOP** and review. Never bypass it with `--force`. Confirm `git worktree list --porcelain` no longer contains this source. **Evidence files remain untouched.**

## Freeze discipline

Current #298 HEAD `26634727ab2323a982bce893421c88c330be46e8` is independent **source/tool** accepted, and the original producer #286 exact SHA remains intact. Do not modify either to update this record. If any executable script/blob differs from the manifest or B01-40 receives a new native-op source, invalidate this freeze and require a **new** independent receiver; do not silently inherit previously green status. HQ alone decides G2/offer/merge after real G1.
