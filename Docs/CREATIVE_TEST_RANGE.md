# Poligon kreatywny

Wejście: menu główne → **Poligon kreatywny — buduj od rdzenia**. Na początku pojazd ma jedynie rdzeń. Wybierz część z katalogu i dodaj ją za darmo; po montażu kolejny egzemplarz tego samego typu jest automatycznie przygotowywany. Bez kosztów dostępne są też moduły bonusowe.

Przycisk **Testuj pojazd** uruchamia kopię na terenie 300 × 300 m, również bez broni i napędu. Jazda nadal wymaga prawdziwego, zamontowanego napędu. Wcześniejsze limity wielkości, liczby kół, modułów, typów broni i magazynu nie obowiązują w trybie kreatywnym; zgodność punktów styku, kolizje i odstępy dział pozostają aktywne. Pierwszy typ broni korzysta z LPM, drugi i kolejne — z PPM. Po powrocie do garażu zachowana zostaje konstrukcja sprzed jazdy, bez jej próbnych uszkodzeń.

Na poligonie naciśnij **Esc → Przeciwnicy — dodaj lub usuń**, wybierz typ, wróć do menu pauzy i wznów grę. Można dodać kolejnych przeciwników lub usunąć wszystkich. Reset próby usuwa ich i odtwarza kopię pojazdu. Zasoby i konstrukcja kreatywna nie są zapisywane jako wyprawa; wyjście do menu kończy sesję.

## Celowana weryfikacja

Unity 6000.3.14f1, testy EditMode z przejściem do Play Mode: 13/13 PASS (5 kreatywnych, 3 dotychczasowego poligonu, 5 grup uzbrojenia), kod procesu 0. Próba fizycznej jazdy bez broni: około 11,7 m, rzeczywiste kontakty R6, syntetyczna klawiatura i kontrolowane wywołanie obsługi wejścia w działającym edytorze bez fokusu. Kontrola obejmuje dodanie przeciwnika w pauzie, wznowienie i usunięcie, powrót do konstrukcji oraz izolację zapisu wyprawy. Wyniki XML i log znajdują się lokalnie w katalogu RoboRougelike-LekkaStylizacja-QA, `Creative-release-tests.xml` i `Creative-release-tests.log`.

Nie wykonano ręcznego testu widocznego Windows Playera ani pełnej regresji. Wcześniejszy raport bazowej regresji zawierał 181 błędów; nie uznano ich za naprawione. Nie przygotowano nowego buildu.
