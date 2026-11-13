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
- Zwolniono Z i C dla przyszłej puli rozstawianych urządzeń: niezależny obrót Omni/Hover używa teraz przecinka i kropki, a HUD pokazuje nowe klawisze. Celowane Unity: **3 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\omni-controls-driving-final.xml` i `.log`. Testy obejmują osobne naciśnięcie Z/C, wejście na poligonie oraz fizyczne przejazdy i obroty Omni i Hover sterowane wirtualną klawiaturą. Nie jest to implementacja deployables ani ręczna gra.
- Dwie starsze próby zakończyły się błędem przed jazdą, ponieważ nowe pojazdy startowe zajmują komórki starego scenariusza, a ceny sklepu wzrosły (`omni-controls-red.xml`, `omni-controls-driving2.xml`). Przygotowanie konstrukcji w testach dostosowano, zapewniono środki na test ruchu i wykonano ponowny przebieg. W trakcie poprawiania testów wystąpiła też kompilacja z konfliktem nazw zmiennych; poprawiono ją przed końcowym przebiegiem. Pełna zmiana klawiszy przez użytkownika w ustawieniach pozostaje w pkt 06.

## 07. Ważony budżet konstrukcyjny — prototyp

- Początkowo 200 punktów; wartości konfigurowalne: pełny klocek 2, połówka 1, koło 2, broń 6/8/10 zależnie od tieru, moduł aktywny 4, pasywny i napędowy po 3. Każda zamontowana część liczy się osobno, również zniszczony plan; magazyn nie zużywa budżetu. Poligon kreatywny pozostaje bez limitu. Obowiązują też istniejące granice przestrzeni i połączeń.
- Zapis zachowuje aktualny limit; dawne zapisy otrzymują wartość konfiguracyjną. Konstrukcje przekraczające limit po wczytaniu nie są automatycznie zmieniane, ale nowy montaż wymaga wolnego budżetu. Garaż pokazuje wykorzystane i dostępne punkty.
- Dwanaście konstrukcji startowych dopasowano przez usunięcie części pancerza z zachowaniem połączeń oraz kół, broni i baterii. Zostawiono zapas na rozbudowę: 20 punktów, a dla lekkich dodatkowo 40. Ciężki pojazd ma trzy zamiast czterech modułów mocy, aby zachować kolejność osiągów wynikającą ze składu. Są to wartości prototypowe do późniejszego playtestu.
- Usunięto dodatkowy sztywny limit 16 kół; montaż nadal podlega budżetowi, obszarowi siatki, mocowaniom i walidacji fizycznych kolizji. Test modelu logicznego montuje 18 kół: zestaw **20 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\wheel-limit-final.xml` oraz `.log`. Nie jest to potwierdzenie konkretnego rozmieszczenia 18 kół w rzeczywistym garażu ani przejazdu takim pojazdem.
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

## 12 i 14. Sumowanie premii — częściowo

- Usunięto limit 8% HP/s z pasywnej naprawy polowej: każdy wkład dodaje 2,5% HP/s, więc T3 daje 10% HP/s. Opis w interfejsie pokazuje rzeczywistą wartość. Zniszczone części nadal nie są automatycznie wskrzeszane, a pojedyncza część nie może przekroczyć swojego maksymalnego HP.
- Celowane Unity: **7 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\repair-stack-final2.xml` oraz `.log`. Sprawdzono rzeczywiste leczenie żyjącego rdzenia, pełną siłę T3, zanik leczenia po zniszczeniu modułu oraz opis.
- Premie `increased` obrażeń z fizycznych bonusów sumują się addytywnie przed zastosowaniem do wartości bazowej; usunięto limit ×3 w kontrolerze ostrzału. Test Unity sprawdził 25% + 20% = 45%, T2 = 40% oraz prawdziwy pocisk przy ×5: **8 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\damage-additive-final.xml` oraz `.log`.
- Premie mobilności również sumują się addytywnie, a mnożnik nie jest już ograniczany do ×2 w sterowniku. Unity: **7 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\mobility-additive-final.xml` oraz `.log`; sprawdzono 25% + 20%, T2 oraz mnożnik docelowej prędkości ×3 i regresję fizycznych boosterów. Nie jest to pomiar faktycznej prędkości maksymalnej w rozgrywce.
- Premie wytrzymałości i rdzenia sumują się w oddzielnych grupach; mnożnik wytrzymałości części nie ma już dawnego limitu ×12. Zmiana premii zachowuje procent pozostałego HP. Unity: **8 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\durability-additive-final.xml` oraz `.log`; test sprawdził rdzeń z 1500 HP i utratę modułu.
- Redukcje czasu między strzałami sumują się addytywnie, bez starego limitu 35% czasu. Czas nie może spaść poniżej zera; HUD pokazuje najwyżej 100% redukcji. Unity: **7 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\reload-additive-final.xml` oraz `.log`, w tym dwa bonusy 20% i granica zerowego czasu. To nie potwierdza rzeczywistej szybkostrzelności wszystkich rodzin broni: część starszych paczek może mieć własne ograniczenia, a obsługa wejścia i fizyki zachodzi w klatkach.
- Nadal pozostają ogólne grupy `more/less`, zakresy i typy obrażeń oraz inne istniejące ograniczenia. Ta zmiana nie oznacza ukończenia punktów 12–14.

## 16–19. Wspólny aktywny impuls naprawczy — częściowo

- Sprawne kopie Repair Pulse wyzwalają jeden wspólny impuls; ich wkłady tierów sumują tempo naprawy wybranego celu. Utrata lub zakłócenie kopii w trakcie działania zmniejsza jego siłę. Aktywacja pobiera łączny koszt atomowo: przy braku energii na całą grupę nie uruchamia się żadna część.
- Rodzaj ma wspólny cooldown, zachowany także po zdjęciu wszystkich kopii, kopiowaniu konstrukcji i zapisie. Przy wczytywaniu starszych zapisów nadal uwzględniane są czasy odnowienia poszczególnych części. Aktywna naprawa nie wskrzesza zniszczonych części.
- Unity 6000.3.14f1: **15 PASS, 0 FAIL**, kod 0: `C:\Users\kubap\Desktop\Prototypy\repair-combined-final.xml` oraz `.log`. Test sprawdza dwie fizyczne kopie, częściową energię, jeden wspólny efekt, utratę kopii, cooldown i zachowanie w zapisie; osobne testy sprawdzają T2 i zgodność checkpointów.
- Booster: sprawne kopie aktywują się razem, każda dodaje własny ciąg i koszt energii. Przy braku energii na cały zestaw żadna kopia nie startuje; czas odnowienia jest wspólny i zachowany w zapisie po wymianie części. Usunięto dzielenie limitu przyspieszenia między kopie. Zestaw Unity: **14 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\booster-stack-final.xml` oraz `.log`; obejmuje porównanie jednego i dwóch fizycznych boosterów, koszt, cooldown, zapis i współdziałanie skoku oraz żyroskopu.
- Inne aktywne rodzaje i uniwersalny system ładunków wymagają dalszej pracy; nie oznacza to zamknięcia całych punktów 16–19. Ręczny Windows Player: **NOT RUN**.

## 34. Ceny zwykłego sklepu — prototyp

- Zakup części w zwykłym garażu używa konfigurowalnego mnożnika ceny bazowej, domyślnie ×3. Dotyczy zwykłych i specjalnych broni oraz innych kupowanych części. Poligon kreatywny pozostaje darmowy. Naprawa, wartość demontażu i fuzja nadal korzystają z dotychczasowych zasad i nie są mnożone przez ×3.
- Unity 6000.3.14f1: **31 PASS, 0 FAIL, 0 SKIP**, kod 0, `C:\Users\kubap\Desktop\Prototypy\shop-pricing-final.xml` oraz `.log`; obejmuje ceny wszystkich podstawowych rodzin, specjalną broń, własny mnożnik i kopię konfiguracji, koszty naprawy i demontażu, budżet oraz fuzję w garażu. Po zmianie cen zwiększono walutę wyłącznie w testach, które potrzebują jej do zakupu części niezależnie od badanego zachowania.
- Vendor z 35 pozostaje do wdrożenia; poniżej opisano częściową obsługę pakietów z 36. Cała ekonomia wymaga późniejszego playtestu. Ręczny Windows Player: **NOT RUN**.

## 31. Koszyki nagród — przelosowanie

- Dodano przycisk przelosowania dostępnych zestawów w obu układach nagród. Koszt jest konfigurowalny, początkowo 10 Z. Wybrany bonus pozostaje, a koszyk trzeba wybrać ponownie. Bez środków, dostępnego koszyka lub rzeczywiście innej oferty waluta nie jest pobierana; sama zmiana kolejności zestawów nie wystarcza.
- Unity 6000.3.14f1: **67 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\reward-reroll-final2.xml` oraz `.log`. Test sterowany API wykonuje walkę, wybiera nagrodę, przelosowuje, sprawdza koszt i brak środków; osobno sprawdza sytuację z tylko jedną dostępną ofertą oraz geometrię obu przycisków. Ręczny Windows Player: **NOT RUN**.
- Sprawny optymalizator odzysku zwiększa liczbę ofert: wstępnie do +1 za jednostkę tieru, konfigurowalne. Wybór nadal obejmuje jeden koszyk; dodatkowe oferty pochodzą z pokonanych przeciwników i muszą różnić się od poprzednich. Przy niewielkiej liczbie łupów może powstać mniej dodatkowych ofert. Oba układy nagród stronicują po trzy pozycje; Q/W/E wybierają widoczną stronę. Przelosowanie wraca na pierwszą stronę i nie zmienia wybranego bonusu.
- Unity 6000.3.14f1: **60 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\basket-extra-final.xml` oraz `.log`. Testy sprawdzają m.in. liczbę ofert z T2, pojedynczy odbiór, brak pozornych dodatkowych ofert dla jednego łupu, wybór klawiszem na drugiej stronie, reroll oraz geometrię dotychczasowych kontrolek. Scenariusz drugiej strony jest sterowany API i wstrzykuje stan modułu w nagrodach; fizyczny zakup, montaż i zniszczenie tego samego typu modułu sprawdzono osobno w pkt 32. Ręczny Windows Player: **NOT RUN**.
- Integracja z przyszłymi fizycznymi łupami ze świata pozostaje do wykonania; cały punkt 31 nie jest jeszcze zamknięty.

## 36. Pakiety części — zakup i istniejący odzysk

- Zwykły sklep przyznaje 5 podstawowych pełnych klocków lub 2 części napędowe za jedną cenę oferty; inne części, w tym kształtowe klocki, pozostają pojedyncze. Ilości są konfigurowalne. Kreatywny warsztat nadal dodaje pojedynczy wybrany element za darmo. W garażu widać liczebność pakietu i wymagane miejsce.
- Istniejący koszyk odzysku przyznaje pakiety części napędowych: oryginał zachowuje swoje ID, nowe egzemplarze dostają własne. Koszyk oraz komunikat o miejscu w magazynie uwzględniają pełną liczbę otrzymanych części. Operacja jest atomowa; montaż, zdejmowanie, fuzja i wczytanie nie mnożą ich ponownie. Bez miejsca na cały pakiet zakup/odbiór nie jest rozliczany.
- Unity 6000.3.14f1: **78 PASS, 0 FAIL** dla fuzji, cen, odzysku i ekranów nagród (`C:\Users\kubap\Desktop\Prototypy\part-bundles-integration-second.xml`); **110 PASS, 0 FAIL** dla dodatkowej regresji pakietów, budżetu, slotów, zapisów i nagród (`part-bundles-critical-first.xml`); **18 PASS, 0 FAIL** dla końcowych testów prawdziwego katalogu i odbioru (`part-bundles-ui-final.xml`). Każdy proces zakończył się kodem 0; logi są obok XML. Test katalogu wymagał poprawki pomocniczego kodu po przeładowaniu domeny Unity i został uruchomiony ponownie.
- Fizyczne podnoszenie nowych łupów w świecie, przyszły vendor i pozostałe nieistniejące źródła nagród wymagają podłączenia do tej samej reguły w swoich zadaniach; dlatego punkt 36 jest **częściowy**. Pełna ręczna gra Windows: **NOT RUN**.

## 32. Optymalizator odzysku — prototyp

- Nowy pasywny moduł jest w katalogu garażu, podlega istniejącym zasadom montażu i rezerwuje 25 energii. Początkowo dodaje 25% złomu za niewybrane części z istniejącego koszyka oraz do jednej oferty na jednostkę tieru; obie wartości są konfigurowalne i jawne w garażu. Kopie i tiery sumują się, a uszkodzony, lecz sprawny moduł działa z pełną siłą. Bonus znika po jego zniszczeniu lub odłączeniu. Zwykły demontaż nie zmienia ceny. Wynik jest zaokrąglany w dół, a wypłata respektuje limit portfela.
- Unity 6000.3.14f1: **34 PASS, 0 FAIL**, kod 0, `C:\Users\kubap\Desktop\Prototypy\salvage-optimizer-final2.xml` oraz `.log`. Testy obejmują podgląd i rzeczywistą wypłatę, kilka kopii, indywidualne uszkodzenie, konfigurowalny bonus, fuzję T2, zapis i odczyt oraz fizyczny montaż i zniszczenie w scenie, sterowane API. Pierwsza próba fizycznego testu wykazała, że pomocnicze `RestoreHealthRatio(0)` ustawia minimum 0,1% HP; ponowiono test, zadając rzeczywiste obrażenia.
- To prototyp wymagający oceny balansu w ręcznej grze. Łupy w świecie i przyszłe rodzaje nagród nie są objęte tą wersją; ręczny Windows Player: **NOT RUN**.

## 29 i 41. Istniejąca obrona i technologia

- **41 — SKIP, działa w obecnym zakresie:** pokonany przeciwnik przekazuje rodzaj swojej broni do trwałego profilu technologii; odblokowanie nie przyznaje egzemplarza ani waluty. Inne aktualne rodziny części są dostępne bez blokady technologicznej, więc nie dodawano im sztucznego systemu odblokowań. Unity: 4 testy modelu oraz 2 testy w scenie **PASS**, kod 0, `C:\Users\kubap\Desktop\Prototypy\backlog-tech-shield-review.xml` i `backlog-tech-shield-gameplay-final.xml`, logi obok.
- **29 — częściowo sprawdzone:** istniejąca tarcza kierunkowa zachowuje własne HP i wyłącza się po zniszczeniu, a obrona punktowa była sprawdzona osobno. Cztery testy istniejącej osłony **PASS**, kod 0, w `backlog-tech-shield-gameplay-final.xml`; nie potwierdzają one odrębnej fizycznej płyty osłonowej ani przyszłej kopuły z pkt 24. Podczas weryfikacji starszy test nadmiernym trafieniem rdzenia niszczył również sąsiednią osłonę przez obecny system propagacji; test ponowiono z kontrolowanym rzeczywistym trafieniem, zachowując weryfikację HP i stanu odzyskanej części. Ręczny Windows Player: **NOT RUN**.
- **33 — WYMAGA DECYZJI:** efekt Cargo Uplink nie został zatwierdzony. **43 — WYMAGA DECYZJI:** szczegóły fizycznych modułów radaru i minimapy nie są ustalone. Nie dodano obu efektów.
