# Warstwa kadłuba — manifest i reguły dla prototypu runtime (`VisualHullPrototype`)

Zakres wg decyzji właściciela (PR #1256, komentarz 6819956851): **wyłącznie wizualna, opcjonalna, odwracalna** warstwa. Bez HP,
bez colliderów, bez wpływu na hit detection, bez nowych części, nic w Save/Load ani w blueprincie. Jedyną prawdą są komórki,
GUID-y części, porty i kolizje gry. Implementacja runtime należy do Codexa — tu są dane, reguły i dowód, że reguły działają
(test generatora + rendery). **Unity: NOT_RUN.**

## Pliki

| plik | zawartość |
|---|---|
| `../hull_kit/<Styl>/R4H_HullKit.glb` / `.fbx` | zestaw siatek stylu: 16 kafli płyty (`plate_00` … `plate_15`; Scrap ma 3 warianty `_0/_1/_2`), `gasket`, `lip` |
| `manifest_<Styl>.json` | parametry stylu, pięć blueprintów testowych z częściami i **fragmentami** (właściciel, wymagania, grupa, maska, macierz), scenariusze zniszczeń |
| `destruction_test.json` | wynik testu generatora dla siedmiu scenariuszy × 5 stylów |
| `../../scripts/rhull.py` | implementacja referencyjna (`fragments`, `check`, `tile`, `kit`) — ok. 150 linii, do przeniesienia 1:1 |

## Model danych

Warstwa robota = lista fragmentów. Fragment to instancja jednej siatki z zestawu:

```
{ kind: "plate" | "gasket" | "lip", mesh: "plate_05", owner: <id części>, requires: [<id części>, …],
  group: "up:1:3", mask: 5, cell: [i, j, k], face: "up", M: 4×4 (lokalnie względem robota, osie Blendera) }
```

- **owner** — część, do której fragment należy logicznie. Trafienie w fragment (jeśli kiedykolwiek będzie raycastowany do
  podglądu / zaznaczania) wskazuje `owner`; fragment nie ma własnego collidera i nie zmienia obrysu trafień.
- **requires** — fragment istnieje tylko, gdy istnieją wszystkie wymienione części.
- **group** — identyfikator scalonej płyty (ściana, warstwa, numer prostokąta). Kafle jednej grupy wyglądają jak jedna płyta.
- **mask** — 4 bity: bit 0 `+u`, bit 1 `−u`, bit 2 `+v`, bit 3 `−v`. Bit ustawiony = po tej stronie płyta biegnie dalej w
  sąsiada z tej samej grupy (kafel dochodzi do granicy komórki, bez krawędzi). Bit zerowy = krawędź płyty (margines, fazka,
  detal stylu: linia światła, listwa ze śrubami, pasek akcentu, nity, wałek).

Układy ścian (`u`, `v`, normalna; osie Blendera, przód = −Y): `right (−Y, Z, X)`, `left (Y, Z, −X)`, `back (X, Z, Y)`,
`front (−X, Z, −Y)`, `up (X, −Y, Z)`. Spodu nie pokrywamy. Kafel kanoniczny leży na ścianie +Z komórki w początku układu;
`M = T(środek komórki) · [u v n]`. Macierze w manifeście są w osiach Blendera; przeliczenie na Unity jest takie samo jak dla pozostałych assetów (Blender `(x, y, z)` → plik `(x, z, −y)`).

## Generowanie (czysta funkcja: komórki + typy części + styl)

1. **Odsłonięte ściany.** Dla każdego klocka kadłuba (Cube oraz klocki węzłowe STRUT) i każdego z pięciu kierunków: ściana jest
   odsłonięta, jeśli sąsiednia komórka w tym kierunku jest pusta (moduł, koło czy człon STRUT = zajęta).
2. **Scalanie.** W każdej płaszczyźnie zachłannie, w porządku rosnących współrzędnych `(u, v)`, bierzemy największy mieszczący
   się prostokąt nie większy niż `plate.big` stylu (A 3×2, B 2×2, C 3×3, D 2×1, E 2×2; obie orientacje). Prostokąt = grupa.
   Scrap: grupa o skrócie `crc32(group, liczba części) < 12 %` nie powstaje (brakujący arkusz), wariant koloru z tego samego skrótu.
3. **Kafle.** Każda komórka grupy dostaje kafel z maską wynikającą z członkostwa sąsiadów. `owner = requires = [ten klocek]`.
4. **Uszczelki.** Jedna na każdy szew 30 mm między dwoma klockami kadłuba oraz między modułem a klockiem, na którym jest
   zamocowany. `owner = min(id)`, `requires = [oba]`.
5. **Nadkola (`lip`).** Jedno na koło, gdy komórka nad kołem jest wolna. `owner = koło`, `requires = [koło, klocek mocowania]`.
   Sięga od burty tylko do 94 mm w stronę koła (opona zaczyna się dalej) — **nie zasłania koła ani jego uszkodzeń z góry**.

Determinizm: żadnej losowości poza skrótami z identyfikatora grupy. Ten sam robot i styl = ta sama warstwa, więc nie ma czego
zapisywać. Zmiana malowania zmienia tylko materiały (strefy `R4P_*` jak w częściach).

## Zdarzenia runtime (lokalnie, nigdy co klatkę)

| zdarzenie | co robi warstwa |
|---|---|
| budowanie, obrót / mirror, naprawa, zmiana stylu, wczytanie blueprintu lub zapisu | pełne przeliczenie (kroki 1–5) |
| odłączenie lub zniszczenie części `p` | usuń fragmenty z `owner == p` lub `p ∈ requires`; kaflom tej samej grupy sąsiadującym z komórką `p` wyzeruj bit maski w jej stronę (podmiana siatki na wariant z krawędzią). **Nie dokładaj** pancerza na ściany odsłonięte przez dziurę — do naprawy widać tam ciemny rdzeń, co czytelnie pokazuje uszkodzenie. Grup nie przeliczaj. |
| zmiana koloru | podmiana parametrów materiałów, geometria bez zmian |
| `VisualHullPrototype = off` | ukryj wszystkie fragmenty; części wyglądają jak rdzenie pass 2 (lub przełącz na geometrię sprzed prototypu) |
| garaż, tryb „pokaż podbudowę” | ukryj fragmenty `plate` i `lip` (uszczelki opcjonalnie), wybór klocków po prawdziwych colliderach |

Koszt zniszczenia jednej części: jej fragmenty (≤ 5 kafli + ≤ 6 uszczelek) i ≤ 4 sąsiadów na ścianę do podmiany siatki.

## Test (generator, nie Unity)

`destruction_test.json`: scenariusze na pojeździe testowym (29 części) — stan wyjściowy, 1 klocek ze środka płyty, 2 sąsiednie
klocki, cała sekcja (przedni rząd), koło, klocek pod bronią (broń odchodzi razem z nim), klocek narożny. Warunki PASS:

1. żaden fragment nie należy do usuniętej części ani jej nie wymaga,
2. każda krawędź płyty zwrócona do usuniętej komórki jest zamknięta (brak wiszącej płyty i otwartej krawędzi),
3. każdy pozostały fragment ma żyjącego właściciela (identyfikacja trafionej części).

Wynik: **PASS w 7 scenariuszach × 5 stylów**. Obraz: `../boards/09_destruction.jpg` (rendery z tych samych fragmentów).

## Liczby

Fragmenty: buggy 29 części → 83, crawler 103 → 224, dreadnought 208 → 494 (Scrap o kilka mniej). Zestaw stylu: 18 siatek
(Scrap 50), 0,8–2,5 tys. trójkątów łącznie. Wszystkie fragmenty stylu dzielą 3–5 materiałów stref → instancing GPU / SRP Batcher.

## Otwarte punkty dla Codexa (NOT_RUN po stronie Fable)

- import zestawu, shader z `COLOR_0`, pomiar draw calls / CPU / GPU / pamięci dla 30 / 100 / 200 / 400 części,
- zachowanie przy deformacji części (warstwa zakłada sztywne komórki),
- czy uszczelki ukrywać w garażu,
- zestaw nie ma jeszcze wypalonego `COLOR_0` (części go mają) — do dodania w kolejnym przebiegu artystycznym,
- reguła „bez płyt od spodu” i limit rozmiaru płyty są parametrami, nie założeniem.
