# MODEL-02 / M02-003 — Koło balonowe na piasek

**Claim:** `Docs/ModelFactory/claims/M02-003.json` na gałęzi `feature/rrl-model-factory-queue-v1`. **Commit CREATE:** `9353c9309be57840c9c119d45c267e5791c8c41b` (2026-10-24 12:48:57 UTC). Agent: `M02`. Ten ID nie może być ponownie wykorzystany.

## Koncepcja i mocowanie

Koło niskociśnieniowe do jazdy po piasku. Szeroka, niemal płaska korona opony z delikatnym wzorem lameli; zaokrąglone barki ograniczają zapadanie się w miękkim podłożu. Dziesięć lekkich szprych po każdej stronie, otwarta piasta i stalowe śruby mocujące. Bez niepotrzebnych kolców lub ostrych, postrzępionych krawędzi.

- **Jednostki:** 1 Blender/Unity unit = 1 metr; orientacja Y-up, oś boczna i obrotu lokalne **+X**.
- **Pivot / mocowanie:** środek piasty `(0,0,0)`; `MOUNT_AXLE → WHEEL_ROTATOR → mesh parts`.
- **Gabaryty:** średnica opony 1,024 m; szerokość do 0,710 m; obrys sprawdzany w teście.
- **Przelotowy otwór:** średnica 90 mm; względem referencyjnej osi 68 mm zapewnia 11 mm luzu promieniowego. Wymaga dopasowania do konkretnego pojazdu.
- **Materiały:** 7 (guma ścian, guma bieżnika, ciemne lamele, tytan, grafit, bursztynowy akcent, stal).
- **Animacja:** jeden sztywny zespół obracany o pełne 360° wokół X; 5 kluczy co 90°, klatki 1/16/31/46/61, 60 FPS. Nie jest to sterownik gameplayu. Ruch względny części nie występuje; brak kolizji wewnętrznych wynikających z animacji. Kolizje z nadwoziem i zawieszeniem: **NOT RUN**.

## Pliki i wykonanie

`generate_m02_003.py` jest źródłem proceduralnym, działającym zarówno w zwykłym Pythonie, jak i wewnątrz Blendera:

```sh
python3 Tools/ModelFactory/M02/M02-003/generate_m02_003.py --self-test
python3 Tools/ModelFactory/M02/M02-003/generate_m02_003.py --out build/M02-003
blender --background --python Tools/ModelFactory/M02/M02-003/generate_m02_003.py -- --out build/M02-003
```

Zwykły Python generuje **rzeczywiste statyczne OBJ/MTL** (obsługiwane przez importer Unity) i `M02-003_manifest.json`. Blender generuje te same OBJ/MTL oraz animowane FBX/GLB. Skrypt usuwa tylko własną kolekcję `MODEL_M02_003`, nie modyfikuje scen gry. **Blender nie został jeszcze uruchomiony; animowanych FBX/GLB nie deklarujemy jako gotowych.**

Workflow `.github/workflows/model-m02-003-qa.yml` testuje topologię i otwór osi, uruchamia eksport OBJ/MTL, sprawdza ich zawartość i porównuje dwa niezależne generowania. Wygenerowane pliki zapisuje także w `generated/` na tej gałęzi oraz udostępnia w artefakcie `M02-003-balloon-sand-wheel-OBJ-MTL` powiązanym z przebiegiem CI. Źródłem geometrii jest generator, nie ilustracja.

## Granice QA i integracji

- Test źródłowy: niezerowe pola trójkątów, zamknięcie krawędzi (każda w dwóch trójkątach), objętość, unikalność nazw, materiały, zakres wymiarów, prześwit piasty, liczba szprych i śrub.
- QA CI: realny eksport OBJ/MTL i deterministyczność. Zapisane pliki są statyczne i nie zawierają klatek animacji.
- **Unity import / Play Mode / render review / Blender export:** NOT RUN, dopóki istnieją niezależne dowody wykonania.
- Collider gameplayowy: sugerowana prosta bryła koła, nie collider każdego detalu. Fizyka jazdy na piasku i parametry przyczepności są poza zakresem M02.
- Nie zmieniać `main`, `develop`, wspólnych scen, prefabów, `ProjectSettings` ani cudzych paczek. PR ma pozostać Draft do zatwierdzenia wizualnego i Unity QA.
