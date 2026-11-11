# Plan stylu A „Sleek” dla 20 łączników z PR #1186

**Propozycja projektowa. Żaden z 20 łączników nie został w tym zadaniu zmieniony ani zbudowany na nowo.** Wykonanie to osobne zadanie, bo dotyczy innej biblioteki, innego zestawu testów zgodności i ma własny koszt wydajnościowy.

## Co zostaje bez zmian

- Identyfikatory części, `occupied_cells`, `connection_ports` (`r4_full`, `rod_mini`), `Socket_Start` / `Socket_End`, pivot i `contact_mask_anchor` z manifestów #1186.
- Długości, rozstaw węzłów i przekroje nośne. Płaszczyzny styku węzłów ±0,26 m i płytek prętów zostają dokładnie tam, gdzie są.
- Kolidery z #1186.

## Co dostaje nowy wygląd

| Rodzina | Element | Propozycja |
|---|---|---|
| STRUT (8) | węzeł 0,52 m | ta sama powłoka co `R4B_Cube` w A: rama cofnięta o 4 mm, krawędzie 20 mm, jedna płyta na wolną ścianę. Ściana, z której wychodzi belka, zostaje ramą bez płyty. |
| STRUT | belka między węzłami | przekrój z zaokrągleniem 12 mm zamiast ostrych faz; na dłuższych belkach jedna płytka szczelina co komórkę, tak jak w `R4B_Beam3` |
| STRUT | przejście węzeł–belka | krótki kołnierz ze stycznym zaokrągleniem zamiast śrub i pierścieni |
| ROD (12) | płytka końcowa 0,23 m | mała zaokrąglona płyta w `R4P_Base` na ramie `R4P_Secondary`; port `rod_mini` nie jest rysowany |
| ROD | pręt | gładki walec 16-boczny, bez żłobień; huby jako krótkie walce z jedną płytą-pierścieniem (jak `R4B_Cylinder`) |
| ROD | łuki i kolana | ten sam przekrój prowadzony po łuku, gładkie normalne |

## Strefy malowania

Akcenty `TeamColor_Red` i `Hazard_Yellow` z #1186 proponuję przenieść do stref: płyty węzłów i płytek do `R4P_Base`, belki i pręty do `R4P_Secondary`, uskoki płyt do `R4P_Trim`. Łącznik maluje się wtedy razem z kadłubem, a nie obok niego.

## Narzędzia, które już istnieją

- `scripts/r4sleek.py` buduje pryzmy z dowolnego profilu, bryły obrotowe i szczeliny per komórka. Węzły, belki, pręty i huby da się z niego złożyć bez nowego generatora.
- `scripts/tests_compat25.py` czyta już manifesty łączników (porty, komórki) i sprawdza je w robotach. Brakuje testu własnego łączników: styk płytek `rod_mini` i prześwit wobec klocków przy wszystkich obrotach.

## Ryzyka do sprawdzenia przed wykonaniem

- **Budżet.** Zatwierdzony `R4C_STRUT_STANDARD` ma duży udział trójkątów w robocie R2. Cel: węzeł poniżej 700 tri, belka poniżej 150 tri na komórkę, LOD1 poniżej 100 tri na łącznik.
- **Łączniki skośne i łukowe.** Belka pod kątem wychodzi z węzła przez krawędź lub narożnik; trzeba zdecydować, czy rama węzła jest tam przycinana, czy belka dostaje kołnierz.
- **Pręty cienkie.** Przy 256 px pręt o średnicy poniżej 60 mm znika. Warto porównać obecną średnicę z nieco grubszym wariantem wizualnym przy tym samym koliderze.
- **Spójność z TrussCube.** Kratownica i pręty powinny mieć ten sam przekrój wizualny, żeby wyglądały jak jeden system.

## Proponowana kolejność

1. Pilot czterech łączników: `STRUT_STANDARD`, jeden STRUT skośny, `ROD_SHORT`, jeden ROD łukowy. Porównanie przed/po na robocie R2.
2. Przegląd właściciela.
3. Pozostałe 16 łączników, pełna macierz zgodności z 30 klockami A, bronią, kołami i modułami ruchu.
