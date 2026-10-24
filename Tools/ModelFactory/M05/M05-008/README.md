# M05-008 — Bateria cylindryczna (MODEL-05)

**Źródło katalogowe:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M05-008.json`, CREATE commit `87eef0f8b4fe3e817f81737905479f6beb493ebb`, `2026-10-24T17:49:30.416Z`.
**Cel:** wyłącznie wizualny moduł baterii do Unity 6 RoboRougelike. Bez nowej mechaniki magazynowania energii.

## Konstrukcja

Pionowa grafitowa bateria cylindryczna z tytanowymi kołnierzami, sześcioma pierścieniami chłodzenia, ceramicznymi listwami osłonowymi, dwiema grubymi izolowanymi wiązkami przewodów prowadzonymi z tyłu, miedzianymi zaciskami i pierścieniami statusu LED. Na przedniej ścianie znajduje się ceramiczna klapa serwisowa z ciemnym panelem, szynami miedzianymi i ostrzegawczym bursztynowym oznaczeniem.

- Jednostki: **1 jednostka = 1 metr**; Blender **Z-up**, Unity **Y-up** po imporcie FBX.
- **Pivot główny:** `RRL_M05_008_MOUNT_ROOT` = (0,0,0), środek dolnego interfejsu mocowania. Oś baterii: +Z w Blenderze.
- **Mocowania/gniazda:** `MOUNT_BOTTOM` (0,0,0), `SERVICE_FRONT` (0,-0.345,0.43), `POWER_REAR` (0,0.355,0.25). Są to Empty w Blender/FBX, a w statycznym OBJ ich współrzędne zapisuje manifest.
- **Animacja:** `RRL_M05_008_HATCH_HINGE_X` na (0,-0.345,0.590), obrót lokalnej osi X w zakresie 0° → **−70°** → 0° (klatki 1,32,64). Wszystkie elementy klapy są rodzicami jednego przegubu.
- **Materiały:** Graphite, Titanium, Ceramic, Copper, Rubber, CyanLED, AmberLED. Emisja dotyczy tylko diod; sylwetka pozostaje czytelna bez niej.
- **Geometria:** domknięte, niezależne siatki, trójkąty o dodatniej objętości skierowanej. Celowe styki konstrukcyjne między pierścieniami a cylindrem i segmentami przewodów nie są traktowane jako przypadkowe kolizje.
- **Prześwit:** kontrola położeń klapy w 71 próbkach od 0 do −70° względem cylindrycznego korpusu i ramki frontowej. Wymagany odstęp w części roboczej >5 mm. Klapa nie przecina obudowy podczas otwierania.

## Generowanie

```bash
python3 Tools/ModelFactory/M05/M05-008/generate_m05_008.py --self-test
python3 Tools/ModelFactory/M05/M05-008/qa_m05_008.py
python3 Tools/ModelFactory/M05/M05-008/generate_m05_008.py --static-obj --out build/M05-008/static
blender -b --factory-startup --python Tools/ModelFactory/M05/M05-008/generate_m05_008.py -- --out build/M05-008/blender
blender -b --factory-startup --python Tools/ModelFactory/M05/M05-008/qa_blender_m05_008.py -- --out build/M05-008/blender
```

Statyczny OBJ+MTL jest rzeczywistym wynikiem tego samego generatora, lecz **nie przenosi animacji ani Empty**. Preferowany import do Unity: FBX; GLB stanowi dodatkowy format podglądowy. Przed integracją należy zweryfikować orientację osi, sockety, kolory, prześwity i animację w Unity. Dla Rigidbody stosować proste kolidery, nie render mesh jako pojedynczy collider dynamiczny.

## QA / ograniczenia

`qa_m05_008.py` uruchamia generator, waliduje manifold, dodatnią orientację normalnych, rozmiary, pivot, 71 próbek animacji, materiały i deterministyczność dwóch rzeczywistych eksportów OBJ/MTL. `qa_blender_m05_008.py` wykonuje generator dwukrotnie **w jednym procesie bpy**, sprawdza animację, brak przyrostu liczby siatek, zachowanie obcego obiektu, nagłówki binarne FBX/BLEND oraz faktyczną animację w GLB. Workflow CI publikuje pliki w osobnych artefaktach.

**Nie deklarować PASS przed otrzymaniem rzeczywistego wyniku workflow.** Testy Unity EditMode, PlayMode i import do Unity są `NOT RUN`, dopóki nie istnieje dowód ich wykonania. Żadne sceny, prefabry, ProjectSettings ani cudze modele nie są zmieniane.
