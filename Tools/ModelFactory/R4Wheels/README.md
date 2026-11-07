# R4Wheels — 12 wariantów kół: rozmiary, bieżniki i rodzaje mocowań

Biblioteka art-only zawiera warianty `R3PartKind.Wheel` w standardzie R41 v6.1, z tym samym portem R4 co bronie (PR #939), moduły ruchu (PR #941) i klocki (PR #963). Biblioteka niczego nie podpina do gry: nie zmienia `Assets/`, scen, prefabów, runtime, fizyki, Save/Load ani ekonomii. Dotychczasowy `R4M_Wheel` z PR #941 zostaje bez zmian.

**Status: BLENDER_PASS 12/12 · siatka i port 12/12 PASS · zgodność z 30 klockami PASS · UNITY_NOT_RUN 12/12.** Unity nie był dostępny.

Pliki pomocnicze:
- pełna tabela: [INDEX.md](INDEX.md);
- dane maszynowe: [INDEX.json](INDEX.json);
- sumy kontrolne: [SHA256SUMS.txt](SHA256SUMS.txt).

## Warianty

| Model | Rozmiar | Mocowanie | Bieżnik / felga | Opona r × szer. [m] | Klipy (24 fps) | Tri |
|---|---|---|---|---|---|---|
| R4M_WheelS_Fork_Road | S | widelec | drogowy / tarcza 6 otw. | 0,205 × 0,15 | Idle, Roll, Reverse, Brake | 9 376 |
| R4M_WheelS_Caster_Slick | S | obrotnica samonastawna (trail 0,075 m) | slick / 5 szprych | 0,17 × 0,12 | + Swivel, Return | 8 548 |
| R4M_WheelM_Side_Offroad | M | **piasta boczna** (Robocraft) | terenowy / 6 szprych | 0,39 × 0,34 | Idle, Roll, Reverse, Brake | 9 110 |
| R4M_WheelM_Side_Slick | M | **piasta boczna** | slick wyścigowy / 5 szprych | 0,37 × 0,30 | Idle, Roll, Reverse, Brake | 7 778 |
| R4M_WheelM_Steer_Road | M | zwrotnica skrętna ±30° | drogowy / 8 szprych | 0,34 × 0,26 | + SteerLeft, SteerRight, Slalom | 10 584 |
| R4M_WheelM_SwingArm_Offroad | M | wahacz wleczony + drążek skrętny | terenowy / 6 szprych | 0,33 × 0,26 | + Compress, Rebound, Bump | 9 800 |
| R4M_WheelM_Twin_Road | M | widelec, koła bliźniacze | drogowy / tarcze 8 otw. | 0,33 × 2×0,16 | Idle, Roll, Reverse, Brake | 17 782 |
| R4M_WheelM_Fork_Spiked | M | widelec | z kolcami (4 rzędy) / 6 szprych | 0,37 × 0,30 | Idle, Roll, Reverse, Brake | 11 436 |
| R4M_WheelL_Side_Offroad | L | **piasta boczna** | terenowy / 8 szprych | 0,56 × 0,42 | Idle, Roll, Reverse, Brake | 10 710 |
| R4M_WheelL_Fork_Balloon | L | szeroki widelec | balonowy / tarcza 8 otw. | 0,50 × 0,43 | Idle, Roll, Reverse, Brake | 10 490 |
| R4M_WheelL_Steer_Offroad | L | zwrotnica skrętna ±30° | terenowy / 6 szprych | 0,48 × 0,34 | + SteerLeft, SteerRight, Slalom | 11 372 |
| R4M_WheelXL_Side_Monster | XL | **piasta boczna** | monster (łopatki) / 8 szprych | 0,80 × 0,58 | Idle, Roll, Reverse, Brake | 7 400 |

Dla porównania: obecne koło runtime (`R4M_Wheel`, PR #941) to widelec M o oponie r 0,39 × 0,364 m.

## Mocowania

Wszystkie warianty mają ten sam **port R4**: kwadrat 0,509 m, równo ze ścianą bloku (0,26 m od środka komórki), wyśrodkowany, z wpuszczonym złączem centralnym, pierścieniem i 4 otworami ustalającymi.

- **Port na górze** — widelec, bliźniacze, zwrotnica, obrotnica, wahacz. `Socket_Mount` (0; 0,26; 0) w osiach Unity. Koło wisi pod klockiem, tak jak dzisiejsze koło runtime.
- **Port z boku (ściana −X)** — piasta boczna. `Socket_Mount` (−0,26; 0; 0). Koło stoi obok klocka, na zewnątrz (+X), oś biegnie prostopadle do ściany, a środek koła jest na wysokości środka klocka. To klasyczne mocowanie z Robocraft.
  - Model jest „prawy”, czyli montowany na prawej ścianie klocka. Wersja lewa to lustro transformem (scale X −1), wtedy port jest na +X.
- **Węzły sterowane przez grę:**
  - `*_Rotor` obraca się tylko wokół lokalnego X; dodatni kąt = toczenie do przodu (+Z).
  - `*_Knuckle` (zwrotnica) obraca się wokół lokalnego Y; dodatni kąt = w prawo; limit ±30°.
  - `*_Swivel` (obrotnica) obraca się wokół lokalnego Y, swobodnie o 360°.
  - `*_Arm` (wahacz) obraca się wokół lokalnego X przy osi drążka skrętnego; dodatni kąt = ugięcie (koło w górę), zakres +10/−8°.
- **Inne węzły:** `Wheel_Axle` (oś) i `GroundProbe` (punkt styku z ziemią w spoczynku). Role węzłów są w `*_model_manifest.json`.

## Co sprawdzono

- **Blender 5.2.3, każdy wariant** (`R4M_*/R4M_*_QA.json`, `batch_log.txt`):
  - topologia: manifold, spójne normalne, UV, bez pływających powłok, budżet trójkątów;
  - zablokowane materiały R41;
  - kolizje wszystkich par części ruchomych w każdej klatce = 0, prześwit > 0;
  - FBX (Unity 6 URP, −Z forward, Y up, `FBX_SCALE_UNITS`, `bake_space_transform=False`) i GLB z ponownym importem — PASS;
  - `*_colliders.glb`, `*_LOD1.fbx`;
  - rendery (6 widoków, hero, pozy, oś czasu MP4).
- **Siatka i port** (`compare/grid_fit.json`, `scripts/r4gridqa.py` na wyeksportowanych GLB) — 12/12 PASS:
  - socket w środku ściany bloku;
  - płaska łata styku (94,7% płaskiej powierzchni, reszta to rowki portu);
  - nic nie przekracza płaszczyzny mocowania w żadnej klatce animacji.

  Koła mogą wychodzić poza swoją komórkę we wszystkich kierunkach oprócz strony portu. Port górny: nic nad komórką. Port boczny: nic po stronie klocka.
- **Zgodność z klockami** (`compat/compat_wheels.json`, `scripts/compat_wheels.py`): każdy wariant zamontowany na każdym pełnym porcie 30 klocków z PR #963, w 4 obrotach i 10 klatkach animacji, co daje 12 × 137 = 1 644 montaże.
  - Styk: 1 644/1 644 PASS, szczelina 30 mm, przesunięcie 0 mm.
  - Przenikanie: wolne są wszystkie porty poza 6 wewnętrznymi ścianami CornerL3 i Tee4.
    - Tam koła widelcowe, skrętne M, z wahaczem i bliźniacze pasują tylko w 2 z 4 obrotów.
    - Koła boczne i duże skrętne L nie mieszczą się w żadnym obrocie, bo koło jest większe niż wnęka między ramionami klocka.
    - Małe koła (widelec S, obrotnica S) pasują wszędzie.
- **Render:** `compat/wheels_on_blocks_34front.jpg` pokazuje wszystkie 12 kół na klocku Cube (`scripts/wheels_showcase.py`).
- **Skala:** `compare/wheels_lineup.jpg` to szereg w jednej skali z podglądu.

## Podobieństwo sylwetek

`compare/silhouette_check.json` (IoU, 4 kamery, sylwetki przycięte do obrysu, czyli **bez skali**).

Oznaczone pary: Side_Offroad|Side_Slick 0,94 · Steer_Road|SwingArm 0,88 · L_Side|XL_Monster 0,88.

To warianty tego samego koła. Metoda usuwa skalę, a różnica między nimi to rozmiar (L 0,56 m vs XL 0,80 m), bieżnik (terenowy vs slick) albo rodzaj ramienia. W szeregu w jednej skali (`compare/wheels_lineup.jpg`) są wyraźnie różne. Jeśli trzeba bardziej rozróżnić Side_Offroad i Side_Slick, można dać slickowi inną piastę lub kołpak.

## Podgląd

Plik `preview/WHEELS_12_PREVIEW.zip` — po rozpakowaniu otwórz `index.html`:
- 12 prawdziwych GLB z animacjami;
- wybór klipu, oś klatek;
- szereg w jednej skali, sylwetki.

three.js r128 jest w `lib/`, więc internet nie jest potrzebny.

## Do integracji (poza zakresem; patrz też `Tools/ModelFactory/R4Blocks/CODEX_INTEGRATION.md` w PR #963)

- Runtime ma jedno koło (`R3PartKind.Wheel`: cylinder r 0,39, widelec, oś 0,26 m pod komórką). Warianty potrzebują wyboru wariantu w danych części (np. `WheelVariant`) i własnych parametrów: promień i szerokość opony do kolizji, wysokość osi, skręt, zawieszenie. To są decyzje balansu i fizyki, nie artu.
- Mocowanie boczne wymaga, żeby gra pozwalała przyczepić koło do bocznej ściany klocka (ContactMask z bitem ±X) i odbijała model lustrem dla lewej strony.
- Duże koła (L, XL) zajmują wizualnie kilka komórek, a `moving_sweep.cells` jest w QA. Gra powinna rezerwować te komórki albo pozwolić na nachodzenie wizualne.
- Zwrotnica, obrotnica i wahacz mają gotowe węzły, ale kąty skrętu i ugięcia musi podawać gra. Klipy to podgląd stanów.

## Odtworzenie

- `scripts/build_wheelvar.py -- <out> <wariant>` — jeden builder parametryczny; tabela `VAR` zawiera rozmiar, mocowanie, bieżnik, felgę, promień i szerokość. Korzysta z `r4lib.py`, `r4qa.py`, `r4mqa.py` i `r4port.py` (kopie z PR #941).
- `scripts/r4gridqa.py`, `scripts/compat_wheels.py`, `scripts/wheels_showcase.py` — testy siatki i zgodności. Potrzebują GLB klocków z PR #963.
- `scripts/silhouette_compare.py`, `scripts/make_index_wheels.py`, `scripts/build_preview.py` — sylwetki, indeks i podgląd.

Ścieżki lokalne usunięto: w `.blend` metodą `scripts/sanitize_blend_paths.py`, stare SHA są w `*_QA.json`. Ścieżki w skryptach są względne lub zapisane jako `%USERPROFILE%`.
