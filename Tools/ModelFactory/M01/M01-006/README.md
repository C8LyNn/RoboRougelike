# M01-006 — Wyrzutnia igieł ceramicznych

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Rezerwacja:** `Docs/ModelFactory/claims/M01-006.json`; CREATE commit `3efd82b9f399feda99f3a7611894a317d245ea4d`. **Agent:** MODEL-01 (M01). Tylko wizualny model 3D; bez zmian w mechanikach WPN-001..006, scenach lub ProjectSettings.

## Wygląd

Pięć ceramicznych igieł z wydłużonymi stożkowymi końcówkami na wysuwanym wózku. Pancerne **białe panele ceramiczne**, sześć ostrych żeber, dwie odkryte tytanowe prowadnice, miedziane tuleje igieł, grafitowy korpus, bursztynowe znaczniki i turkusowe listwy emisyjne. Siedem materiałów: `Ceramic_Ivory`, `Ceramic_White`, `Graphite`, `Titanium`, `Copper`, `Cyan_Glow`, `Amber_Warning`. Emisja jest tylko akcentem.

## Unity 6 — mocowanie i animacja

- Skala: **1 jednostka = 1 metr**.
- Pivot mounta: `M01_006_MOUNT_TOP_Z0` w lokalnym `(0,0,0)` na powierzchni montażowej pojazdu. Blender: **+Y przód, +Z góra**.
- Wózek: `M01_006_RAIL_EXTEND_Y`, animowane przesunięcie **0 → +0,18 → 0 m** po osi +Y. Klatki 1, 9, 17, 25, 31, 39, 47, 55 przy 60 FPS.
- Statyczny OBJ nie przenosi animacji. FBX wygenerowany w Blenderze jest preferowany do Unity. GLB wymaga kompatybilnego importera (np. glTFast). Nie dodano pocisków ani logiki strzelania.

## Pliki i odtwarzanie

`generate_m01_006.py` to wykonywalny skrypt Blender Python `bpy` oraz generator OBJ/MTL działający także bez Blendera. `validate_m01_006.py` niezależnie kontroluje zapisany OBJ/MTL i opcjonalnie animowany GLB. `generated/` zawiera rzeczywiste statyczne pliki OBJ, MTL oraz raport QA. CI wykonuje eksport GLB, FBX i biblioteki BLEND do artefaktu.

```bash
python3 Tools/ModelFactory/M01/M01-006/generate_m01_006.py --self-test
python3 Tools/ModelFactory/M01/M01-006/generate_m01_006.py --out /tmp/m01-006
python3 Tools/ModelFactory/M01/M01-006/validate_m01_006.py
blender -b --python Tools/ModelFactory/M01/M01-006/generate_m01_006.py -- --out /tmp/m01-006-blender
python3 Tools/ModelFactory/M01/M01-006/validate_m01_006.py /tmp/m01-006-blender
```

Blender usuwa tylko własną kolekcję `M01-006_ASSET`, eksportuje tylko zaznaczone własne obiekty i przywraca klatki/zaznaczenie. Plik `.blend` to biblioteka kolekcji do **File → Append**, nie pełna scena.

## Faktycznie wykonane QA

Lokalny Python: **PASS_STATIC**. 63 zamknięte komponenty, 994 wierzchołki, 1736 trójkątów, 7 materiałów, 5 igieł. Zweryfikowano manifold, dodatnią objętość brył, brak zdegenerowanych trójkątów i **37 pozycji** wózka (co 5 mm) oraz **31 820** konserwatywnych kontroli AABB między 43 elementami statycznymi a 20 ruchomymi; bez przecięć. Wykonano niezależny import OBJ przez `trimesh` (7 grup materiałowych, wszystkie watertight).

**Blender/FBX/GLB:** lokalnie **NOT RUN**; dedykowane CI próbuje rzeczywistego eksportu i walidacji GLB. **Unity 6 EditMode/PlayMode i ręczna ocena wizualna:** **NOT RUN**. PR pozostaje DRAFT, bez automatycznego merge.
