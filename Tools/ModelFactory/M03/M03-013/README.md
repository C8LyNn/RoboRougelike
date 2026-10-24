# M03-013 — Osłona progu dolnego (lower sill guard)

**Agent:** MODEL-03 (M03). **Catalog:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M03-013.json` on that control branch, CREATE commit `32226480d40515a42ec9276ee807e7370ce6de30`. **Scope:** standalone modular visual asset for Unity 6 RoboRougelike; no gameplay, shared scenes, ProjectSettings or prefabs modified.

## Mechanical concept

A 1.8 m long, 0.63 m wide underbody sill guard with a central graphite armor spine, segmented side plates, **four actual through-bores** in mounting tiles, four hollow copper collars, eight **thick raised rivet heads** with separate shanks, two rubber impact rails and three ceramic skid/wear runners. Two amber service markers show fore/aft orientation. The 5 independent PBR material roles are graphite steel, ivory ceramic, copper fasteners, dark rubber and amber markings. Heavy rivets are real geometry, not painted decals.

**Coordinates:** 1 unit = 1 m; local +Y up, +Z vehicle forward, X across vehicle. Pivot `MOUNT_BOTTOM_FRAME` is at (0,0,0), the top plane of the four mounting collars. The model extends downward to Y=-0.183 m. Four SNAP locators: `SNAP_BOTTOM_L_Aft`, `SNAP_BOTTOM_L_Fore`, `SNAP_BOTTOM_R_Aft`, `SNAP_BOTTOM_R_Fore` at X=±0.195 m, Y=0, Z=±0.500 m. All four bores have 32 mm radius. The two Z positions are 1 m apart; there is no new grid or mount API. Static asset: **no animation required** by catalog.

**Contact/clearance:** The collars sit on the top face of the bored tiles and preserve the open bores. Rivet shanks contact the lower face of solid armor filler sections; rivet heads touch the shanks without volume overlap. Ceramic runners touch the armor underside; rubber rails touch the outside side-plate boundaries. Thick rivet heads remain at least 13 mm from the rubber rail inner edges and are separated in Z from mounting holes. Coplanar contact between distinct mesh parts is intentional mechanical assembly, not a physics mesh. The static mesh is not itself a collision prefab.

## Files

- `generate_m03_013.py` — dependency-free procedural watertight OBJ/MTL/JSON export, plus Blender `bpy` GLB/FBX/BLEND export.
- `qa_m03_013.py` — independent verification of actual OBJ face winding, positive mesh volume, watertightness, four real bores, rivets, materials and byte-for-byte deterministic regeneration.
- `.github/workflows/ci-m03-013.yml` — static QA and real Blender exporter workflow; publishes downloadable artifacts if successful.

## Commands

```bash
python3 Tools/ModelFactory/M03/M03-013/qa_m03_013.py
python3 Tools/ModelFactory/M03/M03-013/generate_m03_013.py --out build/M03-013
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-013/generate_m03_013.py -- --out build/M03-013
```

**Unity:** OBJ is immediately usable as static geometry; GLB requires a compatible importer and FBX is natively supported. Blender rotates the Y-up procedural root to Z-up for Blender authoring and GLB exporter conversion. Confirm transform/pivot and mesh scale in a real Unity 6 scene before integration. This package does not contain colliders, physics parameters or scripts.

## Verification boundaries

Do not treat source assertions as evidence of a Unity import. The CI workflow executes the actual generator and independent OBJ parser, then installs Blender and checks the exported GLB structure. Blender success is established **only by a completed green workflow run**. Unity EditMode, PlayMode, visual inspection of the complete assembly, material tuning and real vehicle attachment: **NOT RUN** until explicitly executed. The 32-segment rivets and real bore edges are generated geometry, not texture illusions.
