# R4 Style Atlas 5 — biblie stylów (wersja 2, po korekcie kierunku)

Wersja 1 została odrzucona: pięć wariantów różniło się głównie fazami i kolorem. Ta wersja opisuje to, co faktycznie
robi generator w gałęzi `art/1239-atlas5-style-reset`, i wprost mówi, gdzie różnice są jeszcze za słabe.

Zasada nadrzędna: styl ma być czytelny bez koloru — w grayscale, w wireframe i w sylwetce. Dowód jest w
`compare/5way/<Id>.jpg` (pięć trybów × pięć stylów, ta sama kamera i światło) oraz w `compare/style_metrics.json`.

## A — Sleek Futuristic

- **Język form:** precyzyjne zaokrąglenia 20 mm (4 segmenty), płyty o zaokrąglonych rogach, gładkie bryły obrotowe o
  gęstym podziale. Mechanizmy schowane: pręty cieńsze (×0,85), bez odsłoniętych łączników.
- **Szczegółowość:** średnio-wysoka, ale detal jest drobny i świetlny, nie konstrukcyjny.
- **Detal własny stylu:** włosowe szczeliny światła na każdym wolnym panelu, pierścień światła na każdym większym
  cylindrze, świetlny szew wokół podstawy montażowej.
- **Materiały:** jasny lakier (chropowatość 0,22), grafit, polerowany metal; emisja najsilniejsza z piątki (1,6).
- **Tekstura:** `A_detail.png` — czysta powierzchnia z włosową siatką co 1/8 kafla.
- **Animacja:** długie, płynne wejście i wyjście, bez przeregulowania.
- **Kolor:** biel / jasna szarość + grafit + akcent emisyjny.
- **Od pozostałych odróżnia go:** jedyny styl z dodatkowymi elementami świecącymi i bez widocznych łączników.

## B — Brutal Industrial

- **Język form:** twarda faza 10 mm bez zaokrąglenia, głęboki stopień ramy 8 mm, płyty ośmiokątne (rogi ścięte).
  Pierścienie o przekroju kwadratowym, pręty jako siłowniki hydrauliczne (korpus, dławik, tłoczysko).
- **Szczegółowość:** wysoka, konstrukcyjna.
- **Detal własny stylu:** śruby sześciokątne w rogach każdej płyty, dwa żebra na każdym wolnym panelu, kołnierze na
  końcach cylindrów, odlewane zaciski narożne ze śrubami przelotowymi na podstawie montażowej.
- **Materiały:** stal (metaliczność 0,9), węglowa czerń ramy, tlenkowy pomarańcz jako akcent; słaba emisja.
- **Tekstura:** `B_detail.png` — szczotkowana stal ze smugami i ciemniejszymi zaciekami oleju.
- **Animacja:** powolny start, twarde zatrzymanie, tłumione dobicie; koła zamachowe z opóźnieniem.
- **Kolor:** stal + czerń + pomarańcz ostrzegawczy.
- **Od pozostałych odróżnia go:** najwięcej osobnych części mechanicznych; jedyny z odsłoniętą hydrauliką i śrubami.

## C — Arcade

- **Język form:** jedno miękkie zaokrąglenie 36 mm, **bez ramy i bez płyt** — czyste, gładkie bryły. Części grubsze
  (×1,25) i powiększone (×1,10).
- **Szczegółowość:** najniższa. Części mniejsze niż 55 mm nie są budowane wcale.
- **Detal własny stylu:** brak — to jest cecha stylu.
- **Materiały:** matowe, niemetaliczne, bez emisji.
- **Tekstura:** brak (płaski kolor) — celowo.
- **Animacja:** szybkie wejście i wyraźna pauza, bez ruchu wtórnego.
- **Kolor:** miękkie pastele, dwie duże plamy.
- **Od pozostałych odróżnia go:** najmniej trójkątów i części; jedyny bez żadnych linii podziału paneli.

## D — Scrap Wasteland

- **Język form:** cienkie krawędzie giętej blachy (fazy 4–12 mm, inne na każdej części), płyty o ściętych rogach,
  bryły obrotowe cięte w płaskie fasety. Kontrolowana asymetria: części, które nie dotykają płaszczyzn styku, są
  obrócone do ±3° i przesunięte do ±4 mm.
- **Szczegółowość:** wysoka, nieregularna.
- **Detal własny stylu:** każda płyta w innej strefie (blacha / łata / rdza), przyspawane łaty z innego arkusza, nity
  w części rogów, dwa pasy na cylindrach na różnych wysokościach, tuleje spawane na rurach, kątowniki i rura na
  podstawie montażowej.
- **Materiały:** ciemne, przytłumione, chropowatość 0,88–0,97; rdza jako osobna strefa.
- **Tekstura:** `D_detail.png` — brud, wykwity rdzy o ciepłym odcieniu, rysy.
- **Animacja:** nierówne tempo, drgania, lekki dryf (szum z ziarnem per model).
- **Kolor:** najciemniejsza paleta z piątki.
- **Od pozostałych odróżnia go:** jedyny asymetryczny; najwyższy kontrast lokalny w grayscale dzięki teksturze i łatom.

## E — Heroic Toon

- **Język form:** duże zaokrąglenie 30 mm (6 segmentów), płyty w kształcie pigułki, cylindry z wypukłym brzuścem
  (beczka), pręty zwężane, pierścienie i kule pogrubione ×1,35. Wolnostojące części powiększone ×1,18.
- **Szczegółowość:** niska — części mniejsze niż 45 mm nie są budowane.
- **Detal własny stylu:** kremowa obwódka wokół każdej plamy koloru (rama i faza w strefie Secondary).
- **Materiały:** półmat, bez metalu poza jedną strefą; emisja umiarkowana.
- **Tekstura:** `E_ramp.png` — malowana trójstopniowa rampa światła mapowana wysokością modelu (jasna góra, ciemny
  dół, falista granica pędzla), niezależna od oświetlenia sceny.
- **Animacja:** antycypacja, przeregulowanie, sprężyste dobicie.
- **Kolor:** łagodne, ciepłe bloki z jasną obwódką.
- **Od Arcade odróżnia go:** obwódki, malowana rampa, przerysowane proporcje i ekspresyjny ruch; Arcade jest płaski i
  spokojny.

## Wspólny kontrakt (nie zmienia się ze stylem)

Obrys logiczny, płaszczyzny styku ±0,26 m i miara kontaktu z zatwierdzonych klocków, gniazda i porty, nazwy węzłów,
hierarchia, oś czasu i klatki zdarzeń. Dla części z zatwierdzonym rigiem (broń, napędy, koła) gniazda i animacja
funkcjonalna są przenoszone z zatwierdzonego pliku GLB; styl zmienia tylko tempo wewnątrz klipów przejściowych.

## Gdzie różnice są nadal za słabe — wprost

1. **Wspólna kompozycja.** Każdy asset ma jedną definicję układu części, a styl ją interpretuje (inne archetypy brył,
   inny detal, pomijanie i powiększanie części). To daje wyraźnie różne powierzchnie, gęstość i charakter, ale
   **ogólny układ modelu jest ten sam we wszystkich pięciu stylach**. Wymóg „pięć różnych możliwych gier” w pełnym
   sensie wymaga osobno zaprojektowanej konstrukcji per styl — tego ta paczka nie zawiera.
2. **Klocki.** Bryła klocka jest zatwierdzona, a ściana kontaktu musi być w ≥80 % płaska. Suma fazy, stopnia i
   marginesu nie może przekroczyć 40 mm, więc sylwetki pięciu sześcianów są z definicji prawie identyczne; różnią
   się profilem krawędzi, kształtem płyt i detalem w zagłębieniach.
3. **Części z rigiem.** Geometria każdego węzła musi mieścić się w granicach węzła źródłowego, dlatego dodatki
   stylowe (kołnierze, żebra, powiększanie) są tam wyłączone — różnice są mniejsze niż w modułach.
4. **Tekstury** to mapy detalu mnożone przez kolor strefy. Map chropowatości, metaliczności i normalnych nie ma.

Liczbowo: `compare/style_metrics.json` podaje dla każdego assetu IoU sylwetek między parami stylów. Wartość bliska 1
oznacza ten sam obrys — i tak właśnie wypadają klocki.
