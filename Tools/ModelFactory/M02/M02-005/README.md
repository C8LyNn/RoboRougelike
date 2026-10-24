# M02-005 — Koło z rozkładanymi łopatkami (MODEL-02)

**ID:** M02-005, katalog \`feature/rrl-model-factory-queue-v1\`, pozycja M02-005.  
**Agent:** MODEL-02 / M02.  
**Claim:** \`Docs/ModelFactory/claims/M02-005.json\` na gałęzi katalogu; atomowy CREATE, commit \`80ab7c8a7d861dba02d5937812032862a7f81490\`, UTC \`2026-10-24T14:44:55.059Z\`.  
**Zakres:** tylko nowy model wizualny. Bez mechaniki trakcji, AI, zmian scen, ProjectSettings i prefabów.

## Konstrukcja

Boczne koło mechaniczne o gumowej obręczy (promień 0,355 m), otwartej osi montażowej i **ośmiu niezależnie rozkładanych stalowych łopatkach**. Każda łopatka jest połączona z obręczą osobnym zawiasem i ma miedziany korpus łożyska, dwa stalowe sworznie, ceramiczny pasek oraz niewielki sygnalizator bursztynowy. Ramiona zawiasów są świadomie osadzone w gumowej obręczy (mocowanie), a nie unoszą się obok koła.

- **Skala:** 1 jednostka Blender = 1 metr; źródło Z-up, eksport glTF Y-up.
- **Port:** boczna oś \`M02-005_MOUNT_SIDE_X0\` na \`(0,0,0)\`; wał wzdłuż lokalnej osi **+X**.
- **Rotacja:** \`M02-005_WHEEL_ROTOR\` — obrót 360° od klatki 1 do 121.
- **Rozkładanie:** \`M02-005_PADDLE_HINGE_00..07\` — od **80° złożenia** do **0° rozłożenia** (klatki 1, 31, 91, 121; rozkładanie, utrzymanie, składanie). Każdy zawias obraca się względem własnego punktu mocowania, a jednocześnie wraz z kołem.
- **Rzeczywisty otwór osi:** promień 78 mm; nieruchomy wał 50 mm, uszczelnienie 71 mm. Luz promieniowy wał–piasta 28 mm, uszczelnienie–piasta 7 mm.
- **Materiały:** gunmetal, tire_rubber, copper, ceramic, amber_signal. Emisja tylko jako detal, a nie jedyne wyróżnienie.
- **Prawidłowa topologia:** 70 osobnych zamkniętych brył. Elementy o celowym połączeniu konstrukcyjnym (mocowania, sworznie) mogą się pokrywać; test ruchu zabrania przecinania łopatek z oponą i wzajemnego zachodzenia sektorów łopatek.

## Generator i QA

\`generate_m02_005.py\` nie wymaga Blendera do generacji statycznego \`M02-005.obj\`, \`M02-005.mtl\` i raportu JSON. Waliduje krawędzie manifold (każda krawędź dokładnie dwa razy, przeciwny zwrot), dodatnią objętość siatek, 81 pozycji rozkładania, 25 próbek obrotu koła, odległości od opony oraz rozdzielenie kątowych sektorów sąsiednich łopatek.

W prawdziwym Blenderze tworzy dodatkowo \`M02-005.blend\` (biblioteka własnej kolekcji) oraz \`M02-005_animated.glb\`. Generator przy ponownym uruchomieniu zastępuje tylko kolekcję \`M02-005_ASSET\`; nie usuwa obiektów innych modeli. Eksport GLB obejmuje wyłącznie tę kolekcję.

\`\`\`bash
python3 Tools/ModelFactory/M02/M02-005/generate_m02_005.py --self-test --output-dir /tmp/m02-005
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M02/M02-005/generate_m02_005.py -- --output-dir /tmp/m02-005-blender --require-blender
\`\`\`

Workflow \`.github/workflows/ci-model-m02-005.yml\` uruchamia statyczne testy i, jeśli runner pozwala, instaluje **prawdziwego Blendera**, eksportuje GLB i sprawdza rzeczywisty nagłówek glTF 2.0, hierarchię 8 zawiasów oraz kanały animacji. Po pomyślnym wykonaniu może zapisać wygenerowane artefakty tylko na gałęzi tego modelu (bez zmian w develop/main). Faktyczny wynik CI należy sprawdzić w GitHub Actions.

**Nieprzeprowadzone w chwili utworzenia PR:** Unity 6 import, EditMode/PlayMode, ręczny przegląd cieniowania i testy fizyki na pojeździe. Analityczna kontrola nie jest symulacją PhysX. Statyczny OBJ przedstawia złożony stan łopatek i nie przechowuje animacji.

## Integracja z Unity 6

Import statycznego OBJ może posłużyć do podglądu. Animowany GLB wymaga zgodnego importera glTF (lub konwersji do FBX w Blenderze). Nie wolno przypisywać sił i parametrów koła na podstawie samej animacji; fizyka pojazdu, siły napędu i kolizje są oddzielnym zadaniem implementacyjnym.
