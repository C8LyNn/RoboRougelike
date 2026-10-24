# M02-014 — Jednostka napędu hybrydowego

**Agent:** MODEL-02. **Katalog:** `M02-014`, gałąź `feature/rrl-model-factory-queue-v1`.
**Claim CREATE:** `Docs/ModelFactory/claims/M02-014.json`, commit `1a228d10c187edf1d36991e67d5c0a45db013537`, 2026-10-24T23:48:05.010Z. Claim pozostaje WYŁĄCZNIE na gałęzi katalogowej.

## Mechanika i wygląd

Moduł hybrydowy z baterią czterosegmentową, falownikiem, chłodzeniem, silnikiem elektrycznym, wirującą perforowaną osłoną wirnika (rotor bell) wewnątrz **nieruchomej** klatki bezpieczeństwa i przekładnią planetarną. Siedem materiałów: grafit, tytan, miedź, guma, ceramika, niebieskie chłodzenie i bursztynowe wskaźniki.

Przekładnia planetarna: koło słoneczne 16T, nieruchomy pierścień odpowiadający 48T, trzy satelity 16T, obracający się kosz i wał wyjściowy. Przy nieruchomym pierścieniu redukcja wynosi 4:1; satelity mają ruch obiegowy i własny. Widoczne zęby są **stylizowane**, z minimalnym luzem radialnym 0,5 mm i nie są profilami ewolwentowymi. Nie wolno używać tego modelu do symulacji kontaktu zębów ani wyliczania przenoszonego momentu. Kinematyka animacji jest referencyjna i nie zastępuje napędu gameplay.

**Układ współrzędnych:** 1 Blender unit = 1 metr (docelowo 1 Unity unit). +X = oś wału i wyjście napędu, +Z = góra, +Y = bok. Pivot montażowy `M02-014_MOUNT_FRAME__ORIGIN` = (0,0,0), płaszczyzna mocowania Z=0. Cztery stopy mocujące, widoczny wał wyjściowy i osobne nieruchome elementy.

**Ruch:** 121 klatek @30 FPS, rotor 360° wokół +X, kosz i wał wyjściowy 90°, satelity dodatkowo -270° lokalnie względem kosza. Animacja pętli referencyjnej. Klatki skrajne mają ten sam geometryczny prześwit radialny dzięki osiowej symetrii obwiedni.

## Procedura

```bash
python3 Tools/ModelFactory/M02/M02-014/qa_m02_014.py
blender --background --threads 2 --python Tools/ModelFactory/M02/M02-014/generate_m02_014.py -- --out build/M02-014
python3 Tools/ModelFactory/M02/M02-014/qa_m02_014.py --export --out build/M02-014
```

Skrypt dwukrotnie odtwarza scenę w tej samej sesji (kontrola idempotencji), zapisuje edytowalny BLEND, animowany GLB oraz raport JSON. GLB jest formatem glTF 2.0, nie FBX. W Unity 6 wymaga importera glTF, np. glTFast, i sprawdzenia materiałów URP oraz osi/pivotów. Nie zmienia scen, ProjectSettings, prefabów, mechaniki ani istniejących assetów.

## Zakres QA i ograniczenia

Statyczne kontrole sprawdzają poprawność składni, konfigurację przekładni, luz satelitów od pierścienia i słońca oraz obwiednię wirnika. Walidator eksportu sprawdza rzeczywiste BLEND, GLB, glTF 2.0, materiały i kanały animacji. **Nie jest to pełna kolizja trójkąt-trójkąt ani fizyka Unity**. Styki montażowe i części połączone na stałe mogą celowo mieć wspólne powierzchnie. Status Blender/GLB = PASS dopiero po rzeczywistym wykonaniu workflow. Unity EditMode, PlayMode, fizyka pojazdu i ręczny przegląd = NOT RUN, dopóki nie zostaną wykonane.

PR jest DRAFT do `develop`; brak automatycznego merge.
