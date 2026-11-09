# Garaż — powód zablokowanego startu

W przypadku kilku problemów jednocześnie uproszczony garaż wybierał komunikat o wyposażeniu przed właściwą pierwszą przyczyną z `DeployError`. W API-kontrolowanym stanie: odłączony blok oraz brak broni dawały „Brak sprawnej broni”, mimo że walidacja startu odrzucała odłączoną konstrukcję.

Garaż wyświetla teraz pierwszą przyczynę z istniejącej walidacji; zachowuje łączną informację, gdy właściwą przyczyną jest brak broni, a napędu również brakuje. Nie zmieniono zasad startu, edycji, połączeń ani broni.

Unity 6000.3.14f1 EditMode, testy sterowane API z wejściem do Play Mode: `garage_reason_red.xml` 1/1 FAIL przed zmianą; `garage_reason_final2.xml` 4/4 PASS po zmianie, exit 0. Obejmuje również limit dwóch typów broni, brak broni i napędu oraz zachowanie komunikatu ostatniej akcji. Pełna regresja, Player i ręczny test NOT RUN; nie wykonano nowego buildu.
