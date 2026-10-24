# M01-013 — Wyrzutnia siatki zakłócającej

**Agent:** MODEL-01 / M01. **Źródło:** [CATALOG_V1.md](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md). **Zakres:** wyłącznie nowy model wizualny do Unity 6; bez implementacji mechanik WPN, pocisków, obrażeń ani AI.

## Rezerwacja

- ID: `M01-013`, nazwa katalogowa: **Wyrzutnia siatki zakłócającej**.
- Atomowy CREATE w `Docs/ModelFactory/claims/M01-013.json` na **oddzielnej gałęzi kontrolnej** `feature/rrl-model-factory-queue-v1`.
- Claim commit: `61d9ff7c2ea89d3ad65fc1028b4bb95c6d8289d7`.
- Czas rezerwacji: `2026-10-24T22:49:04Z`.
- Własna gałąź modelu: `feature/rrl-model-m01-013-disruption-net-launcher`, utworzona z `develop` `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.

## Sylwetka i materiały

Kompaktowy montowany od góry moduł z mechanicznym kasetowym rotorem wewnątrz wyraźnej ramy ochronnej. Sześć sześciokątnych rolek prowadzących ułożonych jest promieniowo. Między nimi znajduje się osiemnaście widocznych, cienkich, turkusowych filamentów siatki w trzech warstwach; są to wyłącznie elementy graficzne. Obudowa jest asymetrycznie zaakcentowana ceramicznymi osłonami, miedzianymi rolkami, tytanowymi wspornikami i bursztynowymi sygnalizatorami.

**7 materiałów:** Armor_Graphite, Titanium_Frame, Ceramic_Ivory, Copper_Guides, Gasket_Dark, Signal_Cyan, Warning_Amber. Model pozostaje czytelny bez bloom. Każda z 84 części ma oddzielną zamkniętą siatkę z dodatnią objętością i poprawnym windingiem. Konstrukcyjnie zamierzone nakładanie współpracujących części (np. filamentu na rolkę, wspornika na korpus) jest dozwolone; ruchoma kaseta nie przecina nieruchomej obudowy.

## Unity i animacja

- Skala: **1 jednostka = 1 metr**; **Y-up**, **+Z-forward**.
- Pivot montażu `MOUNT_TOP_SOCKET` = `(0,0,0)`, dolna powierzchnia płyty montażowej.
- Ruchoma hierarchia `CASSETTE_ROT_Z`: środek `(0,0.54,0)`, obrót wokół **lokalnej osi Z**, 360° w 73 klatkach przy 30 FPS; 13 kluczy co 30°.
- Nieruchoma oś promień 40 mm przechodzi przez **rzeczywiście wydrążony** pierścień kasety; minimalny prześwit osi do powierzchni ruchomych siatek: **15,73 mm**.
- Zamknięta rama chroni wirującą kasetę; minimalny zmierzony niekontaktowy prześwit AABB: **18 mm**.
- Ograniczenie: to animacja poglądowa. Brak skryptów strzelania, sieci, efektów, prefabów gameplay i automatycznego podpięcia do pojazdu.

## Wykonanie i weryfikacja

```bash
python3 Tools/ModelFactory/M01/M01-013/qa_m01_013.py
python3 Tools/ModelFactory/M01/M01-013/generate_m01_013.py --out build/M01-013
blender --background --python Tools/ModelFactory/M01/M01-013/generate_m01_013.py -- --out build/M01-013 --repeat-blender
python3 Tools/ModelFactory/M01/M01-013/qa_m01_013.py --out build/M01-013 --require-blender
```

- **Lokalnie rzeczywiście wykonano:** Python 3.13, generator i niezależny QA. **PASS**: 84 części, 2354 wierzchołki, 4416 trójkątów, 7 materiałów, 58 części ruchomych, 73 pozycje ruchu, 105850 porównań AABB, 0 wykrytych kolizji ruchome–nieruchome; dodatkowo trójkątowa kontrola radialnego odstępu od osi oraz identyczny SHA-256 OBJ po dwóch uruchomieniach.
- **Lokalnie wygenerowano:** rzeczywiste statyczne `OBJ`, `MTL` i manifest `JSON`. W gałęzi GitHub pliki te generuje i publikuje CI.
- **Blender:** w środowisku lokalnym **NOT RUN** (brak programu); workflow CI uruchamia Blender i weryfikuje BLEND/FBX/GLB oraz kanał animacji rotora. Nie oznaczać Blender PASS bez pomyślnego przebiegu CI.
- **Unity 6:** import, EditMode, PlayMode i integracja z grą **NOT RUN**.
- **Ręczna ocena w Blenderze/Unity:** **NOT RUN**; sam test AABB nie zastępuje wizualnej inspekcji.

## Pliki

- `generate_m01_013.py`: kompletny deterministyczny generator bpy + niezależny eksport statyczny OBJ/MTL.
- `qa_m01_013.py`: niezależne testy źródła i rzeczywistych eksportów.
- `generated/`: pliki wygenerowane przez CI (OBJ, MTL, GLB, FBX, BLEND, JSON) po pomyślnym QA.
- `.github/workflows/ci-model-m01-013.yml`: dedykowany workflow.

**Nie scalać Draft PR bez ręcznej inspekcji importu i animacji w Unity.**
