# Hero Benchmark Pass 4: testy nowych modeli na wyeksportowanych GLB (numpy, bez Blendera i bez Unity)

Wynik: **PASS** — modele 17/17, zestawy pancerza 6/6. `hero1/` i `hero2/` bez zmian: **TAK**. Unity: **UNITY_NOT_RUN**.

 P1 nodes      contract node names and parents identical to the baseline (cosmetic mechanism nodes included: Pass 4 keeps the mechanism set)
 P2 mounting   every node (pivots, sockets, muzzle, port empties) at the baseline rest position (dev < 0.05 mm); identical ports and envelope record
 P3 outline    rest-pose geometry (FX excluded) inside the declared envelope (<= 1 mm); pbuild status BLENDER_PASS, no violations
 P4 animation  identical clips, events, fps, length; every animated node of the baseline is animated; key values of all shared channels agree
 P5 new        the model is NEW geometry: its mesh is not the baseline mesh (vertex count, triangle count and bounding boxes per node differ) and
               the file is not byte-identical to any Pass 1 / Pass 2 file
 P6 files      GLB + FBX + LOD1 GLB + LOD1 FBX + .blend + QA record exist; textures (base, ORM, normal, emission) exist; source .blend exists
Also checked: hero1/, hero2/ untouched. Hull kits: K1 every mesh name the hull-layer rules can ask for exists; K2 plate footprint inside the cell
pitch and not thicker than 60 mm; K3 fairings inside their cell + 200 mm nose reach; K4 files (GLB, FBX, source .blend, four textures).

| model | wzorzec | P1 | P2 | odch. węzłów mm | P3 | poza obrysem mm | P4 | odch. kluczy | kanały | P5 nowa geometria | P6 | trójkąty wzorzec → Pass 4 | wynik |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A_Sleek Battery P | old | P | P | 0.0 | P | 0 | P | 0.0 | 4 | P 3/3 | P | 11224 → 2856 | PASS |
| A_Sleek Gun P | old | P | P | 0.0 | P | 0 | P | 0.0 | 9 | P 7/7 | P | 17364 → 2988 | PASS |
| A_Sleek Wheel P | new | P | P | 0.0 | P | 0 | P | 0.0 | 4 | P 5/5 | P | 8170 → 7048 | PASS |
| B_Brutal Battery P | old | P | P | 0.0 | P | 0 | P | 0.0 | 4 | P 3/3 | P | 3480 → 1960 | PASS |
| B_Brutal Gun P | new | P | P | 0.0 | P | 0 | P | 0.0 | 8 | P 7/7 | P | 2416 → 2976 | PASS |
| B_Brutal Wheel P | old | P | P | 0.0 | P | 0 | P | 0.0 | 3 | P 4/4 | P | 7148 → 5826 | PASS |
| B_Brutal Wheel Q | new | P | P | 0.0 | P | 0 | P | 0.0 | 3 | P 4/4 | P | 6168 → 5392 | PASS |
| C_Arcade Battery P | old | P | P | 0.0 | P | 0 | P | 0.0 | 3 | P 2/2 | P | 2652 → 1512 | PASS |
| C_Arcade Battery Q | new | P | P | 0.0 | P | 0 | P | 0.0 | 3 | P 2/2 | P | 980 → 808 | PASS |
| C_Arcade Gun P | new | P | P | 0.0 | P | 0 | P | 0.0 | 6 | P 5/5 | P | 2228 → 2032 | PASS |
| C_Arcade Wheel P | new | P | P | 0.0 | P | 0 | P | 0.0 | 2 | P 3/3 | P | 4512 → 3568 | PASS |
| D_Scrap Battery P | old | P | P | 0.0 | P | 0 | P | 0.0 | 4 | P 3/3 | P | 3532 → 1180 | PASS |
| D_Scrap Gun P | old | P | P | 0.0 | P | 0 | P | 0.0 | 7 | P 6/6 | P | 4134 → 1390 | PASS |
| D_Scrap Wheel P | new | P | P | 0.0 | P | 0 | P | 0.0 | 3 | P 4/4 | P | 4960 → 5116 | PASS |
| E_Toon Battery P | new | P | P | 0.0 | P | 0 | P | 0.0 | 5 | P 3/3 | P | 1564 → 1532 | PASS |
| E_Toon Gun P | new | P | P | 0.0 | P | 0 | P | 0.0 | 7 | P 6/6 | P | 3028 → 2576 | PASS |
| E_Toon Wheel P | new | P | P | 0.0 | P | 0 | P | 0.0 | 2 | P 3/3 | P | 5294 → 4124 | PASS |

| zestaw pancerza | części | K1 | K2 | grubość płyty mm | K3 | wysięg dzioba mm | K4 | trójkąty zestawu | wynik |
|---|---|---|---|---|---|---|---|---|---|
| A:p | 24 | P | P | 34.5 | P | 162.0 | P | 3884 | PASS |
| B:p | 40 | P | P | 57.0 | P | 126.0 | P | 6788 | PASS |
| B:q | 40 | P | P | 53.0 | P | 172.0 | P | 5680 | PASS |
| C:p | 24 | P | P | 35.0 | P | 110.0 | P | 4244 | PASS |
| D:p | 56 | P | P | 28.0 | P | 192.0 | P | 5404 | PASS |
| E:p | 24 | P | P | 43.0 | P | 122.0 | P | 4628 | PASS |
