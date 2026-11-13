# Backlog mechanik 13.11.2026 — postęp

Źródło: `RoboRougelike_Codex_backlog_mechanik_2026-11-13.md`, dostarczone przez właściciela.
Lista poniżej opisuje tylko faktycznie sprawdzone fragmenty, nie zastępuje źródłowej checklisty.

## 10–11. Fuzja części — częściowo sprawdzone i uzupełnione

- Istniały operacja fuzji trzech fizycznych części oraz przycisk w katalogu garażu. Nowa część otrzymuje średnią procentową stanu wejść, także dla wejścia z 0 HP.
- Uzupełniono podgląd o stan HP wyniku i stany wszystkich trzech składników przed zatwierdzeniem operacji, w obu widokach katalogu.
- Unity 6000.3.14f1, EditMode, `BacklogPartFusionTests`: **6 PASS, 0 FAIL**. Test obejmuje wejścia 50%, 0%, 100% i wynik 50%, zużycie wejść, tożsamość oraz zapis stanu części.
- Unity 6000.3.14f1, `BacklogPartFusionGarageTests`: **1 PASS, 0 FAIL**; test wchodzi w Play Mode i sprawdza fuzję oraz montaż otrzymanej części w garażu.
- Rzeczywisty ręczny test Windows Player: **NOT RUN**. Pełny punkt 10 (czytelność i używalność podglądu podczas gry) pozostaje do potwierdzenia.
- Pierwsza próba Unity w sandboxie zakończyła się awarią edytora podczas startu usługi sieciowej; wynik testowy nie powstał. Ponowna próba poza sandboxem przeszła.

## 01–05. Sloty i limity — wdrożono część, dalsza integracja pozostaje

- **01 — częściowo:** montaż sprawdza do 8 rodzajów pasywnych i 4 aktywnych, liczy rodzaje zamiast kopii i uwzględnia identyfikator bonusu. Istniejące moduły są jednokomórkowe; każdy zamontowany egzemplarz liczy się do budżetu z 07. Starsze konstrukcje nie są automatycznie demontowane.
- **02 — częściowo:** do czterech rodzajów broni, kopie rodzaju korzystają z tego samego slotu. Zmieniono też walidację zapisu oraz blokadę piątego rodzaju. Testy sprawdzają wyzwolenie czterech rodzajów i niezależność grup; pełne zachowanie każdej rodziny, w tym indywidualnych magazynków, wymaga dalszej regresji.
- **03–05 — częściowo:** broń otrzymała sloty LPM/PPM/Q/E, a istniejące aktywne moduły sloty 1–4. Garaż pozwala przenosić rodzaje między slotami; przypisania przechodzą przez kopiowanie i zapis z zachowaniem odczytu dawnego formatu dwóch grup. Blink, zaczep, booster, skok, naprawa i skanery nie wymagają już dawnych stałych klawiszy. Testy naciskają Q/E i klawisze modułów. W poligonie kreatywnym nadmiarowe rodzaje można przypisać do slotu.
- Cztery wspólne sloty urządzeń rozstawianych, konfigurowalne klawisze w Ustawieniach (06), pozostałe zachowania wspólnego użycia/kosztu/cooldownu (16–19) i pełna migracja starszych interfejsów pozostają do wykonania.
- Unity 6000.3.14f1, wybrane testy EditMode, w tym testy przechodzące do Play Mode: **40 PASS, 0 FAIL, 0 SKIP**, proces Unity kod **0**. Zweryfikowano m.in. limity, kopie, cztery fizyczne bronie, Q/E, stare i nowe zapisy, aktywne moduły, skaner bez modułu napędu i tryb kreatywny. Wynik: `C:\Users\kubap\Desktop\Prototypy\slots-integration-final.xml`; log: `C:\Users\kubap\Desktop\Prototypy\slots-integration-final.log`.
- Ręczny test Windows Player oraz pełne testy PlayMode: **NOT RUN**. Nie wykonano nowego buildu.

## 07. Ważony budżet konstrukcyjny — prototyp

- Początkowo 200 punktów; wartości konfigurowalne: pełny klocek 2, połówka 1, koło 2, broń 6/8/10 zależnie od tieru, moduł aktywny 4, pasywny i napędowy po 3. Każda zamontowana część liczy się osobno, również zniszczony plan; magazyn nie zużywa budżetu. Poligon kreatywny pozostaje bez limitu. Obowiązują też istniejące granice przestrzeni i połączeń.
- Zapis zachowuje aktualny limit; dawne zapisy otrzymują wartość konfiguracyjną. Konstrukcje przekraczające limit po wczytaniu nie są automatycznie zmieniane, ale nowy montaż wymaga wolnego budżetu. Garaż pokazuje wykorzystane i dostępne punkty.
- Dwanaście konstrukcji startowych dopasowano przez usunięcie części pancerza z zachowaniem połączeń oraz kół, broni i baterii. Zostawiono zapas na rozbudowę: 20 punktów, a dla lekkich dodatkowo 40. Ciężki pojazd ma trzy zamiast czterech modułów mocy, aby zachować kolejność osiągów wynikającą ze składu. Są to wartości prototypowe do późniejszego playtestu.
- Unity 6000.3.14f1: celowana regresja **43 PASS, 0 FAIL, 0 SKIP**, kod 0: `C:\Users\kubap\Desktop\Prototypy\budget-slot-regression.xml` oraz `.log`. Dodatkowy zestaw pojazdów, fizycznego strzału i wypełnionego budżetu: **10 PASS, 0 FAIL**, kod 0: `C:\Users\kubap\Desktop\Prototypy\budget-starter-physics4.xml` oraz `.log`.
- Wcześniejsze próby wykryły brak zapasu na baterię oraz zaburzoną kolejność przyspieszeń po zmniejszeniu konstrukcji; poprawiono je i uruchomiono testy ponownie. Ręczny Windows Player i pełna regresja PlayMode: **NOT RUN**.

## 08. Progres budżetu i rozmiar — prototyp

- Po każdych dwóch zwycięstwach limit wzrasta o 20 punktów; krok i odstęp są edytowalne w konfiguracji. Numer przyznanego progu i limit przechodzą przez kopię i zapis. Wczytanie wcześniejszego zapisu nadrabia należne progi jednorazowo, bez modyfikowania części i waluty.
- Klasa mała/średnia/duża jest obliczana wyłącznie na podstawie liczby zamontowanych części z HP powyżej zera. Domyślne progi 40 i 80 są tymczasowe i konfigurowalne; tier, rodzaj i koszt części nie wpływają na klasę. Główne widoki garażu pokazują klasę i liczbę części oddzielnie od punktów konstrukcyjnych.
- Unity 6000.3.14f1: **19 PASS, 0 FAIL, 0 SKIP**, kod 0, `C:\Users\kubap\Desktop\Prototypy\budget-progress-checkpoint.xml` oraz `.log`. Obejmuje symulowaną rozgrywkę w Play Mode przez dwa starcia, zapis, restart i wczytanie, bez ponownego przyznania progu; osobno testy starego formatu, konfiguracji i zmiany klasy. To test sterowany API; ręczna gra Windows i niezależna pełna regresja PlayMode: **NOT RUN**.
- Poprawiono także stare wskazanie klawisza V dla impulsu naprawczego w katalogu: aktywacja należy obecnie do przypisanego slotu 1–4.

## 09 i 15. Tiery i uszkodzenia modułów — częściowo

- Zweryfikowano istniejące trzy tiery baterii, regeneracji energii i silnika oraz ich fizyczną integrację. Silnik pasywny miał wcześniej moc proporcjonalną do aktualnego HP, co było niezgodne z nową regułą; obecnie daje pełny wkład swojego tieru przy dowolnym dodatnim HP, a po zniszczeniu, odłączeniu lub wyłączeniu znika jego wkład. Uwzględniono zarówno siłę napędu R6, jak i sterowność podwozia.
- Celowane Unity 6000.3.14f1: **7 PASS, 0 FAIL**, kod 0; test rzeczywistych kół sprawdza 50%, 1% i zniszczenie silnika oraz jego dezaktywację, a pozostałe testy sprawdzają tier i bilans energii: `C:\Users\kubap\Desktop\Prototypy\passive-motor-final.xml` oraz `.log`. Przed poprawką ten sam test wykazał rzeczywisty błąd: 0,5 zamiast 1 jednostki mocy przy 50% HP.
- Obrona punktowa obsługuje fuzję T1→T2→T3, zapis tieru i wkład tieru w obrażenia pocisku przechwytującego oraz jego koszt energii. Celowane Unity: **7 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\point-defense-tier-final4.xml` oraz `.log`. Sprawdzono fuzję i zapis, a także faktyczny wystrzał i przechwycenie rakiety przez T1 oraz T2, zniszczenie, naprawę i zachowanie amunicji. Test używa kontrolowanego trafienia: wcześniejsze 10000 obrażeń niszczyło również podporę przez system przenoszenia obrażeń, co uniemożliwiało naprawę.
- Nie oznacza to ukończenia wszystkich rodzin tierów ani pełnej reguły 15 dla wszystkich urządzeń; potrzebna jest dalsza integracja. Ręczny Windows Player: **NOT RUN**.
