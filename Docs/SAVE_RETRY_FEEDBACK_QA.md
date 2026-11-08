# Powtarzany błąd zapisu — kontrola lokalna

W oknie aktywności powtórzenie zapisu zakończonego takim samym błędem zastępowało konkretny powód ogólnym komunikatem. Każda próba czyści teraz poprzedni wynik przed sprawdzeniem warunków, a ponownie otrzymany komunikat błędu pozostaje widoczny. Sam format, miejsce i mechanizm zapisu nie zostały zmienione.

Na wcześniejszej skompilowanej wersji funkcja zwróciła ogólny komunikat dla dwóch identycznych błędów — **RED**. Po zmianie lokalna kompilacja Roslyn kodu gry i kodu testów: exit 0/0. Wywołania faktycznie skompilowanego kodu w PowerShell/.NET: **6/6 PASS**, obejmują ponowny i nowy błąd, nieaktualny sukces, brak komunikatu, sukces oraz odrzuconą próbę czyszczącą stary błąd. Ostatnia próba używa niezainicjalizowanego obiektu kontrolera i nie zapisuje plików.

Dowody: `Artifacts/Local/CSharp-SaveRetry-20261108/`. Przygotowano również test Unity dla odrzuconego zapisu oraz przypadek powtarzanego błędu. **Unity, Windows Player, rzeczywisty błąd wejścia/wyjścia i ręczna kontrola UI: NOT RUN**, zgodnie z prośbą o lekkie prace. Wcześniejszy ZIP `c332e6a5` nie zawiera tej poprawki.
