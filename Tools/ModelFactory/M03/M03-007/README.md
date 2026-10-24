# M03-007 — Uchwyt pionowy T

**MODEL-03 / RoboRougelike / Unity 6.** Nowa, statyczna część modularnej konstrukcji pojazdu, pozycja katalogowa M03-007. Nie implementuje gameplayu, kolizji fizycznych ani systemu mocowania w Unity.

## Konstrukcja i wygląd

- Sylwetka **T**, pionowy wspornik z górną belką, dwoma trójkątnymi żebrami i czytelnymi śrubami na dwóch wysokościach.
- Wymiary w lokalnym układzie: szerokość około **0,842 m**, wysokość **0,739 m**, głębokość około **0,262 m**. Unity 1 unit = 1 m; oś +Z w górę, front -Y.
- Pivot główny **(0,0,0)** — środek górnego gniazda ramy pojazdu; normalna mocowania **+Z**. Element wystaje ponad powierzchnię ramy.
- Dwa dolne porty z rzeczywistymi otworami: X=±0,34 m, Z=0, osie +Z; dwa górne porty w poprzeczce: X=±0,34 m, Z=0,657 m, otwory na osi Y. Promień otworu **28 mm**, promień trzpienia **19 mm**, luz promieniowy **9 mm**.
- Cztery widoczne łby śrub; dwa wzmacniające gussety, dodatkowe żebra tylne, ceramiczne okładziny, bursztynowe znaczniki oraz wymienne nakładki na dolnej belce.
- Pięć materiałów PBR: grafit konstrukcyjny, tytan szczotkowany, jasna ceramika, miedź śrub, bursztynowe akcenty. Brak emisji wymuszanej sztucznie.
- **27 osobnych zamkniętych siatek, 1100 wierzchołków, 2108 trójkątów.** Statyczny uchwyt: **animacja nie jest wymagana**.

## Konstrukcja bez fałszywych otworów

Każdy z czterech pierścieni montażowych ma rzeczywistą, otwartą powierzchnię wewnętrzną (manifold toroidalny). Główne belki kończą się **przed** otworami, a kołnierze nachodzą na belki wyłącznie poza średnicą otworów. Trzpienie pozostają w świetle otworów z 9 mm luzu; łby stykają się z kołnierzami. Nakładki i wzmocnienia mają celowe zakładki montażowe. Nie są to przypadkowe przecięcia ruchomych elementów. Przed integracją należy wykonać wizualny przegląd wszystkich powierzchni i połączeń.

## Źródła i eksport

- `generate_m03_007.py`: jedyne źródło geometrii; Python 3.10+ bez zależności dla OBJ/MTL i walidacji; opcjonalnie Blender `bpy` dla GLB i BLEND.
- `validate_m03_007.py`: niezależne sprawdzenie wynikowego OBJ/MTL, topologii, czterech prawdziwych otworów i pivotu.
- `spec.json`: wymiary, punkty SNAP i deklarowane interfejsy.
- `.github/workflows/ci-m03-007.yml`: deterministyczne generowanie, walidacja oraz rzeczywisty eksport w Blenderze do artefaktów GitHub Actions.

Uruchomienie:

```bash
python3 Tools/ModelFactory/M03/M03-007/generate_m03_007.py --self-test
python3 Tools/ModelFactory/M03/M03-007/generate_m03_007.py --out build/M03-007
python3 Tools/ModelFactory/M03/M03-007/validate_m03_007.py build/M03-007
blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-007/generate_m03_007.py -- --out build/M03-007
```

Pierwsze dwa polecenia nie wymagają Blendera. W Blenderze skrypt tworzy `M03-007_vertical_T_bracket.glb` i `.blend`. Generator usuwa tylko kolekcję `MODEL_M03_007`, a przy ponownym uruchomieniu nie mnoży własnych obiektów. W workflow CI pliki OBJ/MTL i GLB/BLEND są udostępniane jako artefakty; **nie należy twierdzić, że eksport Blender lub Unity przeszedł, dopóki log CI nie potwierdzi wyniku**.

## QA i ograniczenia

Walidator kontroluje spójność siatek (każda krawędź w dokładnie dwóch trójkątach), dodatnią objętość i niedegenerujące trójkąty. Dodatkowe testy obejmują otwory i wymiary oraz deterministyczność OBJ/MTL. Renderowane oględziny w Blenderze, import i PlayMode w Unity 6, rzeczywiste kolizje w prefabach i podgląd materiałów są osobnymi bramkami. W szczególności brak fizycznego `Collider` w assetach — ma go zdefiniować właściciel integracji.

**Claim:** `Docs/ModelFactory/claims/M03-007.json` na `feature/rrl-model-factory-queue-v1`, atomowy CREATE commit `6ac0597f1cd83ddc8c50324f5800d772ad677109`, blob `e411a0f62d66f8d10ace3ab062570279fe9ea756`, 2026-10-24T16:43:46.153Z. Tylko M03-007. Bez zmian scen, `ProjectSettings`, innych modeli lub rozgrywki.
