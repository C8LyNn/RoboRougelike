# RoboRougelike — krótki test ręczny, 8 listopada

[Pobierz build Windows x64](https://drive.google.com/file/d/1F2hnYZHJfzfS4cgGi1Ph_TxVmP5mIV22/view?usp=drivesdk). Wypakuj **całe** archiwum do nowego folderu, następnie uruchom `RoboRougelike.exe`. Build `c332e6a5`; ZIP SHA-256 `7c792cf43ed6a2a9eeb54b6bc7fc7c64379b243d3b4f6561ca2d794cd470c286`.

1. **Garaż i części.** Menu → „Garaż i poligon testowy”. Znajdź część w katalogu, kup ją, wybierz egzemplarz i zamontuj. Zdejmij ją i spróbuj naprawić uszkodzony egzemplarz. Czy lista i działania są łatwe do znalezienia, a pojazd wystarczająco widoczny?
2. **Brak broni lub napędu.** Zdejmij wszystkie koła lub ostatnią broń. Czy nadal możesz budować, a gra jasno wyjaśnia blokadę startu? Zamontuj części ponownie i wybierz „Testuj pojazd”.
3. **Jazda i podjazd.** W menu wybierz ciężki pojazd → „Garaż i poligon testowy” → „Testuj pojazd”. Spróbuj wjechać na dużą rampę. W garażu dodaj „Silnik kół” z kategorii modułów i powtórz próbę. W „Ustawieniach” znajdziesz opis wzmocnienia i kosztu energii. Oceń łatwość kierowania, kontakt kół oraz zachowanie kamery przy rampie.
4. **Walka i celowanie.** Menu → „Przygotuj pojazd” → następna trasa. Sprawdź początkowe oddalenie kamery, celowniki i widoczność przeciwnika. Porównaj strzały broni podstawowej i pozostałych broni, poziom energii i jej regenerację. Zwróć uwagę, czy rozbrojony przeciwnik nie blokuje walki w nieskończoność.
5. **Nagrody i powrót.** Po zwycięstwie wybierz bonus i łupy, otwórz magazyn i sprawdź zamknięcie go klawiszem Esc. Zobacz, czy wyniki i droga powrotna do garażu są jasne. Możesz też sprawdzić wybór klawiszami 1–3 oraz Q/W/E.
6. **Zamknięcie i zapis.** Wybierz Esc i sprawdź wznowienie oraz powrót do menu, a potem wyjście z gry. Jeśli chcesz sprawdzić zapis wyprawy, najpierw wykonaj kopię własnego `checkpoint.json`: zapis z garażu zastępuje istniejący slot.

Najbardziej przydatna informacja zwrotna to **miejsce, wykonana czynność, oczekiwany i rzeczywisty efekt**. W przypadku jazdy podaj pojazd, zamontowane silniki i liczbę kół; w przypadku UI — rozdzielczość i czy napis lub przycisk jest ucięty.

Automatycznie sprawdzono kompilację i wypakowanie 212 plików, sterowany test garażu i uruchomienia walki, modele, wybrane funkcje energii i broni oraz izolowany zapis i odczyt w osobnych procesach. Ocena grafiki, komfortu sterowania i pełna ręczna wyprawa pozostają do wykonania. Testy nie potwierdzają naprawy historycznego błędu zamykania `C0000005`.
