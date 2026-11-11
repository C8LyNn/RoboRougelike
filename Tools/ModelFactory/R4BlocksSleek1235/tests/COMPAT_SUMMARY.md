# #1235 — wyniki testów zgodności katalogu 30 klocków A_Sleek (`scripts/tests_compat25.py`)

Status: **PASS** · Unity: **UNITY_NOT_RUN** · Windows Player: **WINDOWS_PLAYER_NOT_RUN**

Źródło: prawdziwe pliki GLB wyeksportowane z Blendera (25 nowych z tego PR, 5 pilota bez zmian z #1234) oraz zatwierdzone GLB z PR #963 / #939 / #941 / #1018 / #1186.
Polecenie: `R4_REF=<refs> python3 scripts/tests_compat25.py`. Commity źródeł: blocks `03af520`, weapons `23718ab`, movement `c6b655d`, wheels `fb6910c`, connectors `22974e7`, pilot `5f4470d`.

## Wynik per fala

| Fala | Klocki | S1 interfejs | S2 bryła | S3 styk portów | S4 obroty | S5 moduły | Tri zatw. → A (LOD1) | Wynik |
|---|---|---|---|---|---|---|---|---|
| pilot #1234 | 5 | PASS | PASS | PASS | PASS | PASS | 17726 → 2674 (170) | **PASS** |
| fala 1 | 13 | PASS | PASS | PASS | PASS | PASS | 86784 → 15162 (600) | **PASS** |
| fala 2 | 6 | PASS | PASS | PASS | PASS | PASS | 18572 → 6806 (564) | **PASS** |
| fala 3 | 3 | PASS | PASS | PASS | PASS | PASS | 31872 → 5540 (212) | **PASS** |
| fala 4 | 3 | PASS | PASS | PASS | PASS | PASS | 15048 → 5850 (748) | **PASS** |

## Klocki (S1–S4, S7)

| Klocek | Fala | Komórki | Interfejs = #963 | Poza bryłą LOD0 / LOD1 [mm] | Poza koliderem A / zatw. [mm] | Styk min % LOD0 / LOD1 (zatw.) | 24 obroty | Tri A (LOD1) | Tri zatw. | Sloty mat. |
|---|---|---|---|---|---|---|---|---|---|---|
| R4B_Cube | pilot #1234 | 1 | PASS (0.0 mm) | -0.0 / -0.0 | -0.0 / -0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 684 (44) | 5036 | 3 |
| R4B_Wedge | pilot #1234 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 512 (32) | 3252 | 3 |
| R4B_Tetra | pilot #1234 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 98.8 / 100.0 (93.8) PASS | PASS | 340 (20) | 2306 | 3 |
| R4B_CornerWedge | pilot #1234 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 454 (30) | 2416 | 3 |
| R4B_HalfBlock | pilot #1234 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 77.8 / 77.8 (93.8) PASS | PASS | 684 (44) | 4716 | 3 |
| R4B_InnerCorner | fala 1 | 1 | PASS (0.0 mm) | -0.0 / -0.0 | -0.0 / -0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 682 (50) | 4826 | 3 |
| R4B_HalfWedge | fala 1 | 1 | PASS (0.0 mm) | 0.0001 / 0.0001 | -0.0 / -0.0 | 77.8 / 77.8 (93.8) PASS | PASS | 512 (32) | 1652 | 3 |
| R4B_LongWedge | fala 1 | 2 | PASS (0.0 mm) | 0.0001 / 0.0001 | 0.0 / 0.0 | 88.9 / 100.0 (93.8) PASS | PASS | 936 (32) | 4030 | 3 |
| R4B_WideWedge | fala 1 | 2 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 97.5 / 100.0 (93.8) PASS | PASS | 846 (32) | 4812 | 3 |
| R4B_Pyramid | fala 1 | 1 | PASS (0.0 mm) | 0.0001 / 0.0001 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 454 (30) | 972 | 3 |
| R4B_HexColumn | fala 1 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 100.0 / 100.0 (93.8) PASS | PASS | 1028 (68) | 1764 | 3 |
| R4B_OctColumn | fala 1 | 1 | PASS (0.0 mm) | -0.0 / 0.0 | -0.0 / 0.0 | 95.1 / 95.1 (93.8) PASS | PASS | 1052 (92) | 4844 | 3 |
| R4B_Plate1x2 | fala 1 | 2 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 77.8 / 77.8 (93.8) PASS | PASS | 1172 (44) | 4752 | 3 |
| R4B_Plate2x2 | fala 1 | 4 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 98.8 / 100.0 (93.8) PASS | PASS | 1696 (44) | 6284 | 3 |
| R4B_Beam2 | fala 1 | 2 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 1172 (44) | 8184 | 3 |
| R4B_Beam3 | fala 1 | 3 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 1532 (44) | 11296 | 3 |
| R4B_Slab2x2 | fala 1 | 4 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 97.5 / 100.0 (93.8) PASS | PASS | 1696 (44) | 13356 | 3 |
| R4B_BigCube2 | fala 1 | 8 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 98.8 / 100.0 (93.8) PASS | PASS | 2384 (44) | 20012 | 3 |
| R4B_RoundEdge | fala 2 | 1 | PASS (0.0 mm) | 0.0001 / 0.0001 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 1136 (56) | 3428 | 3 |
| R4B_RoundCorner | fala 2 | 1 | PASS (0.0 mm) | -0.0 / -0.0 | -0.0 / 0.0 | 79.0 / 98.8 (93.8) PASS | PASS | 1560 (82) | 2782 | 3 |
| R4B_Cylinder | fala 2 | 1 | PASS (0.0 mm) | 0.0001 / 0.0 | 4.439 / 7.216 | 100.0 / 100.0 (88.9) PASS | PASS | 864 (96) | 3202 | 3 |
| R4B_Cone | fala 2 | 1 | PASS (0.0 mm) | -0.0 / 0.0 | 3.676 / 27.913 | 100.0 / 100.0 (88.9) PASS | PASS | 912 (120) | 1700 | 3 |
| R4B_Dome | fala 2 | 1 | PASS (0.0 mm) | -0.0 / 0.0 | 24.215 / 31.682 | 95.1 / 100.0 (93.8) PASS | PASS | 942 (142) | 3270 | 3 |
| R4B_RoundTop | fala 2 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 88.9 / 100.0 (93.8) PASS | PASS | 1392 (68) | 4190 | 3 |
| R4B_Step | fala 3 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 77.8 / 77.8 (93.8) PASS | PASS | 1292 (64) | 4966 | 3 |
| R4B_CornerL3 | fala 3 | 3 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 1852 (64) | 11764 | 3 |
| R4B_Tee4 | fala 3 | 4 | PASS (0.0 mm) | 0.0 / 0.0 | 0.0 / 0.0 | 95.1 / 100.0 (93.8) PASS | PASS | 2396 (84) | 15142 | 3 |
| R4B_Arch | fala 4 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | 48.293 / 49.792 | 95.1 / 100.0 (93.8) PASS | PASS | 1714 (116) | 4564 | 3 |
| R4B_CoveEdge | fala 4 | 1 | PASS (0.0 mm) | 0.0 / 0.0 | -0.0 / 5.157 | 77.8 / 79.0 (93.8) PASS | PASS | 1472 (96) | 3344 | 3 |
| R4B_TrussCube | fala 4 | 1 | PASS (0.0 mm) | -0.0 / -0.0 | -0.0 / -0.0 | 95.1 / 100.0 (88.9) PASS | PASS | 2664 (536) | 7140 | 3 |

## Macierz modułów (S5)

51 prawdziwych modułów × 137 pełnych portów (wszystkie komórki klocków wielokomórkowych) × 4 obroty wokół osi portu, pozycja spoczynkowa.

| Wariant klocków | Montaże | Błędy styku | Zablokowane obroty | Porty zablokowane całkowicie |
|---|---|---|---|---|
| approved | 27948 | 0 | 342 | 30 |
| A | 27948 | 0 | 342 | 30 |

Regresje A względem zatwierdzonych klocków: **0**. Zablokowane obroty to realne kolizje dużych modułów (np. koła XL) z sąsiednią komórką tego samego klocka wielokomórkowego; występują identycznie dla zatwierdzonych klocków.

## Roboty (S6)

| Robot | Części | Klocki (części / komórki / rodzaje) | Pary portów | Spójność | Pary moduł–klocek | Przenikania zatw. / A | Tri klocków zatw. / A / A LOD1 | Wynik |
|---|---|---|---|---|---|---|---|---|
| R1_GlacisRover | 98 | 88 / 146 / 13 | 215 | 1 graf | 15 | 0 / 0 | 523140 / 88382 / 3966 | PASS |
| R2_OutriggerSkiff | 60 | 47 / 73 / 9 | 94 | 1 graf | 2 | 0 / 0 | 272564 / 58124 / 4236 | PASS |
| R3_Leviathan | 227 | 210 / 413 / 30 | 522 | 1 graf | 4 | 0 / 0 | 1386972 / 294160 / 18798 | PASS |

Liczby trójkątów to dane źródłowe plików GLB. Nie są pomiarem FPS — Unity ani Windows Player nie były uruchamiane.
