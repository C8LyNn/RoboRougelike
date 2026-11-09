# Garaż — ukryte punkty montażu i większa konstrukcja

Baza: 32b2aebd / PR #1185. Garaż nie tworzy już obiektów i colliderów punktów montażu na ścianach komórek, których sąsiednie pole jest zajęte. Zajętość jest wyliczana z tego samego modelu co dostępne pozycje. Po zmianie konstrukcji garaż przebudowuje rejestr, więc zdjęcie części odsłania nowe miejsce; logika połączeń i walidacja nadal są autorytatywne.

Rzeczywiste Unity 6000.3.14f1 EditMode:
- `large_socket_final.xml`: 17/17 PASS, exit 0; scenariusze garażu, broni, kół, kolizji, niedozwolonego montażu i konstrukcji diagnostycznej.
- `large_socket_removal4.xml`: 1/1 PASS, exit 0; zdjęcie pancerza odsłania punkt sąsiedniego klocka. Wcześniejsze próby 1–3 miały błąd stanu testu po przeładowaniu domeny Unity; po wydzieleniu właściwej procedury odtwarzania test przeszedł.
- Struktura diagnostyczna bez napędu ani broni (nie jest grywalnym pojazdem): 81 klocków, 198 znaczników, 117 dostępnych punktów; 324 klocki, 306 znaczników, 225 dostępnych punktów. Przed zmianą 81 klocków tworzyło 486 znaczników; poprzednie wyniki dostępnych punktów dla 81/324 były również 117/225.
- Pojedyncze pomiary dla 324 klocków: tworzenie punktów 35 → 7 ms, zapytanie o dostępne punkty 319 → 317 ms. To pojedyncze pomiary diagnostyczne, nie wyniki profilowania rozgrywki. Przeszukiwanie grafu nadal wymaga optymalizacji przed bezpiecznym zwiększeniem limitu 180 komórek.

Nie zmieniono limitu ani siatki; nie wykonano pełnej regresji, grywalnego testu dużego pojazdu ani buildu Windows.
