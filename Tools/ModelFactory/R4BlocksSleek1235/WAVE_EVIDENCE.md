# #1235 — dowody per fala (bramki jakości)

Każda fala była budowana w Blenderze dopiero po przejściu poprzedniej (`tests/build_logs/gate_chain.state`). Testy źródłowe S1–S7 działają na wyeksportowanych plikach GLB. **UNITY_NOT_RUN · WINDOWS_PLAYER_NOT_RUN.**

## Fala 1 — 13 wypukłych / prostych

- Klocki: `InnerCorner`, `HalfWedge`, `LongWedge`, `WideWedge`, `Pyramid`, `HexColumn`, `OctColumn`, `Plate1x2`, `Plate2x2`, `Beam2`, `Beam3`, `Slab2x2`, `BigCube2`.
- Bramka Blendera: `wave1 started 2026-11-11 17:46:39 finished 17:47:00 blocks_pass 13/13 GATE_PASS`.
- Blender 5.3.0 dla każdego klocka: siatka manifold, spójne normalne, brak zdegenerowanych ścian, UV, eksport FBX i GLB z ponownym importem, LOD1. Log: `tests/build_logs/build_wave1.log`.
- Testy źródłowe: S1 interfejs PASS · S2 bryła PASS · S3 styk portów PASS · S4 obroty PASS · S5 moduły PASS.
- Macierz modułów: 17136 montaży, błędy styku A: 0, zablokowane obroty A / zatwierdzone: 0 / 0.
- Trójkąty: 86784 (zatwierdzone) → 15162 (A LOD0), 600 (A LOD1).
- Rendery: `A_Sleek/R4B_*/renders/` (Blender: front, bok, 3/4 przód, 3/4 tył, spód), plansza `compare/wave1_blocks_before_after.jpg`.
- Wynik fali: **PASS**.

## Fala 2 — 6 obłych

- Klocki: `RoundEdge`, `RoundCorner`, `Cylinder`, `Cone`, `Dome`, `RoundTop`.
- Bramka Blendera: `wave2 started 2026-11-11 17:47:00 finished 17:47:10 blocks_pass 6/6 GATE_PASS`.
- Blender 5.3.0 dla każdego klocka: siatka manifold, spójne normalne, brak zdegenerowanych ścian, UV, eksport FBX i GLB z ponownym importem, LOD1. Log: `tests/build_logs/build_wave2.log`.
- Testy źródłowe: S1 interfejs PASS · S2 bryła PASS · S3 styk portów PASS · S4 obroty PASS · S5 moduły PASS.
- Macierz modułów: 816 montaży, błędy styku A: 0, zablokowane obroty A / zatwierdzone: 0 / 0.
- Trójkąty: 18572 (zatwierdzone) → 6806 (A LOD0), 564 (A LOD1).
- Rendery: `A_Sleek/R4B_*/renders/` (Blender: front, bok, 3/4 przód, 3/4 tył, spód), plansza `compare/wave2_blocks_before_after.jpg`.
- Wynik fali: **PASS**.

## Fala 3 — 3 wklęsłe

- Klocki: `Step`, `CornerL3`, `Tee4`.
- Bramka Blendera: `wave3 started 2026-11-11 17:47:10 finished 17:47:16 blocks_pass 3/3 GATE_PASS`.
- Blender 5.3.0 dla każdego klocka: siatka manifold, spójne normalne, brak zdegenerowanych ścian, UV, eksport FBX i GLB z ponownym importem, LOD1. Log: `tests/build_logs/build_wave3.log`.
- Testy źródłowe: S1 interfejs PASS · S2 bryła PASS · S3 styk portów PASS · S4 obroty PASS · S5 moduły PASS.
- Macierz modułów: 6936 montaży, błędy styku A: 0, zablokowane obroty A / zatwierdzone: 342 / 342.
- Trójkąty: 31872 (zatwierdzone) → 5540 (A LOD0), 212 (A LOD1).
- Rendery: `A_Sleek/R4B_*/renders/` (Blender: front, bok, 3/4 przód, 3/4 tył, spód), plansza `compare/wave3_blocks_before_after.jpg`.
- Wynik fali: **PASS**.

## Fala 4 — tunel, łuk wklęsły, kratownica

- Klocki: `Arch`, `CoveEdge`, `TrussCube`.
- Bramka Blendera: `wave4 started 2026-11-11 17:47:16 finished 17:47:22 blocks_pass 3/3 GATE_PASS`.
- Blender 5.3.0 dla każdego klocka: siatka manifold, spójne normalne, brak zdegenerowanych ścian, UV, eksport FBX i GLB z ponownym importem, LOD1. Log: `tests/build_logs/build_wave4.log`.
- Testy źródłowe: S1 interfejs PASS · S2 bryła PASS · S3 styk portów PASS · S4 obroty PASS · S5 moduły PASS.
- Macierz modułów: 1020 montaży, błędy styku A: 0, zablokowane obroty A / zatwierdzone: 0 / 0.
- Trójkąty: 15048 (zatwierdzone) → 5850 (A LOD0), 748 (A LOD1).
- Rendery: `A_Sleek/R4B_*/renders/` (Blender: front, bok, 3/4 przód, 3/4 tył, spód), plansza `compare/wave4_blocks_before_after.jpg`.
- Wynik fali: **PASS**.
