# M04-018 — Kotwica stabilizująca

Proceduralny model **czteroramiennej kotwicy stabilizującej** do RoboRougelike (Unity 6, wizualizacja bez logiki gameplayowej).

## Sylwetka i materiały

Ośmiokątna płyta montażowa, uszczelniony grafitowy cylinder, miedziany pierścień serwisowy, teleskopowy tytanowy tłok oraz cztery masywne pazury z ceramicznymi kolcami i gumowymi stopami. 8 materiałów PBR: grafit, tytan, miedź, ceramika, guma, ciemna stal, cyjanowa i bursztynowa emisja. Każdy pazur ma oddzielny boczny przegub.

## Układ odniesienia i montaż

- **1 Blender unit = 1 m**, +Y w górę, +Z do przodu.
- **Pivot całego modelu:** `MOUNT_UNDERSIDE_ORIGIN` w (0,0,0), płaszczyzna spodu ramy Y=0.
- Model jest poniżej ramy (ujemne Y). Elementów nie montować do górnej płaszczyzny.
- `CARRIAGE_PIVOT_Y_TELESCOPE`: wysuw tłoka w osi -Y o **0,22 m**.
- `CLAW_DEPLOY_PIVOT_Z_00..03`: cztery lokalne osie Z, rozwarcie **0–52°**.
- Klatki **1–140**, **30 FPS**. Kolejność: wysunięcie, rozłożenie, zatrzymanie, złożenie, schowanie.

## Uruchomienie

```bash
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-018/generate_m04_018.py -- --out build/M04-018
python3 Tools/ModelFactory/M04/M04-018/qa_m04_018.py --out build/M04-018
```

Generator czyści scenę przed budową i tworzy BLEND, animowany GLB, OBJ/MTL (gdy eksporter dostępny) oraz JSON manifest. Import do Unity należy wykonać oddzielnie — nie zmieniamy scen, prefabs, ProjectSettings ani kodu gameplayu.

## Zakres QA i ograniczenia

Generator sprawdza zamknięte siatki, powierzchnie niezerowe i wszystkie krawędzie (każda użyta dwukrotnie). Dla ponad 25 klatek analizuje rzeczywiste transformowane wierzchołki wszystkich czterech pazurów względem radialnej obwiedni obudowy oraz separacji kątowej między sektorami. Kontakty w przegubach i celowe wsunięcie tłoka w tuleję są **konstrukcyjnie zamierzone** i wyłączone z testu wolnego prześwitu. Nie jest to dokładny test kolizji trójkąt–trójkąt ani test fizyki Unity. Status Unity: **NOT RUN** do czasu wykonania prawdziwego importu/testów.

**Claim:** `Docs/ModelFactory/claims/M04-018.json` na gałęzi `feature/rrl-model-factory-queue-v1`.
