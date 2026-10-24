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
