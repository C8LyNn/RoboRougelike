# #1182 — wytyczne integracji dla Codexa (łączniki ROD / STRUT)

Ten PR to wyłącznie art: modele, manifesty, testy i podgląd. **Nie zmienia mechaniki gry.** Poniżej jest lista zmian runtime, których wymaga pełna kompatybilność. Tam, gdzie nie ma jeszcze kontraktu, wpisałem `PENDING_CODEX_DESIGN`. Tło standardu R4 (port, siatka, ContactMask, BuildSocket) opisuje `Tools/ModelFactory/R4Blocks/CODEX_INTEGRATION.md` (PR #963).

Dane źródłowe dla każdej części: `R4C_<ID>/R4C_<ID>_connector_manifest.json`. Wszystkie współrzędne są w komórkach Unity względem komórki (0, 0, 0), a pivot modelu leży w środku tej komórki.

## 1. Identyfikatory i zapis

- 20 nowych identyfikatorów `R4C_*`, rodziny `rod` i `strut`. Lista jest w `INDEX.json → parts`.
- Typ części: `PENDING_CODEX_DESIGN`. Do wyboru:
  - nowy `R3PartKind.Connector` z polem `ConnectorId`;
  - 20 wartości enum.

  Nie dodawałem niczego do `R3PartKind`.
- Save/Load: zapis musi trzymać `ConnectorId`, komórkę kotwicy, obrót (24 możliwości) i flagę lustra. Stare zapisy nie zawierają łączników, więc nie wymagają migracji.

## 2. Części wielokomórkowe

- `occupied_cells` to jawna lista z rolą komórki:
  - `node` — węzeł z portami;
  - `span` — człon przechodzi przez środek;
  - `crossing` — człon muska komórkę swoją grubością.
- Propozycja: przy stawianiu rezerwować **wszystkie** wymienione komórki. Geometria nie wychodzi poza tę listę — sprawdzono BVH-em w `_QA.json → occupancy_consistent`.
  - Zluzowanie rezerwacji komórek `crossing` to `PENDING_CODEX_DESIGN`. Wymagałoby testu kolizji siatek przy budowie.
- Obrót komórki: `cell' = R · cell + anchor`, gdzie R jest jedną z 24 całkowitych macierzy obrotu. Lustro to `scale X −1`, a ściany left i right zamieniają się miejscami.
  - Sprawdzone dla każdej części (`rotations.check`, 48 transformacji): komórki pozostają unikalne, porty leżą na zajętych komórkach i patrzą na wolne.
  - Dla wszystkich 20 części lustro daje to samo co jakiś obrót, więc wystarczy jeden model na część.

## 3. Porty i reguła połączenia (podstawa triforcingu)

- Połączenie istnieje **tylko** wtedy, gdy spełnione są oba warunki:
  - dwa zadeklarowane porty leżą na przeciwnych ścianach sąsiednich komórek;
  - typy portów są zgodne.

  Samo sąsiedztwo, nawet styk geometrii, **nie jest** połączeniem. Człony nie mają portów.
- Typy portów:

  | typ | wymiar | wolno | nie wolno |
  |---|---|---|---|
  | `r4_full` (STRUT) | port 0,509 m = pełny port R4 | klocki (full/partial), łączniki, broń, ruch, koła | — |
  | `rod_mini` (ROD) | płytka 0,23 m, `patch_half_cells` 0,2091 | klocki, łączniki (połączenie konstrukcyjne) | moduły: broń, ruch, koła |

- `BuildSocket` (BUILD001): `Offset = 0`, `HalfSize = patch_half_cells`.
  - Pole styku `rod_mini` z pełnym portem wynosi 0,175 komórki², więcej niż `MinContactAreaCellsSquared = 0,01`.
  - Sama maska ściany (`R3MountingGrid`, kolejność bitów right, left, up, down, forward, back) **nie wystarczy**: gra musi znać typ portu, żeby odrzucić moduł na `rod_mini`.
  - `contact_mask_bit` jest w każdym porcie manifestu.
- Test reguły w `assemblies/assemblies_summary.json → rule_checks`: 9/9 PASS. Moduł na `rod_mini` jest odrzucany, pary konstrukcyjne są akceptowane.

## 4. Walidacja w garażu

1. Komórki: brak konfliktu z zajętymi komórkami, z uwzględnieniem obrotu i lustra.
2. Połączenie: każda nowa część potrzebuje co najmniej jednej zgodnej pary portów z istniejącą konstrukcją.
3. Kolizja montażu modułu: niektóre porty są geometrycznie zasłonięte.
   - Przykład: wewnętrzne porty STRUT_ARC `P_A_right` i `P_B_back`. Koło XL Monster nie mieści się tam w żadnym obrocie; Plate2x2, Beam3, RotarySaw i MagneticGrapple pasują tylko w części obrotów.
   - Szczegóły dla każdego portu i obrotu: `compat/compat_all.json`.
   - Proponowane rozwiązanie: sprawdzanie kolizji colliderów przy montażu albo statyczna lista zakazów. `PENDING_CODEX_DESIGN`.
4. Spójność: wszystkie części muszą być połączone z kokpitem lub kotwicą (graf jak w `assemble.py`).

## 5. Przepływ obrażeń (#1153)

- Graf wewnętrzny łącznika to `damage_routing.internal_graph`: węzeł → człon → węzeł. Na zewnątrz przepływ idzie tylko przez `connection_ports`.
- Przykład z konstrukcji `two_robots`: kostka między prętami (`B_filler_cube`) styka się z węzłami prętów, ale nie ma z nimi połączenia. Obrażenia ściany pancerza idą do kadłuba wyłącznie przez dwa pręty.
- Do zaprojektowania (`PENDING_CODEX_DESIGN`):
  - podział obrażeń między łącza równoległe;
  - zachowanie po zniszczeniu członu (czy węzły zostają);
  - kaskada odłączeń przy mostach — `graph.single_point_links` w raportach scen.

## 6. Masa, HP, koszt

`proposal` w manifeście to **PROPOSAL_ONLY**. Wzór liczy się z liczby węzłów i długości członów; jednostką odniesienia jest R4B_Cube. Ostateczne wartości ustala balans.

## 7. Kolizje i wydajność

- `_colliders.glb` to propozycja wypukłych kawałków UCX: pudełka węzłów i odcinki członów.
- Siatka wizualna pręta ma 2,6–13,3 tys. tri, rozpory 9,4–20,8 tys. tri. Dla dużych konstrukcji warto rozważyć LOD albo łączenie siatek statycznych. `PENDING_CODEX_DESIGN`.

## 8. Testy po stronie Unity (dziś UNITY_NOT_RUN)

- **EditMode:**
  - import FBX i GLB (+Z przód, +Y góra, skala 1);
  - pozycje pustych obiektów `Port_*` i `Socket_*` zgodne z manifestem (±1 mm);
  - 24 obroty i lustro dają te same komórki co `rotations.check`.
- **PlayMode:**
  - zbudować sceny z `assemblies/*_assembly.json` (lista części, komórki, obroty, montaże);
  - sprawdzić liczbę par portów, brak przenikań colliderów i graf (`graph`) zgodny z raportem;
  - test negatywny: broń na porcie `rod_mini` zostaje odrzucona.
