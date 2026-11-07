# Raport wdrożeniowy dla Codexa — modele R4: bronie, ruch, klocki

Ten dokument opisuje, jak wdrożyć do gry trzy biblioteki modeli przygotowane w Blenderze. Wszystkie trzy są **art-only**: same pliki, skrypty i raporty QA, bez żadnej zmiany w `Assets/`, scenach, prefabach ani runtime. Wdrożenie, czyli import do Unity, podpięcie wizualne, nowe kształty klocków, testy i UNITY_PASS, należy do Codexa.

Status całego materiału: **BLENDER_PASS · UNITY_NOT_RUN**. Unity nie był uruchamiany przy tworzeniu modeli.

---

## 1. Źródła — co dokładnie wdrażać

| Biblioteka | PR | Gałąź | Commit | Folder |
|---|---|---|---|---|
| 8 broni | #939 (draft) | `art/938-r4-weapon-models` | `23718ab` | `Tools/ModelFactory/R4Weapons938/` |
| 11 modułów ruchu | #941 (draft) | `art/940-r4-movement-models` | `c6b655d` | `Tools/ModelFactory/R4Movement940/` |
| 30 klocków + testy zgodności | #963 (draft) | `art/r4-building-blocks` | HEAD tej gałęzi | `Tools/ModelFactory/R4Blocks/` |

- Każdy folder ma `INDEX.json` (dane maszynowe), `SHA256SUMS.txt`, `README.md` i per model `*_QA.json`.
- Bronie i ruch mają dodatkowo `*_model_manifest.json` (hierarchia, role węzłów), `*_animation_manifest.json` (klipy) i `*_palette_manifest.json`.
- Trzy PR-y są niezależne i **nie mają konfliktów**: każdy dodaje tylko własny folder w `Tools/ModelFactory/`.
- `compat/compat_matrix.json` w #963 odwołuje się do GLB z #939 i #941 po ścieżce i SHA-256. Te pliki nie są kopiowane do #963.
- Referencyjny `R41_Turret_v6.1` jest prywatny i nie ma go w repo. Nie jest potrzebny do wdrożenia.

### Formaty
- **FBX** — do importu w Unity: Unity 6 URP, −Z forward, Y up, `FBX_SCALE_UNITS`, `bake_space_transform=False`, metry 1:1.
- **GLB** — podgląd i QA. Projekt nie ma glTFast w `Packages/manifest.json`, więc GLB nie jest ścieżką importu.
- `*_LOD1.fbx` (bronie i ruch) — automatyczny decimate, do oceny ręcznej.
- `*_colliders.glb` — wypukłe kształty UCX, tylko jako referencja; patrz punkt 5.4.
- `.blend` — źródło. Do Unity tego nie importować.

---

## 2. Stan bazowy gry (sprawdzony w repo 2026-11-07)

| Gałąź | Commit | Co zawiera |
|---|---|---|
| `main` = `develop` | `5f06af8` (2026-10-21) | **Brak** `R3MountingGrid`, BUILD001, `R3BuildValidation`; starszy `R3PartKind` (13 rodzajów) |
| `integration/codex-full-merge-270` | `9cd5a52` | R3MountingGrid, BUILD001, R3BuildValidation, `R3PartKind` z IndependentSteering |
| `feature/energy-combat-garage-refinement` | `2d67ef3` (2026-11-06) | to samo + `R3PartKind.Battery`; najnowsza gałąź gry |

**Decyzja Codexa:** wybierz gałąź docelową według własnego procesu. Kontrakt mocowania, z którym modele są zgodne, istnieje dziś tylko na gałęziach integracyjnych i feature, nie na `main`. Odwołania do kodu poniżej dotyczą `feature/energy-combat-garage-refinement`.

Punkty styku w kodzie:
- `Assets/R4_1/R41PartGeometry.cs`:
  - `Grid = 0.55f`, `BlockSize = 0.52f`;
  - `Payload(...)` dziś buduje **prymitywy** (Cylinder, Sphere, Cube) jako wygląd broni i modułów i zwraca `muzzle`.
  - Wywołania: `R3VehicleAssembly.cs:192`, `R3GarageView.cs:184`, `R3RestorationValidation.cs:83`, `P12_PipelineSafeEnemyTurret.cs:1063/1110`, test `RRLW6SawTests.cs:77`.
- `Assets/R3/R3MountingGrid.cs`:
  - `FaceDirections` w kolejności right, left, up, down, forward, back (bity 0–5);
  - `ContactMask` lokalny, obracany ćwierćobrotami;
  - `R3BlockMountPoints` (GroupId, WorldMask, footprint).
- `Assets/R3/R3VehicleModel.cs`:
  - `R3PartKind`, `R3PartState` (Cell, RotationQuarterTurns, ContactMask);
  - `OccupiedCells()`: dziś 1 komórka albo „wide” 2×2×1 dla wybranych rodzajów.
- `Assets/R3/R3BuildValidation.cs`: buduje `BuildDefinition` z `OccupiedCells` i `ContactMask`; każda ściana dostaje `BuildSocket` z `HalfSize (.47,.47)`.
- `Assets/B01Construction/BUILD001/Runtime/BuildSocketGeometry.cs` (namespace `B01.BUILD001`):
  - `BuildSocket` (Cell, Face, Kind, Offset, HalfSize w komórkach), `BuildCellBond`;
  - `Touches()` liczy realne pole styku łat;
  - `MinContactAreaCellsSquared = .01`.

---

## 3. Wspólny kontrakt geometrii (bronie, ruch i klocki)

- **Siatka:** komórka 0,55 m. Ściana bloku leży 0,26 m od środka komórki, czyli BlockSize 0,52. Między sąsiadami jest 30 mm szczeliny.
- **Osie Unity:**
  - +Y = góra, +Z = przód, +X = prawo;
  - Blender → Unity: `(x, y, z)_Unity = (x, z, −y)_Blender`;
  - nazwy kierunków w plikach QA (`right/left/up/down/forward/back`) są semantyczne i odpowiadają `R3MountingGrid.FaceDirections`.
- **Origin** każdego modelu = środek jego komórki kotwicy (węzeł `*_ROOT`).
- **Port R4** (taki sam we wszystkich trzech bibliotekach):
  - kwadratowa płyta 0,509 m, zewnętrzna powierzchnia równo ze ścianą bloku (0,26 m od środka komórki), wyśrodkowana na ścianie;
  - wpuszczone złącze centralne, pierścień i 4 otwory ustalające.
- **Sockety w modelach:**

| Model | Węzeł | Pozycja (Unity) | Ściana montażu |
|---|---|---|---|
| bronie (8) | `Socket_Mount_Bottom` | (0, −0,26, 0) | dół → na górnej ścianie klocka |
| ruch (10) | `Socket_Mount` | (0, +0,26, 0) | góra → pod dolną ścianą klocka |
| Blink | `Socket_Mount` | (0, −0,26, 0) | dół |
| klocki (30) | `Socket_Mount` (+ `Port_<dir>_<cell>[_part]`) | dół (Arch: góra) | każdy port |

---

## 4. Mapowanie modeli na typy runtime

### Bronie (`R3PartKind.Gun` + `R4WeaponKind`)

| Model | `R4WeaponKind` | Klipy (24 fps) | Tri |
|---|---|---|---|
| R4W_Basic | Basic | Idle, AimYaw, AimPitch, Fire, Recoil, Return | 12 572 |
| R4W_Heavy | Heavy | Idle, AimYaw, AimPitch, Fire, Recoil, Return | 13 982 |
| R4W_Shotgun | Shotgun | Idle, AimYaw, AimPitch, Fire, Pump, Reload | 13 124 |
| R4W_Railgun | Railgun | Idle, AimYaw, AimPitch, Charge, Fire, Cool | 18 542 |
| R4W_BeamCutter | BeamCutter | Idle, AimYaw, AimPitch, WarmUp, Fire, Heat, Cooldown | 17 546 |
| R4W_RocketLauncher | RocketLauncher | Idle, AimYaw, AimPitch, Fire, Reload | 15 842 |
| R4W_RotarySaw | RotarySaw | Idle, AimYaw, AimPitch, SpinUp, Spin, SpinDown, Jam | 17 596 |
| R4W_EmpEmitter | EmpEmitter | Idle, AimYaw, Charge, EMP, Recovery | 20 286 |

- `R4WeaponKind.Automatic` nie ma modelu. Zostaje przy obecnym wyglądzie albo dostaje Basic jako zastępstwo; to decyzja Codexa lub właściciela.
- Hierarchia broni:
  - `ROOT → Base`, `Socket_Mount_Bottom`, `GunPivot (0, 0,135, 0) → Yaw (tylko lokalne Y) → Pitch (tylko lokalne X) → Barrel…, Muzzle, MuzzleFlash`;
  - dokładne nazwy, zakresy pitch i odrzut są w `*_model_manifest.json`.
- `Muzzle` = punkt wylotu pocisku. Heavy v2: 0,600 m; pozycje placeholderów runtime nie były zmieniane, do sprawdzenia przy podpięciu.
- Siatki FX to wszystko, co nie zaczyna się od `R4W_`: `MuzzleFlash`, `Beam_*`, `EMP_*`, `Rail_*`, `Rocket_Backblast`, `Saw_*`. Są tylko wizualne, w spoczynku mają skalę 0,001 i nie mogą mieć colliderów.

### Ruch (`R3PartKind`)

| Model | `R3PartKind` | Klipy (⟲ = pętla) | Tri |
|---|---|---|---|
| R4M_Wheel | Wheel | Idle⟲, Roll⟲, Reverse⟲, Brake | 12 078 |
| R4M_CrawlerTrack | CrawlerTrack | Idle⟲, Travel⟲, Reverse⟲, Stop | 31 842 |
| R4M_HoverEngine | HoverEngine | Idle⟲, SpinUp, Lift⟲, Pulse, Cooldown | 7 228 |
| R4M_OmniDrive | OmniDrive | Idle⟲, Forward⟲, Strafe⟲, Diagonal⟲ | 11 818 |
| R4M_Suspension | Suspension | Idle⟲, Compress, Rebound, ActiveLift, Cycle⟲ | 7 670 |
| R4M_Booster | Booster | Idle⟲, Ignition, ThrustLoop⟲, SpoolDown | 6 976 |
| R4M_JumpDrive | JumpDrive | Idle⟲, Charge, Impulse, Recovery | 8 020 |
| R4M_Blink | Blink | Idle⟲, Charge, Activate, Recover | 4 880 |
| R4M_Gyroscope | Gyroscope | Idle⟲, SpinUp, Stabilize⟲, Correct, SpinDown | 9 658 |
| R4M_MagneticGrapple | MagneticGrapple | Idle⟲, Deploy, Fire, Retract | 7 648 |
| R4M_IndependentSteering | IndependentSteering | Idle⟲, SteerLeft, SteerRight, Slew⟲, Return | 6 730 |

- CrawlerTrack to lewa gąsienica. Prawa powstaje lustrem transformu (scale X −1).
- IndependentSteering w runtime używa footprintu „wide” 2×2×1, a model zajmuje 1 komórkę. To decyzja projektowa: zostawić footprint i wyrównać model albo zmienić footprint.
- `Battery`, `BonusModule`, `DirectionalShield`, `PointDefense`, `StructuralScanner`, `ScannerPulse` i `Core` nie mają modeli R4. Zostają przy obecnym wyglądzie.

---

## 5. Plan wdrożenia (etapy, każdy do osobnego commita lub PR)

### E1. Import assetów
1. Skopiuj **tylko FBX** (i ewentualnie `*_LOD1.fbx`) do nowego folderu, np. `Assets/Art/R4/{Weapons,Movement,Blocks}/`. Manifesty JSON kopiuj obok jako TextAsset, jeśli będą czytane w runtime. Nie kopiuj `.blend` ani GLB.
2. Ustawienia ModelImporter (najlepiej przez `AssetPostprocessor`):
   - Scale Factor 1, Convert Units on;
   - **Bake Axis Conversion: on**;
   - Import Cameras/Lights off;
   - Materials: Import (URP Lit) albo remap na wspólne materiały palety R41 według `*_palette_manifest.json`;
   - **Generate Colliders: off**.
3. Animacje: FBX ma jeden take na całej osi czasu. Podziel go na klipy według `*_animation_manifest.json` (`start`/`end` w klatkach 24 fps, flaga `loop`), np. przez `ModelImporter.clipAnimations` w postprocessorze.
4. **Test akceptacyjny osi** (EditMode): po imporcie `R4W_Basic` węzeł `Socket_Mount_Bottom` ma lokalną pozycję (0, −0,26, 0) ±1 mm, a `Muzzle` leży na +Z. `R4M_Wheel/Socket_Mount` = (0, +0,26, 0). `R4B_Wedge`: wysoka krawędź skosu z tyłu (−Z), pełne porty na dole i z tyłu. Jeśli to się nie zgadza, popraw ustawienia importu, nie modele.

### E2. Wygląd broni i ruchu (tylko wizualnie)
1. Dodaj adapter wyglądu, np. `R4ArtVisuals`, który dla `(R3PartKind, R4WeaponKind)` instancjonuje prefab z FBX zamiast prymitywu w `R41PartGeometry.Payload`. Zostaw dotychczasowy prymityw jako fallback, gdy prefabu brak.
2. **Nie zmieniaj fizyki:**
   - collidery, masy, wymiary hitboxów i pozycje logiczne zostają jak w runtime;
   - prefab art ma tylko Renderer i Animator, bez Colliderów i Rigidbody (`solid=false` dla wyglądu);
   - jeśli `Payload(..., solid:true)` dziś daje collider, zostaw go z prymitywu, a art dodaj jako dziecko bez kolizji.
3. Wyrównanie: `Socket_Mount*` prefabu nakłada się na środek odpowiedniej ściany komórki części. Bronie siedzą na górnej ścianie klocka-nośnika, ruch pod dolną.
4. Węzły sterowane przez grę:
   - bronie: `Yaw` (Y), `Pitch` (X), `Muzzle` jako `out muzzle`;
   - piła: `R4W_RotarySaw_Blade` zastępuje `SawRotor`;
   - koło: `R4M_Wheel_Rotor` / `Wheel_Axle`.
   Animacje klipów służą do stanów (Fire, Reload, SpinUp…). Celowanie steruje kodem `Yaw` i `Pitch`, nie klipem `AimYaw`/`AimPitch`.
5. FX (`MuzzleFlash` itd.): skala 0,001 w spoczynku i 1 przy strzale; sterowane z kodu albo klipem.

### E3. Klocki (nowe kształty)
Dane gotowe do przeniesienia leżą w `Tools/ModelFactory/R4Blocks/integration/`:
- `R4_BLOCKS_UNITY.json` — 30 klocków w **osiach Unity i jednostkach BUILD001**: komórki, bondy, sockety (cell, face, offset, half), `contact_mask_anchor`, flaga `mount_modules`.
- `R4BlockCatalog.cs.txt` — szkic C# z `BuildSocket`/`BuildCellBond`, wygenerowany przez `scripts/make_unity_catalog.py`. To plik `.txt`, więc nic go nie kompiluje.

Kroki:
1. **Model danych:**
   - Runtime zna dziś jeden klocek, `R3PartKind.Armor` (sześcian 6 portów).
   - Proponowane rozwiązanie: pole kształtu w `R3PartState` (np. `BlockShape`, domyślnie `Cube`) zamiast 29 nowych `R3PartKind`. Nie rusza to ekonomii, uszkodzeń ani przełączników po `Kind`.
   - Decyzja należy do Codexa.
2. **Footprint:** `OccupiedCells` musi oddawać `cells` z katalogu, z rotacją `R3MountingGrid.Rotate`, dla klocków wielokomórkowych: Beam2/3, LongWedge, Plate2x2, Plate1x2, WideWedge, CornerL3, Tee4, Slab2x2, BigCube2.
3. **ContactMask:**
   - kotwica = `contact_mask_anchor` z katalogu;
   - dla wielokomórkowych potrzebna jest maska per komórka;
   - sockety z katalogu są już per komórka.
4. **BUILD001:** `R3BuildValidation.Definition()` dla klocka bierze `Sockets` z katalogu zamiast generować `.47` na każdej ścianie. Obecny klucz cache `Kind/ContactMask` trzeba rozszerzyć o kształt.
5. **Porty częściowe** (ściany trójkątne i połówkowe, `PartialSocket=true`): łączą **tylko klocek z klockiem**.
   - Płyta modułu 0,509 m jest większa od łaty, więc broń ani moduł ruchu nie może się na nich montować.
   - Dodaj regułę w `CompatibleKinds`/`Touches`: dla ról Gun, Wheel, Suspension i Device łata podpory musi być pełna (HalfSize ≥ .47 w obu osiach). Bez tej reguły obecne `MinContactAreaCellsSquared = .01` przepuści np. broń na trójkącie Tetra.
6. **Collidery klocków:**
   - kształty wypukłe: `MeshCollider (convex)` z siatki FBX albo z prostego hulla;
   - wielokomórkowe i wklęsłe (CornerL3, Tee4, Arch, Step, CoveEdge): kilka BoxColliderów po komórkach i podbryłach;
   - `*_colliders.glb` w repo to referencja kształtów UCX, jeśli wolisz je odtworzyć.
   - Masa i HP klocków to decyzja balansu, nie artu.
7. **Garaż i UI:** ikony z `R4B_*/renders/*_34front.jpg` (tymczasowo), nazwy z `description` w QA.
8. **Save/Load:** nowe pole kształtu musi mieć domyślną wartość `Cube`, żeby stare zapisy wczytywały się bez zmian. Tu trzeba testu migracji.

### E4. Testy (EditMode, bez trybu gry)
- Import: test osi z E1.4 dla wszystkich 49 FBX (socket ±1 mm, liczba klipów = manifest).
- Katalog klocków:
  - każdy socket spełnia `|offset| + half ≤ 0,5` (walidacja `BuildSocketGeometry`);
  - pełne porty mają `HalfSize = .47`;
  - `OccupiedCells` po 24 rotacjach daje rozłączne, poprawne komórki.
- Styk: `BuildSocketGeometry.Touches` daje PASS dla:
  - broń na górnym pełnym porcie klocka;
  - koło pod dolnym pełnym portem;
  - klocek z klockiem na łatach częściowych.
- Reguła z E3.5: broń na łacie częściowej Tetra lub Wedge daje odrzucenie.
- Zgodność z artem: wyniki z `compat/compat_matrix.json` jako dane do asercji, np. lista 6 portów CornerL3/Tee4 z zablokowanymi obrotami.
- Regresja: istniejące testy (m.in. `RRLW6SawTests`), Save/Load starych pojazdów, pojazd startowy.

### E5. Kryteria odbioru
- UNITY_PASS można ogłosić **tylko** po realnym uruchomieniu Unity 6000.3.14f1: import bez błędów, testy E4 zielone, zrzuty garażu i jazdy z nowymi modelami.
- Fizyka, ekonomia i Save/Load bez zmian w zachowaniu. Jedyny wyjątek to nowe kształty klocków, jeśli zostaną dodane, i ich jawnie opisane parametry.

---

## 6. Co jest już sprawdzone (dowody w repo)

- **Każdy model, Blender 5.2.3:** topologia (manifold, normalne, UV), paleta R41, FBX/GLB z ponownym importem < 0,05 mm, rendery. Bronie i ruch mają też sweep animacji, kolizje części ruchomych w każdej klatce = 0 i pętle bez szwów.
- **Siatka i port:** `compat/grid_fit.json` w #939 i #941 — 8/8 i 11/11 PASS (socket na środku ściany, płaska łata, nic nie przekracza płaszczyzny mocowania w żadnej klatce).
- **Klocki:** `R4B_*_QA.json` — 30/30 BLENDER_PASS, łaty portów sprawdzone promieniami.
- **Pojazd testowy:** `compat/compat_assembly.json` — klocki, R4W_Basic, 4× koło i booster; 38 par portów, szczelina 30 mm, przesunięcie 0 mm, 0 przenikań.
- **Macierz zgodności:** `compat/compat_matrix.json` — 19 modułów × 137 pełnych portów × 4 obroty × 10 klatek animacji = 2 603 montaże.
  - Styk 2 603/2 603 PASS (min. 94,7% płaskiej powierzchni).
  - Przenikanie: 2 537 portów wolnych we wszystkich obrotach, 66 w części obrotów, 0 zablokowanych.
  - Zablokowane obroty są tylko na wewnętrznych ścianach CornerL3 i Tee4, gdzie moduł wystający w bok uderza w drugie ramię. Gra powinna tam dopuszczać tylko wolne obroty albo zostawić to fizyce budowy.
  - Klocki bez pełnego portu (nie przyjmują modułów): Cone, Cylinder, HexColumn, RoundCorner, Tetra, TrussCube.

---

## 7. Ryzyka i decyzje do podjęcia

1. **Gałąź docelowa:** kontrakt mocowania nie istnieje na `main`/`develop` (punkt 2).
2. **Osie FBX:** test z E1.4 jest obowiązkowy. Modele wyeksportowano z `bake_space_transform=False`.
3. **Obudowy poza komórką** (opisane w QA ruchu): koło, gąsienica, hover, omni, zawieszenie i skok w dół; booster do tyłu; hak do przodu. Odpowiadają geometrii placeholderów runtime, ale przy gęstej zabudowie mogą wizualnie nachodzić na sąsiednie klocki. Kolizje gry się nie zmieniają.
4. **Wide footprint IndependentSteering** vs model 1×1 (punkt 4).
5. **Automatic** bez modelu; Battery i moduły specjalne bez modeli R4.
6. **Porty częściowe** wymagają reguły E3.5. Bez niej BUILD001 przepuści montaż modułu na zbyt małej łacie.
7. **Rozmiar repo:** pliki `.blend`, FBX i GLB są w zwykłym Git, bez LFS. Do `Assets/` kopiuj tylko FBX.
8. **Prawa:** kształty klocków to ogólny zestaw voxel-builder w stylu R4, bez tekstur, logo ani modeli z Robocraft czy Trailmakers.

## 8. Czego nie robić
- Nie modyfikować modeli w `Tools/ModelFactory/*`. Poprawki artu to osobne zadanie dla artu.
- Nie wiązać colliderów ani fizyki z siatkami artu broni i ruchu.
- Nie ogłaszać UNITY_PASS bez uruchomienia Unity i bez dowodów.
- Nie robić force-push ani merge do `main` bez akceptacji właściciela.
