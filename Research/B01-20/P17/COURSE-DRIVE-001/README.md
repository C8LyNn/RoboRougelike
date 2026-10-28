# COURSE-DRIVE-001/R1 — interaktywny prototyp jazdy po mapach MAPGEN

B01-20 / PASS17 continuation. Funkcjonalność NOWA wobec map źródłowych #331: sterowalny w 2D pojazd Light/Heavy, checkpointy, kolizje, pauza, reset i autopilot. **Nie jest to Unity/PhysX/Rigidbody** i nie jest zintegrowane z grą.

## Realna funkcjonalność i dane

- Samodzielny, lokalny pojedynczy plik HTML, z osadzonymi **18 rzeczywistymi oryginalnym C# MapGenPlanner wygenerowanymi ObstacleCourse** z [PR #331](https://github.com/C8LyNn/RoboRougelike/pull/331) @ 07d7d271b11ffc70e8e09da0043559fba06b5868. Nie wymaga serwera ani Internetu.
- 3 rodziny tras × 6 seedów, Light 2.1×3.2m / Heavy 4.5×5.8m. Rysowane są oryginalne Floor, Boundary, Obstacle, Ramp, start, meta, CP1...CP5.
- WASD/strzałki, R/reset, spacja/pauza, toggle Autopilot. Axis-aligned arcade inertia + grid-independent swept 0.20m collision, 3×3 podparcie terenu i footprint z marginesem 0.25m. Rampy jako obiekty uniemożliwiające płaski objazd. Realny licznik kolizji, czasu i postępu; automatyczne ponowne uruchomienie.
- Testy Node wykonują interaktywny kod ruchu na 18 rzeczywistych mapach × obu profilach. Autopilot służy do badania źródłowej trasy, **nie dowodzi przejazdu Unity**.

## Odtworzenie i gra lokalna

Na izolowanym checkout tej gałęzi, .NET8, Python3.11 i Node20+:

    dotnet run --project Research/B01-20/P17/MAPGEN-COURSE/MapgenCourseP17.csproj --configuration Release -- Research/B01-20/P17/MAPGEN-COURSE/presets.json p17-source-maps
    python Research/B01-20/P17/MAPGEN-COURSE/verify_courses.py p17-source-maps
    P17_BLUEPRINTS_DIR=p17-source-maps node --test Research/B01-20/P17/COURSE-DRIVE-001/core.test.mjs
    python Research/B01-20/P17/COURSE-DRIVE-001/build_viewer.py p17-source-maps
    P17_BLUEPRINTS_DIR=p17-source-maps node --test Research/B01-20/P17/COURSE-DRIVE-001/viewer-bundle.test.mjs

Na Windows CMD ustaw najpierw: set P17_BLUEPRINTS_DIR=p17-source-maps. Otwórz w przeglądarce p17-source-maps/COURSE-DRIVE-001.html po pobraniu artefaktu ZIP z GitHub Actions; wszystkie mapy są osadzone w HTML. Receipt JSON zawiera SHA256 gotowego HTML.

## Zależności, odbiór i rollback

Dokładna baza źródeł: MAPGEN R3.4.1 @ 5be31039411f89f17654b1c332f8692883fa03cc; upstream dane map #331 @ 07d7d271b11ffc70e8e09da0043559fba06b5868. Zmiany wyłącznie Research/B01-20/P17/COURSE-DRIVE-001/** i dedykowany workflow. **Żaden plik Assets, MAPGEN, CHL, PARTSIZE ani Codexa nie jest zmieniony.**

B01-40: sprawdza oryginalne pinning i rzeczywiste wyniki Actions. B01-60: niezależne QA obrysu Heavy, zatrzymywania na przeszkodach i checkpointów oraz przeglądarki. W PASS17 Wave02 wykonano rzeczywisty headless Chromium Playwright na Windows i Ubuntu 24.04 (720p, 1080p, portrait, klawiatura, pauza, reset, Heavy autopilot), z oryginalnymi zrzutami PNG w artefaktach. Nie wykonywano ręcznego testu przez człowieka ani natywnego Unity. Codex może tylko używać prototypu jako referencji oceny przejezdności, bez instalowania JavaScript do Unity.

Rollback: usuń tylko nowe pliki research i zarchiwizowany wynik HTML; nie usuwaj danych użytkownika ani nie edytuj kanonicznego generatora. G0 niezależne PENDING; native G1 NOT_RUN; G2 HOLD; NOT_IN_GAME.

## PASS17 WAVE02 — realna weryfikacja przeglądarki i poprawka resetu

**[GitHub Actions #40757538051](https://github.com/C8LyNn/RoboRougelike/actions/runs/40757538051), producent source+UI, Windows i Ubuntu 24.04 SUCCESS:** per OS **42/42** testów Node oryginalnego ruchu, **3/3** testów samodzielnego HTML, **2/2** rzeczywistych testów headless Chromium. W teście UI rzeczywiście klikano i naciskano przyciski na trzech rozdzielczościach (1280×720, 1920×1080, 720×900), sprawdzono brak overflow i błędów JS, wciśnięcie W, pauzę, reset, zmianę Light→Heavy, wybór innej mapy, postęp autopilota Heavy, zapisano **cztery PNG screenshots na OS**. Wygenerowano oryginalne 18 JSON dla obu OS. HTML byte-deterministic SHA256: `a79ee2aeac353da6170d384285ca754634ebf2d1a52c00c9f744c49782e96398` na Windows i Ubuntu.

- [Pobierz gotowy standalone HTML i 4 PNG z Windows](https://github.com/C8LyNn/RoboRougelike/actions/runs/40757538051/artifacts/12847116288)
- [Pobierz HTML i 4 PNG z Ubuntu](https://github.com/C8LyNn/RoboRougelike/actions/runs/40757538051/artifacts/12846521490)

**Realny RED→GREEN:** na Ubuntu-latest (26.04) oryginalny Playwright 1.56.1 nie mógł zainstalować Chromium; przypięto Ubuntu 24.04. Pierwszy uruchomiony browser test na obu systemach wykrył kruchą kontrolę resetu (timer odczytywany za późno); obecny test odczytuje timer po pierwszej nowej klatce, a logika nowego przejazdu zeruje pozostałość akumulatora fixed-step. Weryfikacja po obu zmianach jest rzeczywiście GREEN [#40757538051](https://github.com/C8LyNn/RoboRougelike/actions/runs/40757538051). To **automatyczny browser smoke**, nie niezależne B01-60 QA, nie ręczny playtest i nie Unity PhysX.
