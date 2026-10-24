# M01-002 — Krótka haubica tłokowa (model wizualny)

**Katalog:** M01-002, top socket, ruch suwadła/odrzutu, gruby stożkowy hamulec wylotowy. **Status:** DRAFT / QA HOLD. **Zakres:** wyłącznie model 3D, bez mechanik strzelania.

## Pliki

- `M01-002.obj` + `M01-002.mtl` — rzeczywista statyczna geometria w pozycji spoczynkowej; siedem materiałów, w tym akcent emisyjny w Blenderze. OBJ nie przenosi animacji.
- `generate_m01_002.py` — pełny, idempotentny generator Blender bpy; tworzy ten sam zestaw części, hierarchię `MOUNT_TOP_SOCKET` i `RECOIL_Y_POSITIVE`, animuje suwadło i eksportuje `.blend`, `.glb`, `.fbx`.
- `validate_static.py` — walidator OBJ/MTL w Pythonie 3 bez zależności.
- `M01-002.static-qa.json` — wynik kontroli podczas generowania geometrii.

## Współrzędne i montaż

Jednostki: metry (1 Unity unit = 1 m). Blender: **+Z góra**, **-Y przód (wylot)**. Pivot mocowania (0,0,0), środek stopy montażowej na osi X=0. Animacja przesuwa cały zespół lufy i komory **+Y o 0,12 m** (klatki 1,4,8,14,20, 24 FPS). Dwa chromowane pręty (długość 0,43 m) są faktycznie wsunięte w **puste pierścieniowe obudowy** tłoków, a nie w pełne walce. W położeniu spoczynkowym zagłębienie wynosi 25 mm; nominalny luz promieniowy pręta względem otworu obudowy to 7 mm. To zamierzona interakcja teleskopowa. Maksymalny ruch pozostawia około 0,055 m odstępu od tylnej grodzi i około 0,165 m odstępu komory od bocznych policzków. Są to odstępy analityczne dla elementów obudowy, **nie pełny test kolizji mesh-to-mesh**.

## Odtwarzanie i eksport

```bash
python3 validate_static.py
blender -b --python generate_m01_002.py -- --output ./export
```

Po eksporcie w Unity: import GLB przez importer obsługujący glTF (lub FBX z Unity), potwierdź orientację przód/-Y i oś animacji, skalę oraz zakres ruchu; ręcznie sprawdź cieniowanie i przenikanie w klatkach 1,4,8,14,20. Dla FBX w Unity sprawdź ustawienia osi i skali (Blender +Z -> Unity +Y). Model nie jest prefabem ani zintegrowaną bronią.

## QA

Poprawiony statyczny OBJ V2: **57 części, 2640 wierzchołków, 5104 trójkąty, 7 materiałów**; walidacja manifold, kierunku normalnych, czterech otworów, dwóch mocowań i 21 pozycji odrzutu: **PASS** w [GitHub Actions 40102122373](https://github.com/C8LyNn/RoboRougelike/actions/runs/40102122373). Blender generator runtime: **NOT RUN** (brak Blendera); eksport GLB/FBX: **NOT RUN**; Unity: **NOT RUN**. Pozostawić PR w DRAFT do pełnej kontroli.

## Poprawka V2 — hydraulika odrzutu, 24.10.2026

W wersji początkowej `Piston_Housing_±1` i `Piston_Housing_Collar_±1` były pełnymi cylindrami, blokującymi rzeczywistą drogę prętów. W pozycji spoczynkowej pręty kończyły się **35 mm przed początkiem obudów**. Mimo zaprojektowanego ruchu odrzutu mechanizm nie był spójną konstrukcją.

**Zmiany konstrukcyjne:**
- Obie obudowy hydrauliczne oraz oba kołnierze mają odtąd zamknięte pierścieniowe siatki `tubeY`, z otworem o promieniu **41 mm** i prętem o promieniu **34 mm**. Daje to **7 mm** luzu promieniowego.
- Długość każdego ruchomego pręta wzrosła z 370 do **430 mm** przy zachowaniu przedniego mocowania. W pozycji spoczynkowej pręt zachodzi **25 mm** w obudowę, a podczas maksymalnego odrzutu **145 mm**.
- Dwie ruchome podpory `Piston_Receiver_Anchor_-1` i `Piston_Receiver_Anchor_1` łączą nakrętki prętów z ruchomym korpusem komory. Ich przenikanie z komorą i nakrętkami jest **celowym złączem konstrukcyjnym**, nie błędem animacji.
- W `validate_recoil_geometry()` sprawdzane są **21 równomiernych pozycji odrzutu**, zasięg prętów i zachowanie minimalnego zazębienia. Niezależny `validate_static.py` analizuje rzeczywisty OBJ, topologię, otwory w siatce i zgodność nazw ze specyfikacją generatora.

**Rzeczywiste pliki:** `M01-002.obj` został zastąpiony poprawioną geometrią V2; `M01-002.mtl` pozostał bez zmian, bo 7 materiałów nie uległo zmianie. Wyniki: 57 zamkniętych części, 2640 wierzchołków, 5104 trójkąty.

**Test wykonany:** [GitHub Actions run 40102122373 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40102122373). Test uruchomił kompilację składni skryptu i walidatora, analizę siatki OBJ i źródłowej specyfikacji, kontrole luzu i 21 pozycji odrzutu oraz manifest.

**Wciąż wymagane:** Blender `bpy`, eksport animacji FBX/GLB, inspekcja wizualna pełnego skoku i import Unity 6 — **NOT RUN**. Obliczenia statyczne nie zastępują fizycznego PlayMode ani pełnego wykrywania przenikania wszystkich brył. Zmiany nie modyfikują mechanik WPN, scen ani ProjectSettings. Włączyć poprawkę wyłącznie po review do oryginalnej gałęzi M01-002 (Draft PR #43).
