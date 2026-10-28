# B01-12 / PASS15 W74–W77 — funkcjonalny prototyp garażu: dwa ostrzeżenia krytyczne

**Status:** osobny działający research HTML, NIE instalacja w grze. Jeden rezultat zgodny z zadaniem zapasowym B01-12 [#320](https://github.com/C8LyNn/RoboRougelike/issues/320). Bez udziału realnych graczy, Unity, PhysX, Windows Player, C# wykonania, zmiany gry, merge ani G1/G2.

## Różnica względem wcześniejszego D03

W50 D03 badał **porównanie form prezentacji A/B** krytycznych powodów i wymagał maskowania moderatora. Ten nowy W74 to **jednookienkowy, przypominający garaż, interaktywny przepływ użytkownika** z graficznym schematem modułów oraz bezpośrednim montażem/demontażem/uszkodzeniem/naprawą. Brak eksperymentu A/B, nowy obszar weryfikacji: zachowanie ostrzeżeń w trakcie rzeczywistych zmian wybranych slotów, na wąskim ekranie, z opcjonalnymi sugestiami OFF, i oddzielną akcją powrotu do edytora.

## Rzeczywista podstawa źródłowa

- Oryginalny Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) na `231005c6d0351945a8ee6684f3856af118f0e94f` (historyczny w chwili tworzenia). `Assets/R3/R3VehicleModel.cs`: Git blob `9f1bf190c7535182c6ce375cbfe67f5b2862e811`:
  - `HasMobility` wymaga dla Wheels i Tracks dodatniego HP po *obu stronach*, Omni/Hover — jednej zdrowej części wybranego napędu.
  - `DeployError` sprawdza najpierw broń, potem mobilność i zwraca tylko **pierwszą** przyczynę; `CanDeploy` w obecnym źródle blokuje start.
- `Assets/R1/R1RunHUD.cs`: Git blob `c5218d468194ad0fc90f55ea8b9785518a49bb11`, korzysta z pojedynczego `DeployError`. P1 dual-warning D03 nadal należy wyłącznie do Codexa.
- Prototyp **symuluje tylko tę część kontraktu funkcjonalnego**. Nie emuluje `Connected`, faktycznej siatki części, ekonomii napraw, kolizji ani walidacji zmontowania Core. „Odłączona część” to oddzielny jawny przełącznik scenariusza.
- Przycisk `Sprawdź możliwość startu` **nie uruchamia gry**; wynik opisuje obecne blokujące `CanDeploy`, **nie** rozstrzyga polityki HQ BLOCK-vs-CONFIRM. Powrót do edytora jest również symulowany jako akcja na stronie.

## Uruchomienie bez serwera

Otwórz `Research/B01-12/W74/garage-readiness.html` w przeglądarce; nie wymaga zewnętrznych zależności ani połączenia sieciowego. Własne przyciski w każdej części: zamontuj/zdejmij/uszkodź do 0 HP/napraw do 100 HP. Przełącz `Koła`, `Gąsienice`, `Omni`, `Hover`, podpowiedzi i odłączoną część.

## Powtarzalny test realnego Chromium

Na Linux z Chromium `/usr/bin/chromium`, Python3 i `playwright`:

```bash
python3 -m pip install playwright==1.57.0
python3 Research/B01-12/W74/test_garage.py
```

**Wykonano**, a nie tylko zaprojektowano: Chromium headless/Playwright **155/155 PASS**, 0 page JS errors, przy 320, 390, 720, 1280, 1920 CSS px. Test zawiera 31 asercji na szerokość (155 łącznie), m.in. podwójny brak → naprawa 2→1→0, mobilność po jednej stronie HP0, osobno Tracks/Omni/Hover, odłączony element, podpowiedzi OFF, brak poziomego przepełnienia, klawiatura i drzewo dostępności tekstowej oraz zamierzony negatywny test usunięcia jednej karty ostrzeżenia z DOM z pozytywnym ponownym renderem. Trzy autentyczne screenshoty finalnej strony (`320`, `720`, `1280` CSS px) i wynik `evidence/test_results.json` znajdują się w archiwum dowodowym z rozmowy.

**Rzeczywiście wykryty i naprawiony przed publikacją błąd CSS:** pierwotny selektor konfiguracji wymuszał 357 CSS px scrollWidth przy viewport 320; `.topbar label{flex-wrap:wrap;min-width:0;max-width:100%}` oraz `select{width:100%;min-width:0}` zmieniły wynik na brak overflow. Stary zrzut `evidence/before-320-overflow.png` służy wyłącznie jako wcześniejsza ilustracja RED, a nie screenshot wersji finalnej.

## Granice i dalszy odbiór

- Nie jest to integracja HUD/garazu w Unity i nie zamyka D03 w oryginalnej grze. Ten szczegół jest już w istniejącej kolejce Codexa [CV-03](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) — bez nowego duplikatu CV.
- B01-11 może wykonać niezależną weryfikację dokładnego commita/HTML. Codex jest wyłącznym właścicielem źródła produkcyjnego i native validation; wymagane prawdziwe wyniki Unity/Windows Player nie zostały uruchomione tutaj.
- Nie ma badania z ludźmi, zwycięskiego wariantu UX, zatwierdzenia HQ, G0/G1/G2, instalacji czy merge.
