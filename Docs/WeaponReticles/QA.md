# Celownik dla każdej broni

Wersja do samodzielnego testowania. Kod powstał na bazie PR #300 (`7cfd1276d4708395144668249223d78fb83c3b19`); źródło końcowego buildu i testów to `6cfdb55a20ac6d40c39cc2bcca69f74ec0232cfc`. Wersja Unity i parametry broni pozostają bez zmian.

W walce i na poligonie zachowano centralny celownik. Każda zamontowana, sprawna broń ma własny numerowany znacznik; numery odpowiadają kolejności w panelu uzbrojenia. Znaczniki pokazują rzeczywiste ustawienie luf po ich istniejącym obrocie w stronę punktu wyznaczonego przez kamerę. Gdy lufy celują w ten sam punkt, znaczniki zbiegają się i pozostają rozróżnialne jako koncentryczne narożniki. Pomarańczowy kolor oznacza ograniczenie: przeszkodę, zasięg lub widok poza ekranem; szary oznacza zakłóconą broń. Znaczniki są usuwane po zniszczeniu lub odłączeniu części.

Znaczniki są wskazaniem kierunku i pierwszego kontaktu, a nie gwarancją trafienia: rozrzut, prędkość pojazdu, ruch celu i penetracja mogą zmienić wynik. Własne pociski są pomijane w podglądzie, aby nie powodować skakania celownika przy strzelaniu. Rakiety uwzględniają również fizyczne wraki, od których zwykłe pociski mają inne zasady kolizji. EMP działa obszarowo, a piła w miejscu kontaktu; ich wskaźniki nie udają dalekiego pocisku.

## Wyniki

| Próba | Wynik |
| --- | --- |
| Natywna kompilacja Unity 6000.3.14f1 | PASS, proces 0 |
| Windows x64 build | PASS, proces 0 |
| EditMode, celowniki i oryginalne testy kamery | PASS 28/28, w tym 6 nowych testów celowników, proces 0 |
| PlayMode, oryginalne testy kamery | PASS 15/15, proces 0 |
| ZIP i wypakowane pliki | PASS: kontrola CRC archiwum oraz hash każdego pliku względem wyniku Unity |
| Windows Player i ręczna ocena HUD | NOT_RUN: zgodnie z prośbą użytkownika gra nie została otwarta, a mysz nie była przejmowana |

Testy nowych znaczników korzystają z rzeczywistych komponentów Unity, kamer, zamontowanych części i colliderów w bezokienkowym EditMode. Obejmują oddzielne znaczniki dwóch luf, ich zbieżność, usunięcie po utracie części, przeszkodę i limit zasięgu, broń obszarową/kontaktową, widok poza ekranem, przeliczanie współrzędnych dla 720p, 1080p i szerokiego ekranu oraz kolizje rakiet z wrakiem. To nie jest test rzeczywistej rozgrywki ani wizualna akceptacja interfejsu.

[Natywne XML, kody procesów i logi](Evidence/summary.json) zachowują także wcześniejsze czerwone próby. Pierwsza wersja testu rakiety traktowała własny pocisk jak przeszkodę; później doprecyzowano, że znacznik powinien unikać migotania od własnego ostrzału, a trwały wrak musi być wykrywany. Następny test odtworzył brak wykrywania wraku i przeszedł po poprawce. Logi są skompresowane i pozbawione identyfikatorów licencji/maszyny; lokalne oryginały mają zapisane SHA-256.

Poprzednie ograniczenia nie zostały ponownie zakwalifikowane jako naprawione: pełny EditMode wcześniejszego buildu #300 miał 1222/1223, a siedem z 15 prób automatycznego przejazdu nie ukończyło trasy. Odbijanie kół, zgłoszone przez właściciela, wymaga oddzielnego zbadania. Nie zmieniano fizyki pojazdu.

## Uruchomienie i próba

Uruchom `RoboRougelike.exe`, wybierz **Przygotuj pojazd**, zamontuj w garażu kilka broni — obecny limit to cztery — i wybierz **Testuj na poligonie**. Poruszaj myszą: numerowane znaczniki będą podążać za lufami i dążyć do centralnego celownika. Porównaj numery z listą po prawej, sprawdź cel za przeszkodą i różne odległości, a następnie usuń lub uszkodź część.

ZIP i jego SHA-256 oraz ścieżka wypakowanego EXE są w [weryfikacji wszystkich bajtów](delivery.json).
