# Hero Benchmark Pass 3: testy wyboru właściciela na wyeksportowanych GLB (numpy)

Wynik techniczny: **PASS** — 5/5 robotów HYBRID. `hero1/` i `hero2/` bez zmian względem gałęzi PR #1274: **TAK**. Unity: UNITY_NOT_RUN. Odbiór artystyczny: **OCZEKUJE NA WŁAŚCICIELA**.

 X1 nodes      identical contract node names and parents (cosmetic mechanism nodes listed in *_QA.json are style-specific extras, reported apart)
 X2 mounting   every contract node (pivots, sockets, muzzle, port empties) has the same rest position in both models (dev < 0.05 mm);
               identical port records and identical declared envelope (collision outline of the type)
 X3 outline    rest-pose geometry of the SELECTED model (FX nodes excluded) inside the declared envelope (<= 1 mm)
 X4 animation  identical clips (name, start, end, loop), events, fps and length; the GLB of both models animates every contract node of the QA
               record, and the key values of the contract nodes agree (max deviation reported; cosmetic mechanism tracks may differ)
 X5 source     the selected file is byte-identical to the blob of its PR branch (git), i.e. the historical model itself, not a regeneration;
               hero1/ and hero2/ are untouched in this branch

| styl | część | wybrana | X1 | X2 | odch. węzłów mm | X3 | poza obrysem mm | X4 | odch. kluczy | X5 | wynik |
|---|---|---|---|---|---|---|---|---|---|---|---|
| A_Sleek | Gun | hero1 (#1270) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| A_Sleek | Wheel | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| A_Sleek | Battery | hero1 (#1270) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| A_Sleek | Cube | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| A_Sleek | Deployer | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| B_Brutal | Gun | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| B_Brutal | Wheel | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| B_Brutal | Battery | hero1 (#1270) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| B_Brutal | Cube | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| B_Brutal | Deployer | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| C_Arcade | Gun | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| C_Arcade | Wheel | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| C_Arcade | Battery | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| C_Arcade | Cube | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| C_Arcade | Deployer | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| D_Scrap | Gun | hero1 (#1270) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| D_Scrap | Wheel | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| D_Scrap | Battery | hero1 (#1270) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| D_Scrap | Cube | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| D_Scrap | Deployer | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| E_Toon | Gun | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| E_Toon | Wheel | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| E_Toon | Battery | hero2 (#1274) | P | P | 0.0 | P | 0 | P | 0.0 | P | PASS |
| E_Toon | Cube | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
| E_Toon | Deployer | hero1 (#1270) | — | — | — | — | — | — | — | P | PASS |
