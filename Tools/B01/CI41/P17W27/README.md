# B01-41 · PASS17 / W27 · COMPOSITE SOURCE INTAKE / R1

**User-visible effect:** none. Independent CI receiver for one selected pending Save/world/ECON V7/physical-source set. This is **not** a replacement gameplay implementation, a game installation, a Save migration, or Unity PhysX validation.

## Immutable scope

| Function | Selected producer | Exact original SHA |
| --- | --- | --- |
| Strict owner/canonical Save root | #787 | f5db40bf293fe9582ec7c6fcb3515aeff6fcf937 |
| One complete world+wallet envelope (V7) | #797 | 2b7983f0f9e09dd1d56906b379a3f7af7b02f21a |
| Rigidbody stage/rollback | #792 | 16cb2a7b5158605b15191b6074f9e392e0d1ebd3 |
| V7 economy binary section | #780 | b4df10a7ad7e6c758f8331013f6b216b3a3689a3 |
| Cold-restart fixture | #750 | 60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb |

Frozen original dependencies: #729 (b8ca29c7707cd6b4338d0b9772d74dee676d49d2), #556 (33094f154c7b91119b36d58aab285e6e83c5de57), #510 (d94400d95d13db7f27e1533df05c9292d5380431). Historical #749, raw #773, #753, #741, #794, #793 and V6 #764 MUST NOT be independently co-installed.

## What CI actually tests

1. Git commit-object existence, exact selected PR head and base, all changes constrained to producer-owned paths, 8+8+10+16+4 path counts, and original critical Git blob IDs.
2. #797 manifest references #787 and exact two original V7 producer blobs from #780. One save.envelope and no auto migration; checks #792 parent and unchanged native NOT_RUN state.
3. Strict POSIX case-sensitive Save root guard, no independent V7 file writer, current Rigidbody rollback source tokens and R5 g5 fixture reference (static regressions, NOT integration tests).
4. Original selected Unity asset closure from producer/dependency commits; original Unity GUID uniqueness, asmdef name uniqueness, exact raw path and casefold collisions; deterministic verified source ZIP.
5. Ten independent unit negatives covering traversal/absolute paths, duplicate archive entries, source mutation and missing or foreign members.
6. Windows 2025 and Ubuntu 24.04 jobs producing original stdout, JSON, SHA and archived evidence.

**The catalog ZIP is REFERENCE ONLY, NOT INSTALLABLE.** It intentionally contains the strict-store source alongside the proposed unified Save source for forensic review. NEVER copy the entire ZIP to Unity; it is not a single co-installable gameplay assembly. No game-facing binder or ABI acceptance is claimed.

## Receiving sequence

**B01-40 / B01-61:** pin five exact SHA again; compare current producer heads; review intended source closure and dependencies; validate symlink/dirty/foreign-file safe scratch and strict rollback (without deleting foreign empty directories); choose ONLY one Save writer and one original #729 lifetime OS .owner. The #780 V7 section belongs inside #797, never beside a separate wallet writer. Original #787 strict-store path is reference/dependency, not a second active Store. No implicit migration or legacy mixing.

**B01-60:** only with owner opt-in run real licensed Unity 6000.3.14f1 isolated PlayMode PhysX, actual restore, collisions, wallet and rollback tests; archive Editor.log, NUnit XML, process exit and source SHA. Source Actions or synthetic CLR shims NEVER equal Unity G1.

**B01-00:** independent G0 and native G1 outcomes decide future Codex handoff. No offer or ACK is implied by this CI41 branch.

## Status

| Gate | State |
| --- | --- |
| CI41 original-source receipt | PENDING verified Actions run |
| Independent composite G0 | HOLD until B01-40 and B01-61 |
| Unity native G1 | NOT_RUN |
| Game-facing bridge and Save integration | NOT_VERIFIED |
| Codex Inbox #79 | NOT_OFFERED |
| G2, merge, release | HOLD / NO / NO |

Rollback: close this Draft and delete only isolated B01-41 paths/CI artifacts. Never touch protected branches, current Codex worktree, or user Save data.
