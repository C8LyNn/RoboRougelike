# M02-002 — Koło wąskie do prędkości

**Zakres:** wyłącznie nowy model wizualny; nie dodaje napędu, gameplayu ani zmian w istniejących scenach. Pozycja pochodzi z katalogu Model Factory V1 (kolejka M02).

## Wygląd i mechanika

- Wąska opona drogowa: promień **0,523 m**, szerokość **0,180 m**; delikatnie wypukły profil, dwie modelowane rowkowe linie bieżnika.
- **10 lekkich szprych** z ceramicznymi wstawkami, pierścień tytanowy, akcent miedziano-bursztynowy, 7 materiałów.
- Boczne mocowanie: płaszczyzna montażowa **X=0**, oś obrotu **+X**, środek opony **X=0,185 m**. Koło wystaje na zewnątrz pojazdu w kierunku +X.
- Statyczny wałek ma promień 0,033 m, otwór łożyska 0,043 m: **10 mm luzu promieniowego**; wałek i mocowanie pozostają nieruchome, reszta obraca się.
- Animacja w `M02-002_narrow_speed_wheel_animated.gltf`: obrót 360° w 2 s, 9 klatek kwaternionowych (co 45°), animowany tylko węzeł `Wheel_ROTATE_X`; `StationaryMount_X0` nie obraca się.

## Artefakty

- `generate_m02_002.py` — odtwarzalny generator Python; `--self-test` nie wymaga zależności zewnętrznych; w zwykłym trybie generuje OBJ/MTL.
- `generated/M02-002_narrow_speed_wheel.obj` i `.mtl` — statyczny model (Unity może importować OBJ, ale nie zachowuje animacji).
- `generated/M02-002_narrow_speed_wheel_animated.gltf` — animowany, samowystarczalny glTF 2.0 z osadzonym buforem, dwoma węzłami i siedmioma materiałami PBR.
- `validate_m02_002.py` — kontrola geometrii i animacji plików wynikowych.
- `generated/M02-002_static_qa.json` — statyczne metryki.

## Uruchomienie

```bash
python Tools/ModelFactory/M02/M02-002/generate_m02_002.py --self-test
python Tools/ModelFactory/M02/M02-002/generate_m02_002.py --out build/M02-002
python Tools/ModelFactory/M02/M02-002/validate_m02_002.py
```

Opcjonalnie w Blenderze 4.x+:

```bash
blender --background --python Tools/ModelFactory/M02/M02-002/generate_m02_002.py -- --out build/M02-002
```

Blender wygeneruje FBX i animowany GLB z tą samą geometrią. **Eksport Blender/FBX nie został tutaj uruchomiony** — środowisko wykonawcze nie ma `bpy`. Do Unity 6 importuj OBJ jako model statyczny albo FBX z Blendera jako model animowany. glTF wymaga kompatybilnego importera (np. glTFast); nie zakładaj, że sam Unity 6 importuje animacje glTF bez dodatku.

## Status QA

Statyczne testy geometrii oraz struktury animacji można uruchomić bez Unity. Nie zastępują one oceny wizualnej, testu clippingu w pełnym cyklu animacji w Blenderze ani testu importu w Unity. Wszystkie te testy pozostają **NOT RUN**, dopóki nie ma rzeczywistych logów i zrzutów z aplikacji. Model pozostaje w DRAFT PR, bez scalania do `develop`.

## Iteracja V2 — zamknięcie szczelin w zespole koła (24.10.2026)

**Zidentyfikowany problem:** pierwotna opona była oddzielona od obręczy szczeliną **2 mm**; dziesięć ceramicznych nakładek szprych miało **1 mm** odstępu od metalowych ramion; przedni dekiel piasty, bursztynowy pierścień i tarcza hamulcowa nie zachodziły na sąsiednie konstrukcje. Statyczne zabezpieczenie osi również nie stykało się z wałkiem. Wszystkie te elementy wyglądały na luźne, mimo że powinny stanowić jeden zespół.

**Naprawy rzeczywistej geometrii:**
- `Forged_Rim` — zewnętrzny promień zwiększony z 0,333 do **0,340 m**, czyli 5 mm osadzenia w oponie przy zachowaniu pierwotnej średnicy opony 1,046 m.
- `Spoke_Ceramic_00…09` — przednie nakładki wsunięte o **4 mm** w ramiona metalowe; nadal widoczne od frontu.
- `Amber_Front_Ring` — osadzenie w obręczy zwiększone do **4 mm**.
- `Hub_Front` — czołowy dekiel zachodzi na nośnik piasty o **3 mm**.
- `Brake_Rotor` — poszerzono zakres X do 0,135–0,151 m i wewnętrzną krawędź R do 0,108 m; uzyskano 6 mm osiowego i 4 mm promieniowego osadzenia w nośniku piasty.
- `Axle_Retainer_STATIC` — cofnięto początek do X=0,256 m, aby zachodził na nieruchomy wałek o **2 mm**. Oś obrotu pozostaje +X; prześwit promieniowy pręt–piasta **10 mm** nie zmienił się.

**Poprawione artefakty:** `generate_m02_002.py`, rzeczywisty `generated/M02-002_narrow_speed_wheel.obj`, **animowany** `generated/M02-002_narrow_speed_wheel_animated.gltf` wraz z osadzonym buforem i normalnymi, `validate_m02_002.py`, raport QA i workflow. Zmieniono położenie/normale **2000 wierzchołków** w 15 komponentach; zachowano 30 komponentów, 6016 wierzchołków, 11952 trójkąty, siedem materiałów oraz dziewięć klatek kwaternionowego obrotu. Korektę osadzonego glTF wykonano programowo, **bez Blendera**. Nie jest to nowy Blender GLB/FBX.

**Wykonane QA:** [GitHub Actions 40109497917 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40109497917). Uruchomiono test generatora, walidację 15 połączeń (pięć połączeń par głównych oraz 10 nakładek szprych), ponowne wygenerowanie OBJ/MTL i porównanie bajt w bajt, weryfikację wszystkich **6016** pozycji w OBJ wobec osadzonego glTF po konwersji osi oraz sprawdzenie normalnych i klatek animacji. Artefakt `M02-002-V2-connected-narrow-speed-wheel` zawiera spójne OBJ/MTL/glTF i raport.

**Ograniczenia:** Blender `bpy`, uruchomiony FBX/GLB exporter, podgląd animacji, Unity EditMode/PlayMode i inspekcja wizualna to nadal **NOT RUN**. Zachowane są też zamierzone rozdziały ruchomego koła i statycznego wału. Samo QA źródłowe nie zastępuje kontroli w silniku. Zmiana jest przygotowana do review w macierzystym Draft PR #45; nie ingeruje w rozgrywkę, prefabrykaty ani ProjectSettings.
