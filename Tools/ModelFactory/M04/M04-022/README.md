# M04-022 — Mikrohover ślizgowy (glide microhover)

**Source:** `Docs/ModelFactory/CATALOG_V1.md` on `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M04-022.json`, original CREATE commit `9e323fc01b2c04d357c3bf68b64c49b406f63d49`.

## Geometry and appearance

Visual-only single-disk microhover with a reinforced low-profile lift pancake, continuous cyan ion-emitter halo, twelve radial emitter vanes, eight armored perimeter tiles, an inset ceramic hub, twin outer composite skids, a suspended titanium cassette, copper power bus, side gimbal bearings and amber warning bars. Eight differentiated PBR materials; emissive surfaces do not substitute for geometry.

**Coordinate contract:** Blender +Z up, 1 unit = 1 metre. Main attachment root `(0,0,0)` lies at the underside chassis plane `Z=0`. All geometry is below that plane. Disk tilt pivot `(0,0,-0.55)` m, rotation about local X. The 121-frame / 30 FPS animation follows 0° → +10° → 0° → −10° → 0°. It is an illustrative loop, not a hover gameplay implementation.

**Collision envelope:** Moving disc and rim occupy a conservative `|X| <= 0.45 m` at every tilt pose; the skid runner's inner face begins at `|X| = 0.4975 m`, leaving at least 47.5 mm lateral clearance. The tilted disc occupies approximately `-0.69 <= Z <= -0.41 m`; skids sit near Z=-0.83 m and the chassis cassette stays above the disc. Intentional contact exists at gimbal bearings and mount interfaces. These analytical envelopes are not a continuous swept triangle-triangle collision proof.

## Reproduce

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-022/generate_m04_022.py -- --out build/M04-022
python3 Tools/ModelFactory/M04/M04-022/qa_m04_022.py --glb build/M04-022/M04-022.glb --report build/M04-022/qa.json
```

Generator clears the scene and unused model data before each build, creates meshes/materials/animation, saves `M04-022.blend`, exports animated `M04-022.glb`, and writes `manifest.json`. Independent QA checks GLB header, materials, meshes, mount and animated pivot, exported quaternion tilt extremes, and side-clearance bound. Workflow installs Blender and NumPy and reopens the native BLEND.

## Boundaries and acceptance

No Unity scene, prefab, ProjectSettings, or other agent's modules are changed. **Unity import / EditMode / PlayMode / URP materials / physical collision / visual review: NOT RUN** until actually executed in a suitable Unity environment. Passing analytical envelope checks does not imply a full collision or physics PASS. Keep PR in DRAFT pending independent Unity integration and visual assessment.
