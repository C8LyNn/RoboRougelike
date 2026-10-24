# M03-003 — Rama mostowa 2×1×1 m

**Agent:** MODEL-03 (ramy i struktury) · **ID:** M03-003 · **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.

## Zakres i wygląd

Dwa niezależne, masywne dźwigary dwuteowe (web + dwie półki na każdy dźwigar), sześć poprzeczek łączących, cztery pionowe narożne słupy, sześć nośników portów SNAP, oznaczenia serwisowe, śruby i okładziny. Rozmiar fizycznej bryły: **2.000 × 1.000 × 1.000 m** (X × Y × Z). Materiały: Gunmetal, Steel, Ceramic, Amber, Rubber (5). Bez animacji — katalog przewiduje sztywną ramę.

**Pivot:** `RRL_M03_003_GRID_ROOT` = (0,0,0), środek modularnej siatki. **Mocowania:** `SNAP_X_POS` (+1,0,0), `SNAP_X_NEG` (−1,0,0), `SNAP_Y_POS` (0,+0.5,0), `SNAP_Y_NEG` (0,−0.5,0), `SNAP_Z_POS` (0,0,+0.5), `SNAP_Z_NEG` (0,0,−0.5). Lokalna oś +Z każdego socketu wskazuje na zewnątrz. W FBX/GLB Blender Z-up zostaje przekształcony do Unity Y-up.

**Styki:** 25 mm połączenie środników z półkami dwuteowników, poprzeczki do środników, słupy narożne do dźwigarów, dystanse do płyt portów oraz mocowania śrub są zamierzonymi stykami/zakładkami konstrukcyjnymi. Nie traktować ich jako losowych przecięć. Wszystkie części mają domknięte siatki; niskopoligonowe zaokrąglenia krawędzi są wykonywane w Blenderze modyfikatorem bevel (3 segmenty) i Weighted Normals.

## Generowanie

```sh
# Geometry self-test bez bibliotek zewnętrznych
python Tools/ModelFactory/M03/M03-003/generate_m03_003.py --self-test

# Rzeczywisty statyczny OBJ+MTL + manifest (bez Blendera)
python Tools/ModelFactory/M03/M03-003/generate_m03_003.py --static-obj --out build/M03-003

# Blender 4.x/5.x: FBX + GLB + manifest
blender -b --factory-startup --python Tools/ModelFactory/M03/M03-003/generate_m03_003.py -- --out build/M03-003
```

Generator usuwa tylko własną kolekcję `RRL_M03_003_BRIDGE_FRAME` i osierocone siatki z prefiksem modelu. Nie modyfikuje scen, gameplayu, ustawień projektu ani wspólnych prefabów.

**Import Unity 6:** preferowany FBX po eksporcie z Blendera. Przy statycznym OBJ należy odtworzyć sockety z manifestu (OBJ nie przenosi obiektów typu Empty). Collidery budować z prostych pudełek i/lub kilku wypukłych brył; nie stosować render mesh jako jednego kolidera dynamicznego Rigidbody. Po imporcie sprawdzić przeliczenie osi, orientację normalnych, położenie wszystkich sześciu portów, materiały i brak z-fightingu.

## QA i status

Generator zawiera `--self-test` sprawdzający unikalność nazw, materiały, dodatnią objętość skierowaną, zamknięcie manifold każdej siatki (2 użycia krawędzi), brak degeneratów, dokładne granice 2×1×1 m, liczbę dźwigarów/słupów i kompletność sześciu portów. Oddzielny `qa_m03_003.py` dodatkowo sprawdza wygenerowany OBJ, MTL i manifest.

**Blender export: NOT RUN** dopóki nie ma dowodu uruchomienia Blendera. **Unity import / Play Mode: NOT RUN.** Wszelkie kontrole wykonywane w CI będą raportowane oddzielnie i nie oznaczają testu w Unity. Ten model jest tylko wizualnym modułem konstrukcyjnym; nie zawiera mechaniki fizycznej.

**Claim:** `Docs/ModelFactory/claims/M03-003.json` na gałęzi kontrolnej, utworzony przez CREATE przed rozpoczęciem produkcji; commit `7e33dadc07cb108d5ef0cf676ceae79041c81d21`. Nie należy ponownie przydzielać tego ID.

### Wynik automatycznego QA — 2026-10-24

- **M03-003 Bridge Frame Geometry QA: PASS** — rzeczywiście uruchomiono `qa_m03_003.py` i generator w trybie `--static-obj`. Wykonano kontrolę składni Pythona, manifold i orientacji trójkątów, wymiarów, liczby portów, materiałów oraz indeksów OBJ/MTL.
- **Artefakt:** `M03-003-bridge-frame-static-OBJ` (OBJ, MTL, manifest), [GitHub Actions run #40095436696](https://github.com/C8LyNn/RoboRougelike/actions/runs/40095436696), artifact ID `12560821521`. Statyczny model jest rzeczywistym wynikiem generatora, nie ilustracją.
- **CI-01 Repository Validation: PASS** — [run #40095436658](https://github.com/C8LyNn/RoboRougelike/actions/runs/40095436658).
- **CI-02 Unity EditMode: BLOCKED**, **CI-03 Unity PlayMode: BLOCKED** — oba workflow zatrzymały się na kroku `Require valid Unity activation credentials`; nie są dowodem niepoprawności modelu ani przeprowadzonych testów Unity.
- **Blender bpy FBX/GLB export: NOT RUN**, **Unity import / geometry preview: NOT RUN**. Przed integracją należy wykonać eksport w Blenderze, sprawdzić pivoty i materiały w Unity 6 oraz zweryfikować widoczne styki.
