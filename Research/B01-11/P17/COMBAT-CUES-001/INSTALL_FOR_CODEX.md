# Receiver / Codex install & rollback

## Inspect and run without touching Unity
```bash
git fetch origin research/b01-11-pass17-combat-cues-001
git show research/b01-11-pass17-combat-cues-001:Research/B01-11/P17/COMBAT-CUES-001/SOURCE/index.html > /tmp/b0111-impactlens.html
# Open /tmp/b0111-impactlens.html in your standard browser.
```
For automation: `python -m pip install playwright`; install Chromium and set executable_path inside `SOURCE/test_browser.py` for your host; then `python SOURCE/test_browser.py`. The test uses `page.set_content` because this sandbox denies file:// navigation. Browser Open File may work normally.

## Game integration suggestion (NOT performed)
Codex may use **UX behavior as reference** in a future separately authorized change: publish authoritative `DamageApplied(partId, hpBefore, hpAfter, source)`, `PartDestroyed`, `WeaponGroupDisabled`, `CoreDestroyed` and `BlockedHit` to a presentation-only HUD. Do NOT copy mock HP or infer weapon readiness from this demo. Use authoritative R3/R4 state, preserve existing LMB/RMB group policy and lifetime, and request B01-12 UI review + B01-60 independent QA before merge. No current active Codex path is claimed or edited.

## Rollback
Remove only `Research/B01-11/P17/COMBAT-CUES-001/` after approval, or simply ignore this branch. The package has no project/asset/scene/runtime import side effects. Do NOT cherry-pick whole branch into Codex's protected worktree without explicit acceptance.
