# MODEL-03 / M03-009 — Płyta pancerza perforowanego

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. Kolejka **M03 wyłącznie**, pozycja 009. **Rola:** boczny, wizualny moduł pancerza ramy pojazdu; nie implementuje wytrzymałości, kolizji fizycznej ani mechanik obrażeń.

## Rezerwacja

- `Docs/ModelFactory/claims/M03-009.json` na gałęzi kontrolnej — **CREATE**, nie UPDATE.
- `claimed_at_utc = 2026-10-24T18:51:41.238Z`, `agent = M03`, `status = CLAIMED`, `run = 2026-10-24T18Z`.
- Commit CREATE: `a7eb19e295855f191aea274d28f42486b802e7e4`.
- Blob SHA po CREATE: `c4304bf89ebc454cb308e6f85952f4f5a1ffc54d`.

## Wymiary i montaż (Unity 6)

Wszystkie wymiary w metrach. **X** — długość boku pojazdu, **Y** — normalna na zewnątrz, **Z** — pion. Pivot `RRL_M03_009_ROOT_PIVOT` znajduje się w `(0,0,0)` na tylnej płaszczyźnie mocowań, zwróconej osią `+Y` ku zewnętrznej stronie pancerza. Model mieści się w `X ±0.561, Y 0…0.122, Z ±0.360`, czyli **1.122 × 0.122 × 0.720 m**. To panel boczny, nie cała ściana 1×1 m.

Cztery otwarte, rzeczywiste gniazda o promieniu wewnętrznym **19 mm** i zewnętrznym **43 mm**: `(±0.518,0,±0.315)`. Każdy pierścień przechodzi od `Y=0` do `Y=0.035`; gniazdo nie jest zaślepione przez panel ani ramę. Odpowiadają im cztery lokatory `SNAP_00..03` o osi montażowej `-Y`, do dopasowania w integracji do istniejącego systemu siatki. Łączniki opierają się o przednią płaszczyznę pierścieni i o zewnętrzne płaszczyzny ramy; nie wymagają wbudowania w gameplay.

## Dwie rzeczywiście perforowane warstwy

1. **Zewnętrzna stal** `Outer_Steel_24_Through_Holes`: jednolita, zamknięta siatka 6×4, **24 otwarte prostokątne otwory**, obrys 780×560 mm, grubość 30 mm (`Y=0.080..0.110`), otwory 82×90 mm. Nie są to malowane kwadraty ani oddzielne nachodzące na siebie beleczki.
2. **Miedziana siatka pod spodem** `Inner_Mesh_96_Through_Holes`: drugi jednolity manifold 12×8, **96 otwartych oczek**, obrys 780×560 mm, grubość 16 mm (`Y=0.055..0.071`), otwory 41×42 mm. Warstwy dzieli **9 mm wolnej przestrzeni**.
3. **Rama grafitowa**: cztery odrębne, stykające się bez nachodzenia objętościowego belki, 80 mm szerokości i 75 mm grubości. Dodatki: cztery puste pierścienie montażowe, cztery mostki, osiem główek nitów z otwartym wgłębieniem oraz cztery bursztynowe oznaczenia boczne.

Materiały: `GraphiteFrame`, `PerforatedSteel`, `CopperMesh`, `TitaniumMount`, `AmberWarning` (5 odrębnych PBR). Model jest **statyczny**, więc brak animacji jest zgodny z katalogiem. Własny generator nie dotyka wspólnych scen, `ProjectSettings`, prefabów ani systemów walki.

## Pliki i odtwarzanie

- `generate_m03_009.py` — pełny generator bpy + deterministyczny eksport OBJ/MTL bez Blendera.
- `qa_m03_009.py` — niezależne QA na faktycznym OBJ, normalne, topologia, rzeczywiste perforacje i otwory montażowe, dwa identyczne przebiegi eksportu.
- `.github/workflows/ci-m03-009.yml` — dwa izolowane joby: statyczny Python oraz faktyczny Blender w GitHub Actions, eksport OBJ/MTL i GLB/FBX/BLEND.
- Wyniki generatora powstają w `build/M03-009/` lub pod ścieżką `--out`; nie commitujemy ciężkich plików binarnych. Udostępniane są jako **artefakty CI**.

Polecenia:

```bash
python3 Tools/ModelFactory/M03/M03-009/generate_m03_009.py --self-test
python3 Tools/ModelFactory/M03/M03-009/qa_m03_009.py
python3 Tools/ModelFactory/M03/M03-009/generate_m03_009.py --static-obj --out build/M03-009
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-009/generate_m03_009.py -- --out build/M03-009
```

**Wymagania bramki:** rzeczywiste przejście QA w GitHub Actions, poprawny GLB/FBX/BLEND, pięć materiałów, cztery SNAP, brak duplikowania obiektów przy dwukrotnym wywołaniu w jednej sesji bpy. **Unity EditMode/PlayMode: NOT RUN**, dopóki aktywacja/licencja runnera nie umożliwi wykonania. Wizualny przegląd materiałów i skali w Unity nadal wymagany przed integracją.

**Zakaz automatycznego merge.** Model tylko na własnej gałęzi od `develop` i w osobnym Draft PR do `develop`.
