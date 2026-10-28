# B01-12 / PASS16 — Nawigacja garażu (badawczy HTML)

**Status: RESEARCH_PROTOTYPE / NOT_INTEGRATED.** Działający, responsywny prototyp kategorii części, podglądu konstrukcji, kosztów zakupu i napraw, oraz osobnych ostrzeżeń o braku sprawnej broni i napędu. Nie zmienia Unity ani istniejących plików Assets.

## Uruchomienie

Otwórz **garage-navigation.html** bezpośrednio w przeglądarce. Test powtarzalny:

    python3 -m pip install playwright==1.57.0
    python3 replay.py > /tmp/b01-12-pass16-replay.json

Test ma przypięty SHA256 HTML: **7548c18a86ef07736d551ba24c587f3842206705c80fbc41353ab1c0450dc4e4**, sprawdza prawdziwy Chromium przez /usr/bin/chromium i zwraca niezerowy kod przy błędzie. Na innym systemie wymagana jest odpowiednia ścieżka do legalnie zainstalowanej przeglądarki.

## Obsługa

- Kategorie **Wszystkie / Uzbrojenie / Mobilność / Struktura** filtrują katalog bez demontażu części. Działają przyciski, Tab i strzałki.
- Budżet startowy **250 testowego złomu**. Ceny demonstracyjne odwzorowują aktualne źródłowe wartości bazowe Codexa: Gun 30, Wheel 15, Track 45, Omni 40, Hover 35. Odbudowa 0HP: ceil(35% zakupu), np. Gun 11, Track 16. Nie przedstawiaj symulacji jako faktycznego zapisu gracza.
- Kupno odejmuje złom, demontaż zachowuje posiadanie części i jej HP, ponowny montaż nie daje darmowego zdrowia. Brak środków zatrzymuje operację z komunikatem.
- Po zmianie napędu na gąsienice schemat pokazuje **Track L i Track R** oraz ich oddzielny stan HP zamiast kół. Uszkodzenie jednej strony przywraca CRITICAL_MOBILITY. Brak broni to osobna przyczyna nawet przy wyłączonych sugestiach.
- Powrót do edytora jest zawsze dostępny jako demonstracyjne działanie lokalnego UI. Start nie uruchamia rzeczywistej gry.

## Proweniencja i rzeczywiste wyniki

Wejście: istniejący B01-12 garaż W74 HTML blob **c9fb2cf145546bb70c3b0ecad23ddeca38faae3f**. Referencja ekonomii: oryginalny R3VehicleModel.cs Codex **561c223f7f01c1e609548bf1c8c66b5132ec5485**, Git blob **8cc68faf89793c3a803e5807594c9faf0df10091**. Wynik: nowy własny HTML blob **55e340df2d5d27981b02bf3b518d3c01a8c31be1**.

**Real Chromium/Playwright: 117/117 PASS** w 320×720, 768×1024, **1280×720 oraz 1920×1080**, zero błędów JS. Osobno faktycznie uruchomiono i opublikowano zgodny bajtowo **replay.py: 32/32 PASS** (to kontrolny podzbiór, nie dodatkowe przypadki produktu). Zrzuty i pełne JSON testów znajdują się w archiwum dowodowym rozmowy, nie w GitHub Actions.

Nowy błąd research-only: pierwsza wersja demonstracyjnego magazynu pozwalała na **0 HP → demontaż → montaż 100 HP bez naprawy**. Faktycznie odtworzony w Chromium jako expected RED; aktualny HTML przechowuje HP odmontowanej części, a naprawa 0 HP obciąża portfel (GREEN). To nie jest dowód błędu w kodzie Unity.

## Odbiór i ograniczenia

**PACKAGE_ID: B01-12-P16-GARAGE-NAV-001.** Manifest w tym katalogu zawiera dokładne bloby, właścicieli i zależności. **B01-11**: niezależny odbiór UX na nowym blobie, koszty/filtry/ostrzeżenia i screenshoty 720p+1080p. **Codex**: ocena wzorca, ewentualna implementacja wyłącznie w zastrzeżonym GUI po decyzji o ekonomii, magazynie i polityce blokowania startu. Natywne CV-03/CV-07 istnieją w #79 i pozostają odrębne.

Brak faktycznych Unity Edit/Play, PhysX, Windows Player, testów użytkowników, CODEX_CLAIMED, merge i G2. Symulator nie odtwarza pełnej geometrii, połączeń, rzeczywistej ekonomii progresji ani decyzji HQ BLOCK-v-CONFIRM.