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
- Wcześniejsze próby wykryły brak zapasu na baterię oraz zaburzoną kolejność przyspieszeń po zmniejszeniu konstrukcji; poprawiono je i uruchomiono testy ponownie. Ręczny Windows Player i pełna regresja PlayMode: **NOT RUN**. Wzrost limitu podczas wyprawy i klasa rozmiaru należą do punktu 08 i pozostają do wdrożenia.
