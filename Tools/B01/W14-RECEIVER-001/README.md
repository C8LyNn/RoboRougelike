# B01-40 W14 — immutable source/GUID receiver + MAPGENWORLD in-place upgrade

**Owned B01-40 tools only**. No Unity gameplay source and no active Codex worktree are edited. `G2=HOLD` regardless of source PASS. No host installation or integration permission is granted.

## Reproduce actual source intake

The workflow downloads original **immutable** GitHub Actions artifacts by run and artifact names. It re-wraps downloaded transport files only; the **inner producer ZIP bytes remain untouched**. Each original source ZIP is validated against its own fixed entire-file SHA256, size, ZIP count, per-file SHA256+size, Unity meta/GUID and asmdefs. Full dependency list is reported; missing upstream assembly references are NOT a successful Unity compile. All four frozen sources are checked; old source receipts cannot be inherited after a SHA change.

`python Tools/B01/W14-RECEIVER-001/receiver_w14.py --artifacts-dir artifacts/pinned --report w14-g0.json`

`B01_W14_ARTIFACTS=artifacts/pinned python -m unittest discover -s Tools/B01/W14-RECEIVER-001/tests -v`

Prerequisites: Python 3.10+, git, the independently built and passed existing `Tools/B01/PACKAGE-AUTO-001/package_auto.py` from **Draft #238**, five pinned actual Actions downloads. Tests use real producer inner ZIPs, and a fake baseline explicitly marked synthetic in isolated disposable detached `git worktree`s. **Never substitute this for live Codex G0_TARGET or Unity G1.**

## World v1.0.1 → v1.1 safe *only in disposable* Git worktree

1. First use PACKAGE-AUTO-001 `sandbox` to create marked **detached, disposable** target at an explicitly pinned commit; it refuses ordinary checkout. No active game use.
2. Stage the old WORLD v1.0.1 exact source asset bytes (or compare existing received bytes) plus existing canonical `Assets/B01Packages.meta`.
3. Run `python Tools/B01/W14-RECEIVER-001/world_upgrade.py plan --target <DISPOSABLE> --old-zip <PINNED_R2_INNER_ZIP> --new-zip <PINNED_R11_INNER_ZIP>`.
4. If its **all-23-file preflight** passes, run `... world_upgrade.py stage ...` against the identical immutable target and two exact archive bytes. It replaces exactly 5 files and adds exactly 2 without touching shared parent GUID.
5. Record emitted journal path. `python .../world_upgrade.py rollback --target <DISPOSABLE> --journal <ABSOLUTE_JOURNAL>` restores all old bytes. Source modified after staging blocks rollback rather than removing user work. Injected-failure test proves automatic rollback.
6. Actual Codex RC2 worktree remains excluded. There is **no force mode**, no arbitrary-version upgrade, and no switch that unlocks G2.

WORLD 1.1 retains `B01.MAPGEN001.Runtime` external dependency. ECONLEG guard requires **both** `B01.Legendary.Unity` and `B01.EconRebuild.Runtime` and authentic R3 reservation proof; never install guard alone as if usable. CAMERA R1.2.2 has a source parent meta GUID different from MAPGEN: preserve destination `Assets/B01Packages.meta`. BUILD R1.4 still requires original Codex negative/positive Unity contact-area regression to be rerun unchanged. NATIVE Unity acceptance tests, manual Windows/physical QA and current private Codex target HEAD/dirty/reservations are NOT_RUN/UNKNOWN here.

**Next Codex QA-only (not sent):** frozen BUILD R1.4 `95707c68...` Unity6000.3.14f1 native **32 Edit+6 Play** + actual unchanged RC2 nested/medium interval safety; CAMERA R1.2.2 `063e755c...` **17 Edit+15 Play** plus paired BASE/R1.1/R1.2 Light 720/1080 camera/actual first-hit/muzzle; WORLD v1.1 `5ce7092...` **7 Edit+5 Play** and seed7/31/83 actual R6; ECONLEG guard `e6c152f...` **4 authored Edit** with both original service assemblies and R3 authoritative proof. Independent B01-60 and B01-00 G2 required. No duplicate full Unity runs solely for unchanged SHA.
