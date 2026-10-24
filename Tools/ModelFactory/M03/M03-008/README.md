# MODEL-03 / M03-008 — Płyta pancerza klinowego

**Źródło przydziału:** Docs/ModelFactory/CATALOG_V1.md na gałęzi feature/rrl-model-factory-queue-v1, pozycja M03-008. Agent MODEL-03. Przed produkcją wykonano CREATE Docs/ModelFactory/claims/M03-008.json na gałęzi kontrolnej, 2026-10-24T17:52:28.150Z. **Claim commit:** aa00254a67072305c932a8dbabc03fda0c9942bd; pierwotny blob SHA: 09c0b0483d507573e546bb309bed795ff494bd07.

## Zastosowanie i wygląd

Nowy, statyczny moduł **pancerza przedniego pojazdu**, bez mechaniki walki ani zmian prefabów gry. Fizyczna płyta klinowa ma trzy wyraźne warstwy:

1. **Tylna nośna płyta stalowa** o wymiarach 960 × 720 × 32 mm, z czterema **rzeczywistymi otworami przelotowymi Ø52 mm** w narożnych segmentach. Tylna powierzchnia jest dokładnie na płaszczyźnie mocowania Y=0.
2. **Środkowa warstwa elastomeru** o grubości 20 mm, rozmiar 740 × 480 mm; styka się z płytą stalową i z tylną płaszczyzną klina, bez przypadkowego przenikania.
3. **Zewnętrzna ceramiczna płyta klinowa** 760 × 500 mm, o wyprofilowanym przekroju. Frontowa powierzchnia odchyla się od Y=−94 mm przy krawędziach do Y=−170 mm na spłaszczonym grzbiecie. Brak ostrego, zerowej szerokości czubka.

**Wykończenie:** dwa grafitowe żebra prowadzone po obu skośnych płaszczyznach; dwa bursztynowe znaczniki ostrzegawcze na grzbiecie; cztery metalowe pierścienie montażowe. Łącznie 5 materiałów: StructuralSteel, Gunmetal, Ceramic, Elastomer, Amber (lekka emisja wyłącznie jako akcent). Bevel i Weighted Normals na wybranych obiektach w Blenderze. Klin i znaki mają prawdziwą grubość, nie są teksturą.

**Gabaryt:** X=960 mm, Y=184 mm (głębokość ku przodowi), Z=720 mm (wysokość). **Pivot:** (0,0,0) w środku tylnej płaszczyzny montażowej; kierunek montażu +Y w Blenderze, kierunek przodu pancerza −Y. Blender jest Z-up; FBX eksportowany jako Unity Y-up (axis_forward=-Z, axis_up=Y). Import GLB/FBX wymaga sprawdzenia osi i jednostek w Unity 6.

**Mocowanie:** SNAP_FRAME_FRONT w (0,0,0) i cztery puste obiekty lokalizujące otwory BOLT_±1_±1, pozycje (±0.410,0,±0.290) m. Każdy ma lokalną oś +Z obróconą w stronę +Y Blendera, czyli na zewnątrz powierzchni montażowej. Otwory są otwarte na pełnej grubości tylnej płyty, z pustym środkiem w pierścieniach. Średnica otworów 52 mm, średnica zewnętrzna pierścienia 80 mm.

**Kontakty:** jedyne zaplanowane objętościowe zakładki to 2 mm pomiędzy zewnętrznymi żebrami/oznaczeniami a ceramicznym frontem; połączenia pozostałych warstw i kołnierzy są powierzchniowe. Otwory narożne mają fizyczny luz od klina i elastomeru. Wszystkie części to niezależne zamknięte siatki, z kontrolą krawędzi skierowanych, dodatniej objętości i braku degeneratów.

## Generowanie

W katalogu Tools/ModelFactory/M03/M03-008/:

- generate_m03_008.py: kompletny generator bpy + niezależny tryb stdlib OBJ/MTL.
- qa_m03_008.py: niezależny walidator rzeczywiście wygenerowanego OBJ, MTL, manifestu, otworów i deterministyczności.
- README.md: dokumentacja i ograniczenia.

Polecenia:

    python3 Tools/ModelFactory/M03/M03-008/generate_m03_008.py --self-test
    python3 Tools/ModelFactory/M03/M03-008/qa_m03_008.py
    python3 Tools/ModelFactory/M03/M03-008/generate_m03_008.py --static-obj --out build/M03-008
    blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-008/generate_m03_008.py -- --out build/M03-008

W trybie stdlib powstają **prawdziwe** .obj, .mtl, _manifest.json; Blender bpy eksportuje dodatkowo .glb, .fbx, .blend. Blender tworzy wyłącznie własną kolekcję RRL_M03_008_WEDGE_ARMOR i przy powtórnym uruchomieniu usuwa wyłącznie obiekty z tej kolekcji i osierocone własne meshe. Nie dotyka scen Unity, ustawień projektu ani mechanik gry.

**Kolizja Unity:** zalecany złożony collider z kilku wypukłych brył; nie używać całego złożonego render mesha jako pojedynczego dynamicznego MeshCollider. Mocowania otworów wymagają osobnego sprawdzenia w prefabie. Model jest sztywny, **animacja nie jest wymagana**.

## QA i ograniczenia

- Własny generator sprawdza 19 zamkniętych komponentów, 5 materiałów, 4 rzeczywiste otwory, 4 pierścienie, geometrię 3 warstw, skierowane krawędzie, brak degeneratów, dodatnie objętości, gabaryty i zaplanowane zakładki.
- Niezależny walidator sprawdza geometrię **wyeksportowanego OBJ**, rzeczywistą pustą geometrię otworów, materiały, spójność siatek, winding, manifest i deterministyczność dwóch osobnych generacji.
- GitHub Actions .github/workflows/ci-m03-008.yml wykonuje testy stdlib i, jeżeli runner pozwoli, uruchamia prawdziwy Blender, eksport GLB/FBX/BLEND, kontrolę węzłów i materiałów GLB oraz powtórne generowanie w jednym procesie. Statusy PASS wolno podawać **wyłącznie po zakończonych workflow**.
- **Unity import/EditMode/PlayMode: NOT RUN**, dopóki nie istnieje rzeczywisty dowód z aktywowanego Unity. Również kontrola wizualna renderu i zderzeń w prefabie jest odrębną bramką integracji.

**Zakres PR:** wyłącznie nowe pliki Tools/ModelFactory/M03/M03-008/ oraz dedykowany workflow CI. Bez zmian wspólnych scen, ProjectSettings, istniejących systemów, main/develop i bez merge.
