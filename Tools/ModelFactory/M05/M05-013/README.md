# MODEL-05 / M05-013 — Moduł naprawczy z manipulatorem

**Zakres:** nowy, wyłącznie wizualny model 3D do Unity 6 RoboRougelike. Nie dodaje napraw, obrażeń, interakcji, IK ani skryptów gameplay.

## Źródło i rezerwacja

- Katalog: `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
- Własna kolejka: `M05`; najniższy wolny numer po potwierdzonych rezerwacjach `M05-001..012`.
- Claim: `Docs/ModelFactory/claims/M05-013.json` na **oddzielnej gałęzi kontrolnej**.
- Atomowy CREATE: **2026-10-24T22:53:10.150Z**, commit **`8a7aa354f5b90d9551064d20baee588bfda7bf83`**.
- Model: `M05-013 — Moduł naprawczy z manipulatorem`, mocowanie boczne, trzy przeguby, odmienny od chwytaka cargo i podnośnika.

## Projekt mechaniczny i wizualny

Płaska boczna płyta mocowania, silnik bazowy, trzy sztywne segmenty przegubowe, miedziane przewody chłodzenia prowadzone po każdym osobnym członie, ceramiczne panele serwisowe, osłony grafitowe, łożyska z tytanu, gumowe izolatory i wyraźnie oddzielona głowica serwisowa. Głowica zawiera cyjanową dyszę naprawczą, dwie boczne osłony, kamerę inspekcyjną i bursztynowe znaczniki. Emisja jest wyłącznie akcentem, a bryły są czytelne bez bloom.

**Materiały (8):** Armor_Graphite, Ceramic_White, Titanium, Copper, Rubber, Repair_Cyan (emisja), Signal_Amber (emisja), Optic_Black.

**Skala:** 1 jednostka = 1 metr. **Osie:** +X na zewnątrz pojazdu (kierunek ramienia), +Y w górę, Z w bok. **Punkt montażu:** `MOUNT_SIDE_SOCKET` w `(0,0,0)`, płyta zlicowana z X=0.

**Punkty przegubów w pozycji neutralnej:**

| Nazwa | XYZ (m) | Rodzic | Obrót |
|---|---|---|---|
| `M05_013_SHOULDER` | (0.270, 0, 0) | MOUNT_SIDE_SOCKET | local Z |
| `M05_013_ELBOW` | (0.650, 0, 0) | SHOULDER (offset X=0.380) | local Z |
| `M05_013_WRIST` | (0.990, 0, 0) | ELBOW (offset X=0.340) | local Z |
| `M05_013_SERVICE_TIP` | (1.316, 0, 0) | WRIST (offset X=0.326) | brak |
| `M05_013_POWER_PORT` | (0.115, -0.102, -0.135) | MOUNT_SIDE_SOCKET | brak |

**Animacja:** 36 klatek przy 30 FPS. Pozycja 1 i 36: neutralna. W klatce 19: ramię +24°, łokieć -32°, nadgarstek +14° względem własnych rodziców. Oś każdego przegubu to lokalna Z; pozycje interpolowane liniowo. Łożyska stałe i ruchome znajdują się na oddzielnych poziomach Z; osie i kołnierze mają zamierzone połączenia w przegubach. Osobne przewody pozostają przypisane do swoich segmentów — nie przecinają przegubów podczas animacji.

## Pliki i wykonanie

`generate_m05_013.py` to kompletny generator `bpy`, który w Blenderze tworzy hierarchię, materiały PBR, animację i eksportuje FBX/GLB/BLEND. W zwykłym CPython tworzy **rzeczywiste** statyczne OBJ/MTL oraz manifest JSON. `qa_m05_013.py` niezależnie kontroluje zawartość rzeczywistego OBJ, materiały, zgodność wierzchołków ze źródłem, zamknięte siatki i ruch.

```bash
python3 Tools/ModelFactory/M05/M05-013/qa_m05_013.py
python3 Tools/ModelFactory/M05/M05-013/generate_m05_013.py --out build/M05-013
blender --background --python-exit-code 1 --python Tools/ModelFactory/M05/M05-013/generate_m05_013.py -- --out build/M05-013 --repeat-check
```

**QA źródłowe:** każda bryła ma zamkniętą siatkę, dodatnią objętość i niezerowe pola trójkątów. Kinematyka sprawdza 361 położeń na całym przedziale 1–36 (również między klatkami), odległość korpusu od odcinków roboczych i brak kolizji nieprzylegających segmentów. Testy nie zastępują pełnej fizycznej detekcji kontaktu siatek; przeguby i osie mają zamierzone nakładanie.

**QA Blender:** wykonanie generatora dwukrotnie w jednym procesie, brak namnażania własnych obiektów/siatek/materiałów, zachowanie obcego obiektu, kontrola rzeczywistych transformacji końcówki w 36 klatkach, eksport FBX/GLB/BLEND i niezależne sprawdzenie nagłówka i animacji GLB w CI.

**Unity:** brak automatycznego importu do projektu, brak zmian w `Assets`, scenach, `ProjectSettings` i gameplay. `Unity import`, `EditMode`, `PlayMode` i ręczna kontrola wizualna pozostają **NOT RUN** do czasu faktycznego wykonania. FBX jest formatem natywnym dla Unity; GLB wymaga odpowiedniego importera. Nie zakładać, że materiał emisyjny GLB przełoży się automatycznie na bloom URP.

**Integracja:** wyłącznie izolowany Draft PR do `develop`; nie scalać bez wizualnej kontroli w Unity. Zgodność z prefabami/gniazdami gry pozostaje do sprawdzenia.
