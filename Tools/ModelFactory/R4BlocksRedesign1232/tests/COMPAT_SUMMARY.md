# #1232 pilot — wyniki testów zgodności (`scripts/tests_compat.py`)

Status: **PASS** · Unity: **UNITY_NOT_RUN** · źródło: prawdziwe GLB wyeksportowane z Blendera oraz zatwierdzone GLB z PR #963 / #939 / #941 / #1018 / #1186.

## Klocki (S1–S4, S7)

| Kierunek / klocek | Interfejs = #963 | Poza bryłą [mm] | Styk portów (min %; zatw.) | 24 obroty | Tri (LOD1) | Tri zatw. |
|---|---|---|---|---|---|---|
| A_Sleek/R4B_Cube | PASS (0.0 mm) | -0.0 | 95.1 (93.8) PASS | PASS | 684 (44) | 5036 |
| B_Industrial/R4B_Cube | PASS (0.0 mm) | -0.0 | 95.1 (93.8) PASS | PASS | 236 (44) | 5036 |
| A_Sleek/R4B_Wedge | PASS (0.0 mm) | 0.0 | 95.1 (93.8) PASS | PASS | 512 (32) | 3252 |
| B_Industrial/R4B_Wedge | PASS (0.0 mm) | 0.0 | 95.1 (93.8) PASS | PASS | 176 (32) | 3252 |
| A_Sleek/R4B_Tetra | PASS (0.0 mm) | 0.0 | 98.8 (93.8) PASS | PASS | 340 (20) | 2306 |
| B_Industrial/R4B_Tetra | PASS (0.0 mm) | 0.0 | 96.3 (93.8) PASS | PASS | 116 (20) | 2306 |
| A_Sleek/R4B_CornerWedge | PASS (0.0 mm) | 0.0 | 95.1 (93.8) PASS | PASS | 454 (30) | 2416 |
| B_Industrial/R4B_CornerWedge | PASS (0.0 mm) | 0.0 | 95.1 (93.8) PASS | PASS | 158 (30) | 2416 |
| A_Sleek/R4B_HalfBlock | PASS (0.0 mm) | 0.0 | 77.8 (93.8) PASS | PASS | 684 (44) | 4716 |
| B_Industrial/R4B_HalfBlock | PASS (0.0 mm) | 0.0 | 72.8 (93.8) PASS | PASS | 236 (44) | 4716 |

## Macierz modułów (S5)

51 prawdziwych modułów × 10 pełnych portów × 4 obroty wokół osi portu, pozycja spoczynkowa.

| Wariant klocków | Montaże | Błędy styku | Zablokowane obroty | Porty zablokowane całkowicie |
|---|---|---|---|---|
| approved | 2040 | 0 | 0 | 0 |
| A | 2040 | 0 | 0 | 0 |
| B | 2040 | 0 | 0 | 0 |

Regresje względem zatwierdzonych klocków: **0**.

## Roboty (S6)

| Robot | Części | Klocki pilota | Pary portów | Spójność | Pary moduł–klocek | Przenikania (zatw. / A / B) | Tri klocków (zatw. / A / B / A LOD1) |
|---|---|---|---|---|---|---|---|
| R1_GlacisRover | 146 | 136 | 311 | 1 graf | 15 | 0 / 0 / 0 | 603480 / 85680 / 29552 / 5488 |
| R2_OutriggerSkiff | 86 | 73 | 149 | 1 graf | 2 | 0 / 0 / 0 | 320116 / 45460 / 15668 / 2900 |
