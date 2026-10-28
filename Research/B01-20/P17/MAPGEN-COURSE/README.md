# MAPGEN-COURSE-P17/R1 — 3 nowe zestawy torów proceduralnych

**B01-20 / PASS17 | Original MAPGEN R3.4.1 source \`5be31039411f89f17654b1c332f8692883fa03cc\`**.

Zawartość **inna niż wcześniejsze PASS16 [areny CombatArena #326](https://github.com/C8LyNn/RoboRougelike/pull/326)**: wyłącznie nowa receptura **ObstacleCourse**. Trzy nowe presety danych, a z każdego **6 unikatowych seedów** (7,31,83,109,202,407): **18 nowych torów**, nie zbiór ręcznie wybranych stałych layoutów. Są wykorzystywane istniejące zabezpieczenia geometrii i checkpointów MAPGEN; nie implementujemy alternatywnego generatora ani pętli walki/nagród/zapisu Codexa.

| ID | Segmenty / checkpointy | LaneWidth | Gęstość | Trudność | Wartość dla gry |
|---|---:|---:|---:|---:|---|
| P17-COURSE-A-SLALOM | 3 | 16m | 40 | .25 | Krótki, prostszy wariant do nauki |
| P17-COURSE-B-CHICANE | 4 | 20m | 65 | .55 | Dłuższa seria przeszkód i ramp |
| P17-COURSE-C-ENDURANCE | 5 | 24m | 90 | .85 | Długi i gęstszy tor z przestrzenią do manewrów |

## Wykonywalne źródła i testy

Projekt .NET8 kompiluje **bez zmian** oryginalne \`MapGenData.cs\`, \`MapGenPlanner.cs\`, \`MapGenRouteValidator.cs\` oraz ich dotychczasowy **headless math-only** \`UnitySurrogate.cs\`. Nie kopiuje własnej implementacji generatora.

Uruchom na **izolowanym checkout PR** z upstream MAPGEN R3.4.1:

    dotnet run --project Research/B01-20/P17/MAPGEN-COURSE/MapgenCourseP17.csproj --configuration Release -- Research/B01-20/P17/MAPGEN-COURSE/presets.json p17-course-output
    python Research/B01-20/P17/MAPGEN-COURSE/verify_courses.py p17-course-output
    python Research/B01-20/P17/MAPGEN-COURSE/render_courses.py p17-course-output

Wynik: 18 rzeczywistych oryginalnych \`MapBlueprint\` JSON z konkretnymi koordynatami elementów, 18 SVG top-down i \`gallery.html\` oraz \`results.json\`. Źródłowe C# sprawdza 36 Light/Heavy original Plan+Validate, powtarzalność seedów, 3/4/5 checkpointów, podłoże, fizyczny zarys szerokości, unikalne hash i 9 **odrzuconych przez oryginalny planner** złych konfiguracji. GitHub Actions Windows+Ubuntu zachowuje pliki jako downloadable artifacts.

## Instrukcja odbioru Codex / rollback

Dostarczone JSON to **readback oryginalnego MapBlueprint, NIE format MapDocument v2**. Codex powinien, po G0/B01-40 i niezależnym B01-60, w zgodnym module MAPGEN R3.4.1 **odczytać config+seed z \`presets.json\` i wywołać istniejące \`MapGenBuilder.Generate(MapRecipe.ObstacleCourse, seed, config, vehicleProfile)\`**. Nie kopiuj plików pod \`Assets/**\` ani nie podmieniaj pracy Codexa bez jego wyraźnej zgody. Do rozgrywki tor musi zostać osobno wpięty w autoryzowaną aktywność; samo API mapy nie przyznaje nagród, nie ustawia runu ani nie zapewnia checkpoint-eventów CHL. Rollback: usuń wyłącznie izolowany research input i ewentualne własne zatwierdzone obiekty generacji przez \`MapGenBuilder.Clear\`, bez usuwania danych użytkownika.

**Falsifiable native acceptance, NOT_RUN:** prawdziwy Unity6000.3.14f1 Edit+Play z Light/Heavy Rigidbody na min. trzech seedach na wariant, rzeczywiste ślady kontaktów i przejazd przez wszystkie checkpointy, brak odciętej trasy i zablokowanego spawnu, reset/regen, 720p/1080p, original XML/Editor.log/exit/screenshots. 36 source-C# planów \`PASS\` nie oznacza PhysX\`PASS\`.

**Własność:** B01-20 wyłącznie \`Research/B01-20/P17/MAPGEN-COURSE/**\` i dedykowany workflow; oryginalny MAPGEN #237, Codex #310, CHL #285, PARTSIZE #286, B01-21/B01-25 niezmienione. G1/G2/HQ i niezależny odbiór pozostają otwarte.
