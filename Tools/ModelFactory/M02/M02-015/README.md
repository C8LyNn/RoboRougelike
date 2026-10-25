# MODEL-02 / M02-015 — Skrzynia biegów z odkrytą zębatką

**Agent:** MODEL-02. **Claim CREATE:** Docs/ModelFactory/claims/M02-015.json na feature/rrl-model-factory-queue-v1, commit 8d702c9f65da86f748a63c529d5aaada50587db6, 2026-10-25T00:43:45.271Z. **Źródło katalogu:** Docs/ModelFactory/CATALOG_V1.md na gałęzi kontrolnej. Własna gałąź od develop: feature/rrl-model-m02-015-exposed-gearbox. Draft PR do develop.

## Geometria i mechanika

Otwarta stalowa skrzynia biegów o dwóch współpracujących kołach zębatych: 18 zębów wejścia, 36 zębów wyjścia. Przełożenie prędkości wyjścia względem wejścia wynosi -1/2. Moduł 12 mm, odległość osi 324 mm. Zęby mają stylizowany trapezowy profil, nie ewolwentowy: asset wizualny, nie przekładnia do obliczeń momentu i kontaktu zębów.

Prawdziwe otwarte przestrzenie między szprychami, przelotowe otwory pierścieni, dwa stalowe wały, pierścienie łożyskowe z luzem, uszczelnienia, miedziane nity, gumowe stopy, otwarta dwupłaszczyznowa kratownica stalowa, śruby i tabliczka serwisowa. Osiem materiałów: grafit, tytan, stal, miedź, guma, ceramika, bursztyn, cyjan.

**Układ współrzędnych:** Blender Z-up, 1 jednostka = 1 metr. Pivot montażowy M02-015_MOUNT_ORIGIN__0_0_0 = (0,0,0) w spodniej płaszczyźnie stóp. Obie osie wałów +Y; wejście (-0.174,0,0.345) m, wyjście (0.150,0,0.345) m. Podstawa leży przy Z=0. Orientację w Unity zweryfikować po imporcie.

**Animacja:** INPUT_GEAR_ROT_Y i OUTPUT_GEAR_ROT_Y, 121 klatek @30 FPS, wejście +720 stopni, wyjście -360 stopni. Liniowe klatki co trzy klatki. Wały, piasty, szprychy i nity obracają się wraz ze swoimi kołami; ramy i łożyska pozostają nieruchome. Celowe połączenia geometryczne elementów tej samej sztywnej grupy są dozwolone.

## Uruchomienie

1. python3 Tools/ModelFactory/M02/M02-015/qa_m02_015.py
2. blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M02/M02-015/generate_m02_015.py -- --out build/M02-015
3. python3 Tools/ModelFactory/M02/M02-015/qa_m02_015.py --verify --out build/M02-015

Generator odtwarza scenę dwukrotnie w tej samej sesji Blendera (kontrola idempotencji). Zapisuje edytowalny BLEND, animowany GLB, próbuje eksportować FBX do Unity i zapisuje JSON QA. Walidator sprawdza 121 konfiguracji obrysów kół w XZ, odstępy od ramy, a po eksporcie prawdziwy GLB 2.0, materiały, węzły, kanały rotacji i opcjonalny plik FBX.

**Ograniczenia:** obrysy 2D i obwiednie nie zastępują pełnych kolizji trójkątów 3D, kontaktu ewolwentowego, wytrzymałości mechanicznej ani fizyki Unity. Blender/GLB/FBX = PASS wyłącznie po rzeczywistym wykonaniu. Unity import, EditMode, PlayMode, fizyka i przegląd wizualny = NOT RUN do czasu testu. Pliki binarne po CI są artefaktami workflow, nie plikami zatwierdzonymi w repo.

**Izolacja:** tylko Tools/ModelFactory/M02/M02-015/ oraz .github/workflows/ci-m02-015.yml na własnej gałęzi. Bez zmian main, develop, wspólnych scen, ProjectSettings, innych modeli i gameplayu. Nie scalać Draft PR przed niezależną weryfikacją.
