# Energia, walka, pojazdy i garaż — raport testowy

Baza: PR #936 / `6d5636672a805619225d20a4097d3784f180ea00`. Kod buildu: `9b29b423e3335e6e1027cb8488970f7b845a4df4`. Gałąź: `feature/energy-combat-garage-refinement`. Unity 6000.3.14f1; nie scalono do main.

## Co zmieniono

- Pojazd zaczyna pierwszą aktywność z pełnym dostępnym zapasem, także jeśli przed wyjazdem zamontowano baterie. Wczytany zapis nie otrzymuje darmowego doładowania. Rdzeń daje 500 pojemności, bateria +100, każdy sprawny moduł pasywny nadal rezerwuje 25.
- Koszty aktywnego użycia broni i urządzeń zmniejszono o połowę. Regeneracja podstawowa wynosi 10/s, a po zatrzymaniu +300%, czyli 40/s. Próg zatrzymania dopuszcza minimalne drgania fizyki.
- Turkusowe pole natychmiast przywraca 50% brakującej energii oraz daje 5% aktualnego maksimum na sekundę przez 10 s; po 10 s może aktywować się ponownie. Niebieska fizyczna kostka z pokonanego wroga znika po zebraniu i daje 10% maksimum/s przez 5 s; efekty kolejnych kostek sumują się.
- Po pokonaniu ostatniego wroga gra daje do 8 s na zebranie pozostałej energii; Enter pozwala przejść do nagród wcześniej.
- Zwykli przeciwnicy otrzymali dodatkowe fizyczne działo korzystające z ich energii i ekwipunku. Mobilni przeciwnicy okresowo próbują taranować także z działającą bronią. Pozbawiony sprawnego uzbrojenia i kół przeciwnik po 5 s zniszczy się, gdy gracz znajdzie się w promieniu 6 m; ostrzega czerwonym sygnałem.
- Startowe pojazdy mają odpowiednio 4/6/8 kół, 1/2/3 działa oraz 0/1/3 baterie. Ciężki jest odchudzony względem poprzedniego projektu. Punkty kół lekkiego rozstawiono dalej wzdłuż kadłuba. Prędkość i skręt wynikają ze sprawnych części, masy oraz liczby kół i są ponownie obliczane po utracie części.
- Na trzech istniejących arenach R5 dodano po cztery szerokie fizyczne rampy 18°, a na poligonie dwie. Krawędzie obniżono po wykryciu niedostępnego progu.
- Garaż ma jeden otwierany katalog typów ze stanem ilościowym. Po kliknięciu stosu pokazuje osobne egzemplarze i ich HP. Zakup trafia do magazynu; po wybraniu egzemplarza montuje się go kliknięciem w pojazd. Usunięto oddzielne przyciski wyboru i bezpośredniego kupowania broni z panelu warsztatu. Esc zamyka katalog przed pauzą.

## Wyniki testów

- Końcowy zestaw nowych funkcji w Unity EditMode: **18/18 PASS**, exit 0, na kodzie buildu.
- Szerszy zestaw EditMode: **41/41 PASS**, exit 0, przed końcowym dodaniem okresowego taranowania; jego funkcję zweryfikowano następnie osobno: **6/6 PASS**, w tym rzeczywisty kontakt uzbrojonego i rozbrojonego wroga oraz autodestrukcję.
- Pełny Unity PlayMode: **478/478 PASS**, exit 0, przed końcową zmianą cyklu AI i testowym rozszerzeniem Playera. Nie powtarzano całego PlayMode po tej niewielkiej zmianie; powtórzono testy dotyczące AI.
- Pierwsza szeroka regresja dała **82/101 PASS**. Wykryte błędy były związane m.in. z nowym podwoziem w starszych konfiguracjach testowych, dodatkowymi częściami nagród oraz testem zużycia piły. Poprawione scenariusze uruchomiono ponownie: **20/21 PASS**, a ostatnią konfigurację 12 kół i osiągi sprawdzono później **3/3 PASS**. Nie przedstawia to ponownego pełnego przebiegu 101/101.
- Unity / fizyka: w teście przyspieszenie po 0,6 s wyniosło 4,08 / 2,06 / 1,77 m/s dla lekkiego / średniego / ciężkiego. Próbę hamowania lekkiego wykonano automatycznie, bez oceny subiektywnej. Oba pojazdy wjechały na fizyczne rampy; mierzona maksymalna wysokość w krótkim przebiegu wyniosła 4,44 m i 1,32 m.
- Windows x64 build: **PASS**, exit 0, 0 błędów, 2 ostrzeżenia. ZIP wypakowano i uruchomiono zawarty w nim Player w tle.
- Wypakowany Windows Player: **PASS**, exit 0 dla czterech rodzajów broni, z kontrolą poprawnego kosztu energii oraz braku kosztu przy zablokowanym strzale; **PASS**, exit 0 dla garażu, fizycznego pocisku wroga, dwóch walk, dwóch nagród i zachowania identyfikatorów części. Są to testy sterowane API, nie rozgrywka człowieka.
- Graficzny przegląd nowego katalogu w Windows, ręczna pełna wyprawa i ocena komfortu jazdy: **NOT RUN**. Nie przejmowano myszy ani sterowania komputerem.

## Build do testu

`Builds/RoboRougelike_EnergyCombat_Garage_TEST_Windows_x64.zip`; 39 647 063 bajty; SHA-256 `59ca7638a54d3953eaac678a1f5a3a3f773d945bb8ceaf3805539400f02c9d89`. Wypakuj całe archiwum i uruchom `RoboRougelike.exe`.

W garażu wybierz **KATALOG CZĘŚCI**, kliknij typ, a następnie egzemplarz z odpowiednim HP lub kup część do magazynu. Po wyborze kliknij powierzchnię pojazdu. Na arenie obserwuj wskaźnik energii, turkusowe pole oraz niebieskie kostki.

## Ograniczenia i ryzyka

Nowe rampy znajdują się na standardowych arenach R5 i poligonie; proceduralnego MAPGEN ani areny LD02 nie przebudowano. Nie przeprowadzono pełnego audytu wszystkich ekranów UI ani ręcznego testu ich czytelności; przeorganizowano głównie garaż i informacje energii. Balans obrażeń, czasu zbierania oraz poruszania wymaga opinii gracza. Starsze zapisy, które polegały na nieistniejących już komórkach dawnych modułów 1×2×2, nadal mogą nie przejść walidacji; nie wykonano ich automatycznej migracji. Logi, XML i kody zakończenia są lokalnie w `Artifacts/Local/EnergyCombat/`.