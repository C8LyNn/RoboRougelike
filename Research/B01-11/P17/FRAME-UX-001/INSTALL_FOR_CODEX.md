# Codex receiver / install, use and rollback

```sh
git fetch origin research/b01-11-pass17-frame-ux-001
git worktree add --detach /tmp/b01-11-frameux research/b01-11-pass17-frame-ux-001
# Open /tmp/b01-11-frameux/Research/B01-11/P17/FRAME-UX-001/SOURCE/index.html in a modern browser.
```

Work in disposable research worktree, not Codex's protected gameplay tree. If the operator has **actual** Codex-produced game screenshots, import them manually with the local file picker (do not publish sensitive user content by default). Mark issues and share the exported JSON+PNG with the exact screenshot/build SHA and resolution. This package has no Unity import requirement; it is an offline research/acceptance tool only. Compatible formats: PNG/JPG/WebP.

Automated checks: `node SOURCE/test_core.cjs`; for real browser checks, install `playwright-core@1.56.1`, set `CHROME_PATH=/path/to/chrome` where necessary, run `node SOURCE/test_browser.cjs`. The CI workflow performs these outside Unity.

**Rollback**: `git worktree remove /tmp/b01-11-frameux` or delete/ignore the isolated directory. There are no scripts imported into Unity and no changes to game save data or settings.

**Suggested receiver**: B01-40 G0 provenance, B01-60 independent UX source/browser QA, B01-00 prioritization and optional Codex intake. No merging or uploading screenshots without explicit owner decision.
