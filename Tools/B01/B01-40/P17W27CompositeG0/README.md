# B01-40 — P17/W27 — exact selected composite SOURCE-G0

This is **executed QA and transport tooling**, not a Save implementation. It never edits a producer PR, current Codex worktree or a real player Save. Source transport may be accepted independently of a **blocked game ABI**.

## One exact dependency set

Read [dependency-manifest.json](dependency-manifest.json). The current selected source IDs are:

| Proposed role | Exact immutable owner PR | Commit |
|---|---|---|
| Strict canonical physical one-owner Store contract | #787 | `f5db40bf293fe9582ec7c6fcb3515aeff6fcf937` |
| **Only** complete world+ECON V7 Save writer | #797 | `2b7983f0f9e09dd1d56906b379a3f7af7b02f21a` |
| Unity real Rigidbody physical stage+rollback R2 | #792 | `16cb2a7b5158605b15191b6074f9e392e0d1ebd3` |
| ECON V7 bounded binary section | #780 | `b4df10a7ad7e6c758f8331013f6b216b3a3689a3` |
| R5 real Unity ColdRestart fixture g4→g5 | #750 | `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb` |

Immutable API #729, physical #510 and Save #556 are pinned by SHA in the manifest. Codex inspected a clean prior receiver base `feature/pass17-reload-fix@f92c873bb1e397a598b133e107ae704e15f86768`; its **active** reserved W26 receiver is `feature/w26-model-intake`. Never assume its latest local HEAD or edit it. #799 collaboration rules apply.

## Executable independent source-G0

`intake.py --out evidence/source-closure.json` operates on exact Git objects available in one **separate isolated checkout** with full Git history. It checks each original PR's exact SHA, ancestry, owner-only path diff and commit status, three immutable root owner blob sets, source-only single-writer selection, selected binary V7 original #780 Git blob identity, corrected R5 NUnit fixture, strict R3 case-sensitive root, Unity `.meta` GUID uniqueness (including 9 new physical and 5 V7), internal asmdef links and serialized YAML m_Script closure. The resulting namespaced manifest includes **actual** SHA-256 and bytes for each selected source. No hand-written imaginary hashes.

It physically stages source *only* into a brand-new unique temporary folder, refuses to roll back if a source file was modified or a foreign EMPTY directory appeared, and preserves separately created user file+empty directory. No destructive shared-file cleanup. Both Windows2025 and Ubuntu24.04 run the same tests; outputs must have byte-identical manifest SHA256. The manifest's SHA and CI run logs are source QA evidence, **not** Unity NUnit XML or proof that gameplay works.

### Important: independently confirmed game ABI failure

Codex reproduced **actual** Unity 6000.3.14f1 Editor compiler failures from #797 source API use: `SHA256.HashData` CS0117, `Convert.ToHexString` CS0117, `File.Move(string,string,bool)` CS1501. [Exact target Editor evidence](https://github.com/C8LyNn/RoboRougelike/pull/797#issuecomment-6588699104). Our test independently pins the exact offending original source symbols, reports `UNITY_797_TARGET_API_REPRODUCER_P0_RED_CONFIRMED`, and does **not** classify that expected RED as a successful Unity integration. B01-62 must version an ABI-compatible patch preserving real same-directory atomic replacement, durable `Flush(true)`, OS exclusion and lost-ACK quarantine. No silent delete-then-move substitute. After new SHA, rerun all gates and revise this manifest. **Do not offer #797 as Unity-ready**.

## Safe handoff / G2 HOLD — instructions for Codex and integrator

1. **Do not install automatically.** Compare the current Codex receiving base and reserved paths against the exact manifest. If target differs from `f92c873...`, stop for fresh scope ACK. Never touch the active `feature/w26-model-intake` worktree or player's `persistentDataPath`.
2. Use an **additional, freshly named, disposable, empty Git worktree / Unity project clone** and a **separate test Save root**. Source objects must be read by exact SHA, not moving branches. Reject occupied paths, symlink/junction/reparse roots, mismatched bytes, duplicate GUID, missing asmdef, serialized script refs or README/manifest collision. Recheck exact source SHA after transport.
3. Take only selected author-scoped Unity Assets from #510/#556/#729/#750/#792/#780; do not copy owner package root `PACKAGE_MANIFEST.json` over another owner's manifest. The runtime source of #787 is an **authority boundary reference**, not a separate `CanonicalSingleAuthorityStoreV3R3` to install next to #797 `WholeGameEconomySlotV2`. Only one selected `.owner`, one same-run/slot+generation CAS, one full file, one writer.
4. **Blocked until versioned owner fix:** #797 provides source-only `CanonicalWholeGameSessionV2` / `WholeGameEconomySlotV2`, no Unity asmdef or game-facing scene binder; do not insert it into the live runtime or replace original Save. #792 standalone physical replacement does not hydrate private original #510 `Rig[]`; only an authorized versioned binder may connect it to actual Unity combat/garage, preserve velocities on failed stage and verify physical root retirement.
5. **Migration is not authorized by source G0.** For any future migration, first record source and target full Save hashes, make private byte-identical backup, test restoration, reject ambiguous/corrupt/torn writes, and preserve original data on any error. NEVER clean existing user folders by source rollback function or delete a live Save.
6. After the exact **new** compatible producer SHA receives independent B01-41/61 and B01-40 G0, B01-60 should run actual activated Unity 6000.3.14f1 on an **explicitly owner-consented** isolated Windows candidate: import/compile, EditMode, four #792 physics PlayMode, #750 R5 ColdRestart, one full world+wallet V7 cold save/restore, two simultaneous contenders, different case-only path under POSIX, loss of ACK before/after rename, duplicate victory/loaner and 0-PAID edge, rollback momentum, no softlock. Collect `Editor.log`, actual NUnit3 XML, exit codes, exact candidate commit/artifact SHA; otherwise `G1=NOT_RUN`.
7. Rollback must delete **only byte-verified files owned by the new disposable stage**, never shared or foreign content. No merge to main/develop, no release, no auto-install. A missing ACK from Codex is `NONE` even when an issue comment exists.

## Gates and responsibility

- Source-only transport G0: **CI-dependent**. Its PASS does not imply the next gate.
- Full product/API G0: **HOLD (confirmed target Unity compiler error, binder missing)**.
- Native Unity G1: **NOT_RUN** for the complete selected package.
- G2/Codex delivery: **HOLD; no re-offer of already received packages**.
- Release: **HOLD**.

NEXT_OWNER: **B01-62** for versioned #797 Unity ABI fix; **B01-20/23/62** for new actual typed Unity bridge; **B01-41/61** for independent exact-new-SHA hostile source tests; **B01-60** for native operator test; **B01-00** for single-candidate selection and Codex handoff. B01-40 maintains only this source transport gate.
