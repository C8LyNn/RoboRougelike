# R4 Style Atlas 5 (#1239)

Eksperymentalna biblioteka pięciu stylów A/B/C/D/E dla istniejących assetów RoboRougelike. Tylko art: nic w `Assets/`,
bez kodu, bez integracji z Unity, bez scalania.

## Stan

| Faza | Zakres | Stan |
|---|---|---|
| 0 — audyt | rejestr kanoniczny, biblie stylów, harmonogram | **wykonana** (ten katalog) |
| 1 — pilot | 12 assetów × 5 stylów | nie rozpoczęta |
| 2 — klocki i łączniki | 30 + 20, × 5 | nie rozpoczęta |
| 3 — broń, koła, napędy | 9 + 12 + 11, × 5 | nie rozpoczęta |
| 4 — moduły i wyrzutniki | 61, × 5 | nie rozpoczęta |
| 5 — pułapki i pozostałe | 6, × 5 | nie rozpoczęta |
| 6 — QA końcowe i pokaz | viewer, roboty 100/200/400 | nie rozpoczęta |

Unity: UNITY_NOT_RUN. Windows player: WINDOWS_PLAYER_NOT_RUN.

## Faza 0 — co ustalono

`inventory/canonical_inventory.json` (+ `.csv`) jest generowany skryptem `scripts/build_inventory.py` z realnych plików
źródłowych, przypiętych do commitów PR-ów źródłowych.

**149 identyfikatorów kanonicznych**, cel 745 wariantów:

| rodzina | liczba | źródło | uwagi |
|---|---|---|---|
| klocki | 30 | #963 (interfejs), #1234 + #1237 (Sleek A) | 5 z pilota + 25 z rolloutu to jedna rodzina 30 |
| łączniki ROD / STRUT | 20 | #1186 | porty `rod_mini` / węzły pełne — bez zmian |
| broń | 8 + 1 | #939; R41 Automatic z #1029 | R41 Automatic istnieje tylko jako FBX od artysty |
| napędy | 11 | #941 | animowane, klipy w manifestach |
| koła | 12 | #1018 | 12 osobnych tożsamości |
| moduły | 44 | #1238 | interfejs = proposal |
| wyrzutniki | 17 | #1238 | 15 z grupy F + generator tarczy + wzmacniacz |
| pułapki świata | 6 | #1238 | |

**Nakładanie się.** Osiem rodzajów ruchu istnieje dwukrotnie: `R4M_*` (#941, animowane modele w standardzie R41) i
`R4X_*` (#1238, moduły 1×1×1, proposal). Rejestr zachowuje obie tożsamości i oznacza wybór jako decyzję właściciela.
`R4M_Wheel` z #941 i 12 kół z #1018 to 13 osobnych pozycji.

**Stan w grze.** `main` (@5f06af8) nie zawiera ani jednego pliku modelu (0 plików fbx / glb / blend / obj) — gra rysuje
części proceduralnie. Kopie FBX klocków, napędów i broni są na gałęzi integracyjnej PR #1029, niescalonej.

**R41 Automatic.** Źródło jest dostępne (FBX na gałęzi #1029), ale bez `.blend`, GLB i manifestów. Jego gniazda, lufa i
klipy trzeba odczytać z FBX w Blenderze przed produkcją — w rejestrze są oznaczone jako nieznane, nie zgadywane.

**Poza zamrożonym katalogiem.** Skan 145 gałęzi o nazwach związanych z artem znalazł 49 starszych gałęzi
(`feature/rrl-model-m01…m04`, `feature/rrl-env…`) z pojedynczymi eksperymentami OBJ/GLB. Nie są częściami w
standardzie R4, nie mają manifestów portów. Są wypisane w rejestrze (`legacy_branch_sources`) ze statusem decyzji
właściciela i nie wchodzą do produkcji ×5. Repozytorium ma 1171 gałęzi; gałęzi o nazwach niezwiązanych z artem nie
otwierałem.

Nóg mecha / pająka, terenu ani rekwizytów areny w standardzie R4 nie znalazłem w żadnym ze sprawdzonych źródeł.

`BLOCKED_SOURCE`: brak. `NEW_ASSET_DELTA`: pusta (zamrożenie 2026-11-12).

## Decyzje potrzebne od właściciela

1. `R4M_*` czy `R4X_*` dla ośmiu rodzajów ruchu — która wersja jest docelowa w grze.
2. Czy starsze gałęzie `rrl-model` / `rrl-env` mają wejść do katalogu.
3. Kontakty, mocowania i dodatkowe strefy z #1238 nadal są proposal — atlas je przenosi bez zatwierdzania.

## Pliki

- `inventory/canonical_inventory.json`, `.csv` — rejestr.
- `inventory/legacy_branch_scan.tsv` — surowy wynik skanu gałęzi.
- `STYLE_BIBLES.md` — pięć biblii stylów i wspólny kontrakt.
- `scripts/build_inventory.py` — generator rejestru.
