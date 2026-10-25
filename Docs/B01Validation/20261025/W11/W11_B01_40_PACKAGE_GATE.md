# B01-40 — W11 / independent package integrity and five gate results

**ACK B01-40** — source and receiver gate controller only. Review date 2026-10-25. No live game import, Codex worktree edits, merge, release or actual Codex integration handoff.

## Exact immutable revision register

| Package | Exact Git SHA | Source PR |
|---|---|---|
| MAPGEN R3.3 | `7334f3b1d8cd3a976098faa4e683f4de3d9a592f` | [#225](https://github.com/C8LyNn/RoboRougelike/pull/225), full #99+#103+#211+#225 |
| B01-21 MAPGEN-DIAG | `73bf4316cae566072b2a23a04510d7c4e23e37d4` | [#228](https://github.com/C8LyNn/RoboRougelike/pull/228), diagnostic only, base R3.2 |
| CAMERA R1.1 | `d48108f56d3fe0790ad715683e77b2de9219e4b1` | [#214](https://github.com/C8LyNn/RoboRougelike/pull/214) |
| BUILD R1.2 | `489676414a3eb45fe70124098e3778a81d0aaff2` | [#215](https://github.com/C8LyNn/RoboRougelike/pull/215) |
| RAC R3.1 | `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e` | [#216](https://github.com/C8LyNn/RoboRougelike/pull/216), full #13+#135+#216 |
| LEGENDARY .NET R1 | `7e9d9ca0a0ef6d4c9bf1ce39fdb187b0d960958d` | [#230](https://github.com/C8LyNn/RoboRougelike/pull/230) |
| LEGENDARY design R0 | `d5f6bfb9e6c019aadcaec2ae70ca3390c3923f61` | [#229](https://github.com/C8LyNn/RoboRougelike/pull/229) |
| Published Codex OBJ target | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` | [#210](https://github.com/C8LyNn/RoboRougelike/pull/210); published reference, **NOT LIVE TARGET ACK** |

## MAPGEN R3.3 — G0_SOURCE REAL BYTE-LEVEL PASS

B01-40 downloaded [actual producer artifact #12596980172](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187335381/artifacts/12596980172), extracted, parsed and independently rehashed:
- **Outer GitHub artifact ZIP:** **64,544 B**, SHA256 `4cad43d9381d92541b287cbd33e54a599da649db971cf4343b4c3f9f5078b918`.
- **INNER `MAPGEN001_R3_3_FULL_SOURCE_AND_QA.zip`:** **71,667 B**, **56 unique paths**, SHA256 **`384ef6909c1ebcb361c55a6b23b22eb105b2aaaed0270e229cbad37a250155a0`** — exact requested hash independently verified on the actual inner ZIP, not confused with outer wrapper.
- **50 source/docs + 5 QA tools = 55/55 actual extracted files** match recorded **SHA256 and byte counts**. Their Git blob SHA1 also matches the immutable Git source tree; the 56th file is the manifest itself, with out-of-band checksum to avoid self-hash recursion. No duplicate/unsafe ZIP names, no symlink entries.
- **20/20 unique .meta GUID**, all Unity source/test/asmdef/scene .meta pairs present; MAPGEN demo MonoScript GUID points to the correct MapGenDemo C# meta. **3 valid asmdef** (runtime + Edit + Play) with no duplicated direct TestRunner reference; **9 Unity C# preprocessor balances PASS**.
- Owned `Assets/B01Packages/MAPGEN001.meta` has been restored in this archive (R3.2 omission fixed). The two headless .NET C# files live under Tools and intentionally have no Unity .meta.
- Producer .NET math-surrogate [CI 40187335381](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187335381): 1015/1015 cases PASS; this is **not Unity G1**. Separate B01-21 diagnostic [PR #228](https://github.com/C8LyNn/RoboRougelike/pull/228) executes 27/27 orthogonal headless diagnostics on **R3.2**, with zero MAPGEN source/doc path overlap, not a Unity R3.3 signoff.

**G0_TARGET caution:** published OBJ #210 has no MAPGEN/B01Packages paths, but current unpublished Codex target branch/paths and true importer ACK are unknown. Cross-package metadata overlap found: MAPGEN `Assets/B01Packages.meta` GUID `b01a0001000000000000000000000001` versus CAMERA #214 **same path** GUID `b01c0000000000000000000000000001`. Never replace target's established shared parent .meta; preserve existing target GUID and both package-owned subfolder GUIDs. This concrete path conflict must be resolved in receiver dry-run. **G0_TARGET=PENDING**.

## MAPGEN R3.3 — independently sourced REAL NATIVE FAIL

Codex [QA PR #232](https://github.com/C8LyNn/RoboRougelike/pull/232) exact QA HEAD `b80faa121e4022d772caa3b93d8b2a482d9a53af` executed full source R3.3 on published OBJ210/PHY125 with licensed Unity 6000.3.14f1:
- **Compile PASS native exit0**; **EditMode 24/24 PASS exit0**.
- **PlayMode 6 PASS / 12 FAIL (18 cases), exit2**. All **12 individually measured primitive AddForce cube drives FAIL** near first waypoint with approximately 0.56 m displacement. This is an actually executed source-test failure, *not* evidence the real R6 vehicle physics must also fail. Root cause of cube stall has not yet been established.
- Minimal standalone **URP Player builds** but runtime generation **FAILS** `MAPGEN001_URP_SHADER_NOT_AVAILABLE`; dynamic `Shader.Find("Universal Render Pipeline/Lit")` and Simple Lit unavailable in built Player, although Editor shader test passes. This is a **confirmed Player dependency/retention defect in a minimal host**, with suspected shader stripping but exact mechanism unproven. Require a serialized, retained URP material/shader reference or rigorously validated ShaderVariantCollection/AlwaysIncluded strategy plus genuine minimal AND target Player verification; generated geometry must render visibly without magenta.
- The R3.3 4-case follow-up true R6 driving in that QA was **NOT_RUN**. Older Codex R3.2 PR #226 physically passed **12/12 base + 7/7 Heavy** with mounted R6-wheel vehicles using API steering, but results are **not portable** to R3.3.
- B01-60 independent Unity verdict for this R3.3 SHA is **not published** in latest #70. Codex receiver tests do NOT replace B01-60.
- B01-20 W11 producer ACK is read-only awaiting explicit B01-00 START; no new accepted R3.4 source/ZIP yet. A new minimal producer fix, exact SHA, rebuilt verified artifact and independent B01-60 regression is required before considering G2.

**MAPGEN: G0_SOURCE=PASS; G0_TARGET=PENDING; G1_NATIVE=FAIL; G1_INDEPENDENT_QA=PENDING; G2_HANDOFF=HOLD.**

## CAMERA, BUILD, RAC — inherit immutable W9 audits (no redundant rehash)

[Existing B01-40 W9 independent report](https://github.com/C8LyNn/RoboRougelike/blob/audit/b01-40-w9-package-gates-20261025/Docs/B01Validation/20261025/W9_B01_40_PACKAGE_GATE.md) checked **111/111 source SHA256**, **50 unique .meta GUIDs**, all manifest Git SHA1/bytes and preprocessor/asmdef RED→static-GREEN checks. **PR heads unchanged** since that audit. Keep existing [W9 installation/rollback and ready Codex phase0 prompts](https://github.com/C8LyNn/RoboRougelike/blob/audit/b01-40-w9-package-gates-20261025/Docs/B01Validation/20261025/W9_B01_40_INSTALL_ROLLBACK_CODEX_PROMPTS.md).

| Package | G0_SOURCE | G0_TARGET | G1_NATIVE at fixed SHA | G1_INDEPENDENT_QA | G2_HANDOFF | Main missing evidence |
|---|---|---|---|---|---|---|
| CAMERA #214 | PASS_STATIC with 37/37 SHA256 sidecar | PENDING Codex live P12 camera/reticle owner and GUID ACK | NOT_RUN; original pre-fix source compiler failed | PENDING B01-60 | HOLD | licensed Unity compile/Edit/Play, Light/Heavy hull/360/zoom/720/1080; avoid dual camera owners |
| BUILD #215 | PASS_STATIC with 37/37 SHA256 sidecar | PENDING R3 mounting, PHY, garage and visual #55 scope ACK | NOT_RUN; original pre-fix source compiler failed | PENDING B01-60 | HOLD | licensed Unity socket hub/base tests, XYZ, orphan 0HP, real Raycast and garage |
| RAC #216 | PASS_STATIC with 37/37 SHA256 sidecar | PENDING actual OBJ/race/checkpoint owner ACK | NOT_RUN; original missing #endif compiler failed | PENDING B01-60 | HOLD | Unity Edit/Play, real AddForce/floor contact, ordered checkpoint/wrong-actor/multicollider/reset |

W9 immutable sidecars have source SHA256. The producer manifests' SHA256 fields remain null and must be consolidated or explicitly accompanied by B01-40 sidecars at eventual handoff. No current B01-60 native PASS was found for these corrected exact sources. **G2 cannot be issued based on W9 static results.**

## LEGENDARY R1 — .NET prototype verified; NOT Unity integration

[Producer PR #230](https://github.com/C8LyNn/RoboRougelike/pull/230), exact SHA `7e9d9ca0a0ef6d4c9bf1ce39fdb187b0d960958d`: **7 source+workflow files**, **38,928 B**, zero Unity Assets changes. Independently fetched and SHA256 hashed **7/7 files**, matching producer `source-sha256.txt` exactly. Actual downloaded [.NET CI evidence artifact #12596144631](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187129527/artifacts/12596144631) **2,667 B / SHA256 `7dcfb3a0a182218339a170b2104efb70b3a7342ded78ece99c2f632e6f044c08`**; CI [run 40187129527](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187129527) build PASS and **25 distinct test PASS / 0 FAIL**, including 2,000 seeded transitions. Independent B01-40 executable rerun **NOT_RUN** (container lacks .NET SDK). These are legitimate **producer .NET tests**, not native Unity cases.

**R0 [PR #229](https://github.com/C8LyNn/RoboRougelike/pull/229) vs R1 contract mismatch:** weapon mass **650 kg vs 220 kg**, hover mass **780 kg vs 150 kg**, design JSON `legendaryDefinitionId/basePartKind=Weapon/partFamily/mountFootprintCells/hitPoints/socketCategory/ability` vs prototype `Id/BasePartKind=Gun/WeaponFamily/Footprint/MountTag/ProposedEffect`. R0 hover family `Hover` vs R1 null weapon family + HoverEngine. Requires explicit owner/B01-10 arbitration and versioned schema migration. Live R3 `R3PartState.Id` stays physical instance authority, R3 `Parts/Inventory` stays sole inventory. R3 removal of dismantled 0HP may discard physical ownership. R1 Railgun and 2 weapon families differ from historic game family/gun cap. Neither physical hover nor penetration, actual mass or mounts are implemented.

**LEGENDARY: G0_SOURCE=PASS_PROTOTYPE_7_OF_7; G0_TARGET=BLOCKED_SCHEMA_AND_LIVE_TARGET_ACK; G1_NATIVE=NOT_RUN_UNITY; G1_INDEPENDENT_QA=PENDING_B01_60; G2_HANDOFF=HOLD.**

## Five-gate W11 summary

| Package | G0_SOURCE | G0_TARGET | G1_NATIVE | G1_INDEPENDENT_QA | G2_HANDOFF |
|---|---|---|---|---|---|
| MAPGEN R3.3 | **PASS** (55 hashes + real ZIP) | PENDING | **FAIL (cube 12/12 and URP Player)** | PENDING | **HOLD** |
| CAMERA R1.1 | PASS_STATIC W9 | PENDING | NOT_RUN | PENDING | HOLD |
| BUILD R1.2 | PASS_STATIC W9 | PENDING | NOT_RUN | PENDING | HOLD |
| RAC R3.1 | PASS_STATIC W9 | PENDING | NOT_RUN | PENDING | HOLD |
| LEGENDARY R1 | PASS .NET prototype | BLOCKED_API/ACK | NOT_RUN Unity | PENDING | HOLD |

## W11 updated source-gated installation/rollback/prepared prompts

- [MAPGEN INSTALL_FOR_CODEX.md](MAPGEN001/INSTALL_FOR_CODEX.md) — full source/ZIP SHA, 20 GUIDs, shared parent .meta, Unity/URP Player failures and rollback.
- [MAPGEN PROMPT_FOR_CODEX.md](MAPGEN001/PROMPT_FOR_CODEX.md) — **only prepared PHASE 0 diagnostics**, not a sent game integration command.
- [LEGENDARY INSTALL_FOR_CODEX.md](LEGENDARY001/INSTALL_FOR_CODEX.md) — purely .NET domain prototype, not a Unity install.
- [LEGENDARY PROMPT_FOR_CODEX.md](LEGENDARY001/PROMPT_FOR_CODEX.md) — **prepared read-only PHASE 0** API compatibility review, no install.
- CAMERA/BUILD/RAC remain pinned at W9 with matching installation, rollback and prepared Codex prompts. No duplicate writing/retesting unchanged files.

## Dispatch/recommendation to B01-00 / #69 / #79

1. **MAPGEN first queue candidate stays P0 but REWORK/HOLD**, not READY. B01-00/HQ must explicitly authorize B01-20 start for narrowly scoped cube driver and URP Player shader retention fixes, new immutable R3.4, fresh 55+ file manifest/ZIP/GUID audit; do not hide failures by increasing timeouts or teleporting.
2. **B01-60 independent QA mandatory:** new source Unity compile/Edit/Play, 12 cube cases, Player visible nonmagenta, 12 genuine R6 Light/Heavy plus 7 Heavy historical cases, XML/log/process/screenshot; run separate CAMERA/BUILD/RAC corrected native tests as scheduled. Lack of independent receipt is never PASS.
3. **LEGENDARY:** B01-10/owner settle schema, mass and Railgun/family rules first; B01-22 isolated .NET negative cases; B01-60 independently reviews ownership and 0HP/replay; no new Unity implementation without authorization.
4. **Codex live target:** demand exact branch/HEAD, worktree clean/dirty/unpushed and reserved path ACK, cross-package shared meta collision resolution; published OBJ210 is not sufficient. Only after G0_TARGET and G1/B01-60 PASS may B01-00/HQ issue G2.
5. **No merge, active game import, release, deployment, GPT OSS duplicate routing or dispatched integration command.** Final all packages: `READY_FOR_CODEX=NO / SENT=NO / CODEX_INTEGRATION_ACK=NO / G2_HANDOFF=HOLD`.

**B01-40 final recommendation:** MAPGEN **SOURCE INTEGRITY VERIFIED / NATIVE REWORK REQUIRED**; CAMERA/BUILD/RAC **STATIC VERIFIED / NATIVE QA BLOCKED**; LEGENDARY **.NET DOMAIN PROTOTYPE VERIFIED / UNITY API INCOMPATIBLE UNTIL ADAPTER**.
