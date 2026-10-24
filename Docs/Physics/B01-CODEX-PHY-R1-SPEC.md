# B01-CODEX-PHY-R1 — Grounded Drive & Core Recovery

**Projekt:** C8LyNn/RoboRougelike  
**Silnik:** Unity 6000.3.14f1, URP  
**Priorytet:** P0 — stabilność fizyki i grywalność  
**Tryb:** Iterative Development / Test-First / Isolated Branch  
**Decyzja:** PROPOSAL — wykonanie dopiero po zatwierdzeniu przez Queen.

## 1. Stan wejściowy i weryfikacja

Repozytorium:
https://github.com/C8LyNn/RoboRougelike

Ostatni potwierdzony integracyjny kandydat to W8 RC1:

- Draft PR #34, HEAD `865932fd78ef936493bdc51ea9ca612fe9ec31eb`.
- Pełny testowany gameplay/build commit `40173ee60a481c07ccc005fe94c517c524aef0fa`.
- `main` / `develop`: `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
- Raport W8: 976/976 EditMode, 398/398 PlayMode, 47/47 natywne API, 6 zwycięskich automatyzowanych starć.
- Znany problem: zamknięcie Alt+F4 w środowisku z podłączoną automatyzacją bywa zakończone `C0000005`.
- Znany problem z playtestu: pojazd może zachowywać napęd mimo przewrócenia na dach.
- CAMERA-001, uzbrojenie i ekonomia są prowadzone jako osobne pakiety przez B01.

**Pierwsza czynność obowiązkowa:** sprawdź aktualną gałąź, najnowsze commity, nieopublikowane zmiany, dostępne raporty oraz to, czy B01-PHY-002 lub fizyczny kontakt kół nie zostały już zaimplementowane później. Nie nadpisuj nowszych prac.

Wybierz rzeczywisty aktualny punkt integracyjny dopiero po zweryfikowaniu historii Git. Nie bazuj na `main` tylko dlatego, że jest gałęzią domyślną.

## 2. Cel iteracji

Wyeliminować możliwość generowania napędu kołowego bez rzeczywistego kontaktu z podłożem i przygotować bezpieczną zdolność awaryjnego samopoziomowania robota przez Core.

Iteracja ma poprawić fizykę bez przepisywania obecnego modułowego systemu pojazdów.

Nie implementuj teraz nowych trybów gry, proceduralnych map, ekonomii ani nowych modeli 3D.

## 3. P0 — PHY-001: Wheel Ground Contact

### Weryfikacja istniejącego kodu

Przeanalizuj:

- `Assets/R6/R6VehicleDrive.cs`
- `Assets/MOVSeriesIntegration/MOVSeriesVehicleAdapter.cs`
- `Assets/MOVSeriesIntegration/MOVSeriesDrives.cs`
- `Assets/MOV009Integration/MOV009VehicleAdapter.cs`
- R3 vehicle assembly i stan zamontowanych modułów
- Istniejące testy R6, MOV009, MOV011

Sprawdź, skąd rzeczywiście pochodzi siła napędu w trybie Wheels, jak liczone są sprawne koła i czy napęd rozróżnia koło zamontowane od koła mającego kontakt z terenem.

### Implementacja

1. Dodaj wiarygodną informację o kontakcie każdego czynnego koła z rzeczywistą powierzchnią.
2. Wykorzystaj punkty kontaktowe lub sondy fizyczne uwzględniające skalę, pozycję i obrót koła.
3. Wyklucz własne collidery, triggery, odczepione moduły i fałszywe trafienia sond.
4. Siłę trakcyjną obliczaj z rzeczywistych kontaktów, stanu części i możliwości napędu.
5. Ogranicz skręt i przyspieszanie, jeżeli kontaktów jest za mało.
6. Gdy żadne sprawne koło nie dotyka podłoża, silnik kół nie może wytwarzać nowego poziomego napędu.
7. Nie kasuj pędu w powietrzu; grawitacja, bezwładność oraz odrębne siły boostera, skoku i Hover muszą pozostać prawidłowe.
8. Zachowaj fizyczne skutki asymetrii kół, rozkładu masy, EMP i odpadania części.

Nie dodawaj ukrytego globalnego AddForce tylko po to, aby pojazd nadal jeździł bez kontaktu.

### Kryteria akceptacji

- Cztery sprawne koła na płaskim terenie: pojazd porusza się i skręca.
- Koła w powietrzu: brak dodatkowej trakcji pochodzącej z Wheels.
- Pojazd na dachu: obracające się koła nie przesuwają go jak przy normalnym kontakcie.
- Zniszczenie koła aktualizuje trakcję.
- Utrata kół po jednej stronie wpływa na kierunkowość i możliwości poruszania się.
- Pojazd zatrzymany przy krawędzi, pochyłości lub przeszkodzie nie otrzymuje fikcyjnej pełnej przyczepności.
- Żaden nowy solver nie duplikuje istniejącego R6.
- Tryby Tracks, Omni i Hover zachowują własną autorytatywną fizykę.

## 4. P0 — B01-PHY-002: Emergency Core Self-Righting

### Cel

Core ma posiadać awaryjną zdolność pomagającą robotowi wrócić na koła, podobnie koncepcyjnie do mechaniki odzyskiwania pozycji w Robocraft.

Nie jest to zwykłe automatyczne utrzymywanie pionu podczas jazdy. Robot nadal może się przechylić, przewrócić lub zostać wyrzucony w powietrze.

### Najpierw sprawdź

- Istniejący Gyroscope / MOV004.
- Stan Core, utratę Core, EMP i status pojazdu.
- Kolizje kadłuba, położenie środka masy i orientację Rigidbody.
- Czy istnieje już mechanika odzyskiwania pozycji i jak jest aktywowana.

Nie twórz drugiego systemu, jeśli istniejący spełnia wymagania.

### Implementacja

- Wykrywaj rzeczywiste długotrwałe przewrócenie, a nie chwilowy przechył podczas jazdy.
- Samopoziomowanie ma być fizycznie ograniczone: kontrolowany impuls unoszący i moment obrotowy, z limitem siły, czasu i częstotliwości.
- Nie teleportuj robota ani nie ustawiaj co klatkę jego rotacji bez udziału fizyki.
- Wykorzystaj Core jako źródło zdolności i istniejący Rigidbody jako jedyny autorytet ruchu.
- Nie przywracaj brakujących kół, części, HP ani uzbrojenia.
- Nie uruchamiaj awaryjnego poziomowania w stanie porażki, przy zniszczonym Core ani podczas zwykłych manewrów.
- Zadbaj o stabilność przy nierównym terenie, ścianach, odpadnięciu ciężkich modułów i asymetrycznym kadłubie.
- Przygotuj dostrajalne parametry: próg kąta, czas potwierdzenia przewrócenia, maksymalny impuls, moment obrotowy i cooldown.
- Wstępnie przetestuj automatyczne uruchomienie po utrzymującym się przewróceniu, bez przejmowania istniejącego klawisza.

### Kryteria akceptacji

- Robot leżący na dachu po potwierdzonym przewróceniu otrzymuje ograniczoną próbę powrotu na koła.
- Nie następuje ciągłe podskakiwanie ani generowanie nieograniczonego pędu.
- Robot jadący po pochyłości nie jest niepotrzebnie prostowany.
- Zniszczony Core nie wykonuje samopoziomowania.
- Przy braku fizycznie możliwej drogi powrotu nie dochodzi do teleportacji przez podłoże lub ścianę.
- System nie umożliwia lotu przez powtarzane wywoływanie zdolności.
- Po powrocie do prawidłowej orientacji napęd pozostaje zależny od kontaktu kół.

## 5. P1 — Regresja bazowych mechanik

Nie przebudowuj istniejącego runu.

Zachowaj:

- Fizyczny montaż i demontaż modułów.
- HP części i ich odpadanie.
- Środek masy i bezwładność.
- MOV009 Active Suspension.
- MOV011 Independent Steering.
- Booster, Jump, Gyroscope, Tracks, Omni i Hover.
- Garaż i przebudowę pojazdu.
- Sześć spotkań Gauntlet, przechodzenie między arenami, nagrody, naprawy i utrzymanie uszkodzeń.
- Istniejące zachowanie walki i AI.

Nie dodawaj w tej iteracji nowych aktywności ani generatora proceduralnej geometrii.

Jeśli kontakt kół wprowadzi regresję innego trybu napędu, napraw ją w miejscu rzeczywistej przyczyny. Nie obchodź problemu globalnym wyłączeniem fizyki.

## 6. Wymagane testy

### Testy automatyczne Unity

Dodaj EditMode lub PlayMode obejmujące co najmniej:

- PHY-GROUND-01: cztery koła z kontaktem.
- PHY-GROUND-02: cztery koła bez kontaktu.
- PHY-GROUND-03: utrata jednego i dwóch kół.
- PHY-GROUND-04: pojazd przewrócony na dach.
- PHY-GROUND-05: asymetryczna konstrukcja i nierówne podłoże.
- PHY-GROUND-06: utrata kontaktu podczas skoku, bez wymazania pędu.
- PHY-CORE-01: prawidłowe wykrycie przewrócenia.
- PHY-CORE-02: brak fałszywej aktywacji podczas normalnej jazdy.
- PHY-CORE-03: awaryjny powrót przy sprawnym Core.
- PHY-CORE-04: uszkodzony/zniszczony Core.
- PHY-CORE-05: limit cooldown/impulsu i brak exploitów.
- PHY-REG-01: MOV009/MOV011 oraz pozostałe tryby napędu.
- PHY-REG-02: utrata części, garaż i przejście do kolejnego starcia.

Testy rzeczywistych sił i kontaktu powinny działać na Rigidbody i colliderach w scenie testowej, a nie wyłącznie na mockach obliczeń.

### Testy całego projektu

Użyj istniejącej infrastruktury:

`Tools/CI/Run-Unity.ps1 -Task EditMode`

`Tools/CI/Run-Unity.ps1 -Task PlayMode`

Po przejściu testów ukierunkowanych wykonaj pełny odpowiedni zestaw regresyjny i Windows x64 Build, jeżeli aktywowane Unity i Windows Build Support są dostępne.

Zapisz rzeczywiste liczniki, XML, logi, exit codes i SHA-256 artefaktów. Nie twierdź, że testy przeszły, jeżeli runner nie wystartował.

### Manualny smoke test Windows

Sprawdź mały i ciężki pojazd, jazdę po płaskim terenie, pochyłości, przewrócenie, powrót na koła, utratę części, działanie kamery, garaż i jedno przejście między starciami.

Wymagane dowody: zrzuty lub wideo, opis konstrukcji, warunki, wersja builda, SHA-256 oraz PASS/FAIL.

Błąd Alt+F4 `C0000005` zbadaj w zakresie reprodukcji i odróżnienia zwykłego zamknięcia od zamknięcia z automatyzacją. Nie deklaruj naprawy bez reprodukcji RED i GREEN.

## 7. Praca z Git i współpraca z B01

1. Przed zmianami ustal aktualny HEAD, aktywną gałąź, czystość worktree i inne prace Codexa.
2. Przeczytaj CODEX INBOX: https://github.com/C8LyNn/RoboRougelike/issues/79
3. Nie dubluj CAMERA-001, MAP-001, GDD ani assetów przypisanych B01.
4. Wybierz aktualną zweryfikowaną bazę integracyjną. Jako udokumentowany punkt odniesienia W8 użyj PR #34.
5. Utwórz osobną gałąź, np. `codex/b01-phy-grounding-core-r1`.
6. Wprowadzaj małe commity: reprodukcja/test RED → poprawka kontaktu → test GREEN → Core Recovery → regresja.
7. Zachowaj Unity `.meta`, GUID, sceny, prefaby, serializowane pola i istniejące kontrakty komponentów.
8. Nie aktualizuj Unity ani pakietów bez uzasadnionej konieczności.
9. Nie wykonuj merge do `main`, `develop` ani istniejących gałęzi integracyjnych.
10. Przygotuj Draft PR, ale bez automatycznego wdrożenia.

Jeżeli aktualny kod różni się istotnie od sprawdzonego W8, dokonaj ponownej oceny zakresu. Nie usuwaj nowszych funkcji na rzecz starszej implementacji.

## 8. Wymagany raport końcowy

Zwróć raport `B01-CODEX-PHY-R1-REPORT` zawierający:

- `BASE_BRANCH` i `BASE_SHA`
- `WORK_BRANCH` i `FINAL_SHA`
- Link do Draft PR
- Zweryfikowany rzeczywisty stan PHY-001 i B01-PHY-002 przed zmianami
- Listę plików zmienionych i dodanych
- RED reproductions i GREEN confirmations
- Liczby EditMode / PlayMode PASS, FAIL, SKIP
- Logi i exit codes
- Wynik builda Windows oraz SHA-256 ZIP/EXE, jeśli rzeczywiście powstały
- Wynik manualnego smoke testu
- Regresje lub znane ograniczenia
- Porównanie zachowania fizyki przed/po
- Status każdego zadania: DONE / PARTIAL / BLOCKED / NOT_RUN
- Niezależnie weryfikowalne dowody do odbioru przez Queen i B01-60
- Zalecany następny krok

Nie uznawaj wygenerowania kodu za dowód działania mechaniki.

**KONIEC ITERACJI:** zatrzymaj się na Draft PR i raporcie. Nie uruchamiaj kolejnej iteracji automatycznie i nie wdrażaj zmian bez zatwierdzenia.