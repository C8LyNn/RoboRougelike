# B01-40 — W14 / RC2 independent frozen package acceptance

**2026-10-26**, role B01-40 Integration & Build, authorization [HQ #71 W14](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6416680391). Owned changes only within new `Tools/B01/W14-RECEIVER-001`, this report and one scoped CI. **G2 HOLD, no merge, no live game import.**

## Actual unmodified physical producer ZIPs

Downloaded original Actions *outer* artifacts, opened inner ZIPs, CRC checked and independently hashed each file, .meta GUID, asmdef. Four incompatible original manifest schemas are parsed in `receiver_w14.py`; no producer SOURCE PASS copied from PR descriptions.

| Package | HEAD SHA | Whole inner source ZIP SHA256 | ZIP entries/bytes | Verified file checks | Unity .meta GUID |
|---|---|---|---|---|---|
| BUILD R1.4 [#260](https://github.com/C8LyNn/RoboRougelike/pull/260) | `95707c68c8071a949b5d9fec7ff57926e92f8198` | `62321794e8f91f1660c32597632b40b9bccdbab1becd3c7e750632f06f8714aa` | 60 / 155320 | 59/59 PASS | 24 |
| MAPGENWORLD v1.1 [#261](https://github.com/C8LyNn/RoboRougelike/pull/261) | `5ce70920478043210edbd78713f746b8d8ff431d` | `a27891c3b9420a3060190c613104121154ef1385c6d15354d2d68aa22f22802a` | 40 / 46467 | 39/39 PASS | 15 |
| CAMERA R1.2.2 [#262](https://github.com/C8LyNn/RoboRougelike/pull/262) | `063e755c50c9728f0a9627d25d3677280625a73d` | `ecf2211e317a35af7a726afe1b484e83e1b7a326faeb5aed3057fe6153b338ee` | 60 / 76032 | 59/59 PASS | 21 |
| ECONLEG guard [#263](https://github.com/C8LyNn/RoboRougelike/pull/263) | `e6c152f0795f105a94743018093b650d453e26a1` | `ec250b8446d89b219eba332244a318e5768e7240c2f02088f7041fa0ca9ecd3b` | 13 / 7379 | 13/13 PASS against **external** manifest | 9 |

Four-source package scan: zero GUID collisions across different paths, zero different-payload overlaps except the known different-byte shared parent `Assets/B01Packages.meta`, which must preserve the destination GUID; **no automatic canonicalization of child GUIDs**. BUILD asmdefs: 3 self-resolved. WORLD: 3 asmdefs with external `B01.MAPGEN001.Runtime`. CAMERA: 3 self-resolved. ECONLEG: 2 asmdefs referring to `B01.Legendary.Unity` and `B01.EconRebuild.Runtime`, which are **NOT INCLUDED** in the guard-only ZIP. Result is **PASS_STATIC_SOURCE** / not standalone Unity compile.

## New executable receiver code and actual tests

Implemented original-manifest normalizer/read-only validator `receiver_w14.py` and **WORLD-only byte-locked transactional updater** `world_upgrade.py`. No global force/overwrite mode. Only marked detached Git worktrees with an exact HEAD marker are writable. Every old WORLD asset byte is compared before any write. Upgrade has precisely **5 old-file replacements + 2 new source/meta additions**; rollback writes old payloads back and removes additions only after checking no external modifications; journal/backups are durable, failure injection triggers verified automatic restore. Wrong old/new source hashes, modified target, wrong HEAD, original repo, missing canonical parent meta, unexpected file scope abort. Existing PACKAGE-AUTO-001 from #238 remains unchanged.

Actual independent GitHub REST tree review against published RC2 #258 head `4f39b4487744f81b3e0075dabd15b67df2f07e25`: all **23/23 old WORLD assets byte-identical** to #243 v1.0.1 exact `5ce9d806d7fc7eb0ab2ef7b586626404c346174f`; 5 changed paths+2 additions in #261 verified via real Git blobs; RC2 has **0** runtime source paths under BUILD, CAMERA or ECONLEG guard. **This is *published* RC2 evidence, not the active private Codex game HEAD.**

Local unittest: **24/24 PASS**, including physical original outer/inner ZIPs and neg SHA/manifest/meta/path/casefold/GUID/dependency, real WORLD 5+2 stage/rollback, simulated interrupted upgrade after third file, protection of external edits, wrong HEAD/mismatched old asset, and original other-three-assets staging and rollback under the previously built and separately tested PACKAGE-AUTO-001. Those three are explicitly QA-only normalized from unchanged original verified asset bytes and are NOT new producer release artifacts.

Windows/Ubuntu GitHub CI status must be recorded from **actual W14 run** after publishing; no CI claim from the local run, no Unity native work by B01-40.

## Priority and owner boundaries

1. **P0 BUILD R1.4** native Unity6000.3.14f1 unchanged original 18 Edit+6 Play plus new 14 Edit (32+6) and **the original two R1.3 negative/positive nested socket area cases from RC2 #258**; exact true area `0.0484m²` / tiny `0.0016cells²` reject, 5 demo states × 720/1080. B01-23 owns source; new native Unity still `NOT_RUN` for #260.
2. **P0 CAMERA R1.2.2** native 17 Edit+15 Play then matched real Light W+LMB 720/1080 BASE/R1.1/R1.2 first-hit/muzzle/reticle A/B/C. Prior R1.2 functional Light aim FAIL 15.306/13.104°, causation unproven; B01-20 owns source.
3. **P1 WORLD v1.1** native 7 Edit+5 Play, generator seed7/31/83 replay, actual R6 physical and rollback. RC2 currently uses previous version; upgrader does not authorize host install.
4. **P1 ECONLEG Guard** needs complete pinned upstream ECON#252 and LEG#242 + R3 authoritative reservation proof, native 4 authored Edit cases, actual save/repair/0HP/transfer. No genuine Unity host-wide proof. B01-22 owns source.

RC2 Windows final ZIP `RoboRougelike_B01_UNIFIED_RC2_Windows_x64.zip` size 38,523,382, claimed SHA `7d35cd1bc823499a8855258e24ed0a0ff1d47e49bd694adb94c031ef8c946de6` is **NOT independently available** in this agent's filesystem, so `G0_RELEASE_BINARY=PENDING_BINARY_ACCESS`. Pre-existing W13 #264 binary-checker can validate it on operator PC; do not infer PASS from PR #258. RC2 first unfiltered Edit 1066/1068 FAIL + targeted fixture recheck2/2 PASS are separate revisions, no full green 1068/1068 evidenced; Play424/424 is *external Codex* evidence, not B01-60 independently run. Human OS UI acceptance and prior C0000005 still NOT_RUN/UNRESOLVED.

**G0_SOURCE=PASS for four exact immutable source archives; G0_TARGET_LIVE=PENDING_ACK; G0_RELEASE_BINARY=PENDING_ACCESS; G1_INDEPENDENT_NATIVE=NOT_RUN; G2=HOLD.** No queue dispatched or active game worktree modified.
