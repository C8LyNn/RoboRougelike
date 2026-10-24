# M02-013 — Silnik osiowy z wirnikiem (axial-flux rotor engine)

**Agent:** MODEL-02. **Katalog:** M02-013 na `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M02-013.json` na oddzielnej gałęzi
kontrolnej, atomowy CREATE `1e81f320dbe4ebb4ffc8ba02abb8ee0ad5cd0682`,
`2026-10-24T22:48:03Z`. Nie przenosić claim do tej gałęzi.

## Konstrukcja

Nowy, wyłącznie wizualny silnik elektryczny o osiowym strumieniu
magnetycznym. Otwarta kratownica z grafitowymi pierścieniami obudowy,
miedziany 12-polowy stojan na żelaznym jarzmie, dwanaście naprzemiennych
magnesów czerwonych i kobaltowych na wirniku, żebra chłodzące, wał
przelotowy, przednie i tylne łożysko z osobnymi bieżniami wewnętrznymi,
ceramiczne izolatory i cztery ucha montażowe. 9 materiałów PBR.
Elementy celowo osadzone w złożeniu (magnes w tarczy, cewka w jarzmie,
tuleja na wale) nie są traktowane jak niezależne luźne elementy.

**Skala:** 1 Blender unit = 1 m = 1 Unity unit. **Y-up, +Z-forward**.
**Pivot:** `MOUNT_FRAME` = `(0,0,0)`, płaszczyzna mocowania do ramy
`z=0`. **Oś obrotu:** +Z, pusty rodzic `ROTOR_SPIN_Z`. Hierarchia
odseparowuje wszystkie elementy nieruchome od wirujących. Animacja
referencyjna: pełny obrót 360° w 61 klatkach @30 FPS, liniowa.

**Luz funkcjonalny:** zewnętrzna nieruchoma bieżnia łożyska zaczyna się
od promienia 35 mm, wirująca bieżnia kończy się na 34 mm (1 mm luzu
promieniowego). Stojan jest oddzielony osiowo od magnesów wirnika o co
najmniej 29 mm. Obracająca się tarcza z żebrami i magnesami ma dodatni
odstęp od pionowych słupków klatki w każdej pozycji kątowej. Te
obwiednie i zamknięta topologia są sprawdzane w generatorze oraz przez
niezależny walidator. W przypadku celowo zachodzących na siebie
elementów nieruchomych nie jest deklarowany test pełnych kolizji
trójkąt-trójkąt.

## Uruchomienie

```bash
python3 Tools/ModelFactory/M02/M02-013/qa_m02_013.py
python3 Tools/ModelFactory/M02/M02-013/generate_m02_013.py --out build/M02-013
blender --background --python Tools/ModelFactory/M02/M02-013/qa_m02_013_blender.py -- build/M02-013
python3 Tools/ModelFactory/M02/M02-013/qa_m02_013.py --check-blender --out build/M02-013
```

Pierwsze dwa polecenia nie wymagają Blendera i tworzą prawdziwe
statyczne OBJ/MTL. Trzecie uruchamia Blender, **dwukrotnie w tej samej
sesji** odtwarza cały zespół, sprawdza hierarchię, transformacje
pięciu klatek animacji, zapisuje BLEND i eksportuje animowany GLB.
Ostatnie polecenie czyta rzeczywisty OBJ, MTL, BLEND i binarny glTF 2.0,
sprawdza strukturę węzłów i kanał rotacji wirnika.

## Unity

- OBJ/MTL: statyczna geometria, natywny import (bez animacji).
- BLEND: edytowalny projekt Blender; import zależny od lokalnej instalacji.
- GLB: animowany asset glTF 2.0; do importu w Unity 6 potrzebny importer
  glTF, np. glTFast, i weryfikacja materiałów URP. Brak prefabów i
  skryptów gameplay.
- Ruch referencyjny silnika **nie** napędza fizyki pojazdu; jest
  wizualizacją mechanizmu, do połączenia z istniejącą logiką.

## QA i granice dowodów

CI: `.github/workflows/m02-013-axial-rotor-asset-qa.yml`.
Po udanym przebiegu publikuje realne OBJ, MTL, BLEND, GLB i JSON
jako artefakt Actions. **Blender / GLB PASS** dopiero po rzeczywistym
uruchomieniu CI. **Unity import, EditMode, PlayMode, fizyka i ręczna
kontrola wizualna: NOT RUN** do czasu odrębnego testu. Nie scalać
Draft PR bez przeglądu animacji i materiałów w Unity.
