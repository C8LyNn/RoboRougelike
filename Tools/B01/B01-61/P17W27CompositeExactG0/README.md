# B01-61 / PASS17 WAVE27 — SINGLE EXACT COMPOSITE SOURCE G0 RECEIVER

**Purpose:** Codex [#79](https://github.com/C8LyNn/RoboRougelike/issues/79) confirmed missing composite G0. This is a real independently executable Git-object source-closure builder, verified deterministic ZIP and receipt, **NOT a new Save implementation, not a Unity game binder, not a Codex install**. Source of truth is [HQ #800](https://github.com/C8LyNn/RoboRougelike/issues/800) and [shared rules #799](https://github.com/C8LyNn/RoboRougelike/issues/799).

## One canonical selected path (immutable ref pins)

| Role | Original PR | Exact unchanged SHA | Source intake |
| --- | --- | --- | --- |
| Canonical path security, exclusive .owner | #787 | `f5db40bf293fe9582ec7c6fcb3515aeff6fcf937` | EXACT original Store+Session *REFERENCE_ONLY*, not an active second writer |
| One world+V7 wallet+victory+loaner Save writer | #797 | `2b7983f0f9e09dd1d56906b379a3f7af7b02f21a` | Only two new real C# source files, **Tools-only awaiting explicit Unity asmdef + typed Game Binder** |
| Physical Unity old-world velocity rollback | #792 | `16cb2a7b5158605b15191b6074f9e392e0d1ebd3` | Exact full Unity physical package + 4 native test fixture source |
| Approved V7 economy policy + one binary section | #780 | `b4df10a7ad7e6c758f8331013f6b216b3a3689a3` | Original Unity `.meta`, runtime asmdef, policy+section; no second economy file writer |
| Original R5 corrected ColdRestart fixture | #750 | `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb` | Exact selected R5 native PlayMode fixture + unchanged host |

Original unchanged dependencies: #510 `d94400d95d13db7f27e1533df05c9292d5380431`, #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, #729 `b8ca29c7707cd6b4338d0b9772d74dee676d49d2`. Importing entire #797 branch **forbidden**: it contains older V1/V6 and candidate #787 raw writer; importing #787 raw Store with #797 full writer would create two Save authorities. Competing #749/#773/#753/#741/#793/#764 hold. The actual physical Save stays one original #729 `.owner`, one generation CAS and one `save.envelope`.

## Run once (isolated Linux or Windows GitHub Actions)

```bash
python Tools/B01/B01-61/P17W27CompositeExactG0/composite_intake.py --out dist/b0161-w27-composite
```

The tool constructs source assets *only from Git blobs at pinned exact original producer SHAs*, verifies original Git blob identity/SHA256 and critical named pins, checks every `.meta` GUID collision and sidecar plus every asmdef dependency within selected source, checks original frozen and V7 economics byte provenance, and emits:
- `COMPOSITE_MANIFEST.json`: each file path, exact source producer SHA, Git blob and SHA256, explicit reference-only #787 files and named active writer #797, expected source-vs-native gates, Unity/asmdef set and missing binder.
- `B0161_W27_EXACT_SOURCE_ONLY.zip`: deterministic cross-OS full source with `SELECTED_SOURCE/Assets/...` and `SELECTED_SOURCE/SourceOnly/Tools/...`, plus namespaced `B01-61/W27/COMPOSITE_MANIFEST.json` (NEVER root `PACKAGE_MANIFEST.json`).
- `SOURCE_RECEIPT.json`: exact ZIP SHA256/byte size, counts and real fresh disposable safe-stage/rollback negative checks.

**Physical safety tests:** fresh empty scratch receiver writes exact bytes; rerun refuses occupied; foreign empty dir is preserved even during owned rollback; empty foreign receiver denied; dirty owned file triggers *atomic refusal of entire rollback* rather than deletion; symlink receiver denied if OS supports test symlinks, otherwise explicitly NOT_RUN. No writes to user game, active Codex worktree, shared save/persistentDataPath, or protected branches. The tool NEVER performs installation or game migration.

## Safe human/Codex intake — must be deliberate and separately authorized

1. Obtain exact PR HEADs and **inner** ZIP hash from CI receipt, not GitHub's outer workflow artifact ZIP SHA.
2. Select a new empty disposable staging directory, independent from the active Codex worktree and its Save. Unpack `SELECTED_SOURCE` there only; verify every file SHA256 against manifest, check Unity .meta GUIDs and project asmdefs; refuse any file, symlink, nonempty/foreign empty directory collision.
3. **DO NOT overlay `Assets/B01Packages.meta`** if it already exists; compare original GUID and hash first, retain host owner metadata and obtain explicit staged resolved closure decision. Same for original frozen #510/#556/#729 and existing game scenes.
4. Explicitly author and test one Unity asmdef adapter for #797 two `SourceOnly/Tools` source files + #780 V7 references, one authorized typed binder to #792 actual `Rig[]` stage and trusted game Core/wallet, and a single #729 OS owner + #797 Save writer. DO NOT import the reference-only #787 raw store as another writer.
5. Do not load or rewrite existing user Save data. Legacy `B0162S01`→`B0162G02` and older V1 migration requires user-authorized backup-first new version with negative rollback, no automatic conversion.
6. Real licensed Windows Unity 6000.3.14f1 with Editor.log, NUnit XML and exact source SHA must test #750 ColdRestart, #792 4 physics cases (real velocities), one-owner Save→new Editor process→cold restore world+economy, same-gen different wallet, ACK loss, partial-root retirement, valid distinct victories, paid-zero quote, one-shot loaner. Without this, `NATIVE_G1=NOT_RUN` and composite GAME_G0/G2 HOLD.
7. CODEX acknowledges only exact package/inner ZIP hash, target isolated branch, reserved file paths, unresolved GUI/physics/single-writer ABI and test evidence in #79. This QA PR alone is not a Codex offer, ACK, integration or release.

**Six gates:** IMPLEMENTED=SOURCE_TRANSPORT_QA; TESTED=actual Actions when complete; QA_ACCEPTED=SOURCE_CLOSURE_TESTED_SCOPE_ONLY; READY_FOR_INTEGRATOR=NO_FULL_GAME; INTEGRATED=NO; RELEASED=NO; G1=NOT_RUN; G2=HOLD. B01-40 formal G0 receiver, B01-41 OS/security peer, B01-60 native QA, B01-00 authorized owner, Codex actual integrator. No automatic background or PC activity.
