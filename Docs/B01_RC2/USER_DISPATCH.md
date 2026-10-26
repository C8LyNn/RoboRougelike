# GLOBAL HQ → CODEX — B01 UNIFIED GAME / RC2

**P0 — JEDNA GRYWALNA GRA, MAKSYMALNA BEZPIECZNA INTEGRACJA**

Repozytorium: https://github.com/C8LyNn/RoboRougelike

## GŁÓWNY CEL

Zakończ etap produkowania osobnych demonstracji i buildów dla każdej mechaniki.

Chcę **JEDNĄ wspólną wersję RoboRougelike**, rozwijaną iteracyjnie, z jednym menu, garażem, pojazdami, walką, aktywnościami, nagrodami i progresją.

Nie twórz kolejnych oddzielnych gier BASE, MAPGEN, CAMERA czy BUILD.

Wszystkie zaakceptowane funkcjonalności mają działać w tej samej aplikacji.

## 1. FUNDAMENT

Rozpocznij od istniejącej gry w PR #248:

https://github.com/C8LyNn/RoboRougelike/pull/248

Weryfikuj rzeczywistą historię Git. Końcowy snapshot działającego kodu gry z RC1 to:

`86eb7bb77e6bfc7787f781ea1ccda547b8a4f643`

RC1 zawiera już:
- Fizykę R3/R6.
- Garaż, części i walkę.
- Obranie i wykonanie aktywności.
- Obronę reaktora.
- MAPGEN R3.4.1.
- Adapter świata MAPGENWORLD v1.0.0.

Nie importuj tych mechanik ponownie, nie duplikuj ich systemów i nie wracaj do starszego RC1 BASE.

Stwórz jedną nową gałąź `integration/b01-unified-rc2` w osobnym worktree.

## 2. NOWE PACZKI DO SPRAWDZENIA

Sprawdź aktualne PR-y i dokładne SHAs, szczególnie:

- MAPGENWORLD v1.0.1 — PR #243.
- BUILD R1.3 — PR #256.
- CAMERA R1.2/R1.2.1 — PR #247/#251, QA #249.
- RAC R3.1 — PR #216.
- LEGENDARY-UNITY — PR #242.
- ECONREBUILD — PR #252.
- REWARDKEEP3 — PR #257.
- Narzędzia PACKAGE-AUTO i RC1 RECEIVER — PR #238/#254.

Dla każdej paczki wykonaj kontrolę stanu źródła, testów, zależności, właściciela i konfliktów z aktualną grą.

Nie traktuj zielonego CI na sztucznych typach Unity jako natywnego PASS.

## 3. AUTORYZACJA EKSPERYMENTALNA

Global HQ zezwala na próbne włączanie nowych paczek do **jednej izolowanej gałęzi RC2**, nawet jeśli ich formalny G2 pozostaje HOLD.

Ta zgoda dotyczy wyłącznie testowania integracyjnego w osobnym worktree.

Przed aktywowaniem każdej mechaniki:
1. Zweryfikuj jej pełne źródła i zależności.
2. Przeprowadź natywną kompilację i odpowiednie testy Unity.
3. Sprawdź kolizje GUID, `.meta`, asmdef i autorytatywnych systemów R3/R6.
4. Podłącz funkcję do istniejącego menu, garażu lub właściwego systemu.
5. Przetestuj ją w rzeczywistym gameplayu.
6. Jeśli powoduje krytyczne błędy, wycofaj wyłącznie ten krok integracji.

Nie naprawiaj szeroko źródeł producenta. Przekazuj takie błędy właściwemu agentowi B01.

Nie podmieniaj autorytatywnych magazynów, ekonomii, fizyki, uszkodzeń ani mechaniki pojazdów.

## 4. PRIORYTETY INTEGRACJI

**Pierwszy:** zachowanie i dopracowanie obecnego grywalnego RC1 z MAPGEN.

**Drugi:** nowszy MAPGENWORLD, jeśli natywne testy potwierdzą poprawne działanie.

**Trzeci:** BUILD R1.3, jeśli cztery znane regresje W9 zostały rzeczywiście usunięte i fizyczna walidacja montażu działa.

**Czwarty:** RAC R3.1, jeżeli kompilacja, checkpointy, timer i realne przejazdy przejdą testy.

**Piąty:** CAMERA, dopiero po wyjaśnieniu problemu celowania. Nie pogarszaj poprawnego działania istniejącej kamery.

**Szósty:** LEGENDARY, ECONREBUILD i REWARDKEEP3 po potwierdzeniu spójnych transakcji oraz autorytatywnego R3. Nie twórz duplikatów systemu wyposażenia ani magazynu.

Nie musisz zmieścić wszystkich funkcji w jednym cyklu za cenę niestabilności. Zachowaj kolejność i udokumentuj blokery.

## 5. JEDEN ZINTEGROWANY GAMEPLAY LOOP

Docelowy przepływ:

MENU → WYBÓR ROBOTA → GARAŻ → WYBÓR AKTYWNOŚCI → ROZGRYWKA → WYNIK → NAGRODY/NAPRAWY → GARAŻ → KOLEJNA AKTYWNOŚĆ.

Wszystkie dodane aktywności mają korzystać z tych samych:
- Robotów i fizyki.
- Autorytatywnego stanu części.
- Systemu zdrowia i uszkodzeń.
- Ekwipunku.
- Waluty i progresji.
- Mechanizmu zapisu i odczytu, w zakresie faktycznie obsługiwanym.

Nie twórz osobnego garażu, ekonomii ani równoległej progresji dla nowych trybów.

## 6. TESTY

Po każdym etapie integracji uruchom odpowiednie testy, a na końcu:

- Pełną kompilację Unity 6000.3.14f1 URP.
- EditMode i PlayMode.
- Windows x64 build.
- Test uruchomienia rzeczywistej gry.
- Test menu, garażu, sterowania i walki.
- Test obrony reaktora.
- Test wygenerowanej mapy.
- Test nagród, uszkodzeń i powrotu do garażu.
- Kontrolę różowych materiałów i błędów konsoli.
- Test zwykłego sterowania, nie wyłącznie wywołań API, w dostępnym zakresie.

Zbadaj również znany problem `C0000005` podczas zamykania aplikacji. Nie deklaruj jego naprawy bez rzeczywistej reprodukcji i pozytywnego retestu.

Jeśli dana paczka nie przejdzie testów, utrzymaj jej integrację poza końcowym buildem i zapisz dokładną przyczynę.

## 7. WYMAGANY REZULTAT

**TYLKO JEDNA DOCELOWA WERSJA WINDOWS x64:**

`RoboRougelike_B01_UNIFIED_RC2_Windows_x64.zip`

Nie przygotowuj osobnych końcowych buildów BASE, MAPGEN, BUILD czy CAMERA.

Chcę jedną aplikację, w której działają wszystkie mechaniki faktycznie dopuszczone do tego RC.

Dostarcz:
- Prawdziwy EXE i kompletny ZIP.
- SHA256.
- Jedną instrukcję uruchomienia.
- Listę aktywnych funkcjonalności.
- Listę pominiętych i zablokowanych paczek.
- Raport testów całej gry.
- Oddzielny Draft PR zawierający wspólny kod integracyjny.
- Punkt rollback do poprzedniej działającej wersji.

Nie usuwaj historycznych buildów i raportów. Nie traktuj ich jednak jako osobnych docelowych wersji gry.

## 8. OGRANICZENIA

- Nie wykonuj merge do main/develop.
- Nie zmieniaj aktywnego worktree.
- Nie nadpisuj niezacommitowanych zmian.
- Nie wykorzystuj błędnych paczek jako działających funkcji.
- Nie deklaruj G2 bez decyzji B01-00.
- Nie poświęcaj stabilnej wersji gry dla pozornego ukończenia wszystkich integracji.

**Najważniejszy rezultat: jeden realnie grywalny RoboRougelike z maksymalną liczbą poprawnie działających mechanik.**

Pracuj iteracyjnie, zapisuj sprawdzone checkpointy i kontynuuj do przygotowania jednego wspólnego buildu. Po zakończeniu opublikuj dokładny raport oraz listę pozostałych blokerów.