# B01-50 PERFVIEW-001 R1 — interaktywny offline performance inspector

**Status:** nowy, rzeczywiście działający prototyp narzędzia developerskiego, **nie** zintegrowany z RoboRougelike. Rola/producent: **B01-50**. Odbiór G0/proweniencji: **B01-40**, niezależne QA: **B01-60**, ewentualne zastosowanie w procesie Codexa: **Codex/HQ**. Decyzje o integracji i G1/G2 pozostają poza tą paczką.

## Co rzeczywiście działa

- Otwórz **`perfview-standalone.html`** w zwykłej przeglądarce z obsługą Canvas2D i File API. Zero pakietów, serwera, instalatora, sieci, analytics i localStorage. Możesz przejrzeć od razu przykład syntetyczny (fioletowa etykieta). Nigdy nie traktuj go jako pomiaru RoboRougelike.
- Załaduj dla sesji A i B po **trzy oryginalne pliki**: `receipt.json`, `frames.csv`, `objects.csv` z opcjonalnego B01-50 live recorder w [PR #328 @ `b5fc00428f6f663016e1525da556afbfe775facb`](https://github.com/C8LyNn/RoboRougelike/pull/328). Pola są sprawdzane z oryginalnym schematem walidatora `validate_live.py` (Git blob `7060aa3e364b2bc69de99501faa99bba57e84f82`). Recorder `B0150LiveFrameCapture.cs` blob `7112a748d0ca560403d91b166481088b8db85932`.
- Wykres P95 `Time.unscaledDeltaTime` w oknach 10-sekundowych, średnie części/pociski/przeciwnicy z oryginalnych 1Hz scanów, zestawienie p50/p95/p99 i próbek w obu sesjach. Używa strict CSV (obsługa cytowanych pól, odmowa brakujących/nadmiarowych danych), nie wstawia nazw scen jako HTML.
- **Ochrona interpretacji:** gdy różni się source SHA, ZIP SHA, Unity, CPU, GPU, OS lub rzeczywiste sceny, UI pokazuje **`WARUNKI RÓŻNIĄ SIĘ — NIE WYCIĄGAJ WNIOSKÓW`**. Gdy metadane są takie same, wynik to nadal tylko **eksploracja**, bez wniosków przyczynowych.

## Jak uruchomić i przetestować

1. W zwykłej przeglądarce otwórz `perfview-standalone.html`, kliknij „Załaduj przykład syntetyczny”. Wyświetlą się 8 metryk i 12 wierszy. Zaimportuj trzy pliki sesji A i analogicznie B. Dane są odczytywane wyłącznie z lokalnych plików.
2. Alternatywa dla developmentu: `python -m http.server 8765` w folderze paczki → http://127.0.0.1:8765/index.html (wymagany do modułów ES na niektórych przeglądarkach). Do produkcyjnego użytkowania polecany standalone.
3. **Node 20+:** `node --test tests/core.test.mjs` (na source `core.mjs`, pozytywne i negatywne asercje). **Python 3:** `python bundle.py` zdeterministycznie tworzy standalone z `index.html` / `style.css` / `core.mjs` / `app.mjs`.
4. **Opcjonalne prawdziwe Chromium / Playwright:** `python tests/browser_e2e.py`; wymaga zainstalowanego Playwright i Chromium (zmienna `PLAYWRIGHT_CHROMIUM_EXECUTABLE` nadpisuje ścieżkę). W tym środowisku runtime zablokował nawigację `http://127.0.0.1` i `file://` polityką administratora, dlatego Chromium testowało **dokładną wygenerowaną zawartość standalone** w dokumencie `about:blank` przez `page.set_content`, a nie dowolny fragment HTML. Obsługa File API / Canvas / JS została rzeczywiście wykonana.

### Testy faktycznie wykonane przez producenta

| Test | Wynik i zakres |
|---|---|
| `node --test tests/core.test.mjs` na R1 | **14/14 PASS**, wartości syntetyczne tylko do testów logiki |
| `python tests/browser_e2e.py` Chromium 144 | **6/6 PASS** (1280×720, 1920×1080, 720×900; rzeczywisty render, file input, niepoprawny receipt, reset/recovery), **0 błędów JS / 0 poziomego overflow** |
| `python bundle.py` i `sha256sum` | Samodzielny HTML wygenerowany na podstawie opublikowanych źródeł; manifest `MANIFEST.json` weryfikowany przed publikacją |
| Unity 6000.3.14f1, Windows Player, fizyka, FPS, CPU/GPU profiler | **NOT_RUN / NOT_MEASURED** przez B01-50, żadnych rzeczywistych obserwacji wydajności gry |

## Ograniczenia i bezpieczeństwo

**Ważne:** `Time.unscaledDeltaTime` nie jest `MsBetweenDisplayChange` (PresentMon), nie mierzy GPU, Main/Render Thread, GC. Licznik obiektów opiera się na nazwach trzech typów pocisków — jest niepełny; scan sam wprowadza narzut. Metadane z `receipt.json` mają status **`FILE_STRUCTURE_VALIDATED_ONLY_NATIVE_PROVENANCE_NOT_VERIFIED`**: interfejs nie potwierdza kryptograficznie rzeczywistego środowiska, logów, sterownika, autoryzacji ani Playera. Dane demonstracyjne są wyraźnie oznaczone. Nawet zgodne metadane A/B nie dowodzą odtworzenia identycznej trasy i warunków testowych. Dla decyzji o optymalizacji potrzebne oryginalne zrzuty Windows/Unity w istniejącej kolejce **CV-12** i niezależny odbiór.

## Własność, instalacja, rollback

Paczka zawiera wyłącznie `Tools/B01/B01-50/PERFVIEW001/**` i żadnych plików `Assets/**`, `Packages/**`, `ProjectSettings/**`, CHL #285 lub PARTSIZE #286. Jest to osobny prototyp research/QA, nie trzeci produkcyjny strumień mechanik gry. Nie jest autoładowany przez grę, nie dotyka użytkownika `persistentDataPath`, nie ingeruje w pracę Codexa.

**Instalacja:** skopiuj cały folder Tools do osobnego workspace lub wyodrębnij `perfview-standalone.html` i otwórz go lokalnie. **Rollback:** usuń folder Tools w swoim odizolowanym workspace; brak migracji, rejestru systemowego, ustawień Unity i zapisów do cofnięcia. Zawsze czytaj tylko kopię oryginalnych dowodów; nigdy nie nadpisuj oryginalnych plików. Nie integruj automatycznie do `Assets`.

**Handoff Codex (gotowy prompt):** „Na własnym autoryzowanym Windows/Unity buildzie uruchom istniejący CV-12, wyeksportuj oryginalne `receipt.json` + `frames.csv` + `objects.csv` ze scen o zbliżonych warunkach A/B, zachowaj hash i log, potem w odizolowanym workspace użyj PERFVIEW-001 R1 do wizualnego porównania. Nie wyciągaj wniosków FPS/CPU/GPU z samego `Time.unscaledDeltaTime`. Potwierdź przez B01-40 rzeczywisty SHA paczki i odbiór B01-60. Nie instaluj w grze bez odrębnej zgody.”

**Dependency pin:** [Codex #310](https://github.com/C8LyNn/RoboRougelike/pull/310) HEAD na starcie PASS17 `682dc0c5c13615577334375ffb6c27be5cdebf4a` (zmienny; nie stanowi dowodu integracji), źródłowy format PASS16 PR #328 `b5fc00428f6f663016e1525da556afbfe775facb`. Źródła tamtej paczki pozostają niezmienione.
