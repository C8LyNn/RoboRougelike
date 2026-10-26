# RoboRougelike — pełna integracja Codex / Issue #270

**READY — do uruchomienia i odbioru użytkownika. Wszystkie obowiązkowe dowody automatyczne są kompletne; nie jest to zatwierdzenie G1/G2 ani produkcyjne wydanie. Ręczna kontrola GUI/Alt+F4 pozostaje osobno oznaczona.**

Data lokalna: 2026-10-27, Europe/Warsaw. Unity **6000.3.14f1**, dotychczasowe URP i pakiety; bez migracji silnika.

## Dokładne źródła i zakres

- Kod dostarczanego EXE: `88b8eed60345c49481648833e1fde6dc1a400258`.
- Nienaruszona baza/rollback RC2 #258: `056911a8284f7d61f2a8d13ed2669e453c3d9915`.
- Gałąź: `integration/codex-full-merge-270`; Draft PR [#272](https://github.com/C8LyNn/RoboRougelike/pull/272), baza `integration/b01-unified-rc2`.
- Kompletna [macierz paczek](CODEX_PACKAGE_MERGE_MATRIX.md), [224 historyczne PR-y](Intake/all-pr-index.json), [ich rozstrzygnięcia](Intake/pr-dispositions.json) i [zamrożone źródła](Intake/source-snapshots.json).
- Zgodnie z zakresem właściciela w [Issue #270](https://github.com/C8LyNn/RoboRougelike/issues/270) odrębne szkice producenta B01, które nie były wcześniej działającą funkcją Codexa, nie są automatycznie obowiązkową zawartością tej integracji.

Do wspólnej gry podłączono pełne aktualne źródła: BUILD R1.4 #260 `95707c68c8071a949b5d9fec7ff57926e92f8198`, CAMERA #269 `ba90bca4a33ee37bdf7923b7d4fad14dc75adc69`, MAPGENWORLD v1.1 #261 `5f6df2af0a1365779347c3dc9b6b3a81708bb2e6`, RAC #13/#216 `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e` oraz MAP-R6 #3 `79b9605e07864b60fb38892c9e38f1cbb4072218`. Szczegółowe manifesty są w `Intake/*-integrity.json`; zachowano istniejący `Assets/B01Packages.meta`.

Zachowane funkcje: sześć etapów wyprawy W2–W5, R6/PHY, fizyczne moduły i bonusy W3/W4, siatka 1×2×2, obrót XYZ i rzeczywiste mocowania, KEEP3/łupy/naprawy, WPN-001–006, DEF-001/002, MOV-001–009 i MOV-011, COM/SEN, osłony/zagrożenia/VFX oraz OBJ-001. Nie importowano osobno starszych duplikatów #17/#18 ani wcześniejszych rewizji tych samych mechanik.

Wszystkie aktywności korzystają z istniejących R1/R3/R4/R6: jeden stan części, HP, obrażeń, ekwipunku, ekonomii, wyników i Rigidbody. Demo-sześcian oraz prototypowy driver MAP-R6 nie zastępują pojazdu gracza.

## Rzeczywiste testy Unity

| Etap / dokładny snapshot | Wynik | Dowód |
| --- | --- | --- |
| Pełna kompilacja i Windows build, `b97b434c8682bc26dc7a681ddd1d247079c59910` | PASS, oba exit 0 | [pełny przebieg](Evidence/FM270-FinalFullRelease-20261026T214843Z/) |
| Pełny EditMode, ten sam snapshot | **FAIL 1193/1197**, exit 2; cztery błędy zachowane | [XML](Evidence/FM270-FinalFullRelease-20261026T214843Z/EditMode.xml) |
| Pełny PlayMode, ten sam snapshot | **PASS 474/474**, exit 0 | [XML](Evidence/FM270-FinalFullRelease-20261026T214843Z/PlayMode.xml) |
| Naprawy czterech konkretnych niepowodzeń i regresja BUILD/CAMERA/R1/MOV/DEF, `0ac75cc41d0369eef8630b61695d0f49c83c3a6b` | **PASS 113/113 EditMode + 21/21 PlayMode**, kompilacja/build/testy exit 0 | [przebieg](Evidence/FM270-FinalDelta-20261026T224338Z/) |
| Złącza/preflight dróg, `8b91c5499615d33fc15c1aa8834071896b5ee48b` | **PASS 29/29 EditMode**, kompilacja/build/testy exit 0 | [przebieg](Evidence/FM270-FinalRoadClosure-20261026T230141Z/) |
| Końcowa kamera i integracja, dostarczany `88b8eed60345c49481648833e1fde6dc1a400258` | **PASS 52/52 EditMode + 15/15 PlayMode**, kompilacja/build/testy exit 0 | [przebieg](Evidence/FM270-FinalCameraClosure-20261026T232326Z/) |

Pierwotnego wyniku 1193/1197 i jego kodu 2 **nie przepisano na PASS**. Wszystkie cztery pełne nazwy nieudanych testów mają późniejszy rzeczywisty wynik PASS w grupie 113/113. Po tej grupie zmieniły się tylko host dróg, adapter kamery i ich celowane regresje; ostatnie 52/52 obejmuje wszystkie testy FullMerge i oryginalne EditMode kamery, a 15/15 jej PlayMode. Nie powtarzano niezmienionych testów .NET z GitHub Actions. [Kontrola wydania](delivery-verification.json) sprawdza zgodność plików, źródeł i zamknięcie każdego niepowodzenia po dokładnej nazwie.

Logi, XML, rzeczywiste kody procesów i hashe plików są w [Evidence](Evidence/README.md). Czas trwania należy czytać z `*-process.json`; zegar NUnit może resetować się przy przeładowaniu domeny. Zapisane są też wcześniejsze niepowodzenia, nieukończone próby i celowe przerwania.

## Windows i ścieżka gracza

**Metoda:** wybór aktywności, nagród i rozstrzygnięcia walk jest sterowany API. Przejazdy używają prawdziwego R3/R6, kontaktów kół, sił, colliderów źródłowych i obserwacji checkpointów; bez teleportacji ani zapisu prędkości w próbach przejazdu. Przeciwnicy naprawdę poruszają się i strzelają; kontrolowane zakończenie walk niszczy ich rzeczywiste rdzenie po obserwacji aktywnego AI. To nie jest deklaracja samodzielnego ukończenia przez człowieka.

Na wcześniejszym `8b91c54` oba pojazdy rzeczywiście ukończyły ten sam sześciostopniowy cykl: RAC → drogowa próba czasowa → ewakuacja przy zamkniętym moście → przetrwanie → obrona reaktora → zwykły finał. Heavy: 501,91 m / 12 kontaktów; Light: 503,83 m / 4 kontakty; oba exit 0, sześć wyników/nagród, garaż/naprawy i restart. Po ostatniej poprawce kamery oba cykle ponownie wykonano z sukcesem na dokładnym dostarczanym kodzie, zgodnie z tabelą poniżej.

### Końcowe osiem prób kamery — ten sam EXE `88b8eed`

| Konstrukcja | 1280×720 | 1920×1080 |
| --- | --- | --- |
| Light | PASS, exit 0 | PASS, exit 0 |
| Heavy | PASS, exit 0 | PASS, exit 0 |
| Tall, pięć rzeczywistych dodatkowych bloków w górę | PASS, exit 0 | PASS, exit 0 |
| Wide, dwanaście rzeczywistych dodatkowych bloków na boki | PASS, exit 0 | PASS, exit 0 |

Każda próba: obrót 360°, zakres zoomu i pochylenia, faktyczne zdarzenia W/LPM, rzeczywiste pociski, odłączenie części, wspierane materiały i zapis obrazu. We wszystkich ośmiu: **0 przecięć promienia z własną konstrukcją, 0 kolizji kamery ze światem, 0 klatek uciętej bryły, 0 naruszeń deklarowanej prędkości ruchu**. Błąd ustalonego celowania maksymalnie 0,0198° przy asercji ≤0,05°. Pełne chwilowe błędy akwizycji celu pozostają w CSV; nie nazwano ich bezbłędnym śledzeniem każdej klatki.

| Próba na dostarczanym kodzie | Wynik rzeczywisty | Dowód |
| --- | --- | --- |
| Loop / Heavy / 1920 | PASS, exit 0; 6/6 etapów, 501.86 m, 12 kontaktów | [logi, wynik i obrazy](Evidence/FM270-Windows-Loop-Heavy-1920-20261026T233058Z/) |
| Loop / Light / 1280 | PASS, exit 0; 6/6 etapów, 503.76 m, 4 kontaktów | [logi, wynik i obrazy](Evidence/FM270-Windows-Loop-Light-1280-20261026T233427Z/) |
| Input / Standard / 1280 | PASS, exit 0 | [logi, wynik i obrazy](Evidence/FM270-Windows-Input-Standard-1280-20261026T233854Z/) |
| Input / Standard / 1920 | PASS, exit 0 | [logi, wynik i obrazy](Evidence/FM270-Windows-Input-Standard-1920-20261026T233926Z/) |
| Mapgen / Standard / 1920 | PASS, exit 0 | [logi, wynik i obrazy](Evidence/FM270-Windows-Mapgen-Standard-1920-20261026T233958Z/) |
| Reactor / Standard / 1920 | PASS, exit 0 | [logi, wynik i obrazy](Evidence/FM270-Windows-Reactor-Standard-1920-20261026T234029Z/) |
| Boss / Heavy / 1920 | PASS, exit 0 | [logi, wynik i obrazy](Evidence/FM270-Windows-Boss-Heavy-1920-20261026T234100Z/) |

Wszystkie **15/15** końcowych automatycznych uruchomień Windows zakończyły się kodem 0; oba pełne cykle mają po sześć kanonicznych wyników i restart. Ostatnie pięć prób uruchamiało **EXE bezpośrednio z wypakowanego ZIP-a**, po weryfikacji zgodności DLL. Mapgen obejmuje rzeczywiste generowanie seed 7/31, materiały, fizyczne obrażenia gracza i wroga, nagrody, naprawę, montaż odzyskanej części oraz cleanup po porażce/restart. Reactor obejmuje trzy fale, rzeczywiste obrażenia obu celów, zwycięstwo, porażkę i ponowny start. Input obejmuje Enter/garaż, W/D, LPM, PPM, Esc/pauzę i wznowienie.

| Wydajność finalnego pełnego cyklu | Średnia klatka | Najwolniejsza obserwowana klatka | Obserwacja AI |
| --- | --- | --- | --- |
| Heavy, limit60FPS | 16.670 ms | 22.591 ms | max 2 jednocześnie, 11 wrogich pocisków, ruch 6.16 m |
| Light, limit60FPS | 16.669 ms | 17.469 ms | max 2 jednocześnie, 11 wrogich pocisków, ruch 6.15 m |

Pomiary dotyczą żywych faz aktywności, rzeczywistych części i renderowania na RTX 5080, bez równoległego drugiego Playera. Nie obejmują pełnego kosztu przejść ani długotrwałego benchmarku wielkich armii. Pole `errors` wszystkich końcowych prób jest puste; komunikaty HID po wyłączeniu Input System zachowano w logach.

## Wykryte i naprawione problemy

1. Powtórzone referencje do TestRunner w starszych asmdef MAP-R6 — usunięto tylko duplikaty, zachowując wszystkie testy.
2. RAC obserwował Rigidbody zegarem renderowania — fizyczny runner jest obserwowany w FixedUpdate, z zachowaniem progów prędkości, promieni i czasu. Osobne demo otrzymało jawny materiał kontaktu; nie zmieniono sił R6.
3. R3 API Buy/Mount mogły omijać fizyczną walidację; błędne wsparcie przez koło/lufę oraz utrata autoryzowanych socketów po rebuild — walidacja poprzedza wydanie zasobów, używa pełnej rzeczywistej geometrii i poprawnej ramy obrotu.
4. Odbudowa mogła pomijać obcą bryłę albo tracić pochodzenie fabrycznego koła — oryginalny runtime ID/poza/maska są zachowane, a obca geometria z podrobionym ID nadal blokuje naprawę bez opłaty.
5. Krótki CapsuleCollider koła stawał się sferą i przenikał oś/pancerz — kolizję zapewnia istniejąca, niezmieniona wizualnie siatka cylindra jako convex MeshCollider. Naprawiono odczyt zniszczonego collidera z cache.
6. BUILD odrzucał zwykłą statyczną siatkę podłoża jako niezweryfikowaną — kandydat nadal musi być prymitywem/convex, statyczny mesh sąsiada podlega prawdziwemu ComputePenetration, a mesh na Rigidbody pozostaje odrzucony. Testy dodatnie i ujemne potwierdzają brak obejścia kolizji.
7. Trzy starsze konstrukcje testowe fizycznie przenikały sąsiednie części — poprawiono pozycje/wybór prawdziwego mocowania, zachowując części, ceny, zasoby i asercje. Czwarty błąd był rzeczywistą niezgodnością z podłożem z pkt 6.
8. Wygrany wyścig nie kończył stanu Combat; puste aktywności mogły być błędnie uznane za wygraną — jedna kanoniczna ścieżka wyniku/nagrody, utrata gracza ma pierwszeństwo, timeout/pauza/cleanup działają.
9. Po zmianie mapy stare bounds zawyżały obrys pojazdu; drogowe AI używało granic starej areny — obrys wynika z rzeczywistych lokalnych kształtów, AI z aktualnej areny i naprawdę porusza się poza dawnym limitem.
10. Płaskie podesty przecinały pochyłą drogę, blokując Heavy — host buduje istniejące źródłowe złącza MapR6JunctionPlanner/MapR5RoadMesh. Cała trasa i aktywne złącza są sprawdzane przed zmianą progresji; wymagany obrys jest zaokrąglany na zewnątrz. Nie zmieniono solvera, sił, źródłowych limitów ani timeoutu 180 s.
11. CAMERA przycinała legalny Heavy/Wide na limicie demo — automatyczne dopasowanie uwzględnia rzeczywistą bryłę, ręczny zakres zoomu pozostaje. Własne pociski i wraki nie przejmują własnego celu ani kolizji kamery.
12. Ruch własnej lufy zmieniał kotwicę kamery i powodował oscylację celu — niezależny RED wykazał 0,05414 m ruchu kotwicy przy nieruchomym podwoziu. Adapter używa pełnej, stałej obwiedni obrotu działa oraz aktualnych kształtów reszty konstrukcji. Jeden właściciel pozy, bez zmiany szybkości obrotu działa i bez osłabienia progu celowania.

## Niezintegrowane pozycje i ograniczenia

| Pozycja | Stan i przyczyna |
| --- | --- |
| Prototypowe modele ModelFactory i pilot #55/W01/H01 | **USER_EXCLUDED** — wyraźne polecenie właściciela: ignorować do opracowania metodologii modeli. Nie pobierano modeli/importera ani nie włączano bindera. |
| MOV-010 #2 | **SOURCE_UNAVAILABLE_HISTORICAL** — oryginalne zadanie WAIT/PACKAGE_UNAVAILABLE, bez dostarczonej implementacji runtime; nie odtwarzano z opisu. |
| LEGENDARY-UNITY #242, ECONREBUILD #252/guard #263 | Odrębne propozycje producenta, bez wcześniejszego działającego hosta Codexa; poza obowiązkowym zakresem właściciela #270. Nie zastępują kanonicznego, działającego systemu napraw/łupów. |
| Warnings #266, RUN-SAFETY, sizing/zoom/analytics i kontrakty producenta | Rozliczone w macierzy; nie były wcześniejszą grywalną funkcją Codexa. Nie tworzono drugiej polityki CanDeploy ani nie przedstawiono smoke testu źródła jako gameplayu. |
| Samodzielna rozgrywka człowieka i klikanie GUI | **NOT_RUN** do nowego potwierdzenia. Dozwolony helper komputerowy kończył się przed podłączeniem błędem `failed to write kernel assets` / OS error 3; bez obejścia UIA/SendInput. Zrzuty są rzeczywistym renderowaniem Playera, wejście W/LPM/PPM/Esc jest osobno wstrzykiwane przez Input System. |
| Historyczny C0000005 przy automatyzacji zamykania | **Nie uznano za naprawiony.** Sukces kontrolowanych Application.Quit nie dowodzi usunięcia błędu UIA/Unity/środowiska. Celowe przerwania mają osobne receipts; nie są uznane za crash C0000005. Występują też komunikaty HID po Shutdown Input System; nie ukryto ich. |
| Wydajność | Próby z limitem 60 FPS na RTX 5080; wartości zmierzone, nie deklaracja minimalnego sprzętu ani szeroki benchmark dużych armii. |

Nie ma znanej brakującej wcześniej działającej mechaniki Codexa poza jawnym wyłączeniem modeli; MOV-010 nie dostarczyło źródeł. Nie przyznano G1/G2, nie scalono do main/develop, nie opublikowano produkcyjnego wydania.

## Uruchomienie i rollback

Wypakuj cały ZIP, uruchom `RoboRougelike.exe`, wybierz pojazd i **Przygotuj pojazd**. W garażu są zwykła trasa, generowana arena, reaktor, wyścig oraz przycisk trybu dróg z przełącznikiem ›. Montaż/naprawy/ekwipunek pozostają w tym samym garażu. [START_HERE.txt](START_HERE.txt) opisuje pełne sterowanie i jest dołączony do archiwum.

Rollback: zamknąć Player, uruchomić nienaruszony RC2 #258 / jego wcześniejszy ZIP. Nie nadpisano starego projektu ani obu RC1. Źródłowo wrócić do `056911a8284f7d61f2a8d13ed2669e453c3d9915` w osobnym checkout, bez resetowania cudzych zmian.

## Artefakt

Kod: `88b8eed60345c49481648833e1fde6dc1a400258`. ZIP: **38,600,657 B**. SHA-256: `eb1057ec877cfaa880a9973880029f51f0848671bc306d7fc6ae652d895867bf`.

Lokalizacja: `C:\Users\kubap\Desktop\Prototypy\Releases\ROBOROUGELIKE_FULL_INTEGRATION_270_20261027\RoboRougelike_FULL_INTEGRATION_270_Windows_x64.zip`.

CRC oraz SHA-256 każdego wypakowanego pliku: PASS. Faktyczne uruchomienie wypakowanego EXE: **PASS**, pięć niezależnych zakończonych prób, każdy exit 0. [Pełny receipt i manifest plików](release-receipt.json).

## Decyzja i sposób publikacji

Automatyczna [kontrola dostarczenia](delivery-verification.json) zakończyła się kodem 0: dokładne źródła/build, kompletność i GUID-y, pełny wcześniejszy przebieg, zamknięcie każdego z czterech niepowodzeń po pełnej nazwie, końcowe delty, osiem prób kamery, dwa pełne cykle, pięć uruchomień z ZIP-a oraz hashe wypakowanych plików. **READY do gry i niezależnego odbioru**, z jawnymi ograniczeniami ręcznego QA, historycznej diagnostyki zamykania i zakresu wydajności powyżej. Nie ma zatwierdzenia G1/G2.

ZIP jest dostępny lokalnie we wskazanym katalogu użytkownika. Nie tworzono produkcyjnego GitHub Release; kod, raport i dowody są w Draft PR #272, a odnośnik i wynik trafiają do Issue #270.
