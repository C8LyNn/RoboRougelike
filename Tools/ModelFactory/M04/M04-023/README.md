# M04-023 — Przepustnica jonowa

**Katalog:** Docs/ModelFactory/CATALOG_V1.md, kolejka M04 na gałęzi kontrolnej feature/rrl-model-factory-queue-v1. **Typ:** wyłącznie model wizualny, bez nowej mechaniki gry.

## Sylwetka i montaż

Tylny moduł przepustnicy jonowej: gruby ceramiczny pierścień wylotowy, miedziany induktor, grafitowy kanał, cztery tytanowe wsporniki, sześć irydowych przysłon z ceramicznymi inkrustacjami, sześć małych błękitnych soczewek oraz duża centralna soczewka ogniskująca. Osobne cyjanowe ścieżki jonowe i bursztynowe wskaźniki serwisowe. Osiem materiałów PBR; emisja jest dodatkiem, a nie jedynym sposobem identyfikacji kształtu.

**Skala:** 1 jednostka Blendera = 1 m w Unity. **Osie:** +Z góra, -Y tył.
**ROOT_MOUNT_Y0:** (0, 0, 0), interfejs do tylnej płaszczyzny ramy Y=0. Nie obracać modelu wokół środka dyszy podczas montażu: zachować ROOT_MOUNT_Y0.

## Animacja

Sześć osobnych pivotów SHUTTER_01_SLIDE ... SHUTTER_06_SLIDE. Każdy przesuwa swoją przysłonę promieniowo na zewnątrz o **70 mm**. Klatki 1/32/64/95/121: zamknięta/otwarta/otwarta/zamknięta/zamknięta, 30 FPS. Nie jest to fizyczne zamknięcie szczelne: pomiędzy sześcioma sektorami pozostaje **20° odstępu kątowego**. Model prezentuje sterowanie przepływem jonowym, nie symuluje rzeczywistego napędu.

**Kontrole analityczne:** skrajny promień przysłon 0,560 m, wewnętrzny promień pierścienia 0,610 m: min. **50 mm** odstępu. Najbliższy punkt przysłon od środka 0,250 m, promień centralnej soczewki 0,180 m: min. **70 mm** odstępu. Dodatkowe soczewki i prowadnice są odsunięte osiowo od ruchomych przysłon. Generator próbuje 121 klatek i sprawdza manifold wszystkich siatek. Jest to kontrola obwiedni, a nie ciągła detekcja zderzeń trójkątów ani walidacja fizyki Unity.

## Generowanie i QA

Blender 4.x/5.x:

    blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-023/generate_m04_023.py -- --out build/M04-023

Wynik: M04-023.blend, animowany M04-023.glb, manifest.json.

    python3 Tools/ModelFactory/M04/M04-023/qa_m04_023.py --glb build/M04-023/M04-023.glb --manifest build/M04-023/manifest.json --report build/M04-023/qa.json

Skrypt czyści scenę przy każdym wykonaniu, nie dubluje obiektów. GitHub Actions wykonuje Blender, ponownie otwiera BLEND, waliduje GLB i publikuje artefakty. Nie ma modyfikacji scen, prefabów, ProjectSettings, fizyki ani modułów innych agentów.

**Ograniczenia:** brak pełnego testu mesh-mesh swept collision, ręcznego przeglądu wyglądu, importu Unity 6 oraz testów EditMode/PlayMode w tym samym workflow. Do czasu ich faktycznego wykonania raportować NOT RUN.
