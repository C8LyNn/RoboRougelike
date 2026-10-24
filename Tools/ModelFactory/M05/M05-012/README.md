# MODEL-05 / M05-012 — Generator awaryjny

**Źródło prawdy:** `Docs/ModelFactory/CATALOG_V1.md` z gałęzi
`feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M05-012.json`,
SHA rezerwacji `8c8dc2fd6c128872ae59c9dcbf3b0cf830c9d1a0`,
czas `2026-10-24T21:50:51.040Z`. Agent: MODEL-05.

## Zakres i identyfikacja

Wyłącznie wizualny **awaryjny alternator** w spawanej klatce serwisowej,
nie kolejny reaktor ani nowa mechanika energii. Rozpoznawalny po
żółtych pasach serwisowych, metalowych prądnicach, widocznym
miedzianym uzwojeniu, czarnych amortyzatorach, dwu diodach
(READY zielona / FAULT czerwona), przedniej osłonie i uchwycie.
Generator ma 9 materiałów: Graphite, Titanium, Copper, Ceramic,
Gasket, ServiceYellow, StatusGreen (emission), StatusRed (emission)
i CoolantBlue. Emisja jest dodatkiem, nie jedyną cechą designu.

**Skala:** 1 jednostka Blender = 1 metr, +Z do góry.
**Mocowanie:** wewnątrz ramy. `MOUNT_FRAME_ORIGIN=(0,0,0)` leży
na dolnej płaszczyźnie; cztery śruby pomocnicze `MOUNT_FRAME_PP/PN/NP/NN`
w `(±0.422,±0.302,0)`. Baza ma 1,02 × 0,78 m.
Eksport FBX: `axis_forward="-Z"`, `axis_up="Y"`.

**Animacja:** prądnica obraca się wokół lokalnej osi **+X**
w punkcie `(0,0,0.43)`, w klatkach 1/16/31/46/61 (360°),
60 FPS. Jeden parent `ALTERNATOR_ROTOR_X_PIVOT` przenosi wał,
laminowany rdzeń, miedziane pokrywy i uzwojenia, osiem metalowych
łopatek chłodzących i osiem żółtych indeksów. Nie jest to nowa
implementacja generatora energii w Unity.

**Kontrola prześwitów:** wewnętrzny promień stojana wynosi 235 mm,
a wszystkie animowane wierzchołki muszą pozostać w promieniu 220 mm,
czyli z zapasem co najmniej 15 mm. Wał o promieniu 38 mm obraca się
w pierścieniach łożyskowych o minimalnym promieniu 45 mm, co
pozostawia 7 mm nominalnego luzu. Przednia i tylna kratka są
poza maksymalnym końcem wału. QA próbuje wszystkie 61 klatek
i analizuje rzeczywiste współrzędne wierzchołków z Blendera.
Nie zastępuje to pełnego testu kolizji i przeglądu wizualnego w Unity.

## Uruchomienie

```bash
blender -b -t 2 --python Tools/ModelFactory/M05/M05-012/qa_blender_m05_012.py
blender -b -t 2 --python Tools/ModelFactory/M05/M05-012/generate_m05_012.py -- --out build/M05-012
```

Generator tworzy `M05-012_emergency_generator.glb`,
`M05-012_emergency_generator.fbx`,
`M05-012_emergency_generator.blend` i `M05-012_qa.json`.
Workflow `.github/workflows/ci-model-m05-012.yml` publikuje artefakt
`M05-012-emergency-generator-GLB-FBX-BLEND-QA` przez 14 dni.

QA wymaga: poprawnej topologii i dodatniej objętości siatek,
co najmniej 3 materiałów (tu 9), 5 markerów mocowania,
stabilności zasobów po dwóch uruchomieniach w tej samej scenie,
zachowania obcego obiektu, rzeczywistej animacji, kontroli prześwitów,
GLB z animacją i materiałami oraz plików FBX/BLEND.

**Unity 6 / URP / EditMode / PlayMode:** `NOT RUN`, dopóki nie
ma rzeczywistego dowodu uruchomienia. Brak zmian w scenach,
ProjectSettings, prefabach, gameplayu i cudzych paczkach.
Gałąź modelu nie jest scalana automatycznie. PR ma status Draft.
