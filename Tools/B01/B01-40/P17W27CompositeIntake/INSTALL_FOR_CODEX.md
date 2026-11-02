# B01-40 W27 — safe intake of selected five source packages (REVIEW ONLY)

## Human summary
**Contains:** source bytes at fixed PR SHAs (#787, #797, #792, #780, #750), per-file SHA256/Unity GUID and dependency manifest. The ZIP is NOT a playable Unity project and is NOT a ready-to-install Save component.

**Not verified:** Unity 6000.3.14f1 full compile of #797, native Rigidbody PlayMode, live combat/garage Save+Economy binding, migration of existing player Save, destructive rollback, actual end-user feel.

## Strict dependency order
1. Original immutable game #510 and binary snapshot #556 (W06 stable Light/Heavy IDs).
2. Original #690 R5 physical owner host and #729 canonical Gateway/physical `.owner`.
3. #750 overrides ONLY the #690 ColdRestart NUnit test fixture, generated g4→g5. #732 is a competing historical correction: **DO NOT** overlay.
4. #792 (new physical port) replaces #772 two changed owner files and retains other #772 asset bytes. Must replace old private #510 scene host rather than run both; missing authorized binder remains BLOCKED.
5. #780 ECON V7 standalone **section**, not an independent writer; verify 65% floor, purchase1->paid0/free=false, win+loaner IDs and replay.
6. #787 is the exclusive lineage/OS owner proof and selected canonical path R3. **Do not install #787 raw public store beside #797.**
7. #797 is the **ONLY candidate full world+economy writer** under the one original #729 `.owner`. Its current net8-only methods cause Unity API compatibility HOLD; do not attempt a live installation until a versioned, tested, exact-SHA repair is independently accepted.

## Scratch-only receipt process, no Codex worktree or user Save changes
- Obtain source at exact SHAs from manifest (not moving PR head). Verify `package-sha256.json` against the physical inner ZIP and `PACKAGE_MANIFEST.json` per-file SHA256.
- Before extraction, check all entries are regular files under `SOURCE/` or `REVIEW_ONLY/` (or the two root text receipts). Refuse absolute paths, `..`, symlinks, duplicates, conflicting `.meta` GUID and asmdef names, unusual scripts.
- Extract to an exclusively NEW empty, short-path, user-chosen disposable scratch directory **outside** any live game, editor/cache, persistentDataPath or active Codex worktree. Do not flatten separate source manifests into one `PACKAGE_MANIFEST.json`.
- Preserve original gameplay project and player Save. Never run original producer rollback on a shared folder. Any foreign file/EMPTY folder, symlink, unexpected mutation, dirty byte or collision means **STOP**. Quarantine the disposable scratch and do not delete unknown content.
- G0 transport PASS permits isolated review, not direct Assets overlay. Native Unity requires an **owner-approved versioned binder**, mutually exclusive writer and independently approved migration with a tested backup+restore.
- Read Codex target snapshot `f92c873bb1e397a598b133e107ae704e15f86768` as *inspection only*. Obtain refreshed actual target SHA/scope ACK before any import. Files absent from a published SHA may still exist locally; ask Codex, don't assume.
- B01-60 should conduct opt-in licensed Unity 6000.3.14f1 G1 with actual Editor.log, NUnit3 XML, exit codes and hashes, including cold restart, Rigidbody velocity rollback, one-loaner entered/spent and duplicate victory/wallet same-generation rejection.
- Return to B01-00 with exact final SHA and independent B01-60 QA before a **new** #79 handoff; current W26 is already received. No second offer without real new version and explicit owner authorization.

## Failure and rollback contract
No merge, no protected push, no user-PC action. Revert by closing isolated QA Draft or discarding exclusively owned scratch **only if exact original-owned bytes and no alien files/directories exist**. Rollback of Git does not roll back player Save; no player Save is touched by this package.

**Status pending independent CI:** SOURCE_TRANSPORT_G0=PENDING; Unity ABI=HOLD; G1=NOT_RUN; FULL_GAME_INTEGRATED=NO; G2=HOLD; CODEX_ACK_NEW=NONE.
