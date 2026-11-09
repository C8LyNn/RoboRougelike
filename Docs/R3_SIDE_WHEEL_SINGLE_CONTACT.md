# Jeden styk bocznego koła

Przed montażem boczne koło może zostać dopasowane do lewej lub prawej strony. Nowo zamontowane ma jeden styk osi, skierowany do zgodnego, sprawnego wspornika; ten sam kierunek określa istniejący wybór lustrzanego modelu. Demontaż przywraca obie opcje w magazynie, a ponowny montaż wybiera stronę na nowo. Dotyczy także zakupu bezpośredniego. Koła widelcowe zachowują swój górny styk. Starsze zapisane pojazdy z dwiema stronami nie są automatycznie reinterpretowane; zapis nowego koła zachowuje jeden styk i ID.

Jeżeli po obu stronach nowego koła znajdują się zgodne wsporniki, wybór jest deterministyczny i zgodny z kierunkiem modelu, ale nie jest jeszcze jawnie wybierany przez kliknięcie konkretnego wspornika. Pełne powiązanie podglądu wskazanego portu z zapisaną stroną wymaga dalszej pracy. Obrys 3×3 dużych bocznych kół nie uwzględnia jeszcze całego wystawania bardzo szerokiej opony w osi X; fizyczna kontrola kolizji pozostaje osobną ochroną.

Unity 6000.3.14f1: pierwsze uruchomienie 47/48, jedyne niepowodzenie oczekiwało dawnych dwóch styków. Asercję dostosowano do nowego wymagania. Ponowny celowany zestaw: **18/18 PASS**, `wheel_socket_green.xml`, exit 0. Sprawdza montaż, zapis/odczyt, demontaż, montaż z drugiej strony, warianty i model. Windows Player, osobny PlayMode i pełna regresja: NOT RUN.

Podgląd w garażu używa teraz tej samej decyzji o pojedynczym styku co montaż, więc zielone oznaczenie nie pokazuje drugiej, nieużywanej strony. Celowany test prawdziwego garażu przed poprawką wykazał tę rozbieżność; po poprawce **8/8 PASS**, `wheel_preview_green.xml`, exit 0. Nie jest to jeszcze swobodny wybór między dwoma wspornikami przez kliknięcie.
