# TEST_PLAN — executed vs outstanding

**Producer actually executed locally:** 13/13 Python unittest PASS, processing *real* pre-existing M04 exported GLB. Independently checks SHA256 of real 854456B input, output SHA256 ab26d649e31a5ea4d21320480c52b3b9781bd7f3752a687f265c24bbf473df4b, 105 unchanged mesh definitions, 8 materials, 4 animation channels, unchanged GLB BIN chunk, unchanged trimesh scene AABB, 3 inert anchors and negative errors for malformed, duplicate, wrong root etc. Six orthographic PNG silhouette/geometry projections created from actual 29,472 triangles; NOT native Unity shaded screens.

**M01 actual binary:** on pinned GitHub branch, original GLB Git blob 8776da637b22fb50361f0fff025b2dbdc586d16b, new actual Git blob d1b551fbb1849f88b143e0c55fcc166ecfd19e1c, real edited GLB SHA256 795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd (JS SHA256 cross-checked on abc known vector), parsed real meshes=53, materials=7, animation channels=1, three typed inert marker empties.

**CI workflow:** authored source and test runner, downloads original M04 artifact 12586794828, validates known source SHA, builds actual new binary and two-model ZIP, tests negative cases, emits checksums. Job outcome NOT_YET_VERIFIED until Actions concludes.

**NOT_RUN:** new Blender FBX export, Unity import/EditMode/PlayMode, 720p/1080p actual URP Player, narrow-phase continuous triangles, host physics collider tests, mass/COM calibration. AABB does NOT prove actual triangle non-intersection.

**Known FAIL/BLOCK:** M01 3.672m visual longitudinal length does not fit proposed footprint 1.10m depth. No configured live authoritative MobilityHub socket yet. Do not approve G2.
