# M05-020 — Nawigacyjna antena kratowa

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M05-020.json`, CREATE commit `77e5b158507e2964d7037404eae2c1ccee158065` (2026-10-25T01:25:16.371Z).

## Zakres
Nowy **model wizualny** kratowej anteny nawigacyjnej. Nie dodaje skanowania, radarów, nawigacji ani systemu sensorów. Grafitowy cokół, tytanowy maszt i kratownice, ceramiczne panele, miedziane usztywnienia, szkło optyczne, gumowa uszczelka, sygnałowa emisja cyan i amber — 8 materiałów. Dwa ażurowe skrzydła z trójkątnymi zastrzałami i listwami emitera składają się w tył, a cała głowica obraca się o 360°.

## Kontrakt techniczny
- Blender Z-up; 1 jednostka = 1 metr; +Y = przód.
- `MOUNT_TOP_ORIGIN` jest rzeczywistym punktem mocowania w (0,0,0).
- `YAW_PIVOT_Z` = (0,0,0.28) względem korzenia, oś obrotu Z.
- `HINGE_L_Z` / `HINGE_R_Z` = (-0.26,0,0.83)/(0.26,0,0.83) względem yaw, osie obrotu Z.
- Rozłożenie: klatka 42, kąt skrzydeł 0°. Złożenie: klatki 1 i 100, kąty +65° (lewe) i -65° (prawe).
- Yaw: 0° -> 360° od klatki 1 do 100, 30 FPS.
- Centralny panel ma szerokość 0.35 m; zawiasy znajdują się 0.26 m od osi, co ogranicza ryzyko przecięcia. Łączenia kratownicy mają zamierzone styki/spawy.
- `NAV_FORWARD_POS_Y`, `NAV_UP_POS_Z` i `SERVICE_PORT_REAR` są markerami orientacji/serwisu, nie funkcjami gry.

## Generowanie
```bash
blender -b --factory-startup --python Tools/ModelFactory/M05/M05-020/generate_m05_020.py -- "$PWD/generated/M05-020"
```
Skrypt generuje BLEND i GLB, próbuje również FBX, jeśli dostępny jest eksporter. Nie ingeruje w obiekty poza własną kolekcją i usuwa własne nieużywane datablocki przy ponownym wykonaniu.

## QA
```bash
blender -b --factory-startup --python Tools/ModelFactory/M05/M05-020/qa_blender_m05_020.py
```
QA sprawdza 2 uruchomienia generatora, zachowanie obcego obiektu, zamknięte manifold siatki, 100 klatek animacji i minimalny prześwit skrzydeł od centralnego panelu w lokalnym układzie głowicy. **Nie jest to pełny test par kolizyjnych wszystkich siatek.** Workflow `.github/workflows/ci-model-m05-020.yml` wykonuje test w prawdziwym Blenderze, generuje binaria i waliduje GLB. Do czasu zakończenia Actions: **Blender/eksport NOT RUN; Unity/URP NOT RUN**.

Nie modyfikować scen, ProjectSettings, mechanik ani cudzych modeli. PR ma pozostać Draft do niezależnej kontroli i testów Unity.
