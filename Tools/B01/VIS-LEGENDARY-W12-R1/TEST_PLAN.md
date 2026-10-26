# TEST_PLAN — executed vs outstanding

**Producer actually executed locally:** 13/13 Python unittest PASS, processing *real* pre-existing M04 exported GLB. Independently checks SHA256 of real 854456B input, output SHA256 ab26d649e31a5ea4d21320480c52b3b9781bd7f3752a687f265c24bbf473df4b, 105 unchanged mesh definitions, 8 materials, 4 animation channels, unchanged GLB BIN chunk, unchanged trimesh scene AABB, 3 inert anchors and negative errors for malformed, duplicate, wrong root etc. Six orthographic PNG silhouette/geometry projections created from actual 29,472 triangles; NOT native Unity shaded screens.

**M01 actual binary:** on pinned GitHub branch, original GLB Git blob 8776da637b22fb50361f0fff025b2dbdc586d16b, new actual Git blob d1b551fbb1849f88b143e0c55fcc166ecfd19e1c, real edited GLB SHA256 795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd (JS SHA256 cross-checked on abc known vector), parsed real meshes=53, materials=7, animation channels=1, three typed inert marker empties.

**CI workflow:** authored source and test runner, downloads original M04 artifact 12586794828, validates known source SHA, builds actual new binary and two-model ZIP, tests negative cases, emits checksums. Job outcome NOT_YET_VERIFIED until Actions concludes.

**NOT_RUN:** new Blender FBX export, Unity import/EditMode/PlayMode, 720p/1080p actual URP Player, narrow-phase continuous triangles, host physics collider tests, mass/COM calibration. AABB does NOT prove actual triangle non-intersection.

**Known FAIL/BLOCK:** M01 3.672m visual longitudinal length does not fit proposed footprint 1.10m depth. No configured live authoritative MobilityHub socket yet. Do not approve G2.

## R1.1 source-independent animation E2 plan

CI executes `python3 tools/swept_geometry_qa.py --glb <edited.glb> --output <output.json> --samples 121` on both reworked GLBs, checking all actual glTF positions under each node's full parent TRS and all animation channels with LINEAR/slerp interpolation. Test math with analytical translation, rotation, scene nesting, invalid accessor length, non-LINEAR rejection, real M01 and generated M04 combined clip cases. See `tests/test_swept.py`.

At first successful R1.1 run, M01 sampled extent 1.172×0.9125×3.672m; M04 *combined* sampled extent 1.57756×0.884306×1.57756m. For M04, four separate single-channel clips also retained. **Sampling 121 poses is not continuous BVH triangle/Unity Collider proof**: high-frequency narrow overlap between sample times remains NOT_RUN; `MaxDistance`/suspension/COM/hover behavior NOT_RUN. Unity model importer, URP shader/material and mounted camera silhouettes still NOT_RUN.

CI R1.1 source run [40292486059](https://github.com/C8LyNn/RoboRougelike/actions/runs/40292486059) **SUCCESS**: 20/20 stdlib tests at each of two pipeline stages (before M04: one M04 integration test SKIP due missing CI input; after download and retrofit: all 20 executed), 121-source-sample pose evaluation for each clip, precise M04 fifth combined 4-axis pose. Deterministic two-build ZIP byte comparison PASS on run. Explicit Unity CI native jobs require credentials and stop before editor; EDIT/PLAY NOT_RUN.
