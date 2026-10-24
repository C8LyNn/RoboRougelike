# MODEL-04 / M04-006 — Dopalacz płaski szczelinowy

**Źródło zadania:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M04-006.json` — commit `7c242843ac2e026b31165bf6a2d7d9f98c2b72d6`.
**Zakres:** wyłącznie nowy, wizualny asset do Unity 6. Nie zmienia gameplayu, thrust, AI, scen ani ProjectSettings.

## Projekt

Szeroki, pojedynczy, **płaski wylot szczelinowy** zamiast dwóch okrągłych komór M04-005. Profilowana klinowa obudowa ceramiczna, grafitowe boczne ścianki, tytanowe ranty, miedziane radiatory, dwa rodzaje emisyjnych linii (cyjan i bursztyn), pięć poziomych ruchomych żaluzji. Model przeznaczony do mocowania na **tylnej ścianie ramy**. Siedem materiałów; emisja tylko jako dodatkowy akcent.

**Układ:** Blender Z-up, +Y skierowane do przodu pojazdu, wylot w kierunku −Y. 1 jednostka = 1 m. `M04-006_MOUNT_ROOT` w `(0,0,0)` na płaszczyźnie mocowania tylnego. Gabaryt projektowy ok. 1,55 × 0,87 × 0,79 m. Każda z pięciu żaluzji ma niezależny pivot na osi X w `(0,-0.705,z)`, gdzie z = −0.20, −0.10, 0, +0.10, +0.20 m.

**Animacja:** 1/20/40/60/80 klatka, kąty 0° → +18° → 0° → −8° → 0°. Dziesięć animowanych brył: pięć łopatek i pięć przypiętych do nich końcówek. Ruch odbywa się wewnątrz pustej szczeliny. Osie i pozycje są jawne w `geometry.json`.

## Generowanie

```bash
python3 Tools/ModelFactory/M04/M04-006/generate_m04_006.py --self-test --output-dir /tmp/m04-006
python3 Tools/ModelFactory/M04/M04-006/qa_mesh_m04_006.py --asset-dir /tmp/m04-006
blender -b --factory-startup --python Tools/ModelFactory/M04/M04-006/generate_m04_006.py -- --output-dir /tmp/m04-006-blender --require-blender
python3 Tools/ModelFactory/M04/M04-006/qa_mesh_m04_006.py --asset-dir /tmp/m04-006-blender --glb /tmp/m04-006-blender/M04-006_animated.glb
```

Generator tworzy deterministyczne `M04-006.obj` / `M04-006.mtl` i raport JSON bez Blendera. Blender `bpy` dodatkowo tworzy `M04-006.blend` jako **bibliotekę kolekcji** (Append Collection, nie cały plik sceny) i `M04-006_animated.glb`. GLB zawiera obiekty modelu i kanały animacji; Unity 6 może wymagać importera glTF lub konwersji do FBX.

Generator jest idempotentny względem własnej kolekcji `M04-006_ASSET`, nie usuwa obcych obiektów. Przy ponownym uruchomieniu usuwa tylko swoją kolekcję i odtwarza ją. Nie ingeruje w sceny gry.

## QA i integracja

- **Python QA:** zamknięte bryły, orientacja ścian, brak zdegenerowanych trójkątów, indeksy OBJ, co najmniej trzy materiały, unikalne nazwy i deterministyczne wyjście.
- **Kinematyka:** obliczenie rzeczywistych wierzchołków w 27 pozycjach od −8° do +18° co 1°, kontrola wszystkich brył ruchomych względem stałych i względem **innych żaluzji** (elementy tej samej żaluzji są intencjonalnie połączone).
- **CI:** `.github/workflows/ci-model-m04-006.yml` uruchamia generator, niezależne QA, Blender i walidację animowanego GLB; publikuje artefakty. Raport PASS dopiero po rzeczywistym zakończeniu odpowiednich jobów.
- **Unity EditMode/PlayMode i kontrola wizualna:** **NOT RUN** do czasu uruchomienia w docelowej grze. Brak deklaracji, że asset jest już zintegrowany.

Importuj jako nowy asset; **nie zastępuj** M04-004/M04-005 ani istniejących skryptów dopalacza. Draft PR do `develop`; decyzja integracyjna należy do Codexa.
