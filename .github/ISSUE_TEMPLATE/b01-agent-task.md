---
name: "B01 — zadanie agenta (AC + DoD)"
about: "Zadanie roju z kryteriami akceptacji, definicją ukończenia i udokumentowanym przekazaniem."
title: "[B01][TASK] "
labels: []
assignees: []
---

> **Zasady nadrzędne:** [własność i standard ukończenia #276](https://github.com/C8LyNn/RoboRougelike/issues/276), [CODEX INBOX oraz G0–G2 #79](https://github.com/C8LyNn/RoboRougelike/issues/79). Ten szablon **nie zmienia uprawnień**, **nie uruchamia agentów**, **nie jest zgodą na merge/release**. Przed pracą sprawdź nowsze decyzje HQ i rzeczywisty zakres Codexa.

## 1. Cel i kontrakt zadania

- **Problem gracza / cel biznesowy lub techniczny:**
- **Oczekiwany, obserwowalny rezultat:**
- **PACKAGE_ID / VERSION / priorytet:**
- **Właściciel wykonania (jeden producer):**
- **Koordynator / odbiorca:** B01-00 / Codex lub inny jawnie zatwierdzony odbiorca
- **Weryfikator kompatybilności:** B01-40
- **Niezależne QA:** B01-60 (lub jawnie wyznaczony weryfikator)
- **Źródłowa gałąź, bazowy commit SHA, wersja Unity (jeśli dotyczy):**
- **Dozwolone / zabronione ścieżki:**
- **Zależności, kontrakty API, możliwe konflikty i wymagany SCOPE ACK:**
- **Poza zakresem / zakazy:**
- **Powiązane issue, PR i decyzje projektowe:**


### Pilotaż oceny ryzyka (Risk Assessment) — tylko wybrane nowe zadania

**Na początek:** zastosuj tę krótką ocenę w **3 kolejnych, uzgodnionych przez B01-00 nowych zadaniach**. Nie przepisuj wstecz całego backlogu, nie zmieniaj zasad G0/G1/G2, niezależnego QA ani uprawnień Codexa. To **wspomaganie planowania testów**, nie automatyczna zgoda na merge i nie zastępstwo dla #276.

- **Prawdopodobieństwo awarii:** niskie / średnie / wysokie / nieznane — z czego wynika ocena?
- **Wpływ awarii:** niewielki / istotny / krytyczny — co może ucierpieć (np. jazda, ekonomia, save, dane gracza)?
- **Odwracalność skutków:** łatwa / trudna / niemożliwa / nieznana — czy rollback kodu przywróci również dane/stany?
- **Poziom końcowy:** `LOW` / `MEDIUM` / `HIGH`, **uzasadnienie jednym zdaniem**. Gdy możliwa jest nieodwracalna utrata danych, rozważ `HIGH` nawet przy małym prawdopodobieństwie. Nieznane ryzyko oznacz jawnie, nie zgaduj.
- **Plan działania:** 1–3 najbardziej wartościowe testy, wymagane zabezpieczenie/rollback, kto weryfikuje i jaki dowód powstanie.

**Praktyczne minimum kontroli:**
- `LOW`: sprawdzenie zmienionego zachowania + podstawowy smoke; nie pomijaj obowiązujących gate/QA.
- `MEDIUM`: sprawdzenie zmiany, przypadków brzegowych i powiązanych systemów.
- `HIGH`: dodatkowo testy regresji/integracji odpowiednie do skutków, niezależny odbiór i realnie sprawdzony plan ochrony danych lub wycofania, gdy dotyczy. Jeśli wymagane środowisko nie jest dostępne: `NOT_RUN/BLOCKED`, a nie PASS.

**Wynik pilotażu:** B01-00 porównuje po trzech zadaniach: trafność ocen, wykryte regresje, czas/zużycie pracy QA i liczbę poprawek. Dopiero na tej podstawie HQ decyduje, czy rozszerzać praktykę na inne zadania/roje.


## 2. Acceptance Criteria (AC): co ma działać

Zdefiniuj **przed implementacją** konkretnie dla tej funkcji. Każde kryterium ma mieć wynik możliwy do zaobserwowania i sposób weryfikacji. Nie są to ogólne czynności typu „napisz testy” — to wymagania wobec zachowania produktu lub systemu.

- [ ] **AC-01 — normalne użycie:** Gdy **[stan początkowy]**, po **[działanie]**, wtedy **[mierzalny/obserwowalny efekt]**. **Weryfikacja:** …
- [ ] **AC-02 — przypadek brzegowy/negatywny:** Gdy …, po …, wtedy …. **Weryfikacja:** …
- [ ] **AC-03 — zgodność między systemami / regresja:** Po zmianie … nadal działa …. **Weryfikacja:** …
- [ ] **AC-04 — UX/game feel, gdy dotyczy:** Gracz widzi/słyszy/rozumie …; akceptowalny wynik to …. **Ręczny test:** …

**Protokół oceny AC:** `AC-ID | PASS/FAIL/NOT_RUN/BLOCKED | na jakim SHA | środowisko | link do dowodu | kto potwierdził`. Automatyczny test kodu nie zastępuje rzeczywistej oceny game feel.

## 3. Definition of Done (DoD): kiedy wolno ogłosić ukończenie

Zastosuj wymagania odpowiednie dla rodzaju pakietu; **#276 oraz nowsze decyzje HQ mają pierwszeństwo**. Nie odhaczaj czynności, której nie wykonano.

- [ ] Fizycznie istnieje wynik pracy: działające źródła/assety, wymagane `.meta`/GUID i minimalne demo/reproducer, jeśli dotyczy; nie tylko raport lub deklaracja.
- [ ] Jeden właściciel źródła, dozwolone ścieżki, zależności i kontrakt API zostały zweryfikowane; brak kolizji z aktualnym zakresem Codexa.
- [ ] Paczka ma manifest i niezmienny `SOURCE_SHA`, opis ograniczeń, instalację, wycofanie (rollback), a także wymagane przez #79/#276 pliki i sumy kontrolne.
- [ ] AC mają zapisane wyniki; pozytywne, negatywne i regresyjne testy zostały **rzeczywiście wykonane**, a PASS/FAIL/SKIP/NOT_RUN/BLOCKED, wersja środowiska, logi/XML/kody zakończenia są uczciwie opisane.
- [ ] Dla paczek wymagających natywnego Unity wykonano niezbędne EditMode/PlayMode i test w grze na dokładnym SHA. Gdy nie wykonano: `NATIVE_NOT_RUN`; bez deklaracji `CODEX_READY` lub natywnego PASS tam, gdzie te testy są warunkiem bramki.
- [ ] Niezależny B01-60 zweryfikował właściwy SHA; B01-40 potwierdził integralność plików, zgodność, manifest, instalację i rollback.
- [ ] B01-00 potwierdził odpowiednią bramkę G0/G1/G2 i dopiero po spełnieniu warunków oznaczył pakiet `READY_FOR_CODEX`; w przeciwnym razie `QA_PENDING` / `BLOCKED` / `REWORK`.
- [ ] W [#79](https://github.com/C8LyNn/RoboRougelike/issues/79) zapisano realny stan, dokładne SHA, dowody i następnego właściciela. `OFFERED` ≠ `ACKNOWLEDGED` ≠ `INTEGRATED`; faktyczny odbiór Codexa wymaga jawnego ACK.

**Dwa osobne zakończenia:** `PACKAGE_COMPLETE` oznacza zweryfikowaną paczkę spełniającą wymagane bramki przekazania; `GAME_ACCEPTED` wymaga osobnego odbioru integracji w kanonicznej grze (testy Unity/Windows, regresje, manualny playtest zależnie od AC). Nie utożsamiaj tych stanów.

## 4. Przepływ komunikacji / następny właściciel

1. **B01-00** przydziela zadanie i zakres → producent potwierdza `CLAIM / SCOPE ACK` przed zmianami.
2. **Producent** tworzy izolowane źródła i raport własnych testów → Draft PR z dokładnym SHA.
3. **B01-60** niezależnie ocenia AC/testy → **B01-40** sprawdza artefakty, konflikty i instalację.
4. **B01-00** rozstrzyga gate/handoff → wpis w **#79** z konkretną wersją i SHA.
5. **Codex** odrębnie potwierdza odbiór, integruje i przekazuje wynik natywnych testów; błąd wraca do wskazanego właściciela zgodnie z #276.
6. **Właściciel gry / HQ** podejmuje decyzję wymagającą jego akceptacji; brak milczącego zatwierdzania merge/release.

Komunikaty przechowuj w odpowiednim Issue/PR GitHub, nie wyłącznie w rozmowie AI. Zamrożonej wersji po `OFFERED/FROZEN` nie nadpisuj; poprawka wymaga nowej wersji/SHA i właściwego przekazania własności.

## 5. Raport końcowy do skopiowania w Issue/PR

```text
ROLE | PACKAGE_ID@VERSION | PRODUCER | SCOPE | PR@IMMUTABLE_SHA
AC: AC-01=PASS/FAIL/NOT_RUN/BLOCKED (evidence URL), AC-02=..., AC-03=...
G0 SOURCE: ... | G1 TESTS/NATIVE: ... | INDEPENDENT QA: ... | G2: ...
EVIDENCE_URLS: ... | KNOWN_LIMITATIONS: ... | ROLLBACK: ...
HANDOFF: NOT_SENT/OFFERED_FROZEN/SENT/ACK_PENDING/CODEX_CLAIMED/INTEGRATED/RETURNED_FOR_REWORK
CODEX_ACK_URL: ... | BLOCKER: ... | NEXT_OWNER: ... | NEXT_ACTION: ...
```
