# B01-40 / Independent RC1 receiver — 2026-10-26

**Status:** SOURCE G0 PASS (published commit comparison), CAMERA R1.2 SOURCE G0 PASS, **G0_TARGET active Codex PENDING**, G1 INDEPENDENT PENDING, B01-00 G2 HOLD.

## Independently verified real source provenance

Compared GitHub REST **actual recursive Git blob trees** at exact: RC1 final game `86eb7bb77e6bfc7787f781ea1ccda547b8a4f643`, MAPGEN `5be31039411f89f17654b1c332f8692883fa03cc`, MAPGENWORLD `de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7`, OBJ BASE `4c9236a4cdc27814ea2e7679872ec4a524150cc8`. Source-integrity receipt fixed under #248 report head `5c29e8e28e7b34fedf0c992fca49451828b02314`.

- MAPGEN **35/35 original asset+meta Git blob SHA1 identical** in final RC1 commit.
- MAPGENWORLD **23/23 original asset+meta Git blob SHA1 identical** in final RC1 commit.
- Six protected areas **byte-identical** with base: Assets/Pack (38 files), Assets/R3 (22), Assets/R6 (28), Assets/scene 1.unity (1), Packages (2), ProjectSettings (25).
- `Assets/B01Packages.meta` Git blob **`38bd6da78864e817f6c8905aa0805a83528d2a25`** matches both frozen producers in RC1, with its GUID preserved.

## CAMERA R1.2 new source #247 / external native #249

Downloaded physical Actions producer artifact #12610554389, checked real inner source ZIP: **41 entries / 46,544 B / SHA256 `d00e3a3f4aea9ce15a8ecc4d95838fcc5a42329060568da63d0e2a72817cbd9d`**, **40/40 actual per-file SHA256, byte lengths and Git blobs**, **16 distinct GUID**, **3 valid asmdefs**, no missing Unity .meta, no scope escapes or ZIP path traversal. Camera source's shared parent `Assets/B01Packages.meta` GUID `b01c0000000000000000000000000001` differs from RC1 canonical MAPGEN `b01a0001000000000000000000000001`; **never blanket-overwrite**.

Actual Codex QA #249 separately observed native compile exit0, Edit5/5 Play13/13, camera own-ray and wall intersection regression checks PASS; nevertheless two Light tracking rows FAIL and final Windows diagnostic process exits2. **G1 functional FAIL_OBSERVED, B01-60 INDEPENDENT pending, no CAM integration G2.**

## RC1 binary source of truth & blockers

The RC1 PR #248 publishes two Windows ZIP paths **on owner's C: drive** and digest claims, but neither actual raw ZIP is supplied in a downloadable GitHub artifact. Cannot independently hash or extract them from this isolated receiver: **PENDING_BINARY_ACCESS**. An existing `native-summary.json` is producer/external evidence, not binary transfer. Do not report binary G0 PASS. RC1 previously observed hidden-window UI shutdown `C0000005` (`-1073741819`) while final visible automation exits0, so the shutdown fault is **OPEN**, not a successful release qualification. Human full game session/PC1-PC2/manual FPS also NOT_RUN.

## Iteration and independent tests

New executable script `Tools/B01/RC1-RECEIVER-001/receiver_gate.py` with positive + negative selftests, fixed handling of mutated compressed ZIP (`zlib.error`) and valid Windows retail directories (without unsafe ZIP paths). Native Unity was NOT_RUN by B01-40. No game, Codex source, producer package, or independent B01-60 QA code modified. B01-40's only owned paths are RC1 receiver tool, its CI, and this report.

**Next actions:** 1. B01-00 assign CAMERA aim-tracking contract defect to corresponding camera/weapon owner; 2. owner provides actual two Windows ZIP bytes for independent receiver G0 release hash; 3. B01-60 review and human test; 4. B01-00 separate G2 decision. No automatic dispatch/integration/merge.

**Additional executable transaction proof:** locally used original #238 PACKAGE-AUTO-001 against the actual CAMERA R1.2 ZIP (not merely a duplicate ZIP parser); detached synthetic Unity6/URP worktree with existing MAPGEN parent GUID, `24` package-owned files stage/restore PASS, parent preserved, injected failure after 3 files auto-rollback PASS; wrong pinned hash rejected. Local complete suite **15/15 PASS**. This does not upgrade live target G0.
