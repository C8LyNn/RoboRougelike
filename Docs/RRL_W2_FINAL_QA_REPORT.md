# RRL-W2 — raport integracji i QA

**BLOCKED — zdalny CI Unity wymaga aktywacji. Lokalne mechaniki, nowy garaż i zwykłe ręczne zamknięcie W2: PASS. Zamykanie po automatyzacji UI nadal ma osobny wynik FAIL.** Ten dokument nie stanowi zgody na scalenie.

- Repozytorium: `C8LyNn/RoboRougelike`, PR [#1](https://github.com/C8LyNn/RoboRougelike/pull/1).
- Gałąź: `feature/unified-rewards-debris-density` → `develop`. Bez zmian `main`, bez merge i force push.
- Bazowy SHA PR przed naprawami: `07ba9caae03f6e5f4e478ae5e8882cdbb1eb6f29`.
- `develop`/`main` na początku: `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
- Końcowy zweryfikowany SHA implementacji: [`c16c6aeb81ec20e44cf5b3ef171a1ad9bf14e753`](https://github.com/C8LyNn/RoboRougelike/commit/c16c6aeb81ec20e44cf5b3ef171a1ad9bf14e753). Kolejne commity zawierają wyłącznie raport i dowody; dokładny końcowy head publikacji podano w opisie PR, aby nie tworzyć samoodwołania SHA we własnym commicie.
- Unity: **6000.3.14f1** (`d68c3f99a318`), lokalny Windows 11.
- Implementacja i testy w izolowanym worktree istniejącej gałęzi PR.

## Poprawione problemy

1. Wspólna transakcja nagród waliduje bonusy i części przed mutacją; 0–3 łącznie, bez powtórzeń i drugiego obowiązkowego ekranu bonusów. Zachowane są źródłowe ID/HP, demontaż niewybranych części i finał `Won`.
2. Uszkodzenie bliskie 100% przestało dawać darmową naprawę. Naprawy awaryjne odbudowują potrzebne moduły za pełną cenę i do 100% HP, po walidacji całej sumy; brak pieniędzy nie powoduje częściowych zmian. Naprawa zbiorcza i awaryjna pokazują koszt przed zakupem.
3. Liczebność rzeczywistego spawnu, opis trasy i budżet zagrożenia używają tej samej definicji. Eskorta jest zwykłym Scoutem. Trasa bezpieczna: **2/3/4/2/3/1**, boss samotny, bez dodatkowego mnożenia HP/obrażeń.
4. Collidery i grawitacja odłamków są wyłączane w `Awake`, przed kolejnym krokiem fizyki. Cały fragment jest najpierw składany, następnie rozpoczyna 0,45 s separacji; ciężkie części otrzymują tę samą zmianę prędkości 6 m/s na zewnątrz + 2,5 m/s w górę.
5. Pocisk przechodzi przez martwą część lub odłączony wrak także po przywróceniu jego kolizji ze światem. Odtworzony przypadek dwóch trafień w jednym kroku nie zatrzymuje drugiego pocisku na właśnie zniszczonym przednim klocku.
6. Garaż chwilowo ukrywa geometrię areny, która po walce przemysłowej zasłaniała pojazd. Wyjście przywraca pierwotny stan przeszkód, również po poligonie i powtórnym otwarciu; bez teleportowania pojazdu, zmian HP lub darmowych napraw.
7. Ekran nagród pokazuje rodzaj, źródło, HP, koszt naprawy/odbudowy, selekcję i złom; paginacja oraz skróty 1–3/Enter respektują wspólny limit. Magazyn rozróżnia odbudowę od naprawy.

## Dowody automatyczne

Surowe XML, logi Unity i kody zakończenia znajdują się w ignorowanym `Artifacts/Local/`. Nie należy mylić wcześniejszego wyniku MOV 522/522 z weryfikacją tej implementacji. Końcowe pełne ponowienie W2 to **561/561** (385 EditMode +176 PlayMode). Obydwa buildy zawierają dziewięć DLL MOV-001–009; obserwator W2 jest tylko w QA. [Sumy binariów](Verification/RRL-W2/build-products.json). Cztery ostrzeżenia dotyczą dotychczasowych nieużywanych pól wejścia prototypów Gyro/Grapple/MOV009; brak nowych błędów kompilatora.

| Próba | Wynik | Folder w `Artifacts/Local/` |
|---|---|---|
| Bazowe testy modeli przed naprawami | 25/28; 3 niezgodne stare założenia | `W2-Baseline-9a56a288bd674b46b30c3fa14717f65a` |
| Nowe reprodukcje RED | 8/31; 23 przypadki zakończone błędem | `W2-RED-805e4812028b4dbfbc359af378b665bc` |
| Pierwsze GREEN | 31/31, native exit 0 | `W2-GREEN-74aaf2050c3d42c18bd6ce0ade652399` |
| Integracja świata RED | 3/5; brak obsługi klawiatury i NRE złożonego asserta fragmentu | `W2-Integration-RED-b85399cfda78406196a6326ad75cb86f` |
| Fizyczny wrak po separacji RED | 2/3; wrak pochłaniał pocisk | `W2-Edges-RED-c421c4d0c266403ba0285cf252de1e73` |
| Zestaw ukierunkowany GREEN | **48/48**, 0 pominiętych, native exit 0 | `W2-Targeted-All-07091bd9f6db456d8e4bff558a939e53` |
| Pełny EditMode — pierwsza próba | 372/384, 12 błędów starych oczekiwań/fixture, native exit 2 | `20261021T201228Z-abc2c2e41b294ddca914603d6407f0c7` |
| Ponowienie 12 dostosowanych przypadków | **12/12**, 0 pominiętych, native exit 0 | `W2-Legacy-Adaptation-GREEN-f89b689ec78d4dd8ac9c43008faf853f` |
| Pełny EditMode — ponowienie | **384/384**, 0 pominiętych, native exit 0 | `20261021T203421Z-599be3afac2b457d8f6a68a26d049aa1` |
| Pełny PlayMode | **176/176**, 0 pominiętych, native exit 0 | `20261021T205359Z-076ecdb2792740969ee56e6951f48b4b` |
| Build Windows retail + QA przed późniejszą poprawką garażu | **PASS**, native exit 0; po 0 błędów i 4 istniejące ostrzeżenia | `20261021T205445Z-ac1d6a95922c4028a18ad4f11990782b` |
| Garaż przy hali: reprodukcja RED | **FAIL**: hala przecina widok pojazdu | `W2-Garage-Visibility-RED5-9bd3b22407ed497f82bd055a316afb68` |
| Garaż / poligon / areny GREEN | **14/14**, 0 pominiętych, native exit 0 | `W2-Garage-Visibility-GREEN2-2e9fe58777b348d084affc11fa17597f` |
| Pełne EditMode po poprawce garażu | **385/385**, 0 pominiętych, native exit 0 | `20261022T075210Z-095bd98f839a474f95f2752ee84706f4` |
| Pełne PlayMode po poprawce garażu | **176/176**, 0 pominiętych, native exit 0 | `20261022T075210Z-095bd98f839a474f95f2752ee84706f4` |
| Build Windows po poprawce garażu | **PASS retail + QA**, native exit 0; każde 0 błędów / 4 istniejące ostrzeżenia | `20261022T075210Z-095bd98f839a474f95f2752ee84706f4` |
| Narzędzia CI (Python unittest) | **16/16** | wykonane lokalnie |

Pierwsze 38 nowych przypadków W2 obejmuje wszystkie 10 kombinacji liczby części/bonusów 0–3, limit i duplikaty, niedozwolony pancerz/rdzeń, 0 HP i pełną cenę odbudowy, koszty przy 0/1/25/50/99/100% oraz prawie 100% HP, brak środków, niepowtarzalną transakcję, żywy kontroler/parametry, sześć spawnów i rzeczywiste łupy, ciężkie odłamki, pauzę i przywracanie kolizji, połączony fragment oraz fizyczne pociski za wrakiem.

Dotychczasowych testów nie usunięto. Trzy nazwy zaktualizowano wraz z zasadą: awaryjna odbudowa modelu, awaryjna odbudowa w scenie oraz odzysk/odbudowa zniszczonej broni. Pozostałe dostosowania dotyczą wspólnego zatwierdzenia nagród i zwiększonych liczebności; nie zmieniono kodu mechanik MOV.

Porównanie rzeczywistych nazw przypadków XML potwierdza **346/346 zachowanych przypadków bazowych + 39 nowych = 385** (po uwzględnieniu trzech powyższych zmian nazw), zero usuniętych. Razem z 176 PlayMode daje to **561/561**. Dodatkowy 39. przypadek odtwarza zasłanianie garażu przez halę i sprawdza widok, przywrócenie przeszkód, ponowne otwarcie, poligon w osobnym zestawie oraz zachowanie części/HP/pozycji/środków. Pierwszy pełny przebieg ujawnił osiem dawnych odmów odzysku 0 HP dla MOV, test Blink sprawdzający kolizję przed końcem nowej separacji, dawną cenę naprawy pancerza 2 zamiast 1, dawny licznik zabitych 2 zamiast 3 oraz odczyt `enemy.name` przez fixture po usunięciu obiektu. Zachowano oryginalne sprawdzenia montażu, demontażu i blokowania Blink przez fizyczny wrak; dodano pełnopłatną odbudowę każdego rodzaju modułu MOV oraz jawne sprawdzenie kolizji po separacji. Nazwę źródła fixture zapisuje przed usunięciem przeciwnika.

Pełna próba potwierdziła sześć zwycięstw fizyczną bronią z aktywnym AI (`SixEncountersCanBeWonWithActualPlayerGunAndPhysicalHits`). Osobne piloty bossa: ciężki pojazd 45,96 s, lekki 50,90 s, odpowiednio maks. 5 i 4 aktywne pociski. To automatyczne piloty wejścia w Editorze, nie dowód ręcznej gry Windows. Prefiksy fixture w testach samego bossa nie są zaliczane jako fizyczna pełna wyprawa.

NRE przy początkowym złożonym assercie fragmentu nie ma potwierdzonej diagnozy produkcyjnej. Rozbicie asserta na jawne sprawdzenia żywotności właściciela/fragmentu i struktury dało GREEN. Pełna regresja oraz obserwacja gry pozostają obowiązkowe; nie opisujemy tego jako naprawionego błędu produkcyjnego.

## Rozgrywka Windows i obserwacja interfejsu

Metoda: rzeczywiste kliknięcia menu, tras, wyboru nagród, magazynu, opłaconej odbudowy i montażu w widocznym EXE. Walka korzysta z pilota WASD/LPM oraz programowego celowania kamerą, z aktywnym AI i fizycznymi pociskami. Pilot nie wywołuje API obrażeń, nagród, magazynu ani waluty. To **test operatora UI + pilot wejścia**, nie deklaracja pełnej ręcznej gry człowieka. Obserwator jest warunkowy (`MOVSERIES_VISUAL_QA` + jawne argumenty); nie występuje w retail.

Próba `703ed911019742708a44638d4629820f` (1280×720) przed poprawką garażu:

| Przypadek | Obserwacja |
|---|---|
| Trzy bonusy | Bezpośredni garaż, 3 bonusy, 0 części; rdzeń ciężkiego pojazdu 180→324 HP (kadłub ×1,2, rdzeń ×1,5) |
| Bonus + dwie części | Zniszczona ciężka armata 0 HP i całe koło w magazynie; 3 wybory łącznie |
| Odpłatna odbudowa | Cena 55, złom 231→176, HP 0→100%; przed odbudową montaż wyłączony |
| Montaż odzyskanej broni | Złom pozostaje 176, magazyn 2→1, konstrukcja 139→140, druga broń strzela fizycznie w kolejnej walce |
| Naprawy zbiorcze | Zapowiedziane 8/27/22/10 pobrane dokładnie raz; po naprawie oferta 0 |
| Zero nagród | 0/3 zatwierdzone, niewybrane elementy +52 złomu; brak dodatkowego ekranu bonusów |
| Trzy same części | 3/3, bonusy 0, następne opcje wyłączone, złom z demontażu +30 |
| Liczebność | Pierwsze pięć tras faktycznie 2/3/4/2/3; dodatkowe jednostki normalne, trasy i HUD zgodne |
| Dużo łupów | 3–4 strony po sześć kart; źródło, HP, cena, licznik, złom i zatwierdzenie widoczne bez nakładania w 1280×720 |
| Garaż po arenie przemysłowej | **Ujawniony błąd**, model zasłonięty mimo „Dopasuj widok”; późniejsza poprawka potwierdzona w końcowym EXE poniżej |
| Pełny finał Windows | **Nie zaliczono tej próby**: cztery fizyczne zwycięstwa, w piątej pilot utknął przy osłonie po utracie kół |

Próba zebrała 1562 rzeczywistych pocisków, maks. 11 aktywnych pocisków i 11 odłamków, 16559 klatek walki / 275,97 s ≈60 FPS przy limicie 60 na RTX 5080. To pomiar tej konfiguracji, nie benchmark dla innych komputerów. Brak zarejestrowanych błędów/wyjątków C#. Śledzone wraki, także 160 kg, rozpoczynały fazę bez kolizji i odzyskiwały grawitację/kolizje. Przesunięcia po kilku sekundach obejmują także lot i zderzenia; nie są pomiarem samego impulsu początkowego. Dokładne prędkości, pauzę i fizyczny strzał w żywy cel za wrakiem potwierdzają osobne testy Unity.

[Zapis obserwacji](Verification/RRL-W2/native-run-before-garage-fix.json) i [obrazy](Verification/RRL-W2/images/) zachowują także ujawniony błąd, zamiast przedstawiać go jako PASS. Retail osobno przeszedł menu → garaż → poligon → pauza → powrót. Skróty nagród mają GREEN w teście wejścia Unity; syntetyczne klawisze narzędzia Windows nie dały potwierdzonego przełączenia, więc fizyczna klawiatura człowieka pozostaje **NOT RUN**.

Drugie podejście `71a353441f1444199e77cf04acb0d8aa`, 1920×1080: **6/6 fizycznych zwycięstw, 15 przeciwników, finał Won**. Trasy 0/0/1/0/1/0, bez ingerencji w HP lub AI. Potwierdzono również 2 bonusy + 1 część, wybór poniżej limitu (2 i 1), zachowanie zaznaczenia po zmianie strony, uszkodzone koło 43% HP → naprawa 3 złomu (319→316) →100% HP, wybór zera po piątej walce i finałowy odzysk jednej części bez bonusów, bez pośredniego garażu. Stan końcowy: złom 732, 140 części pojazdu, 7 bonusów. [Obserwacje z pełnej wyprawy](Verification/RRL-W2/native-six-wins-1920.json).

Układ nagród i garażu sprawdzono natywnie w **1280×720 i 1920×1080**. [Pełny render gry 1920×1080](Verification/RRL-W2/images/game-render-1920x1080-rewards.png), [dwa bonusy i część](Verification/RRL-W2/images/native-1920-two-bonuses-one-part.jpg), [wynik 6/6](Verification/RRL-W2/images/native-1920-gauntlet-won-six-of-six.jpg). Oba powyższe przebiegi używały binariów sprzed późniejszej poprawki zasłaniania garażu (runtime SHA256 `869ee5b…`); po zmianie wyłącznie widoku garażu wykonano osobną regresję nowego EXE opisaną poniżej. Całkowicie ręczna walka człowieka: **NOT RUN**. Regresje MOV-001–009 i dotychczasowe scenariusze pozostają GREEN; jedynym nowym istotnym problemem ujawnionym przez ogląd gry był zasłonięty garaż, obecnie naprawiony. Awaria zamykania występowała także przed tą poprawką i jest rozliczona osobno.

Końcowy build z `c16c6ae` / runtime `316ee6f…`: próba `c73e598854844f92a5d7c770e8d620a4`, 1280×720, **trzy fizyczne zwycięstwa → garaż po arenie przemysłowej → dopasowanie kadru → obrót → poligon → pauza → powrót do garażu: PASS**. Hala nie zasłania pojazdu; po powrocie złom 306, 140 części i zapowiedziana naprawa 34 pozostają niezmienione. Osłony są ponownie obecne w walce; test 14/14 dodatkowo sprawdza ich fizyczne przywrócenie i pierwotny stan aktywności. Brak zarejestrowanych błędów C#. Ta próba celowo kończy się po 3/6 i nie jest przedstawiana jako drugi pełny finał na końcowych binariach. [Zapis regresji](Verification/RRL-W2/native-final-garage-regression.json), [garaż po poprawce](Verification/RRL-W2/images/native-fresh-industrial-garage-clear-after-fix.jpg), [po poligonie](Verification/RRL-W2/images/native-fresh-industrial-garage-after-range.jpg).

## Zamykanie Windows / silnik

**FAIL po automatyzacji UI:** Alt+F4 kończy własny player `0xC0000005` w `UnityPlayer.dll`, RVA `0xb1b031`, zarówno D3D12, jak i kontrola D3D11. Pasujący publiczny PDB wskazuje `PlatformAccessibilityManager::StaticDestroy` +0x51. Objaw odpowiada UUM-146676, wymienionemu jako poprawiony w [oficjalnym Unity 6000.6.0b5](https://unity.com/releases/editor/beta/6000.6.0b5). To dopasowanie diagnostyczne, nie potwierdzenie przyczyny przez producenta. Nie potwierdzono poprawki dla wymaganego 6000.3.14f1. [Szczegóły i granice diagnozy](Verification/RRL-W2/windows-shutdown.md).

Zwykłe ręczne zamknięcie bieżącego W2: **PASS**. Użytkownik potwierdził Alt+F4, a metadane własnego procesu wykazały native exit **0**, 22.10.2026 o 09:30:51 UTC. Próba `e43e857dd7934304bb9bde2f7bffce29` używała końcowego retail (Assembly-CSharp SHA256 `11fc980013da89824eadeb2a4530087bcfa1a7eed33602fd14852d7d9a7a728e`), bez interakcji narzędzia UI z tym procesem i bez automatycznego limitu zamknięcia. [Dowód ręcznego wyjścia](Verification/RRL-W2/manual-retail-shutdown.json). Wcześniejsza próba `611f863…` została zatrzymana przez limit skryptu po 30 minutach i pozostaje NOT RUN dla ręcznego zamknięcia. Wynik obecnego W2 jest niezależny od wcześniejszego potwierdzenia R6. Nie zmieniano silnika ani ustawień dostępności; awaria po UI Automation pozostaje odrębnym ograniczeniem automatycznego QA.


## Ograniczenia i decyzje

- Niezależny przegląd całej ówczesnej gałęzi (świeży reviewer, `gpt-6-astra`, xhigh): **0 krytycznych, 0 istotnych**. Przegląd poprzedzał późniejsze odkrycie usterki garażu w EXE; tę poprawkę sprawdził implementujący przez RED→GREEN, 14 testów ukierunkowanych i pełne 561/561. [Zakres przeglądu](Verification/RRL-W2/code-review.md). Jedna odłożona uwaga P3: zniszczona część magazynowa może mieć zielony podgląd geometrycznie poprawnego miejsca i komunikat „Zielony podgląd”; przycisk oraz model blokują montaż do opłaconej odbudowy. Wymaga dopracowania komunikatu/podglądu, bez luki ekonomii.
- Ostatnie starcie oferuje odzysk części bez niepotrzebnych bonusów na kolejną walkę, zgodnie z istniejącą specyfikacją W2.
- Zmieniono dawną darmową pomoc awaryjną 25% HP: obowiązuje pełnopłatna odbudowa 100% HP, ponieważ nowe wymagania obejmują wszystkie ścieżki napraw.
- Nie dodano MAP/VFX, nie zmieniono wersji Unity, pakietów ani sceny. Nie omijano aktywacji Unity.
- GitHub CI na commicie implementacji `c16c6ae`: [CI-01 Repository Validation PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/39747539340); [CI-02 EditMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/39747539354) i [CI-03 PlayMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/39747539416) mają status joba failure, lecz **Unity NOT RUN / BLOCKED**: krok aktywacji kończy się przed silnikiem komunikatem „Unity activation is missing. No tests or build executed.” Wymagana jest obsługiwana konfiguracja `UNITY_EMAIL`, `UNITY_PASSWORD` oraz `UNITY_LICENSE` lub `UNITY_SERIAL`, zgodnie z `Docs/CI_SETUP.md`. Nie publikowano ani nie przenoszono lokalnej licencji. CI-04 nie uruchamia się dla feature PR (tylko push main/develop lub ręczny dispatch): **cloud Windows build NOT RUN**; lokalne oba buildy PASS.
- Decyzja: **BLOCKED** przez brak aktywacji Unity w GitHub CI. Normalne ręczne zamykanie końcowego retail ma PASS / native exit 0; awaria zamykania po kliencie UI Automation pozostaje znanym ograniczeniem tej ścieżki QA. Mechaniki, pełne lokalne testy, oba buildy, pełny natywny przebieg sprzed ostatniej poprawki widoku i regresja końcowego garażu mają osobne dowody PASS; nie są deklaracją naprawy silnika.

## Zmienione pliki

[Pełne listy plików](Verification/RRL-W2/changed-files.md) obejmują zarówno naprawy względem początkowego head PR, jak i cały PR względem develop. Runtime po poprawce garażu: SHA256 `316ee6f27e2780b7479dadf0e7bf4d5e61dfc2648c417286c3c1800edb641ae9`, 558 plików; [manifest](Verification/RRL-W2/tested-source.json). Brak zmian sceny, pakietów i ProjectSettings.
