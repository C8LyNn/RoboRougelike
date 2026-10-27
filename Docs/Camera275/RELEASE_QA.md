# CAMERA275 — stabilizacja, responsywność, zoom i celowanie

Status roboczy: **IN_PROGRESS — pełna regresja Unity i macierz Windows trwają**. Wyników niezakończonych prób nie zaliczamy jako PASS. Nie wykonano merge do main/develop.

## Źródło i zachowanie integracji

- Baza: Draft PR [#277](https://github.com/C8LyNn/RoboRougelike/pull/277), HEAD `0c7d0ba3d6b5d8ba31f728c3fb650ea79414c4cd`; zawiera kompletną integrację #272 i poprawki wyjścia z gry. Stan #277 ponownie odczytany przed publikacją: nadal draft/open, ten sam HEAD.
- Zamrożone źródło kodu i końcowego Windows Playera: **`c61894a6389e43727db42a6dd58f153a496923ee`**. Późniejsze zmiany dotyczą raportu i narzędzi analizy, nie kodu gry.
- Oddzielny worktree: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-Camera275`, gałąź `fix/issue-275-camera-feel`. Istniejące worktree i wydania zachowane.
- Unity **6000.3.14f1**, istniejący URP, Windows x64 Mono, rzeczywisty render D3D11. Nie zmieniono pakietów, wersji Unity, solvera R6 ani parametrów obrażeń/zasięgu.
- [Kontrola integralności](integrity.json): **983 unikalne GUID**, brak brakujących `.meta`, duplikatów, usunięć oryginalnych assetów i zmian istniejących `.meta`. `Assets/B01Packages.meta`, manifest, lock i ProjectVersion zachowane bajtowo.
- Przeczytano Issue #275 i komentarze. Szersze zmiany ekonomii, rodzin broni i garażu nie są częścią tego zadania. Prototypowe modele pozostają wyłączone z zakresu na życzenie właściciela.

## Co zmieniono i dlaczego

Rigidbody interpolation, jeden właściciel kamery i kolejność LateUpdate były już poprawne. Zachowano je: P12 kamera/rig wykonuje końcową pozycję w LateUpdate100, wspólny kontroler broni pobiera rzeczywisty promień kamery w LateUpdate500. Nie dodano drugiej kamery ani solvera ruchu.

Poprzednie opóźnienie podążania działało również na świadomy obrót myszą. Kamera dodatkowo obracała się w stronę surowego punktu kadrowania z opóźnionej pozycji. Zmieniono odbiornik tak, aby stabilizował punkt podążania i wymiary przed wyznaczeniem widoku. Obrót myszą jest bezpośredni; tłumienie nie jest filtrem wejścia celowania. Zachowano stabilną obwiednię obracających się dział, aktualny rzeczywisty kadłub jako autorytet bezpieczeństwa, kadrowanie i sprawdzanie końcowego promienia.

Oryginalny standalone CAMERA zachowuje domyślny limit 15 m/s i dotychczasowe testy. Odbiornik P12 rozdziela świadomą translację orbity od niezamówionego ruchu podążania. Nie podniesiono potajemnie limitu, aby zaliczyć testy; oba składniki są mierzone osobno. Sprawdzenie kolizji obejmuje cały ostateczny ruch kamery i objętość near-plane.

Przyczyną udowodnionego problemu dalekiego celowania był wymuszony minimalny kąt **18°**, mimo że scena miała ustawione −5°. Promień kończył się na ziemi zamiast na widocznym celu 100 m. Usunięto wymuszenie, zachowując ochronę własnego kadłuba. Nie wydłużano życia pocisków ani zasięgu broni.

Zoom działa kółkiem jako zmiana odległości od stale śledzonego robota, bez odczepiania kamery i bez przesuwania pojazdu. Docelowa odległość dochodzi płynnie; obrót myszą pozostaje natychmiastowy. Żądany zakres wynosi 3–45 m; minimalna odległość faktyczna uwzględnia rozmiar konstrukcji, a przeszkody ograniczają pozycję dla bezpieczeństwa.

Parametry są dostępne w istniejącym komponencie P12 w Inspectorze: poziome podążanie 0,06 s, pionowe 0,12 s, obrys 0,22 s, zoom 0,10 s, czułość 0,12°/piksel. Bez nowego menu ustawień.

Celownik jest wyśrodkowany na faktycznym promieniu. Pokazuje ograniczenie zasięgu, obracanie luf i przeszkodę przy wylocie. Korzysta z konfiguracji rzeczywistych modułów, sprawdza początkowe nakładanie colliderów i szerokość pocisku. To informacja o geometrii toru, nie gwarancja trafienia ani zastępstwo wskaźników amunicji, ciepła i odnowienia. Po utracie broni lub wyłączeniu sterowania poprzedni stan jest czyszczony.

## Udowodnione reprodukcje i poprawki

| Próba | Wynik rzeczywisty | Dowód |
| --- | --- | --- |
| RED, źródło `d7badbcc` | FAIL 0/2: dla 12° myszy widok osiągał 4,209549°; cel 100 m zastępowało trafienie w `P12_Ground` | `FM270-CAM275-RedNative-20261027T092251Z` |
| Pierwsza poprawka `814525e6` | PASS 5/5 celowanych testów; te same dwie asercje bez osłabiania | `FM270-CAM275-Green-20261027T094935Z` |
| RED nakładania lufy `9b88be58` | FAIL 0/1: lufa wewnątrz przeszkody raportowała Clear=1 | `FM270-CAM275-MuzzleRed-20261027T101331Z` |
| Celowane źródło `69938a0e` | PlayMode PASS **15/15**, exit0; EditMode 42/44, dwa błędy przygotowania testów konstrukcji pozostawione w dowodach | `FM270-CAM275-ReceiverGreen-20261027T101615Z` |
| Końcowe konstrukcje `c61894a6` | PASS **7/7**, exit0: Light/Standard/Heavy/Tall/Wide, obrys i samoprzecięcia, utrata części, korekty przy ścianie, utrata ostatniej broni, nakładanie lufy | `FM270-CAM275-ConstructionRetest-20261027T102225Z` |
| Końcowa kompilacja i Windows build | PASS, oba rzeczywiste exit0 | ten sam etap, Compile/Build-process.json |
| Pełny EditMode / PlayMode `c61894a6` | **IN_PROGRESS** | `FM270-CAM275-ReleaseFull-20261027T124058Z` |

Testy konstrukcji używają prawdziwych zmontowanych pojazdów i fizyki colliderów, lecz krok kamery 30/60/120 jest deterministyczny, a podwozie jest utrzymane w miejscu. Nie zastępuje to rzeczywistych przejazdów Windows.

W dowodach pozostawiono także błędy narzędzi: początkową literówkę `Transform.SetActive`, próbę zdjęcia ostatniej broni mimo poprawnej blokady garażu, kupowanie kolejnych kopii tej samej broni aż do wyczerpania Złomu, zajęte/nielegalne komórki testowej konstrukcji, ścianę ustawioną poza badanym orbitowaniem oraz błędne oczekiwanie zakresu przy symetrycznym scrollu. Skorygowano przygotowanie prób, nie zasady gry ani asercje bezpieczeństwa. Urządzenia syntetyczne izolują wejście w swoim procesie i przywracają poprzedni stan po teście.

## Windows: pociski, FPS i rzeczywista gra

Końcowe **28/28** prób broni przeszło, proces zakończył się **0 / 0x00000000**. [Tabela obrażeń dla 10/30/60/100 m](WINDOWS_SHOT_MATRIX.md) i pełne dane zawierają końcowy punkt promienia, collider, wylot, kierunek, błąd lufy i rzeczywisty spadek HP. Działo podstawowe: 40 HP na każdym dystansie; Heavy: 100 HP; Automatic: 24 HP w tej próbie. Shotgun trafia w 10/30 m, przy 60/100 m zachowuje limit; Beam Cutter zachowuje 16 m. Railgun i Rocket trafiają na 100 m bez zmiany konfiguracji. Cel był statyczny; nie jest to dowód trafiania w poruszający się cel lub ręcznej walki z AI.

[Macierz przejazdów](WINDOWS_DRIVE_MATRIX.md) rozdziela ukończenie trasy od bezpieczeństwa kamery. Używa istniejącego poligonu, rzeczywistego R3/R6, natywnych stanów WASD/myszy i renderowania Playera, bez teleportacji i zapisów prędkości. Trasa obejmuje postój, przyspieszenie, hamowanie, skręt, niskie stopnie, nierówności, rampę oraz obrót i maksymalne oddalenie. Każda para konstrukcji/FPS ma własny log, CSV i kod procesu. Nieudany pilot pozostaje FAIL, nawet jeśli kamera nie naruszyła bezpieczeństwa.

W pierwszej próbie Standard60 automat utknął przy niskim stopniu w `(13.275, 0.761, -16.066)`, po 27,17 m, exit1 na diagnostycznym limicie150 s; własne przecięcia i nakładania kamery:0. Standard120 utknął później na dojeździe do rampy. To nie crash Playera ani dowód przyczyny fizycznej; pilot nie ma procedury cofania/odzyskania położenia. Problemu nie ukrywa się jako pełnego PASS trasy.

Przed zmianą algorytmu zachowano natywne przejazdy Standard60 i Heavy60 na źródle `aa81912a`; oba exit0. Po pierwszej poprawce Standard60 `814525e6` również exit0, z zerem samoprzecięć i kolizji kamery. Trasy mają te same punkty, ale fizyczne trajektorie nie są deterministycznie identyczne, więc statystyki nie są laboratoryjnym A/B klatka w klatkę. [Surowe i obliczone pomiary](motion-results.json) rozdzielają świadomy ruch myszy i korekty kolizji od niezamówionego ruchu widoku. Nie deklarujemy zerowych drgań we wszystkich warunkach; pozostają sporadyczne korekty brzegowe i ruch od rzeczywistego podwozia.

**Ocena właściciela po rzeczywistej grze, źródło `814525e6`:** kamera została znacznie poprawiona i jest aktualnie wystarczająco dobra; większym zauważonym problemem było odbijanie kół. To pozytywna ocena komfortu tej wersji, a nie wynik automatu ani deklaracja idealnego działania na każdym FPS. Właściciel doprecyzował następnie zoom: płynnie bliżej/dalej, stale przy pojeździe. Funkcjonalny test tego zoomu przeszedł. Ręczna kontrola końcowego ZIP-a z nowym zoomem: **PENDING**.

Pełna regresja sześciu starć Heavy oraz końcowe wejście z wypakowanego ZIP-a: **PENDING**. Starszego PASS z #277 nie przepisano na bieżący build. Historyczny C0000005 związany z dostępnością/UIA pozostaje **NOT PROVEN FIXED**, zgodnie z #277. Nie aktualizowano silnika ani nie zabijano Playera jako sposobu normalnego zamknięcia. Fizyczny kontroler/gamepad oraz dynamiczne trafienia w ruchomy cel na wszystkich dystansach: **NOT_RUN**.

## Wydanie i ręczna próba

- [Weryfikacja archiwum i wszystkich wypakowanych bajtów](delivery-verification.json).
- ZIP: `C:\Users\kubap\Desktop\Prototypy\Releases\ROBOROUGELIKE_CAMERA275_20261027\RoboRougelike_CAMERA275_Windows_x64.zip` — **38 613 209 B**.
- SHA256: `ef1be93d92d530dda606381ad1bcbdc329a74edd4e8332ef3cf2a3c5fe6194d8`.
- EXE: sąsiedni folder `RoboRougelike_CAMERA275_Windows_x64\RoboRougelike.exe`. Zawiera kompletny UnityPlayer, folder danych, Mono i wymagane biblioteki.

Uruchom EXE → **Przygotuj pojazd** → **Testuj na poligonie**. Mysz obraca widok, kółko płynnie przybliża/oddala stale śledzony pojazd; WASD prowadzi, LPM strzela, Esc otwiera istniejące menu pauzy. Sprawdź skręt, hamowanie, nierówności oraz bliski i daleki kadr. Zakończ przyciskiem **Wyjdź z gry**. Nie dodano trybu wolnej kamery.

Rollback: użyj zachowanego wydania #277 albo przywróć gałąź gry do bazy `0c7d0ba3...` w odrębnym checkoutcie. Ta gałąź jest propozycją do przeglądu; nie została scalona. Obserwacje odbijania kół i nieudanych pilotów wymagają oddzielnej diagnostyki napędu/testowego sterownika, bez cofania obecnych ulepszeń kamery.
