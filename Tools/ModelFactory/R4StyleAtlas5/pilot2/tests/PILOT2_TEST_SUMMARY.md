# Pilot 2: testy na wyeksportowanych GLB (numpy)

Wynik techniczny: **8/8 typów z kompletem 5/5 stylów PASS** (40 wariantów). Unity: UNITY_NOT_RUN. Odbiór artystyczny: **OCZEKUJE NA WŁAŚCICIELA** (testy techniczne go nie zastępują).

 T1 files      GLB + LOD1 GLB load; triangle counts equal the Blender QA record; LOD1 <= LOD0; materials are paint zones
 T2 envelope   rest-pose geometry (FX nodes excluded) inside the declared envelope (collision outline of the type)
 T3 swap       the five styles of one type expose IDENTICAL node names, hierarchy and socket / port empties (position dev < 0.05 mm)
 T4 animation  every variant carries animation channels in the GLB for each animated object of its QA record
 T5 textures   A, B, D: every painted material has baseColorTexture + metallicRoughnessTexture + normalTexture and keeps the zone paint
               colour / metallic / roughness as FACTORS; E: painted base-colour ramp only; C: no images (both by design)
 T6 different  the five meshes of one type are different geometry (no two styles share a vertex-set hash)
Technical tests only - the artistic acceptance of the pilot is a separate decision of the owner.

| asset | T1 A-E | T2 | T3 | T4 | T5 | T6 | wynik |
|---|---|---|---|---|---|---|---|
| R4B_Cube | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4C_STRUT_STANDARD | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4D_ExplosiveTrap | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4M_HoverEngine | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4M_WheelM_Side_Offroad | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4W_Basic | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_Battery | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
| R4X_ExplosiveTrapDeployer | PPPPP | PPPPP | P | PPPPP | PPPPP | P | PASS |
