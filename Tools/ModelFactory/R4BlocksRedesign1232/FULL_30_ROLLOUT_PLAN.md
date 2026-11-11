# Plan rozszerzenia na 30 klocków i 20 łączników (#1232)

**Nic z tego planu nie zostało wykonane.** To propozycja do realizacji dopiero po wyborze kierunku (A lub B) przez właściciela.

## Zasada

Generator `scripts/r4panel.py` przyjmuje wypukłą bryłę opisaną punktami i buduje z niej ramę oraz płyty. Interfejs (puste obiekty `Port_*`, `Socket_Mount`, `cells`, `ContactMask`, łaty portów) jest kopiowany z zatwierdzonego pliku, nie liczony na nowo. Dlatego rozszerzenie nie zmienia gniazd ani logiki połączeń.

Każdy kolejny kształt przechodzi te same bramki co pilot: QA w Blenderze, S1–S7 z `tests_compat.py`, porównanie przed/po na robocie.

## Pozostałe 25 kształtów

| Grupa | Kształty | Co trzeba dodać | Złożoność |
|---|---|---|---|
| 1. Wypukłe, gotowe dla generatora | InnerCorner, HalfWedge, LongWedge, WideWedge, Pyramid, HexColumn, OctColumn, Plate1x2, Plate2x2, Beam2, Beam3, Slab2x2, BigCube2 | tylko punkty bryły; dla wielokomórkowych decyzja o podziale płyt (niżej) | niska |
| 2. Wypukłe z krzywizną | RoundEdge, RoundCorner, Cylinder, Cone, Dome, RoundTop | płyta gięta: jedna płyta na powierzchni walca / kuli zamiast płaskiej; gładkie normalne | średnia |
| 3. Wklęsłe, złożone z brył wypukłych | Step, CornerL3, Tee4 | łączenie kilku brył w jedną powłokę bez ścian wewnętrznych | średnia |
| 4. Wklęsłe z krzywizną lub ażurowe | Arch, CoveEdge, TrussCube | osobne modelowanie: tunel, wklęsły łuk, kratownica bez płyt | wysoka |

Decyzje projektowe do podjęcia przed grupą 1:
- **Klocki wielokomórkowe** (Beam, Slab, BigCube, Plate, CornerL3, Tee4): jedna duża płyta na ścianę albo płyta na komórkę. Rekomenduję płytę na komórkę z płytkim rowkiem, bo wtedy belka wygląda tak samo jak rząd sześcianów i kadłub zachowuje jeden rytm.
- **Ostre krawędzie** (HalfWedge, LongWedge): rama cofa krawędź tym mocniej, im ostrzejszy kąt. Dla klinów ½ trzeba sprawdzić, czy nos nie robi się zbyt tępy, i ewentualnie zmniejszyć zaokrąglenie tylko na tej krawędzi.
- **TrussCube** powinien zostać ażurowy i używać tylko stref `R4P_Secondary` i `R4P_Trim`.

Proponowana kolejność: grupa 1 → przegląd właściciela → grupa 2 → grupy 3 i 4. Po każdej grupie robot testowy z nowymi kształtami.

## 20 łączników (PR #1186)

Gniazda i porty łączników zostają bez zmian. Zmienia się tylko dressing:

| Rodzina | Co dostaje nowy wygląd | Co zostaje |
|---|---|---|
| STRUT (8) | węzły 0,52 m: ta sama rama i płyty co Cube w wybranym kierunku; belki między węzłami: profil z fazą jak rama | `Port_*`, `Node_*`, `Socket_Start/End`, `occupied_cells`, wymiary belek |
| ROD (12) | płytki końcowe 0,23 m: mała płyta w kolorze `R4P_Base`; huby: rama `R4P_Secondary`; pręt bez zmian przekroju | porty `rod_mini`, długości, łuki |

Akcenty `TeamColor_Red` i `Hazard_Yellow` na łącznikach proponuję przenieść do strefy `R4P_Secondary`, żeby malowały się razem z resztą robota.

## Czego ten plan nie obejmuje

- Integracji z Unity (materiały URP, prefaby, LOD Group) — to osobne zadanie po akceptacji.
- Mechaniki malowania i jej zapisu.
- Nowych koliderów. Zatwierdzone kolidery pasują, bo nowa geometria mieści się w starej bryle.
- Zmiany szczeliny 30 mm między klockami.
