# R4 Weapon Art Set — 8 modeli broni w standardzie R41_Turret_v6.1 (Issue #938)

Status paczki: **BLENDER_PASS ×8 · UNITY_NOT_RUN** (v2: 7 broni przebudowanych dla różnorodności sylwetek, Basic bez zmian) — biblioteka izolowana (poza `Assets/`), bez integracji, bez merge/release.

- Kanon mechaniki sprawdzony: `feature/energy-combat-garage-refinement` @ `90021b6f` (`R4WeaponKind`: 9 typów; tu 8 nowych, `Automatic` = R41 = wzorzec).
- Referencja: `R41_Turret_v6.1.fbx` z Drive właściciela, SHA-256 `03c60486…5d6a` **zweryfikowane**; źródło `R41_Turret_v6.blend` (`bafc1552…95db`) i builder `build_turret_v6.py` z lokalnego folderu właściciela. **Binaria/skrypty R41 nie są w tym repo** (tylko pomiary w `style_lock/` i rendery porównawcze).
- Blender 5.2.3 LTS, uruchamiany naprawdę (background, `--factory-startup`). Każdy PASS pochodzi z faktycznego eksportu i reimportu.
- Intake: na `main` i gałęzi kanonu nie ma modeli dla tych 8 `R4WeaponKind` (wcześniejsze M01 to inne assety) — brak duplikatu.

## Wersja v2 — zróżnicowane sylwetki (po uwadze właściciela)

Właściciel ocenił 8 broni jako zbyt podobne: wszystkie stały na tej samej podstawie R41 i miały tę samą głowicę z policzkami. W v2 **7 broni dostało własny typ podstawy i własną architekturę głowicy**; `Basic` zostaje wzorcem R41 bez zmian. Paleta R41, pivot `GunPivot` (0; 0,135; 0), `Socket_Mount_Bottom`, osie Yaw/Pitch i listy klipów są bez zmian.

| broń | podstawa v2 | głowica v2 |
|---|---|---|
| Basic | R41 (bez zmian) | policzki R41 (bez zmian) |
| Heavy | schodkowy ośmiokąt (3 kondygnacje) | niskie sponsony, bęben-jarzmo, szeroka pochyła tarcza, krótka gruba lufa |
| Shotgun | prostopadłościenny cokół + skrzynia amunicyjna z boku | jedno ramię wspornikowe (+X) + bębnowy magazynek z boku (−X) — asymetria |
| Railgun | smukła kolumna z sześcioma żebrami na okrągłej płycie | wąskie wysokie widły z otworami + 3 pionowe kondensatory |
| BeamCutter | kolumna z radiatorem 18 żeber | kulista głowica w pierścieniu kardanowym |
| RocketLauncher | trójnóg z siłownikami | centralna wieża z radarem + **dwa boczne kontenery** (2 rury każdy) |
| RotarySaw | „krab”: niski kadłub, wąska szyja, 4 wysięgniki | ramię piły bez zmian (v1) |
| EmpEmitter | żebrowany bęben z izolatorami | głowica impulsowa bez zmian (v1) |

**Kontrola sylwetek** (`compare/silhouette_check.json`, `compare/silhouettes_v1_v2_128.png`, `compare/before_after_34front.jpg`): czarne sylwetki GLB w pozie spoczynkowej z 4 kamer (gra, bok, przód, góra), IoU 64 px, para „zbyt podobna” przy średnim IoU > 0,85.
- v1: **4 pary oznaczone** (Shotgun↔BeamCutter 0,94 · Basic↔Heavy 0,90 · Shotgun↔RocketLauncher 0,89 · BeamCutter↔RocketLauncher 0,85), średnia wszystkich par 0,64.
- v2: **0 par oznaczonych**, średnia 0,57. Najbliższa para: Basic↔Heavy 0,80 (Heavy pozostaje „cięższym kuzynem” wzorca R41), dalej Heavy↔BeamCutter 0,77, Shotgun↔BeamCutter 0,76.

QA v2: jeden spójny przebieg wszystkich 7 przebudowanych broni (`batch_log_v2.txt`) — **BLENDER_PASS ×7**: topologia, paleta w pasmach R41, 0 kolizji w sweepie celowania (yaw 0…350° × pełny zakres pitch), 0 kolizji animacji we wszystkich klatkach, podstawa w komórce 0,55 m, korpus yaw w r ≤ 0,275 m, wyloty wolne, reimport FBX i GLB — PASS. Unity: `UNITY_NOT_RUN`.

Zmiany kontraktu wizualnego do wiadomości integracji (mechanika gry bez zmian):
- Heavy: widoczny `Muzzle` przesunięty z 0,700 m na 0,600 m (krótsza lufa).
- RocketLauncher: 4 rury w dwóch kontenerach po bokach (x ±0,130 / ±0,200), logiczny `Muzzle` na osi między kontenerami; doszła druga tylna pokrywa (w klipach w spoczynku).
- Wszystkie `.blend` (także Basic): absolutne ścieżki lokalne zastąpione w miejscu ścieżkami `//` (`scripts/sanitize_blend_paths.py`); poprzednie SHA w `*_QA.json`.

Odtworzenie v2: `scripts/make_v2.py` generuje `build_<broń>.py` z kodu v1 (`build_<broń>_v1.py`) jawnymi łatkami; nowe elementy w `scripts/r4base.py` (podstawy) i `scripts/r4heads.py` (głowice); RocketLauncher v2 ma własny `build_rocketlauncher.py`. Uruchamianie: `scripts/_batch_v2.py`.

## Raport końcowy (weapon_kind → … → known risks)

| weapon_kind | model | klipy (24 fps, zakresy w `*_animation_manifest.json`) | 4 rodziny barw dark / light / team / hazard (+tech) | tris (LOD1) | Blender QA | Unity |
|---|---|---|---|---|---|---|
| `Basic` | `R4W_Basic/R4W_Basic.fbx` | Idle, AimYaw, AimPitch, Fire, Recoil, Return | 76.8 / 15.6 / 2.4 / 0.4 (+4.7) % | 10 834 (5 416) | PASS | NOT_RUN |
| `RotarySaw` (WPN-005) | `R4W_RotarySaw/…fbx` | Idle, AimYaw, AimPitch, SpinUp, Spin (loop), SpinDown, Jam | 82.8 / 10.2 / 1.4 / 0.4 (+5.2) % | 16 018 (8 006) | PASS | NOT_RUN |
| `Heavy` | `R4W_Heavy/…fbx` | Idle, AimYaw, AimPitch, Fire, Recoil, Return (cykl zamka klinowego) | 80.3 / 12.1 / 2.5 / 0.6 (+4.5) % | 12 442 (6 218) | PASS | NOT_RUN |
| `Shotgun` (WPN-001) | `R4W_Shotgun/…fbx` | Idle, AimYaw, AimPitch, Fire, Pump, Reload | 83.6 / 9.9 / 2.2 / 0.6 (+3.8) % | 11 540 (5 768) | PASS | NOT_RUN |
| `Railgun` (WPN-002) | `R4W_Railgun/…fbx` | Idle, AimYaw, AimPitch, Charge, Fire, Cool | 74.9 / 17.6 / 1.6 / 2.0 (+4.0) % | 17 104 (8 550) | PASS | NOT_RUN |
| `BeamCutter` (WPN-003) | `R4W_BeamCutter/…fbx` | Idle, AimYaw, AimPitch, WarmUp, Fire (loop), Heat, Cooldown | 79.7 / 16.0 / 1.2 / 1.1 (+2.0) % | 16 120 (8 060) | PASS | NOT_RUN |
| `RocketLauncher` (WPN-004) | `R4W_RocketLauncher/…fbx` | Idle, AimYaw, AimPitch, Fire, Reload | 79.8 / 13.4 / 2.7 / 1.8 (+2.4) % | 14 448 (7 220) | PASS | NOT_RUN |
| `EmpEmitter` (WPN-006) | `R4W_EmpEmitter/…fbx` | Idle (loop), AimYaw, Charge, EMP, Recovery | 79.6 / 12.5 / 2.5 / 1.8 (+3.5) % | 18 930 (9 460) | PASS | NOT_RUN |

R41 v6.1 (zmierzone): 79.1 / 12.3 / 2.5 / 0.15 (+6.0) %, 11 452 tris. SHA-256 wszystkich plików: `SHA256SUMS.txt`, zbiorczo: `INDEX.json`.

## Co sprawdza QA (dla każdej broni, `*_QA.json`)
- **Topologia** każdej siatki: 0 non-manifold, 0 krawędzi brzegowych, 0 ścian zerowych, spójna orientacja normalnych (test pętli krawędzi), dodatnia objętość, UV.
- **Paleta**: tylko zablokowane materiały R41 (wartości PBR 1:1), udział powierzchni 4 rodzin w pasmach wokół R41.
- **Kontrakt**: `GunPivot` (0; 0,135; 0), Yaw/Pitch z początkiem w pivocie, `Muzzle*` oś +Z w pliku/Unity, `Socket_Mount_Bottom` na dolnej ścianie komórki 0,55 m.
- **Sweep 3D**: yaw 0…350°/10° × pitch co 2° — 0 kolizji z bazą i yaw; `static_occupied` w komórce; korpus yaw w okręgu r=0,275 m; lista komórek sąsiednich wchodzonych przez `moving_sweep`; `line_of_fire`/`fire_exit` (raycast 6 m od każdego wylotu, w kilku elewacjach).
- **Kolizje animacji**: wszystkie klatki osi czasu, wszystkie pary ruchomych części (np. lufa↔kołyska, zamek↔gniazdo, tarcza↔osłona, rakieta↔pokrywa).
- **Eksport** FBX z ustawieniami v6.1 (`bake_space_transform=False`, Forward −Z, Up Y, `FBX_SCALE_UNITS`) + GLB + `_colliders.glb` + automatyczny `_LOD1.fbx`; **reimport FBX** i porównanie z `.blend` we wszystkich klatkach granicznych klipów (najgorzej: 0,0002 mm pozycji, 0,0004 mm geometrii, 0° obrotu, 0 zmian rodziców); reimport GLB w pozie spoczynkowej.
- **Rendery**: przód/tył/bok/góra/¾ przód/¾ tył, wireframe ×2, collidery+sweep, pozy kluczowe każdego klipu, EEVEE hero, porównanie obok prawdziwego R41 v6.1 (oba z FBX). W repo jako JPG ≤1100 px; pełne PNG zostają u właściciela w `Desktop\Blender_Modele\wynik\R4_Weapons_938`.

## Import w Unity 6 (URP) — do wykonania przez właściciela
Scale Factor 1, **Convert Units ON**, **Bake Axis Conversion ON**; Animation: jedna take → podziel na klipy wg zakresów z `*_animation_manifest.json` (klipy `loop=true` są bezszwowe). Kontrola jak w RAPORT_QA_v6.1: korzeń rotacja 0, `GunPivot` (0; 0,135; 0), Yaw/Pitch zerowe.

## Znane ryzyka / decyzje do integracji (nic z tego nie zmienia gry)
1. **Rotary Saw**: wizualna tarcza r=0,30 m, środek 0,62 m przed pivotem. Runtime `RotarySawConfiguration` ma `bladeRadius 0,9 m / halfThickness 0,14 m`, a placeholder `R41PartGeometry.SawRotor` ~0,47 m — kontaktowy dysk gry obejmuje własny pancerz modelu (`extra_checks.game_contact_disc`). Decyzja B01. 2800 RPM: klip `Spin` = 120 rpm przy speed 1 (×23,33 dla 2800) lub obracanie `Blade` z RPM przez runtime; `Saw_BlurDisc` powyżej ~450 rpm.
2. Pozycje wylotów modeli (0,41–0,84 m) różnią się od placeholderów runtime (`Wylot` w bloku 0,52) — jak w R41 v4–v6, integracja przepina transformy, mechanika bez zmian.
3. Widoczne wyloty ≠ liczba strzałów: Shotgun 6 otworów (8 śrucin w grze), Rocket 4 tuby + logiczny `Muzzle`, EMP bez lufy (radialny `Muzzle` +Y, bez Pitch).
4. Heavy ma zakres pitch −8…+30° (pozostałe −10…+35°) — tylko w modelu.
5. Animowane świecenie to siatki FX skalowane 0,001↔1 (przeżywa FBX); siła emisji nie jest animowana.
6. LOD1 jest automatyczny (Decimate 0,5), nie ręczny.
7. Unity nie było uruchomione → `UNITY_NOT_RUN`.

## Odtworzenie
`blender -b --factory-startup --python scripts/build_<broń>.py -- <out_dir>` (wymaga lokalnego `R41_Turret_v6.1.fbx` tylko do renderu porównawczego). `scripts/r4lib.py` — geometria/paleta/rig/animacja (technika R41 v6), `scripts/r4qa.py` — QA/eksport/rendery, `scripts/r4_style_probe.py` — pomiar R41.
