# M04-013 — Śruba wodna w pierścieniu (Ringed Water Propeller)

**MODEL-04** · Unity 6 RoboRougelike · **visual-only** · ID z katalogu `Docs/ModelFactory/CATALOG_V1.md` na gałęzi kontrolnej `feature/rrl-model-factory-queue-v1`.

**Rezerwacja:** `Docs/ModelFactory/claims/M04-013.json`, atomowy CREATE commit `84feba40925d36c372ae9dc9d83cc8f7581628ae` (2026-10-24T22:52:02.702Z). Ten model nie implementuje pływania, wyporu, hydrodynamiki ani mechaniki sterowania.

## Projekt mechaniczny i wygląd

Zespół pięciołopatowej śruby o skręconym i stopniowo zwężanym profilu, z charakterystycznym skośnym sweepem łopat. Wirnik pracuje w **otwartym kanale pierścieniowym** z przednim ceramicznym wlotem i tylnym tytanowym obramowaniem. Za śrubą znajduje się **zintegrowana kratka** z ośmioma promieniowymi prętami i centralną osłoną. Kratka nie przecina przestrzeni obrotu łopat. Zewnętrzny pierścień ma miedziany pas chłodzący, osiem wąskich radiatorów oraz dwie cyjanowe lampki serwisowe. Cztery ukośne wsporniki prowadzą obciążenia do otwartej płyty mocującej.

Materiały (7): grafit, szczotkowany tytan, ceramika, brąz łopat i pierścienia, gumowe uszczelki, cyjanowy status i bursztynowe śruby serwisowe. Czytelność sylwetki nie zależy od emisji.

**Wymiary i układ współrzędnych:** 1 jednostka = 1 m; X prawo, Y góra, **+Z tył pojazdu** (oś śruby). `MOUNT_REAR_SOCKET` to punkt `(0,0,0)`, płaszczyzna montażowa Z=0. Kanał o promieniu wewnętrznym 0,315 m, zewnętrznym 0,382 m i zakresie Z 0,435–0,880 m. Wirnik obraca się wokół `ROTOR_Z` w `(0,0,0.650)`. Z tyłu osadzono kratkę na Z 0,885–0,940 m. Pięć łopat ma rzeczywiste bryły 3D z grubością, skrętem i zamkniętą topologią, a nie płaskie trójkąty.

**Animacja:** 360° wokół lokalnej osi Z, 121 klatek @30 FPS (klucze 1, 31, 61, 91, 121, co 90°). Hierarchia `MOUNT_REAR_SOCKET → ROTOR_Z → 9 ruchomych siatek` (piasta, spinner, dwa pierścienie i pięć łopat). Grille, duct i wsporniki są nieruchome.

## Generator i testy

```bash
python3 Tools/ModelFactory/M04/M04-013/generate_m04_013.py --self-test
python3 Tools/ModelFactory/M04/M04-013/qa_m04_013.py
python3 Tools/ModelFactory/M04/M04-013/generate_m04_013.py --out build/M04-013
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M04/M04-013/generate_m04_013.py -- --out build/M04-013
python3 Tools/ModelFactory/M04/M04-013/qa_m04_013.py --glb build/M04-013/M04-013_ringed_water_propeller.glb
```

Generator Python bez Blendera produkuje **rzeczywisty OBJ + MTL + manifest JSON**, a uruchomiony z `bpy` dodatkowo **BLEND, animowany GLB i FBX**. Blender exporter zachowuje materiały PBR i emisję; statyczny MTL to tylko podgląd. FBX jest natywnie obsługiwany przez Unity, GLB wymaga odpowiedniego importera.

Kontrole: brak zdegenerowanych trójkątów, każda bryła osobno zamknięta (manifold), dodatnia objętość skierowanych ścian, unikalne nazwy, kompletność materiałów, odtwarzalność plików OBJ/MTL/JSON, zgodność faktycznych wierzchołków OBJ ze źródłem, radialny odstęp wirnika od wewnętrznej ściany kanału oraz osiowy odstęp od kratki dla całego obrotu (121 klatek). Test GLB sprawdza rzeczywiste siatki, materiały, węzły i kanał obrotu `ROTOR_Z`.

**Granice QA:** wsporniki i pasy radiatorów mogą celowo przenikać nieruchomą obudowę (montaż konstrukcyjny). Łopaty celowo zachodzą korzeniami na piastę. Testy prześwitów sprawdzają wirnik względem kanału i kratki, nie zastępują pełnej detekcji kolizji mesh–mesh, testów fizyki ani oceny wyglądu. Blender/Unity i kontrola wizualna otrzymują status **NOT RUN**, dopóki CI lub operator faktycznie ich nie wykona.

**Integracja:** importować FBX do Unity 6 jako oddzielny moduł; zachować pivot montażowy, oś +Z i osobny obrót wirnika. Nie zmieniać głównych scen, ProjectSettings, istniejących mechanik WPN/DEF/SEN ani prefabów innych agentów. Draft PR do `develop` pozostaje do przeglądu.
