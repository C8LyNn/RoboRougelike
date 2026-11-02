# B01-13 · PASS 17 / WAVE 27 · REAL-PLAYTEST-OBSERVATION-001/R1

**Właściciel:** B01-13 HUMAN PLAYTESTING. **Zlecenie:** HQ [#800](https://github.com/C8LyNn/RoboRougelike/issues/800), reguły współpracy #799. **Konkretny produkt:** osobna, działająca lokalnie aplikacja do rejestrowania **faktycznych** obserwacji z gry. To uzupełnienie funkcjonalne istniejącego, oryginalnego [Draft #786](https://github.com/C8LyNn/RoboRougelike/pull/786) (poprawiony fokus w ośmiozadaniowym teście rozumienia zasad), **nie nowy równoległy symulator mechanik**.

## Co fizycznie wykonuje

`observer.mjs` generuje samowystarczalny, polskojęzyczny offline HTML. Operator testu:

1. Przed startem **musi** uzyskać dobrowolną zgodę uczestnika, zapoznać z celem i możliwością przerwania; zaznacza zgodę i brak zbierania danych identyfikacyjnych. Wpisuje **prawdziwy, aktualnie testowany 40-znakowy SHA buildu/commita**, nie SHA tej paczki badań. Bez tych pól aplikacja nie rozpocznie sesji.
2. W grze RoboRougelike wykonuje osiem dokładnie określonych czynności; z góry znane warunki wstępne, działania uczestnika i oczekiwany obserwowalny rezultat podano na kartach. Obserwator **ręcznie** zapisuje dla każdej karty jeden wynik `PASS`, `FAIL`, `BLOCKED` albo `NOT_RUN`, 0–3600 s rzeczywistego czasu, 0–20 podpowiedzi, kategorię problemu konstrukcji/gry (np. DOCK, CLEARANCE, COLLIDER, SOFTLOCK, SAVE, LOANER) oraz wagę P0/P1/P2/NONE. Nie ma pola na imię, konto, e-mail, audio, surowe notatki ani dane zdrowotne.
3. Formularz **fail closed**: `NOT_RUN` nie może mieć czasu, podpowiedzi ani problemu; `BLOCKED` nie ma wymyślonego czasu; `FAIL` wymaga zidentyfikowanego problemu; `PASS` nie może ukrywać krytycznego P0; `NONE` issue musi odpowiadać severity NONE. Ponowny start i cofnięcie nie powodują automatycznego wysłania danych.
4. Po ośmiu kartach aplikacja mierzy obiektywne **kryteria tego protokołu**, liczby PASS/FAIL/BLOCKED/NOT_RUN, kategorię i wagę problemów, podpowiedzi, medianę faktycznie wpisanych czasów. `BLOCKED` lub `NOT_RUN` oznacza `INCOMPLETE`, **nie PASS**. Wynik `CRITERIA_MET` to tylko wyliczenie z **samodzielnie wpisanych przez prowadzącego obserwacji**, nigdy zatwierdzenie Unity G1 czy uznanie gry.
5. Operator może pobrać **prawdziwy plik JSON i CSV z wprowadzonymi informacjami** (wyłącznie po kliknięciu). Reset bez zachowania danych usuwa bieżącą pamięć strony. Nie ma telemetrii, `fetch`, localStorage ani sieci. Pobrany plik pozostaje poza kontrolą aplikacji; prowadzący jest odpowiedzialny za ochronę i późniejsze usunięcie danych.

**Przed uruchomieniem prawdziwego badania:** sprawdź, czy dany build naprawdę zawiera wskazaną funkcję. Brak połączenia CanDeploy, Save/Load lub ekonomii V7 z grywalną gałęzią Codexa oznacza `BLOCKED`/`NOT_RUN`, a nie udawany PASS. W szczególności PT05, PT06 i PT08 wymagają realnych autorytatywnych odpowiedników. Jeśli wymagany rodzaj wyzwania nie jest dostępny, zadanie BLOCKED i nie wolno mintować zwycięstwa.

## Osiem scenariuszy, dane i kryteria

| ID | Czynność | Obserwuj przede wszystkim |
|---|---|---|
| PT01 | Sprawny robot z montażem i próbną jazdą | osie, styki, stabilność, czytelność CanDeploy |
| PT02 | Montaż/obrót 1×2×2 przy zajętej strefie | obrót, kolizję, komunikat |
| PT03 | Przesłanianie sektora strzału LPM/RPM | clearance broni, strzał we własną osłonę |
| PT04 | Opcjonalna część 0 HP przy żywym Core | brak nieuzasadnionej blokady starcia |
| PT05 | Jednorazowy słabszy robot zastępczy | jednorazowość po porażce, brak duplikacji nagród |
| PT06 | Odbudowa i rozliczenie następnego zwycięstwa | darmowy przydział raz na wygraną, V7 floor(65%), NEXT-win |
| PT07 | Odłączanie części i fizyka Rigidbody | reakcja konstrukcji, nagłe impulsy, znikanie komponentów |
| PT08 | Zapis, zamknięcie, wczytanie | stałe ID, portfel, zastępca, prędkość/pozycja |

### Progi „sukcesu badawczego” (robocze, do zatwierdzenia przez B01-00/10/60)

- **C1:** co najmniej 6 z 8 zadaniowych wyników PASS.
- **C2:** ZERO zaobserwowanych błędów P0 oraz ZERO SOFTLOCK.
- **C3:** suma maksymalnie 2 podpowiedzi podczas wykonanych zadań.
- **C4:** mediana czasu wykonanych zadań najwyżej 180 sekund.
- Każde niewykonane lub zablokowane zadanie → **INCOMPLETE**, a nie sukces; nieporównywalnego buildu i braku funkcji nie maskować.

**Nie są to zatwierdzone progi wydania gry.** B01-00/B01-10 mogą zatwierdzić lub skorygować AC po pierwszych rzeczywistych sesjach. Zaobserwowanie konkretnego problemu P0 w realnej sesji nie może zostać zignorowane, nawet przy 7 innych PASS.

### Słownik problemów i P0

- `DOCK`: nieprawidłowy styk, rotacja lub przeskok montażu;
- `CLEARANCE`: obstrukcja lufy/strzał we własny pancerz;
- `LOCOMOTION`: niezrozumiała utrata napędu/sterowności;
- `COLLIDER`: penetracja, drżenie, niestabilna fizyka;
- `UI`: mylący komunikat garażu;
- `SAVE`: brak części, reset waluty/identyfikatorów po załadowaniu;
- `LOANER`: nieprawidłowa dostępność lub podwójne zużycie;
- `SOFTLOCK`: brak dozwolonego sposobu kontynuacji;
- `OTHER`: inny zaobserwowany problem bez opisów PII.

P0 to utrata grywalności/Save lub powtarzalny softlock; P1 to poważna wada bez całkowitego zablokowania; P2 to drobna wada wizualna/komunikacyjna. Obserwator określa priorytet; aplikacja nie może samodzielnie go udowodnić.

## Realne uruchomienie i testy

Z katalogu z odzyskanymi prawdziwymi źródłami W27, na Node.js >=20:

```bash
node Research/B01-13/P17W27/observer.mjs --output /ABSOLUTE/PRIVATE/safe-observer.html
node --test Research/B01-13/P17W27/test_observer.mjs
python Research/B01-13/P17W27/browser_test.py /ABSOLUTE/PRIVATE/safe-observer.html
```

Generator `O_EXCL` tworzy **fizyczny HTML**, odmawia nadpisania i zapisu w istniejącym checkout Git. Używa tylko ścieżki zadanej przez operatora; brak automatycznego uruchamiania Windows, Unity czy testów ludzi. Python browser QA korzysta z prawdziwego Chromium (`CHROME_BIN` na maszynie z lokalnym Chromium albo Playwright browser installation) i wykonuje automatyczne **symulowane wpisy operatora**: NIE świadczy o uczestnikach. CI: Windows2025/Ubuntu24.04 × Node20/22, oddzielny Chromium. Manifest fizycznych Git blobów, bajtów i SHA256; oddzielne artefakty HTML/JSON dowodów CI. `actual_human_sessions=0` w danych wydania oznacza faktyczny brak udokumentowanego uczestnictwa, a nie zapowiedź.

## Granice, zależności i odbiór

- **Stosowana gałąź:** W27 własna, add-only gałąź wychodząca z oryginalnego własnego W25 PR #786 @ `b79b6e817b7d905d243fc1a02cee7cf90063c151`. Żadne pliki #786 ani wcześniejsze nie są edytowane. Do integracji w linii właściwego produktu wymagana jest decyzja o osobnym G0 #769/#786 i zgodne włączenie narzędzia operatora albo dostarczenie go poza Unity. Nie wchodzi w aktywny worktree Codexa `feature/pass17-reload-fix`.
- **Nie dotyczy:** sklejania #787/#797/#792/#780/#750, Save writer, Unity GUID/asmdef, prywatnego frozen `Rig[]`, faktycznych fizycznych manipulatorów, natywnego PlayMode. W27 źródłowe testy browsera **nie certyfikują Unity G1**. Nie udostępniać jako paczki integracji mechanik, tylko materiały do **przyszłego human UX QA** po realnym buildzie.
- **G0** B01-40 (source manifest, niekolizyjność), **niezależny metodologiczny QA** B01-60, **design** B01-10/20/22 (akceptacja progów i warunków), **decyzja/publikacja** B01-00. **Codex inbox #79:** nie wysyłać ponownie bez G0 i owner decyzji, bo to nowy zakres, a stary #786 jest już w produkcji.
- **Bezpieczny rollback:** zamknąć niezmergowany Draft; operator może bezpiecznie skasować tylko własny lokalny HTML. Nigdy nie usuwaj cudzych katalogów, artefaktów, zapisów rozgrywki ani osobno pobranych wyników uczestnika bez decyzji prowadzącego.

**W chwili wydania tej paczki: `ACTUAL_HUMAN_SESSIONS=0`, `NATIVE_UNITY_G1=NOT_RUN`, `CODEX_ACK=NONE`, `INTEGRATED=NO`, `RELEASED=NO`.**
