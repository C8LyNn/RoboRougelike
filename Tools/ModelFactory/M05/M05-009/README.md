# M05-009 — Chłodnica cieczowa boczna (MODEL-05)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M05-009.json`, atomowy CREATE commit `b273126cf96c242ec32882e696d74887ff72d539`, `2026-10-24T18:53:58.045Z`. **Agent:** MODEL-05. **Status:** CLAIMED.

## Przeznaczenie i wygląd

Wyłącznie model wizualny chłodnicy cieczowej montowanej do **bocznego gniazda** pojazdu w Unity 6 RoboRougelike. Bez dodawania chłodzenia do logiki gry, bez modyfikacji istniejących sensorów, WPN, DEF, scen ani ProjectSettings.

Przemysłowa kaseta wymiennika ciepła: 21 tytanowych lameli, dwa miedziane kolektory cieczy, obwodowe obwody chłodzenia z widocznymi podświetlanymi wskaźnikami płynu, grafitowy korpus, ceramiczne osłony narożne i szeroki wentylator kanałowy z **siedmioma zakrzywionymi łopatkami**. Kolorystyka: ciemny grafit, srebrzysty tytan, ciepła miedź, matowa ceramika, turkusowy coolant, bursztynowe znaczniki. 8 materiałów, emisja tylko na wskaźnikach.

## Osie, mocowania, ruch

- Skala: **1 Blender Unit = 1 m**. Blender **Z-up**, +X na zewnątrz pojazdu, Y szerokość. Konwersję do Unity Y-up i rzeczywiste osadzenie należy sprawdzić po imporcie.
- Pivot całego modułu i **`MOUNT_SIDE_SOCKET`**: **(0, 0, 0)** w płaszczyźnie mocowania. Oś wyjściowa gniazda: **+X**.
- Cztery puste punkty śrub montażowych `MOUNT_BOLT_±1_±1`: X=0, Y=±0,317 m, Z=±0,236 m.
- Pivot wentylatora **`ANIM_FAN_ROTOR_X`**: **(0,450, 0, 0,025) m**, obrót lokalnej osi **+X**, pełne 360° w klatkach **1→61 @60 fps**, interpolacja liniowa, cykl.
- Promień zewnętrzny łopatek: 0,178 m; promień wewnętrzny nieruchomej osłony kanału: 0,205 m; minimalny prześwit promieniowy **27 mm**, niezależny od kąta obrotu.
- Analizowane granice osiowe: przód żeber x≤0,328 m, tył wirnika x≥0,423 m (minimum **95 mm**); przód wirnika x≤0,483 m, tył przedniej kratki x≥0,543 m (minimum **60 mm**). Sprawdzane w kodzie jako konserwatywne obwiednie; styki mocujące osłonę do kasety są zamierzone.

## Generator i QA

`generate_m05_009.py` wymaga prawdziwego `bpy`. Nie tworzy pozorowanego FBX/GLB bez Blendera. Tworzy zamknięte bryły, fazowane panele, rurki, obręcze i ruchomy wirnik. Sprząta wyłącznie własną kolekcję `RRL_M05_009`, zachowując obce obiekty. Parametr `--repeat-check` uruchamia dwa przebiegi w jednym procesie, porównuje liczby mesh/curve/action/material oraz kontroluje obcy obiekt sentinel.

```bash
blender -b --factory-startup --python Tools/ModelFactory/M05/M05-009/generate_m05_009.py -- --out build/M05-009 --repeat-check
python3 Tools/ModelFactory/M05/M05-009/qa_artifacts.py build/M05-009
```

Eksportuje **FBX** (zalecany import Unity), **GLB** (podgląd animacji), **BLEND** i `M05-009_QA.json`. `qa_artifacts.py` niezależnie czyta rzeczywisty GLB i sprawdza animację, hierarchię pivotu, gniazdo, materiały, liczbę siatek oraz nagłówki FBX/BLEND.

**Brak dostępnego lokalnie Blendera i Unity w środowisku przygotowania.** Status QA zmienia się na PASS tylko po faktycznym wykonaniu CI. Blenderowe pliki generuje GitHub Actions (artefakt z 14-dniową retencją), nie są deklarowane jako istniejące w repo przed uruchomieniem. Unity EditMode, PlayMode, import, ocena wizualna i weryfikacja osi pozostają **NOT RUN** do uzyskania dowodu z Unity. Model nie dodaje funkcji chłodzenia do gameplayu.
