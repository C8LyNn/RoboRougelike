# R4Movement940 — 11 animowanych modeli ruchu (Issue #940)

Biblioteka art-only w standardzie **R41_Turret_v6.1**. Niczego nie podpina do gry: nie zmienia
`Assets/`, scen, prefabów, runtime, fizyki ruchu, Save/Load, ekonomii ani modeli broni z PR #939.
Nie dodaje nowych `R3PartKind` / `R3DriveKind` / MOV.

**Status: BLENDER_PASS 11/11 · UNITY_NOT_RUN 11/11** (Unity nie był dostępny — żaden import do Unity nie został wykonany).

Pełna tabela: [INDEX.md](INDEX.md) · dane maszynowe: [INDEX.json](INDEX.json) · sumy: [SHA256SUMS.txt](SHA256SUMS.txt)

## Modele i klipy (24 fps, jeden take w FBX/GLB, podział wg `*_animation_manifest.json`)

| Model | part_kind | Klipy (⟲ = pętla) | Tri | Charakter sylwetki |
|---|---|---|---|---|
| R4M_Wheel | Wheel | Idle⟲, Roll⟲, Reverse⟲, Brake | 12 078 | widelec z poprzeczką, opona r 0,39 z 20 klockami, 6 szprych |
| R4M_CrawlerTrack | CrawlerTrack | Idle⟲, Travel⟲, Reverse⟲, Stop | 31 842 | niski sponson, 40 ogniw, koło napędowe 14 zębów |
| R4M_HoverEngine | HoverEngine | Idle⟲, SpinUp, Lift⟲, Pulse, Cooldown | 7 228 | okrągła czapa, pylon, kanał r 0,26 z wirnikiem 7 łopat |
| R4M_OmniDrive | OmniDrive | Idle⟲, Forward⟲, Strafe⟲, Diagonal⟲ | 11 818 | koło z 10 rolkami 45°, boczny moduł silnika, prowadnica |
| R4M_Suspension | Suspension | Idle⟲, Compress, Rebound, ActiveLift, Cycle⟲ | 7 670 | wieża, wahacz wleczony, amortyzator ze sprężyną |
| R4M_Booster | Booster | Idle⟲, Ignition, ThrustLoop⟲, SpoolDown | 6 976 | dysza dzwonowa z 8-płatkową przysłoną, turbina wlotu |
| R4M_JumpDrive | JumpDrive | Idle⟲, Charge, Impulse, Recovery | 8 020 | ośmiokątna kolumna, 3 zbiorniki, tłok ze stopą |
| R4M_Blink | Blink | Idle⟲, Charge, Activate, Recover | 4 880 | heksagonalny bęben od spodu, 6 płatków, kryształ |
| R4M_Gyroscope | Gyroscope | Idle⟲, SpinUp, Stabilize⟲, Correct, SpinDown | 9 658 | wiszące jarzmo U, dwa pierścienie gimbala, wirnik |
| R4M_MagneticGrapple | MagneticGrapple | Idle⟲, Deploy, Fire, Retract | 7 648 | wyrzutnia skrzynkowa, szpula, głowica z 3 szponami |
| R4M_IndependentSteering | IndependentSteering | Idle⟲, SteerLeft, SteerRight, Slew⟲, Return | 6 730 | bęben serwa, zębnik + wieniec, zwrotnica z piastą |

Dokładne zakresy klatek i opisy: `R4M_*/R4M_*_animation_manifest.json`.

## Mocowanie i siatka — standard portu R4 (aktualizacja)

Każdy model ma teraz takie samo mocowanie w stylu Robocraft: **kwadratowy port 0,509 m** na jednej ścianie komórki montażowej. Jego zewnętrzna powierzchnia leży równo ze ścianą bloku gry (0,26 m od środka komórki; BlockSize 0,52 na siatce 0,55). Port jest wyśrodkowany na ścianie, tak jak porty środka ściany w `R3MountingGrid`. `Socket_Mount` stoi w środku tej powierzchni. Powierzchnia styku jest płaska; złącze centralne, pierścień i 4 otwory ustalające są wpuszczone. Od strony modelu port ma ramkę, 4 śruby, żółte znaczniki narożników i czerwony znacznik przodu.

- Port na górze: Wheel, CrawlerTrack, HoverEngine, OmniDrive, Suspension, Booster, JumpDrive, Gyroscope, MagneticGrapple, IndependentSteering. Port na dole: Blink.
- OmniDrive: socket przeniesiony z x 0,16 na środek ściany.
- MagneticGrapple: szpula zwężona (x −0,240…−0,150, 5 zwojów), bo wcześniej wystawała 1 mm poza komórkę.
- Niezależny test na wyeksportowanych GLB (`scripts/r4gridqa.py`, wynik w `compare/grid_fit.json`):
  - socket w środku ściany bloku,
  - płaska łata styku (13×13 promieni na 0,42 m),
  - nic nie wystaje za płaszczyznę mocowania w żadnej klatce animacji,
  - część stała w komórce 0,55 m, poza nią tylko urządzenie w kierunku z kontraktu runtime (koło, gąsienica, hover, omni, zawieszenie i skok w dół; booster do tyłu; hak do przodu).
- Wynik: **przed zmianą 0/11, po zmianie 11/11 PASS**. Pełne QA w Blenderze po przebudowie: BLENDER_PASS 11/11 (`batch_log.txt`).

## Co sprawdzono w Blenderze 5.2.3 (każdy model, jeden spójny batch — `batch_log.txt`)

- Topologia: manifold, spójne normalne, UV, brak pływających powłok, budżet trójkątów.
- Paleta R41: zablokowane materiały/PBR (Armor_Dark, Armor_Gunmetal, Armor_Plate_Light, Steel_Light, TeamColor_Red, Hazard_Yellow, Energy_Hot…).
- Montaż: `Socket_Mount`, `mount_cell`, `static_bounds`, `moving_sweep`, obudowa w komórce 0,55 m (ściśle albo z opisaną polityką PARTSIZE #141), `collision_envelope` vs placeholder runtime, `ground_contact` (Wheel/Crawler/Omni/Jump — odchyłka < 3 mm), `clearance` (BVH), obroty 90/180/270° i lustro X.
- Animacja: kolizje wszystkich par ruchomych części w każdej klatce = 0; pętle bezszwowe.
- Eksport: FBX (Unity 6 URP, −Z forward, Y up, FBX_SCALE_UNITS, `bake_space_transform=False`) + ponowny import (poz/geom < 0,05 mm, rot < 0,01°) — PASS; GLB + ponowny import — PASS; `*_colliders.glb`, `*_LOD1.fbx` (auto decimate).
- Rendery: 6 widoków + wireframe, hero EEVEE, kluczowe pozy, porównanie z R41, sylwetki z 4 kamer, MP4 osi czasu.

## Różnorodność sylwetek

`compare/silhouette_check.json`, `compare/silhouettes_128.png`, `compare/thumbnails_128.png`:
czarne sylwetki 4 kamer (gra, bok, przód, góra), IoU 64 px; próg zbyt podobnej pary 0,85.
**Brak oznaczonych par.** Najbliższe: JumpDrive|Blink 0,735 · Wheel|CrawlerTrack 0,684 · Wheel|JumpDrive 0,681.
Każdy model ma inny sposób montażu (poprzeczka widelca, słupek z kołnierzem, okrągła czapa, prowadnica, siodło, ośmiokąt, bęben od spodu, jarzmo U…), bez wspólnej płyty górnej.

## Podgląd dla właściciela

`preview/MOVEMENT_11_PREVIEW.zip` — rozpakuj i otwórz `index.html`:
11 prawdziwych GLB (osadzone w `models.js`, ładują się same, bez GitHub API i bez wyboru folderów),
lista z miniaturami, orbit/zoom, wybór klipu, prędkość, pauza, pętla, oś klatek, komórka 0,55 m i sockety,
szereg 11 modeli obok siebie (także jako czarne sylwetki) i tablica sylwetek.
three.js r128 (MIT) jest dołączony w `lib/` — **internet nie jest potrzebny** (tylko czcionki Google spadną do systemowych).
Bez WebGL: `offline.html` (MP4). Źródło podglądu: `scripts/preview_src/`, generator `scripts/build_preview.py`.

## Znane ograniczenia / ryzyka

- `UNITY_NOT_RUN` — import, podział klipów, materiały URP i kolizje w Unity nie były testowane.
- IndependentSteering nie ma placeholdera w runtime (tylko blok) — model jest propozycją wizualną.
- Placeholder runtime żyroskopu (pierścień d 0,598) wychodzi poza komórkę; model mieści się w niej.
- CrawlerTrack to lewa gąsienica; prawa = lustro transformem (scale X −1).
- Obudowy Wheel/Crawler/Hover/Omni/Booster/Grapple wychodzą poza komórkę tam, gdzie robi to geometria runtime (opisane w QA).
- Skoki/ruchy (skok 0,10 m, kabel haka, płatki Blink) są wyłącznie wizualne; wartości gry zostają w runtime.
- LOD1 jest automatyczny (decimate) — do ręcznej oceny.
- Ścieżki lokalne usunięte: w `.blend` absolutne ścieżki zastąpiono w miejscu `//` (`scripts/sanitize_blend_paths.py`, pliki ponownie otwarte w Blenderze), w logach/skryptach `%USERPROFILE%`. Stare SHA `.blend` zapisano w `*_QA.json`.

## Odtworzenie

`scripts/_batch_all.py` uruchamia Blendera headless dla 11 builderów (`scripts/build_*.py` + `r4lib.py`, `r4qa.py`, `r4mqa.py`).
Ścieżka do Blendera i folderu roboczego w `_runner.py` jest lokalna — dostosuj ją.
Referencyjny `R41_Turret_v6.1` jest prywatny i nie jest publikowany w repo.
