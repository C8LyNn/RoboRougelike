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
4. **Kamera i rampa.** Wejdź na poligon, jedź na rampę lekkim, następnie ciężkim pojazdem. Obróć kamerę, przybliż i oddal ją, jedź na szczyt i zjedź. Zapisz, czy skacze, zakrywa pojazd/przeciwnika albo przenika przez teren. Jedna automatyczna próba Light na rampie 18° przejechała 8,478 m bez wykrytego przenikania; nie dowodzi komfortu ani działania na pozostałych rampach.
5. **Zapis i odczyt.** Po zabezpieczeniu poprzedniego pliku rozpocznij wyprawę, zmodyfikuj pojazd, zapisz w garażu, normalnie zamknij grę i uruchom ponownie. Wczytaj wyprawę; porównaj części, ich HP, grupy broni, energię, złom i postęp. Zapis przed pierwszą walką → wczytanie → nowa bateria → pierwsza walka również powinien dać pełne dostępne maksimum. Testy Unity dla zapisu i energii: PASS; pełny ręczny cold restart finalnego ZIP-a: NOT RUN.
6. **Modele i broń.** W garażu obejrzyj dostępne kształty pancerza i rodzaje kół, obróć część przed montażem. W walce sprawdź celowniki, dwa wyloty Automatic i automatyczne przeładowanie rakiet. Siedem kształtów jest dostępnych; nie oznacza to integracji wszystkich 30 modeli z #963. W niektórych pozostałych modelach porty nadal wymagają poprawki producenta.
7. **Różne rozdzielczości i wyjście.** Jeśli wygodne, sprawdź 1280×720 i 1920×1080; pionowe 768×1024 tylko jeśli środowisko na to pozwala. Oceń czytelność komunikatów i przycisków. Wyjdź z menu oraz przez Esc w walce; po zamknięciu uruchom grę ponownie. Nie opieraj oceny zamykania na opisanej wyżej nieudanej próbie ukrytego okna.

## Jak zapisać wynik

Przy każdej usterce podaj numer próby, pojazd, mapę i krótki opis: co zrobiłeś, czego oczekiwałeś i co się stało. Przy kamerze dopisz rodzaj rampy, kierunek jazdy i przybliżenie; przy problemie z przeciwnikiem — czy miał broń i koła oraz czy był przy ścianie. Informacja „działa / przeszkadza” dla każdego punktu wystarczy do ustalenia kolejności kolejnych zmian.
