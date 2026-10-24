# M02-011 — Zawieszenie podwójnego wahacza

**Agent:** MODEL-02 · **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1` · **Rezerwacja:** `Docs/ModelFactory/claims/M02-011.json` · **Commit CREATE:** `af7093964cd3f2de1565c1f4c27bd16d87c106d6` · **Data UTC:** `2026-10-24T20:51:06Z`.

## Konstrukcja

Nowy, samodzielny model wizualny **podwójnego wahacza**, nie wariant ani kopia pojedynczego wahacza M02-010. Własna, górna i dolna trójkątna belka A-arm z oddzielnymi osiami mocowania i tulejami, stały rozstaw pionowy przegubów 300 mm, osobna zwrotnica z piastą sześcioma śrubami, widoczny amortyzator teleskopowy z zewnętrzną sprężyną oraz odrębne punkty mocowania do ramy. Oba wahacze poruszają się synchronicznie w zakresie **±10°** wokół dwóch osi +X, a zwrotnica przesuwa się pionowo, nie przechylając się. Jest to uproszczona kinematyka równoległoboczna; nie symuluje rzeczywistej zmiany camberu ani geometrii Ackermanna.

**Układ osi:** Blender +X przód/tył, +Y na zewnątrz pojazdu, +Z w górę. **Skala:** 1 jednostka = 1 metr. **Pivot mocowania:** `ROOT_MOUNT_PIVOT=(0,0,0)`. **Mocowanie:** boczne gniazdo pojazdu, obiekt `VEHICLE_SOCKET`. Dwie niezależne osie wahaczy na Y=0,120 m, Z=-0,140 i +0,160 m; zewnętrzny punkt osi piasty w położeniu neutralnym Y=0,600 m. Koło montuje się do czoła piasty wzdłuż +Y. Wszystkie elementy wizualne i mocowania stanowią nową paczkę — nie modyfikują istniejących prefabów ani gameplayu.

**Ruchome grupy w hierarchii Blendera:** `LOWER_ARM_RIG`, `UPPER_ARM_RIG`, `KNUCKLE_RIG`, `SPRING_RIG`, `DAMPER_RIG`, `ROD_RIG`. Amortyzator celuje od nieruchomej wieży do ruchomego punktu na dolnym wahaczu; tłoczysko teleskopuje w obudowie, a sprężyna dostosowuje długość do rzeczywistego położenia dolnego ramienia. Animacja ma 97 klatek / 24 FPS. W eksporcie GLB należy zachować hierarchię i kanały transformacji.

**Materiały:** SatinTitanium, GraphiteArmor, RubberBushing, CopperHardware, AmberCoil, ChromedRod, CyanIndicator, CeramicWhite (8 materiałów). Kolory metalu, grafitu, gumy i miedzi ułatwiają rozpoznawanie stref ruchomych. Akcenty cyjanowe nie są jedynym źródłem czytelności.

## Wytwarzanie

Statyczny podgląd OBJ/MTL i raport JSON nie wymagają Blendera:

```bash
python3 Tools/ModelFactory/M02/M02-011/generate_m02_011.py --self-test
python3 Tools/ModelFactory/M02/M02-011/generate_m02_011.py --static-obj --out build/M02-011-static
python3 Tools/ModelFactory/M02/M02-011/qa_m02_011.py
```

Prawdziwy projekt BLEND i animowany GLB wymagają `bpy`:

```bash
blender -b --factory-startup --python Tools/ModelFactory/M02/M02-011/generate_m02_011.py -- --out build/M02-011-blender
python3 Tools/ModelFactory/M02/M02-011/qa_m02_011.py build/M02-011-blender/M02-011_animated.glb
```

W repo znajduje się generator, niezależny walidator i izolowany workflow `.github/workflows/ci-m02-011.yml`. Artefakty OBJ/MTL/BLEND/GLB powinny być dostępne w Actions dopiero po faktycznie zakończonych zadaniach CI; **nie należy traktować samego kodu eksportera jako dowodu wykonania Blendera**. Skrypt usuwa własną kolekcję przed regeneracją, aby nie dublować modeli.

## Kontrole i ograniczenia

Self-test bada zamknięcie każdej siatki, niezerowe trójkąty, dodatnią objętość, 97 pozycji ruchu, stały rozstaw przegubów zwrotnicy 300 mm, zgodność punktów końcowych obu wahaczy, długość teleskopu, prześwit sprężyny do belek i luz tulei względem sworzni. Kontrole prześwitów są **analityczne**, a nie pełnym solverem kolizji siatek. Test glTF analizuje faktyczny wyeksportowany GLB i obecność kanałów rotacji, translacji i skali. Testy Unity 6 EditMode, PlayMode, fizyka i ręczna ocena wizualna nie są wykonywane przez sam generator.

**Zakres integracji:** jedynie `Tools/ModelFactory/M02/M02-011/` oraz dedykowany workflow. Bez zmian w `Assets/`, `ProjectSettings/`, wspólnych scenach, mechanikach uzbrojenia i cudzych modelach. Paczka wymaga oceny wizualnej oraz testu importu i animacji w Unity przed scaleniem.
