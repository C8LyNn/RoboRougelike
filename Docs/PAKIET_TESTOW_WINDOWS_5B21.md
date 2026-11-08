# RoboRougelike — pakiet testów Windows

## Wersja

- Build: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Backlog_5b21ad80_Windows_x64.zip`
- Kod gry: `5b21ad804c84d886084f52dca30c5153a0299842`; późniejszy `b12c7238` zmienia wyłącznie test kamery i raport.
- SHA-256 ZIP: `67F8021BDEEB107396BEDC999074BA442542114E5514C339B5926074AABF9A15`.
- Wypakuj całe archiwum i uruchom `RoboRougelike.exe` z wypakowanego folderu.
- Status: Unity zbudowało Windows x64 bez błędów; ZIP wypakowano i porównano EXE. EXE z wypakowanego archiwum uruchomił się w ukrytym oknie, lecz próba `CloseMainWindow` nie wysłała żądania i proces usunięto po nieudanej próbie normalnego zamknięcia. **Nie jest to PASS wyjścia ani test grywalności.**
- Test garażu opisany niżej wykonano w rzeczywistym Playerze wcześniejszego `1bea5602`; kod garażu jest taki sam, ale końcowy build wymaga ponownej kontroli.

Przed próbami zapisu skopiuj obecny plik `%USERPROFILE%\AppData\LocalLow\DefaultCompany\RoboRougelike\Run\checkpoint.json`, jeśli jest dla Ciebie ważny. Przycisk zapisu może nadpisać istniejący stan. Nie trzeba zapisywać, aby sprawdzić garaż i poligon.

## Kolejność prób

1. **Garaż i komunikaty.** Menu → „Garaż i poligon testowy”. Wybierz „Zdejmij” i zdejmij obie bronie. Oczekiwane: komunikat o wykonanej czynności oraz informacja o braku broni są widoczne jednocześnie; testowanie jest zablokowane. Wybierz broń z listy lub magazynu i zamontuj ją ponownie. Sprawdź, czy łatwo znaleźć część, a komunikaty nie zachodzą na przyciski. Powtórz z wszystkimi zdjętymi kołami. **Windows, finalny ZIP: NOT RUN.**
2. **Pierwsza energia.** Menu → „Przygotuj pojazd” dla Standardowego → poligon → powrót do garażu → zdobądź i zamontuj dodatkową baterię → następna trasa → pierwsza walka. Po jej rozpoczęciu powinna być pełna energia **700/700**, jeżeli pozostałe części startowe nie zostały zmienione. Nowa wyprawa zaczyna się bez złomu: zakup wymaga jego zdobycia, np. przez ostrożny demontaż zbędnych części; nie trzeba tego robić tylko dla tej próby — jeśli brakuje środków, oznacz ją jako niewykonaną. Test Unity dodał złom testowy i przeszedł rzeczywistą drogę poligon → garaż → walka: PASS; ręczna gra w finalnym ZIP-ie: NOT RUN.
3. **Przeciwnik i przeszkody.** Walcz w pobliżu ścian i ramp, obserwuj skręcającego wroga. Sprawdź, czy nie zatrzymuje się mimo wolnej drogi i czy nie przejeżdża przez ściany. Rozbrój przeciwnika i zbliż się: powinien ostrzegać przed wybuchem przez co najmniej 5 s, a następnie wybuchnąć w pobliżu pojazdu. Dla dużego pojazdu sprawdź kontakt jego skrajną częścią. Testy fizycznego zapytania i czasu ostrzeżenia: PASS; bezpośrednia jazda człowieka i zachowanie przy zablokowanym obrocie: NOT RUN. Nie zakładać, że robot uwolni się z korytarza zbyt wąskiego do obrotu.
4. **Kamera i rampa.** Wejdź na poligon, jedź na rampę lekkim, następnie ciężkim pojazdem. Obróć kamerę, przybliż i oddal ją, jedź na szczyt i zjedź. Zapisz, czy skacze, zakrywa pojazd/przeciwnika albo przenika przez teren; sprawdź też, czy ciężki pojazd może wjechać. W kontrolowanej automatycznej próbie na tej samej rampie 18° Light przesunął się o 8,478 m, Heavy o 0,396 m; kamera w próbkach nie przeniknęła przez teren. Samochody ustawiono początkowo na rampie, więc przyczyna różnicy oraz przejezdność od dojazdu wymagają osobnej kontroli.
5. **Zapis i odczyt.** Po zabezpieczeniu poprzedniego pliku rozpocznij wyprawę, zmodyfikuj pojazd, zapisz w garażu, normalnie zamknij grę i uruchom ponownie. Wczytaj wyprawę; porównaj części, ich HP, grupy broni, energię, złom i postęp. Zapis przed pierwszą walką → wczytanie → nowa bateria → pierwsza walka również powinien dać pełne dostępne maksimum. Testy Unity dla zapisu i energii: PASS; pełny ręczny cold restart finalnego ZIP-a: NOT RUN.
6. **Modele i broń.** W garażu obejrzyj dostępne kształty pancerza i rodzaje kół, obróć część przed montażem. W walce sprawdź celowniki, dwa wyloty Automatic i automatyczne przeładowanie rakiet. Siedem kształtów jest dostępnych; nie oznacza to integracji wszystkich 30 modeli z #963. W niektórych pozostałych modelach porty nadal wymagają poprawki producenta.
7. **Różne rozdzielczości i wyjście.** Jeśli wygodne, sprawdź 1280×720 i 1920×1080; pionowe 768×1024 tylko jeśli środowisko na to pozwala. Oceń czytelność komunikatów i przycisków. Wyjdź z menu oraz przez Esc w walce; po zamknięciu uruchom grę ponownie. Nie opieraj oceny zamykania na opisanej wyżej nieudanej próbie ukrytego okna.

## Jak zapisać wynik

Przy każdej usterce podaj numer próby, pojazd, mapę i krótki opis: co zrobiłeś, czego oczekiwałeś i co się stało. Przy kamerze dopisz rodzaj rampy, kierunek jazdy i przybliżenie; przy problemie z przeciwnikiem — czy miał broń i koła oraz czy był przy ścianie. Informacja „działa / przeszkadza” dla każdego punktu wystarczy do ustalenia kolejności kolejnych zmian.

## Dodatkowo w kolejnym buildzie

Zmiana opisów modułów bonusowych powstała **po buildzie 5b21** i nie znajduje się w ZIP-ie wskazanym wyżej. Gdy będzie nowy build, sprawdź uszkodzony bonus w magazynie: jego zakup powinien być opisany jako dostępny tylko z nagród, a garaż powinien wskazywać naprawę posiadanego egzemplarza. To zmiana napisów; sama naprawa i zasady nagród nie były zmieniane.

Na kolejnym buildzie sprawdź też opis baterii: lista grup powinna mówić ogólnie „Bateria”, a poszczególne egzemplarze pokazywać pojemność właściwą dla poziomu: T1 +100, T2 +200, T3 +400. Sama pojemność energii nie była zmieniana.

W następnym buildzie sprawdź również, czy garaż wyjaśnia brak złomu lub pełny magazyn, czy naprawa sprawnej części jest oznaczona jako niepotrzebna oraz czy uszkodzony egzemplarz można znaleźć i naprawić. Porównaj masę i nominalną siłę tego samego koła na różnych poziomach; wartości dotyczą pełnego HP. Zweryfikuj czytelność długich nazw w poziomym i pionowym układzie. Te zmiany nie są w ZIP-ie 5b21.

W kolejnym buildzie sprawdź nową szybką naprawę obok każdej części w poziomym katalogu. Wybierz wrak z zerowym HP: katalog powinien pozostać otwarty, aby można było go naprawić, rozmontować lub wybrać do łączenia. Po naprawie wybór części powinien wrócić do montażu. Sprawdź czytelność długich nazw i ceny w wąskim wierszu. Ta zmiana również nie jest w ZIP-ie 5b21.

Po naprawie kilku egzemplarzy z magazynu sprawdź, czy widok nie przeskakuje, podgląd naprawionej części staje się dostępny, a złom zmniejsza się o podaną cenę tylko raz. Optymalizacja tej ścieżki i jej test Unity są przygotowane, ale jeszcze nieuruchomione; ZIP 5b21 ich nie zawiera.

Sprawdź też rozmontowanie egzemplarza: pierwsze kliknięcie ma poprosić o potwierdzenie bez zmiany złomu, drugie usunąć właściwą część i raz przyznać wskazaną kwotę. Widok pojazdu nie powinien się przebudowywać. Ta optymalizacja również oczekuje testów Unity i nie występuje w ZIP-ie 5b21.

Na kolejnym buildzie rozpocznij potwierdzenie rozmontowania, ale zamknij katalog, zmień grupę lub napraw część. Po powrocie pierwsze kliknięcie rozmontowania powinno ponownie prosić o potwierdzenie. Ta poprawka również nie występuje w ZIP-ie 5b21.

Kod gry i kod testów do zmian garażu skompilowano lokalnie w C# na stanie `f62ce4f9`; testy Unity nie zostały uruchomione. Szczegóły i ograniczenia tej kontroli: `Docs/GARAGE_LOCAL_CSHARP_COMPILE_QA.md`.

W kolejnym buildzie sprawdź też magazyn dostępny z ekranu nagród: sprawny egzemplarz powinien wskazywać, że naprawa jest niepotrzebna, a uszkodzony — jej rzeczywisty koszt; amunicja pozostaje widoczna. Dotyczy obu układów ekranu. Kompilacja lokalna C# kodu gry i testów: exit 0; testy Unity oraz ręczne sprawdzenie interfejsu: NOT RUN. ZIP 5b21 nie zawiera tej zmiany.

W informacjach garażu o napędzie kołowym sprawdź komunikat o aktualnym wzmocnieniu silników, jego limicie oraz koszcie energii. Sprawny silnik T1 daje nominalnie +20% siły i rezerwuje 25 maksymalnej energii; łączny bonus siły ma limit +100%. To opis istniejącej mechaniki, a nie zmiana fizyki. Czytelność w oknie informacji i rzeczywista jazda po dołożeniu silnika wymagają sprawdzenia w nowym buildzie; ZIP 5b21 tego opisu nie zawiera.

**Do sprawdzenia z właścicielem przy balansie:** pierwotna instrukcja mówiła o 25 energii na moduł pasywny; późniejszy backlog opisuje wyższe koszty mocniejszych poziomów jako kierunek do testów, bez zatwierdzenia konkretnych wartości. Aktualna gra nalicza T1/T2/T3 odpowiednio 25/30/35 za sprawny moduł pasywny — zarówno w modelu pojazdu, jak i w działającym banku energii. Istniejące testy źródłowe oczekują 30 i 35; ich obecność nie potwierdza akceptacji balansu przez gracza. Nie zmieniono naliczania. W nowym buildzie zamontuj osobno silnik lub regenerator każdego poziomu i porównaj spadek maksimum energii z opisem w garażu; test ręczny i testy Unity dla tych zmian pozostają NOT RUN.

W katalogu garażu użyj „Akcje” przy sprawnym egzemplarzu: katalog ma zostać otwarty, a łączenie i rozmontowanie mają dotyczyć tej konkretnej części. Bezpośredni wybór do montażu nadal powinien zamykać katalog. Sprawdź oba układy i czytelność opisów; szczegóły w `Docs/GARAGE_CATALOG_ITEM_ACTIONS_QA.md`. Test Unity i Windows: NOT RUN; ZIP 5b21 nie zawiera tej zmiany.

W kolejnym buildzie wybierz w garażu „Zaznacz” i wskaż sprawny, a następnie uszkodzony pancerz lub inne części pojazdu. Panel powinien pokazać nazwę, HP i koszt naprawy wskazanej części zamiast poprzedniego wyboru z magazynu. Sprawdź też wskazanie koła w obu układach ekranu. Przygotowany test formatu informacji, test Unity, wizualna kontrola i ręczne kliknięcia: NOT RUN. Kod gry i testów skompilowano lokalnie w C# (exit 0/0); ta zmiana nie jest w ZIP-ie 5b21.

Ten sam podgląd powinien pojawić się również przed kliknięciem części w trybach „Napraw” i „Zdejmij”, aby można było sprawdzić jej stan. Zamiana wyglądu koła pozostaje dostępna tylko przy „Zaznacz”. Oba przypadki wymagają ręcznej kontroli w nowym buildzie.

Sprawdź stos zawierający wyłącznie zniszczone egzemplarze: lista części powinna zachować jego łączną liczbę i dopisać „napraw”. Po naprawieniu jednego egzemplarza oznaczenie powinno zniknąć. Pusty stos nie powinien sugerować naprawy; przy niedostępnej technologii nadal pokazuje blokadę. Przygotowane przypadki testowe Unity i rzeczywisty wygląd etykiet pozostają NOT RUN; starszy ZIP nie zawiera zmiany.

Jeżeli masz bonusy tego samego rodzaju na różnych poziomach, sprawdź ich stos: nazwa grupy nie powinna sugerować poziomu pierwszego egzemplarza, a poziomy poszczególnych części powinny pozostać widoczne w ich szczegółach. Test Unity i ręczna kontrola obu układów: NOT RUN; ZIP 5b21 nie zawiera tej zmiany.

W ustawieniach garażu sprawdź naprawę zbiorczą oraz, jeśli jest dostępna, odbudowę awaryjną. Wynik i faktyczny koszt powinny pojawić się w otwartym oknie; po jego zamknięciu i ponownym otwarciu stary wynik nie powinien wyglądać na nową transakcję. Przygotowany test Unity i sprawdzenie obu układów: NOT RUN. ZIP 5b21 nie zawiera tej zmiany.

Na ekranie wyboru pojazdu opis liczby w m/s mówi teraz „Limit napędu”: jest to wyliczona wartość bazowa, nie pomiar prędkości osiągniętej na każdej nawierzchni. Sprawdź czytelność podpisu w obu układach; renderowanie i ręczna gra: NOT RUN. ZIP 5b21 nie zawiera tej zmiany.
