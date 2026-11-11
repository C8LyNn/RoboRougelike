# R4 Style Atlas 5 — biblie stylów (faza 0)

Pięć kierunków artystycznych dla tych samych części. Styl nie jest rzadkością ani tierem: identyczna część ma w każdym
stylu ten sam obrys logiczny, te same porty, gniazda, kolizje i zdarzenia animacji. Różni się wyłącznie geometria
dekoracyjna wewnątrz zatwierdzonej bryły, materiały domyślne i charakter ruchu.

Parametry poniżej są wejściem generatora (`scripts/`), nie opisem życzeniowym. Obrazy wzorcowe (hero) powstają w fazie 1
z realnych modeli pilota — w fazie 0 nie ma żadnych ilustracji zastępczych.

| | A — Sleek Futuristic | B — Brutal Industrial | C — Arcade Geometric | D — Scrap Wasteland | E — Heroic Toon |
|---|---|---|---|---|---|
| Krawędzie | zaokrąglenie 20 mm, 4 segmenty | twarda faza 12 mm, 1 segment | jedna duża faza 30 mm, płaska | nierówne fazy 5–22 mm, różne na każdej części | duże zaokrąglenie 34 mm, 6 segmentów |
| Panele | jedna płyta na ścianę, róg R 40 mm, szczelina 6 mm | mała płyta w szerokiej ramie, rogi ścięte, stopień 8 mm | brak ramy i płyt — czyste płaszczyzny | 2–3 niedopasowane łaty na ścianę, różne strefy koloru | wypukła płyta z grubym rowkiem 12 mm, obwódka w kolorze akcentu |
| Bryły obrotowe | gładkie, 20–28 segmentów | 12 segmentów, kołnierze i pierścienie na końcach | 6–8 segmentów, płaskie ściany | 10 segmentów, wgniecenia (szum promienia) | gładkie, 32 segmenty, pogrubione |
| Detal dodany | brak (precyzja, ukryte mechanizmy) | śruby sześciokątne, żebra, wsporniki, tłoczyska | brak; drobne części pomijane | nity, spoiny, pasy, rura, łata z innej blachy | pogrubione pierścienie i kule, pas akcentu |
| Asymetria | brak | brak | brak | kontrolowana: obrót ±3°, przesunięcie ±4 mm części, które nie dotykają płaszczyzn styku | brak; przerysowane proporcje |
| Domyślna paleta | biel / jasna szarość, grafit, cyjan | stal, węglowa czerń, tlenkowy pomarańcz | 3 duże płaskie plamy koloru | spłowiała blacha, rdza, stara farba | nasycone bloki koloru, jasna obwódka |
| Materiał | lakier, niska chropowatość | surowa stal, wysoka metaliczność | matowy, bez metaliczności | chropowaty, strefy rdzy | półmat, wyraźne plamy światła |
| Ruch | płynne wejście i wyjście, bez przeregulowania | powolny start, ciężkie zatrzymanie z tłumionym dobiciem | skok i pauza (krok co 2 klatki) | nierówne tempo i drgania | antycypacja, przeregulowanie, sprężyste dobicie |

## Co jest wspólne (kontrakt)

- Płaszczyzny styku ±0,26 m: w każdym stylu płyta kontaktu leży dokładnie na płaszczyźnie i przechodzi tę samą miarę
  `patch_test` co zatwierdzone klocki. Styl nie może cofnąć ani wysunąć ściany kontaktu.
- Geometria każdego wariantu mieści się w bryle wariantu źródłowego (klocki: zatwierdzona bryła analityczna #963;
  moduły: komórka 0,52 m; broń, napędy, koła, łączniki: zatwierdzone granice i przemiatanie z rekordów QA).
- Nazwy węzłów, hierarchia, gniazda, lufy, `Port_*` — identyczne we wszystkich pięciu wariantach.
- Animacja: wspólna oś czasu i wspólne klatki zdarzeń (np. wystrzał, `Deploy_Out`); styl zmienia tylko kształt krzywych
  między zdarzeniami i kanały czysto kosmetyczne.
- Strefy malowania: `R4P_Base`, `R4P_Secondary`, `R4P_Trim`; `R4P_Metal` i `R4P_Glow` pozostają **proposal**.
- Trzy presety malowania na styl: neutralny, kontrastowy drużynowy, jasny alternatywny — dane w manifeście palety,
  bez wypalonego światła.

## Rozróżnialność

Kryterium z zadania: styl ma być rozpoznawalny w skali szarości i w sylwetce. Dla części zamkniętych w ciasnej bryle
(sześcian 0,52 m) sylwetka z definicji różni się mało — tam różnicę niosą profil krawędzi, podział paneli i detal.
Raport pilota podaje to liczbowo (IoU sylwetek między stylami, gęstość krawędzi) i nie udaje, że kostka ma pięć
różnych obrysów.

## Tekstury

Atlas używa kolorów materiałów i stref, bez malowanych tekstur. Rdza w stylu D i plamy koloru w E są strefami
geometrii, nie mapami. Mapy chropowatości / metaliczności / emisji nie są autorskie — to ograniczenie zapisane w
raportach, nie pominięcie.
