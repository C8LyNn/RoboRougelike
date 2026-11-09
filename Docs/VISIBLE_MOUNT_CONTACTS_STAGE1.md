# Widoczne punkty styku — pierwszy etap

Gałąź opiera się na poprawkach garażu z PR #1152. Siatka pozostaje w skali 0,55 m.

## Działanie

- Wybór części w trybie montażu pokazuje niebieskozielone punkty na zarejestrowanych gniazdach. Uwzględniane są żywe elementy należące do pojazdu, ścieżka do rdzenia, wolna komórka, kierunek i zgodność punktów.
- Punkt jest wskazówką zgodnego styku, a nie gwarancją montażu: miejsce, kolizje, ekonomię i strefy broni sprawdza dotychczasowy walidator. Po wskazaniu rzeczywisty wynik pokazuje zielone lub czerwone widmo.
- Obszar kliknięcia i znacznik mają szerokość 0,18 m. Przyciąganie w interfejsie przyjmuje tylko gniazdo z tej samej listy, z której powstają znaczniki; nie dodano równoległego systemu fizyki.
- Lista jest zapamiętywana do zmiany części, obrotu albo przebudowy pojazdu.

## Celowana weryfikacja

Unity 6000.3.14f1, tryb wsadowy: 16/16 testów EditMode PASS, obejmujących widoczność przed wskazaniem, dopasowanie obrotu broni, niezgodny boczny styk, kliknięcie i montaż, klin, koło oraz istniejącą nakładkę. Osobny test ciężkiego pojazdu PASS: 46 punktów, pierwsze obliczenie 24 ms w tym środowisku; kolejne odczyty używają tej samej listy.

Próba testu symulującego fizyczne odłączenie elementu zakończyła się NullReferenceException w kodzie testu; przyczyna nie została ustalona. Usunięto niesprawny eksperyment testowy i zachowano wynik jako NIEPOTWIERDZONE. Kod filtruje elementy pozostające poza pojazdem oraz korzysta z istniejącego grafu drogi do rdzenia, ale tego scenariusza nie uznano za przetestowany.

Nie wykonywano ręcznej kontroli w Windows Playerze ani nowego buildu. Poprzednie pełne testy EditMode na bazie miały 36 błędów; nie wykonano ponownej pełnej regresji.

## Kolejny etap

Powiększanie części nie jest wdrożone. Obecny model podstawowej broni w manifeście deklaruje pojedynczą komórkę, a model pojazdu rezerwuje jedną komórkę na część. Wielokomórkowe warianty wymagają danych obrysu i punktów dla konkretnego modelu, atomowej kontroli kolizji oraz jawnej zgodności zapisów. Wizualne powiększenie bez dopasowania fizyki i punktów jest niewystarczające.

Rozprowadzanie obrażeń przez połączenia pozostaje poza tą zmianą i oczekuje na zapowiedziany raport. Punkty nie otrzymały własnych HP.
