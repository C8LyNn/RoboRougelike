# M01-017 — Szybkostrzelny miotacz kul

**Źródło:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Zakres:** wyłącznie model wizualny do Unity 6 RoboRougelike; bez zmian gameplayu WPN.

## Sylwetka i materiały
Wydłużona, rzeczywiście wydrążona lufa wyrzutowa, wielka kulista komora zasobnika z trzema zewnętrznymi obręczami oraz odsłonięty boczny ośmiopozycyjny podajnik kul. Osobny wspornik łożyska podajnika i przemysłowa podstawa montażowa. Materiały: Graphite, Titanium, CeramicWhite, Copper, SignalOrange, EmissiveCyan, Rubber i AmmoSteel. Oznaczenia nie polegają wyłącznie na emisji.

## Układ współrzędnych i animacja
- 1 jednostka Blendera = 1 metr.
- Blender: Z-up, -Y do przodu. Eksport GLB/FBX: Y-up, +Z do przodu.
- `MOUNT_TOP_SOCKET`: punkt (0,0,0) w podstawie.
- `FEED_ROTOR_X_AXIS`: pivot (0.67,-0.25,0.84) w Blenderze; oś lokalna X.
- Podajnik: 6 obrotów w 73 klatkach przy 60 FPS; klatki co 3, interpolacja liniowa.
- Cała ruchoma geometria jest dzieckiem pivotu. Obudowa, kulisty zasobnik, lufa i wspornik łożyska są statyczne.
- Prześwity między ruchomym podajnikiem a statycznymi częściami są testowane na każdej z 73 klatek (AABB). Styki wewnątrz jednego zespołu ruchomego są zamierzone. AABB nie dowodzi braku wszystkich kolizji rzeczywistych powierzchni pomiędzy próbkami.

## Generowanie i QA
```bash
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-017/generate_m01_017.py -- --repeat --out Tools/ModelFactory/M01/M01-017/generated
python3 Tools/ModelFactory/M01/M01-017/qa_m01_017.py --out Tools/ModelFactory/M01/M01-017/generated
```

Generator usuwa obiekty, osierocone siatki, materiały i akcje przed kolejnym przebiegiem. `--repeat` sprawdza deterministyczną liczbę siatek i materiałów. QA sprawdza zamknięcie siatek, brak degeneratów, dodatnią objętość, animację, prześwity AABB i binarną strukturę eksportów. Po udanym CI dostępne są: `.blend`, animowany `.glb`, `.fbx`, statyczne `.obj` + `.mtl` i `_qa.json`. Pipeline próbuje również opublikować wygenerowane pliki na własnej gałęzi.

## Ograniczenia
Import Unity 6, testy EditMode/PlayMode, test ciągłych kolizji siatek, przegląd wizualny i integracja gameplayu wymagają odrębnej weryfikacji. Nie zmieniać scen, prefabów, ProjectSettings ani istniejących mechanik WPN.
