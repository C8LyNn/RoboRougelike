# M04-011 — Rozkładana płetwa aerodynamiczna

**Źródło:** katalog `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. Kolejka **M04**, identyfikator **M04-011**. Atomowy claim: `a224774439dc883c4cc84f006674583e5cac4a50`.

## Projekt

Wizualny moduł mobilności specjalnej do Unity 6, bez implementowania fizyki aerodynamiki ani gameplayu. Boczne mocowanie **X=0** (normalna **+X** na zewnątrz kadłuba); Blender Z-up, 1 jednostka = 1 metr. Trzy odrębne płetwy tworzą segmentową, zwężającą się powierzchnię. Każda ma własną stalową oś, parę łożysk, grafitowy płat, metalowe wzmocnienia krawędzi, miedziane żebro, ceramiczną końcówkę, cyjanowy akcent i bursztynowy wskaźnik. Zastosowano 7 materiałów; emisja jest dodatkiem do fizycznie czytelnej geometrii.

Trzy osie obrotu są **równoległe do Y**, w położeniach `(0.180, -0.270, 0.160)`, `(0.180, 0, 0.160)` i `(0.180, +0.270, 0.160)` m. Ruch 0 → **−78° / −82° / −84°** → 0, ze stopniowanym początkiem i zatrzymaniem; 96 klatek przy 30 FPS. Ujemny obrót Y unosi płaty ku +Z. Przy złożeniu długość X wynosi 0,710 m; trzy płaty nie nachodzą na siebie wzdłuż Y. Montaż znajduje się **z boku**, nie na górze.

## Generowanie

```bash
python3 Tools/ModelFactory/M04/M04-011/generate_m04_011.py --self-test
python3 Tools/ModelFactory/M04/M04-011/qa_m04_011.py
python3 Tools/ModelFactory/M04/M04-011/generate_m04_011.py --static-obj --out build/M04-011
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-011/generate_m04_011.py -- --out build/M04-011
python3 Tools/ModelFactory/M04/M04-011/qa_m04_011.py --check-glb build/M04-011/M04-011_deployable_aero_fin.glb
```

Skrypt `bpy` usuwa przy ponownym uruchomieniu tylko kolekcję `RRL_M04_011_DEPLOYABLE_AERO_FIN`; pozostałych kolekcji nie rusza. Eksport `GLB` korzysta z zaznaczenia wyłącznie elementów tego modułu. Wygenerowane statyczne `OBJ/MTL/JSON` oraz animowane `.blend/.glb` publikowane są jako artefakty GitHub Actions, nie jako ręcznie dopisywane pliki binarne w repo.

## QA i ograniczenia

- 51 zamkniętych obiektów, 1642 wierzchołki, 3104 trójkąty, 7 materiałów.
- Każdy obiekt: zamknięta dwukierunkowa siatka manifold, brak trójkątów o zerowym polu, dodatnia objętość, spójna orientacja ścian.
- **81 próbek** wychyłu, **34 992** porównania konserwatywnych obwiedni AABB ruchomych płatów ze statycznymi elementami niebędącymi łożyskami. Najmniejszy wynik: **16 mm**. Oddzielnie zweryfikowano rozdzielenie sąsiednich zespołów na osi Y.
- Łożyska i obracające się wałki sprawdzane odrębnie: wewnętrzny promień łożyska 19 mm, promień wału 14 mm, **5 mm luzu promieniowego**. Ich celowy kontakt konstrukcyjny w osi Y nie jest traktowany jako kolizja.
- Niezależne QA odczytuje **faktyczny OBJ**, sprawdza topologię, liczbę materiałów, wymiary i **bajtową deterministyczność** eksportu OBJ/MTL/JSON w dwóch procesach. QA GLB sprawdza 51 siatek, materiały i trzy kanały obrotu odpowiadające osiom płetw.
- Test AABB jest konserwatywną kontrolą geometrii, **nie** testem fizyki Unity ani oceną wizualną renderu. Po imporcie do Unity należy sprawdzić normalne, materiał emisji, transformacje osi, działanie trzech animacji oraz obwiednię całego robota.
- **Unity EditMode/PlayMode i import do Unity: NOT RUN** do czasu rzeczywistego wykonania. Blender/GLB PASS wolno zadeklarować dopiero po zielonym jobie CI, nie na podstawie kodu.

**Zakres zmian:** wyłącznie `Tools/ModelFactory/M04/M04-011/` oraz `.github/workflows/ci-m04-011.yml`. Bez zmian scen, ProjectSettings, innych modeli, mechanik lub istniejących prefabów. Draft PR do `develop`, bez merge.
