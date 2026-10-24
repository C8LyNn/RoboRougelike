# MODEL-04 / M04-012 — Lotka korekcyjna tylna

**Źródło:** [katalog M04](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md), pozycja **M04-012**. Claim `Docs/ModelFactory/claims/M04-012.json` utworzony atomowo na gałęzi kontrolnej, commit `790d2846649009a6e33cb4aa7f830e604b622207`.

## Przeznaczenie i wygląd

Tylny ster korekcyjny do modułowego pojazdu Unity 6: szeroka, trójpanelowa lotka o ciemnych grafitowych powierzchniach, wyraźnych tytanowych żebrach pancernych, ceramicznych zakończeniach, miedzianym grzbiecie i dyskretnych cyjanowych oraz bursztynowych akcentach. Dwa boczne wysięgniki przenoszą siły z tylnej płyty montażowej na przegub osiowy; widoczne puste łożyska zapewniają fizyczny luz na obrót osi.

**Układ współrzędnych:** 1 jednostka Blendera = 1 m; Z do góry; -Y to tył pojazdu. Powierzchnia montażu tylnego gniazda ramy znajduje się na **Y=0**. Pivot obrotu lotki: **(0, -0.370, 0.180) m**, oś **lokalna X**, kąt **-16° do +20°**. Ruch `0° → +12° → +20° → 0° → -16° → -8° → 0°`, 121 klatek / 30 FPS. Wersja modelowa i animacja nie implementują sił aerodynamicznych ani gameplayu.

## Uruchamianie

```bash
python3 Tools/ModelFactory/M04/M04-012/generate_m04_012.py --self-test
python3 Tools/ModelFactory/M04/M04-012/qa_m04_012.py
python3 Tools/ModelFactory/M04/M04-012/generate_m04_012.py --static-obj --out build/M04-012
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-012/generate_m04_012.py -- --out build/M04-012
python3 Tools/ModelFactory/M04/M04-012/qa_m04_012.py --check-glb build/M04-012/M04-012_rear_correction_aileron.glb
```

Generator Blender `bpy` usuwa tylko własną kolekcję `RRL_M04_012_REAR_CORRECTION_AILERON` przed ponownym utworzeniem obiektów. Tworzy siedem materiałów PBR, z wyraźną emisją jedynie w akcentach. Zapisuje `.blend`, animowany `.glb`, statyczny `.obj`, `.mtl` oraz manifesty JSON. Eksport statyczny nie wymaga Blendera; wszystkie geometrie są trójkątami o zamkniętych, poprawnie skierowanych powierzchniach. Wygładzone cylindry mają płaskie zamknięcia.

## QA i ograniczenia

Test źródłowy sprawdza każdą siatkę (manifold, orientację ścian, dodatnią objętość i brak degeneratów), siedem materiałów, rzeczywisty otwór łożyska o promieniu 22 mm względem wału 16 mm (**6 mm luzu promieniowego**), 73 równomiernie rozłożone kąty wychylenia i konserwatywne AABB między lotką a nieruchomymi elementami niebędącymi łożyskami. Niezależne QA ponownie parsuje zapisany OBJ, sprawdza geometrię i porównuje dwukrotny eksport bajt po bajcie. Test GLB sprawdza rzeczywisty kanał rotacji przegubu. Przenikanie ozdobnych żeber z własnymi panelami jest zamierzonym sposobem ich osadzenia. Analiza AABB nie zastępuje dokładnego testu siatek ani testów fizyki Unity.

**Unity EditMode/PlayMode, rzeczywisty import Unity oraz ręczna inspekcja wizualna** nie są automatycznie zaliczone i pozostają **NOT RUN**, dopóki nie zostaną uruchomione. Repozytorium nie zawiera żadnych zmian w głównych scenach, ProjectSettings, prefabach ani istniejących mechanikach.
