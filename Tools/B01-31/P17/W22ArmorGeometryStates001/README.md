# B01-31 — PASS17 / WAVE22 — Modular armor 1×2×2, three *actual geometric* states

**Package:** `B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001/P17-W22-R1`. B01-31 **Concept Art & Visual Exploration**, [HQ W22 #713](https://github.com/C8LyNn/RoboRougelike/issues/713). This is a **genuinely new original module visual** for robot vehicle construction, not another W16–W20 split cable variant, W21 tri-jaw service gripper, material repaint, decal or stock image.

## Physically authored three original meshes, not a texture swap

Three separate actual binary glTF 2.0 models with matching world/mount pivot `(0,0,0)`, six original standard glTF base-color/metallic/roughness materials and a shared illustrative **1×2×2 build-grid envelope**. The dimension expresses source-art grid units; **not a claim about configured Unity source gameplay part sizes or collider scale**.

- `B0131ArmorModule1x2x2-OK.glb` — **204 actual original indexed 3D triangles**: full solid face armor plating, status strips, protected center, rubber edge, complete fasteners/frame.
- `B0131ArmorModule1x2x2-DAMAGED.glb` — **264 actual original indexed 3D triangles**: six physically separate irregular convex **torn** armor plate islands with gaps, bent amber flanges, exposed conductor, torn seal. Actual closed 3D thickness and new silhouette; there is no alpha shader trick.
- `B0131ArmorModule1x2x2-DESTROYED.glb` — **288 actual original indexed 3D triangles**: reduced shielding, six independent jagged 3D plate remnants, exposed copper ribs, ruptured seal and partial status light. Real removal of front-face coverage with a physically open appearance, not just orange/red paint.

The shared hub, mounting ring, fasteners and root pivots are preserved **by design consistency** across three *separate independently authored meshes*. The entire drawing is original procedural geometry, not modified copyrighted models. Each model is source-art only. No engine Collider, Rigidbody, damage handler, part-ID, stable item identity, cost, repair/free-rebuild authority, or automatic prefab has been authored.

**Original actual human review assets:** three original 1800×640 RGB PNG three-view boards (ISO, face, profile) physically drawn from the true 3D mesh triangles for every state, plus one 1800×650 actual three-state isometric comparison image. Actual machine-readable `states.json` and physical SHA256/byte `receipt.json`, and one reproducible **nine-member real ZIP**. This is *NOT* a Unity Editor screenshot or proof that Unity imports these files.

## Executable independent original mesh QA

```bash
python -m pip install --disable-pip-version-check "Pillow==11.3.0"
python Tools/B01-31/P17/W22ArmorGeometryStates001/build_armor.py --output build/B0131W22
python Tools/B01-31/P17/W22ArmorGeometryStates001/verify_armor.py --source build/B0131W22
```

The physical verifier **does not import the producer**: each actual glTF 2.0 binary is opened, and every real triangle `POSITION`/`NORMAL`/`indices` accessor read. Every actual vertex/face and outward normal must be nondegenerate and finite, all same root pivots preserved, all three original geometries truly differ, and the physical source axis spans stay within 1×2×2. It independently integrates real forward-facing (negative X) front armor triangle surface areas; **complete face coverage OK > torn face DAMAGED > open DESTROYED**; this distinguishes genuine damaged armor topology from cosmetic color swaps and provides measurable geometry QA. Four real RGB boards are re-opened with a pixel reader; each three-view/state crop must differ. Exact machine source SHA receipts and nine-member physical ZIP byte parity + CRC are required. **Eight deliberately malformed GLB semantics/headers must be rejected.**

CI executes original producer and verifier on Ubuntu24.04, then **re-renders *every* actual GLB/PNG/states/receipt/ZIP file twice and compares all file SHA byte parity**. An independent Windows2022 receiver downloads the actual Ubuntu-generated GLB/PNGs/ZIP and reopens them with read-only validator; Windows never calls the generator. **Do not claim PASS before actual logs**.

## W22 governance, scope, receiver & safe rollback

The source-only artifact has no causal effect on current release-blocking host P0. [HQ #713](https://github.com/C8LyNn/RoboRougelike/issues/713): frozen #510, #556, #596 and selected R4 #647 are untouched. Corrected host R5 [#690](https://github.com/C8LyNn/RoboRougelike/pull/690), canonical API [#691](https://github.com/C8LyNn/RoboRougelike/pull/691), storage/quarantine [#712](https://github.com/C8LyNn/RoboRougelike/pull/712), independent original source tests #707/#710, and Windows local Unity G1 [#667](https://github.com/C8LyNn/RoboRougelike/issues/667)/[#711](https://github.com/C8LyNn/RoboRougelike/pull/711) remain owned by their designated specialists. **No Save/Load, ownership lease, rollback, ID or Economy code is edited.** No alternative R4/API source mixed.

**B01-30/40 formal G0:** independently accept authentic original geometry and actual physical artifact SHA/ZIP, verify no foreign/`Assets/**` import overwrite in a *disposable approved* workspace. **B01-60 native G1:** if operator actually launches a valid Unity 6000.3.14f1 Windows Editor via reviewed #667/#711, inspect GLB importer availability, URP shader appearance, realistic perceived damage state and consistent pivot. An approved glTF importer or converter may be required; **until genuine logs, G1=NOT_RUN**. **B01-10/22/13:** optional human gameplay/repair readability critique only; no assumptions about free-rebuild cadence or anti-softlock outcomes. **B01-00:** future gated intake, never invented Codex ACK.

**Six lifecycle fields:** `IMPLEMENTED=YES_SOURCE | TESTED=EXACT_REAL_CI_PENDING | QA_ACCEPTED=NO | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`. Source art G0 pending actual physical CI, independent formal B01-30/40 G0 PENDING, native Unity G1 NOT_RUN, G2 HOLD, Codex ACK NO, integrator feedback NOT_MEASURED. **No auto merge**.

**Rollback:** delete only new `Tools/B01-31/P17/W22ArmorGeometryStates001/{build_armor.py,verify_armor.py,manifest.json,README.md}` and dedicated `.github/workflows/b01-31-p17w22-armor-3geometry-states.yml`; if explicitly authorized to import, delete only exact W22-namespaced `B0131ArmorModule1x2x2*` files in isolated disposable scratch. Preserve all other W16-W21 art, frozen game, foreign even-empty directories, and Codex code.
