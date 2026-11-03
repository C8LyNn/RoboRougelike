# B01-31 · P17 / WAVE33 · REAL existing R41 v5 Editor rig correction (candidate-only)

## Exact canonical boundaries

- Existing playable game: [Codex #873](https://github.com/C8LyNn/RoboRougelike/pull/873) → [#877](https://github.com/C8LyNn/RoboRougelike/pull/877), no B01-31 gameplay changes or alternative weapon.
- Original v5 adapter: B01-31 [#893](https://github.com/C8LyNn/RoboRougelike/pull/893) → [#904 @ `ba92a52a9bb73fb63b17e94a3af4a199868ef92e`](https://github.com/C8LyNn/RoboRougelike/pull/904) → own [#914 @ `b611776842bb336722144b09a062d22abe823549`](https://github.com/C8LyNn/RoboRougelike/pull/914). W33 is a **versioned successor of own #914**, not a new independent turret/Save/project.
- Independently reproduced source P0: [B01-30 #909 @ `f8517d85549cdfbd2049a33d1881c843a68eee76`](https://github.com/C8LyNn/RoboRougelike/pull/909), tested original unchanged W31 Editor at SHA256 `34e1f8d54cf340cb839659764493b3e73a7dafda438876fab7eb3c09c3a8ce8a`. Former `copy.transform.SetParent(pitch, true)` parented **the whole imported turret including fixed base under pitch**. #909 SOURCE negative detector 4/4 real Win/Ubuntu CI jobs succeeded; **this certifies red witness, not fixed source/Unity**.
- Producer FBX SHA256 `fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533` is **external** and NOT supplied here; no fake FBX/GLB/model or screenshots.

## W33 real code delta (existing Editor producer, not a second system)

The *same* existing `Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs` is corrected. Existing SHA path validation, `bakeAxisConversion`, W31 *before-instantiate* MonoBehaviour/Joint and marker rejection, original +Z dual adapter muzzles, 0.55m grid geometry, visual-only clone physics stripping, output-once safety, original fallback all remain.

The original Unity imported hierarchy is admitted **only when clearly separable**:
1. one unique `R41_Yaw`, one `R41_Pitch`, original `R41_Pitch` must be a descendant of `R41_Yaw`; both `Muzzle_L` and `Muzzle_R` are under Pitch;
2. at least **one existing source Renderer in each separate segment**: outside yaw (fixed mount), inside yaw but outside pitch (rotating housing), inside pitch (elevating barrel). If labels/ownership/renderers are ambiguous → named refusal **before any clone**, rather than guessing or tilting an entire turret;
3. on separately instantiated copy only, move `SOURCE_R41V5_VISUAL_NOT_GAMEPLAY` to candidate **root** (fixed base). Move original `R41_Yaw` visual subtree to wrapper `R41_ADAPTER_YAW`, then original `R41_Pitch` subtree to `R41_ADAPTER_PITCH`, preserving world-space neutral pose; this avoids double or entire-root elevation;
4. check fixed base position and rotation invariant during original **4 yaw × 3 pitch** dual marker loop, in addition to existing source imported marker ↔ wrapper position comparisons. Reject on mismatch; never auto-rescale, rewrite FBX or silently ignore obsolete source pivots.

**Actual Unity model outcome still unverified:** the original external v5 FBX may not present these three distinct renderer branches in the required names/locations. That is an explicit `SOURCE_RIG_INCOMPATIBLE` acceptance blocker, **not** evidence that visual geometry is acceptable. Do not bypass named failures to show a demo.

## Tests and evidence (separate static/native/real-model status)

- New **4 Unity Editor NUnit [Test] sources** in existing owner W30 TestAssemblies assembly: 3 tests of source safety on disposable original Unity primitives (positive 3-segment, missing/wrong hierarchy negative, muzzle/absent fixed mount negative), and 1 **real external SHA-gated v5 import** calling original `CreateCandidatePreview` to check fixed base/world orientation, separate yaw and pitch, dual markers across 12 poses and zero clone Rigidbody/Collider. The 3 fixture tests **are not model-import tests**; if original v5 FBX missing, the real test is `Assert.Inconclusive`, not native PASS.
- Independent #909 already has **2 native actual-v5 FBX tests** checking root remains outside Pitch and static over -20/0/+10/+20 degrees. Do **not** duplicate or override #909 branch. At accepted integration, B01-60 selectively includes these 2 tests alongside 4 prior W30 + 4 prior W31 + 4 W33, then captures real Unity 6000.3.14f1 `Editor.log`, NUnit XML and exact selected source SHA; fixed mount renders still require screenshot inspection.
- W33 deterministic Python source gate checks exact parent/changed paths, original editor base SHA256, source-order/scope, Unity GUID uniqueness and six intentionally corrupt/removed-guard mutations. It runs on GitHub Win/Ubuntu. **SOURCE check only**; static script success is not Unity compilation and cannot grant G1.
- Original single gameplay `Assets/R4_1/R41PartGeometry.cs` still has one `Wylot` at +Z 1.8m; v5 visual adapter has two muzzles at ±0.06m X / +Z 0.70m / pivot Y 0.135m. **No two-shot firing mode authorized**; Codex/B01-00 decide actual projectile origins/cadence. No changes to #915/#910 V7 ECON, #913 Save schema, #834 anti-softlock, #877 garage, #861 RotarySaw.

## Acceptance and rollback

`IMPLEMENTED=YES_W33_EXISTING_EDITOR_SOURCE | SELF_SOURCE_QA=PENDING_EXACT_CI | B01-30/40_FORMAL_G0=PENDING | UNITY_G1=NOT_RUN | REAL_V5_FBX=EXTERNAL_ONLY | WINDOWS_PLAYER=NOT_RUN | GAME_INTEGRATED=NO | RELEASED=NO`.

**Gates:** B01-30 and B01-40 perform fresh independent exact W33 G0 (not inherited #904/#909 cert); B01-60 runs real original external v5 in licensed Unity6000.3.14f1, actual no-tilt visual tests, gun mount/projectile/own-hull obstruction, and extracted Windows x64 Player. B01-00/Codex may only approve later optional intake after full accepted G0/G1. Keep old R41 AutomaticGun untouched throughout.

**Rollback:** on authorized isolated checkout, omit W33 new documentation/QA/CI files and restore the **sole W33 modified existing owner Editor source** to exact parent #914 blob `eefcd49cafb25dea69e6f14a1f21706ea5eacd75`. Do not delete other agents' files/directories or mutate the external FBX, #904, #914, #877, main/develop or user's live worktree.
