# B01-12 · PASS 17 / WAVE 34 — REAL GAME GARAGE NON-PROJECTILE ZONE R3

**REAL integration branch parent:** CODEX [#877@a0de194fd382830fab4b7d013d204b0697b25078](https://github.com/C8LyNn/RoboRougelike/pull/877) (containing [#873](https://github.com/C8LyNn/RoboRougelike/pull/873) EMP, actual R5 JumpPad, integrated W27 #866 five-zone overlay and genuine R3 weapon mounting gate). This is **NOT** a new browser simulator, alternative GameObject UI, placement validator or new Save writer. It directly revises the **already included B01-12-owned** original Unity overlay/test files in a new isolated descendant branch. Do not copy all of #864 into the newer Codex receiver: that version has a different live red/blue and socket presentation.

## Actual source regression

In original W29 integrated `B0112GarageSpatialOverlay.AddFunctionalEnvelope(root,kind)`, orange Rotary Saw swept disc was emitted only when `root.GetComponent<R3MountedPart>()?.Weapon==RotarySaw`. The **unmounted garage ghost has no `R3MountedPart`** (original `R3GarageView.CreateGhost()` makes a primitive/payload, without installing a real mounted-part component). Thus a **new Rotary Saw preview** shows violet weapon hints but **omits its orange moving-blade clearance zone** — the exact scenario #877 now prohibits through its authoritative `R3BuildValidation` physical disc gate.

**Fix:** feed original `R3MountedPart.Weapon` for genuinely installed parts and original `R3GarageView.MountWeapon` for the selected uninstalled ghost into the ONE existing `AddFunctionalEnvelope(root,kind,weapon)` code path. A saw shows its orange informational disc in preview; Basic/EMP do not gain a fake saw disc. The default hint dims (0.28m thickness × 1.8m diameter) are the actual original WPN005 default `bladeHalfThickness=0.14` and `bladeRadius=0.9`. This visual hint remains advisory: only #877 `R3BuildValidation.Validate` can reject physical mounting.

Two minor corrections in this **same live code**: use reusable `GetComponentsInChildren(..., List<T>)` buffers for 3 traversal hot paths (mesh/weapon endpoints/BUILD001 sockets) during 10Hz rebuild, and `DestroyImmediate` for the Awake primitive when in EditMode. No changes to existing tint, red/blue presentation, 256 priority, BUILD001 sockets, lookups, purchase/weapon guards, `Save`, `Scrap`, or physics. No changes to Codex `Assets/R3/**` or frozen sources.

## Test evidence and exact manifests

`python Tools/B01/B01-12/P17W30GarageSpatial/source_gate.py`:
1. Exact immutable original #877 Git blob and current W30 production files with strict Git blob+byte lengths.
2. Reproduction of R1 RED in the real original C# and real original garage ghost creation.
3. Correct current source wiring for installed vs uninstalled saw; absence of basic/EMP hijack; hint geometry consistent with real WPN005 default.
4. Reused buffers and EditMode disposal, preservation of original `PlacementIssue` authority, unchanged 4 canonical R3/WPN005 host Git blobs.
5. **12 authored Unity NUnit Editor cases**, including NEW functional direct invocation of original production `AddFunctionalEnvelope` on an uninstalled ghost (and Basic negative), exact hint pose, cleanup. **None have been executed in Unity by B01-12**.
6. Dual-OS read-only workflow and deliberately altered copy of both production C# and test C#, each **must exit 2 FAIL_CLOSED**.

These are **SOURCE-ONLY** static checks and original-byte witnesses. They are **not proof of compilation, real 3D Player render, functional purchase, physics or G1**. The existing #873 inherited workflow pins the older 5-zone producer blob; if it fails due to a deliberate author-owned overlay delta, B01-00 must requalify the original source-intake SHA manifest rather than silently tamper with its pinned original. This branch does NOT update Codex-owned source-intake code.

## How to verify in actual Unity 6000.3.14f1

After independent B01-40 source G0 on this **exact** descendant commit and on an **isolated, authorized** checkout, run original 12 Unity EditMode tests and complete garage PlayMode/Windows Player smoke. Create real saw Ghost, point at attached armor where the disc intersects own part: **orange saw swept zone appears**, existing #877 authoritative validator denies with red explanation, Scrap/ID unchanged. Rotate/move to an open exterior grid cell: orange zone tracks muzzle and valid placement succeeds. Compare real Basic/EMP ghost to confirm they don't display Saw disc. Inspect 320/720/1080/4K GUI and actual large-vehicle profiler allocations, shadowing/URP materials; verify #873 JumpPad and EMP still work. Preserve real Editor.log, NUnit XML, Windows ZIP SHA, Player.log and exact gameplay SHA. Tests/API-driven are not a human playtest.

**Status before those gates:** IMPLEMENTED=SOURCE, SOURCE_CI=PENDING, G0=PENDING, UNITY_G1=NOT_RUN, GAME_INTEGRATED=NO_NEW_W30_INSTALL, BUILD=NOT_RUN, G2=HOLD, CODEX_ACK=NONE. No merge/release. A failed native test means HOLD, never fake PASS.

**Receiver:** B01-40 independent G0 + previous #873 receipt migration review, B01-60 genuine native Unity/Player, B01-00/Codex authorized newer game integration. Rollback only this descendant branch's 2 modified original B01-12 files and 3 add-only QA files; preserve parent #877, active code and data.

## W31 exact game successor + version policy

Current Codex #877 head `a0de194fd382830fab4b7d013d204b0697b25078` supersedes W30 `9634b1e5b4576aca4304d1d5b4c3d01446737650` with ONE revised canonical source gate for both incoming mounting and batch restoration (original `R3BuildValidation.cs` and `R3RestorationValidation.cs`). B01-12 checked the original immutable host blobs and both original calls to `FunctionalWeaponPlacementIssue`/`ExistingMountedWeaponPlacementIssue`. These are structural cross-source assertions only; reverse-mount and repair physics still need actual G1. This R2 is a **rebased successor of existing #889**; original R1 `6bf055461137a31ea5832808f3cc675c630bec09` remains immutable in Git history and its branch is updated only through verified force-with-lease. No cherry-pick of competing #887 or overwriting original Codex #877. Parent #877 is the exclusive gameplay baseline. 

**W31 blockers unchanged:** original #873 W29 provenance checker pins the older game-overlay blob, and must be requalified by B01-00 after independent G0 (do not disable it); Unity EditMode+PlayMode / actual Windows Player tests NOT_RUN. Existing original #877 runtime gameplay gate is not edited by this B01-12 update. **Source tests are not native functional acceptance.**

## W32 product correction: weapon-type-specific spatial warnings

**Selected current game parent: Codex #877 @ `3bd9b159997924dcecb4e1cd1d41137896e5199e`.** This is R3 of SAME existing #889 (not a competing garage UI), extending original R2 while preserving original R3/physical restoration authority and original W29 gameplay. The previous version displayed two directional projectile-looking purple volumes for EVERY gun type, including EMP and Rotary Saw, despite the selected R3BuildValidation explicitly exempting EMP radial area and handling Saw by a circular rotating disc. This gives players inaccurate fitting expectations.

Actual original `B0112GarageSpatialOverlay.AddFunctionalEnvelope` now distinguishes:

* Projectile/beam types: original local purple barrel clearance and *informational* long purple shot line unchanged.
* Rotary Saw: only the original orange 0.9m-radius physical disc hint (not a purple projectile). The genuine R3 physical Saw placement gate is still authoritative.
* EMP: a compact purple SOURCE/emitter marker (0.55m cube) ONLY, **not** an invented 9m physical mounting exclusion or fake straight projectile. The real EMP attack radius and line-of-sight is governed by its already existing gameplay owner.

**Test changes:** three additional genuine original Unity Editor NUnit methods invoke the production C# `AddFunctionalEnvelope` using an unmounted weapon ghost: real EMP with `Wylot` has exactly one compact source marker, Saw with `Środek piły` shows only an orange disc, Basic retains exactly two purple near+far indications. A separate source test validates the public weapon discriminator. Total now **15 authored native NUnit tests / 0 native executed**. Strict Git manifest/provenance source gate includes the unchanged exact current game's R3/Restoration/WeaponConfig blobs and adds W32-specific R3 authoritative classification + original method/test assertions. Dual-platform GitHub source checks and mutated-code fail-closed do NOT assert actual Unity compilation/rendering/Player/G1.

**No scope expansion:** No modification of `Assets/R3/**`, Codex shared validator, EMP gameplay pulse, Saw physics, ECON V7, Save writer, menus, GUIDs, protected branches or user computer. One PR with six owner-scoped files, no merge. W29 inherited source provenance checker still requires owner-authorized new-version requalification after independent G0; do not disable it.

**Gates:** IMPLEMENTED=SOURCE | SOURCE_CI=PENDING_ON_NEW_SHA | G0=PENDING_B01-40 | UNITY_G1=NOT_RUN | WINDOWS_PLAYER=NOT_BUILT | GAMEPLAY_ACCEPTED=NO | G2=HOLD | INTEGRATED_IN_CODEX_FINAL=NO | RELEASED=NO. Reviewer B01-60 native Unity EditMode15 plus scene PlayMode, real EMP/Saw/Basic garage ghost graphical verification and no economic mutation; B01-00/Codex integration/provenance requalification when approved.

## W34 superseding delta — active preview visibility under a 256-zone cap

**Confirmed original-source starvation path:** The W32 observer appended the CURRENT
ghost's `AddFunctionalEnvelope(source.Preview,source.MountKind,source.MountWeapon)`
only **after** `foreach(var p in assembly.Parts)`. A large R3 Standard/Heavy build
may fill `MaxBoxes=256` with installed wheel/track renderers, mechanism hint boxes,
and BUILD001 socket markers before the chosen Saw/EMP/Basic candidate's functional
hint can be appended. This is a source-established ordering defect, not a claim that
a full native 139-part Player stress test has been executed.

**W34 repair within the SAME B01-12 MonoBehaviour and PR #889:** Draw actual
host-authorized selected occupied cells first, THEN reserve current candidate
Saw/EMP/Basic visual warning slots, THEN optional green nearby sockets, THEN
installed-part hints. No new renderer, writer, collider, physical validator, 256
cap, wallet, IDs, Save, or #903 status copy. A red rejected physical candidate
remains red; orange/purple hints stay informational and cannot override
`R3GarageView.PlacementIssue`. The current part's hints now cannot be starved
by later installed-part geometry.

**Tests:** 16 genuine Unity EditMode NUnit source cases authored in the original
suite (formerly 15), including a new bounded 256+40 excess installed-hint
test invoking the original private production envelope and box append through
reflection. W34 exact-source guard adds historical OLD-order RED witness
and requires cells < candidate < sockets < installed parts. The NUnit case
establishes bounded first-in prioritization; source guard pins the actual
RebuildReadout ordering. Neither substitutes for activated Unity 6000.3.14f1
Editor/PlayMode and real Light20/Standard83/Heavy139 garage visual/Player QA.

**Handoff:** Independent B01-40/61 exact new HEAD G0 and B01-60 native
Editor/NUnit/Windows Player, including candidate Saw/EMP/Basic near 256 visual
boxes and physical 256-hit validator deny. Original W29 Codex intake provenance
still expects the old overlay blob and will legitimately fail until Codex
re-ACKs and re-pins an approved newer source. Never bypass that gate.
The dynamic whole-world Save #923 and V7 next-victory settlement are separate
P0 owner tasks; do not represent them as completed by this visual-only patch.
Rollback: omit latest own #889 commit; no other agent's code changes.
