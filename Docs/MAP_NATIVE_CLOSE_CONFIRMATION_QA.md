# Ostrzeżenie przy zamykaniu niezapisanej mapy

Baza `c0f76b6d` (Draft PR #1150). W Windows Playerze `Application.wantsToQuit` sprawdza niezapisaną mapę przed zamknięciem X. Skrót Alt+F4 przechodzi przez ten sam warunek. Przy niezapisanych zmianach gra pokazuje ostrzeżenie i pauzę; kolejne potwierdzenie dotyczy tylko tej samej wersji mapy. Powtórne zdarzenie w tej samej klatce nie potwierdza wyjścia. Gdy mapa jest zapisana albo edytor map nie jest aktywny, zachowano dotychczasową ścieżkę zamykania. Nie dodano wymuszania zakończenia procesu.

Unity 6000.3.14f1, batch/nographics, testy EditMode z UnityTest wchodzącym do Play Mode:

- Przed implementacją nowy test **0/1 FAIL**, exit 2: brak procedury sprawdzającej zamknięcie. [XML](QA/MapNativeQuit-20261108/Before.xml), [proces](QA/MapNativeQuit-20261108/Before-process.json).
- Po zmianie **7/7 PASS**, exit 0: nowy test, dotychczasowe potwierdzenia map i testy pauzy. [XML](QA/MapNativeQuit-20261108/After.xml), [proces](QA/MapNativeQuit-20261108/After-process.json).
- Po doprecyzowaniu komunikatu o możliwości ponownego zamknięcia okna celowany przypadek: **1/1 PASS**, exit 0. [XML](QA/MapNativeQuit-20261108/Message-retest.xml), [proces](QA/MapNativeQuit-20261108/Message-retest-process.json).
- Po usunięciu drugiej ścieżki Alt+F4 dla Windows Playera celowane testy logiki w edytorze: **5/5 PASS**, exit 0. Kompilacja i wywołanie warunkowej ścieżki Windows Playera pozostają NOT RUN. [XML](QA/MapNativeQuit-20261108/Win-routing-retest.xml), [proces](QA/MapNativeQuit-20261108/Win-routing-retest-process.json).
- Wyjście poza pauzą, w tym przycisk ekranu błędu, używa tej samej ochrony. Nowy test przed zmianą wykazał brak widocznego ostrzeżenia: **0/1 FAIL**; po zmianie trzy scenariusze przeszły **3/3 PASS**, exit 0. [Przed](QA/MapNativeQuit-20261108/Outside-pause-before.xml), [po](QA/MapNativeQuit-20261108/Outside-pause-after.xml). Wygląd ekranu błędu pozostaje NOT RUN.

Test wywołuje decyzję o zamknięciu bezpośrednio w edytorze Unity; **nie symuluje rzeczywistego X ani Alt+F4 w Windows Playerze**, a rejestracja systemowego zdarzenia jest wyłączona w edytorze. Test Playera, wizualne potwierdzenie ostrzeżenia i pełny zestaw PlayMode pozostają NOT RUN. Nie przygotowano nowego buildu. Pełne Editor.log pozostały lokalnie w `Artifacts/Local/QA-MapNativeQuit-Red-20261108/` i `Artifacts/Local/QA-MapNativeQuit-Green-20261108/`.

W Windows Playerze nie obsługujemy Alt+F4 drugi raz w Input System: decyzja korzysta wyłącznie z systemowego żądania zamknięcia. Ochrona w kodzie odrzuca też zduplikowane żądania w jednej klatce. Rzeczywiste dostarczenie i ewentualne powtórzenia zdarzeń systemowych nadal wymagają próby Playera; nie należy jeszcze uznawać ochrony Alt+F4 za potwierdzoną.

Do ręcznego sprawdzenia na jednorazowej mapie: zmienić mapę, użyć X, anulować przez wznowienie, ponowić X i potwierdzić; powtórzyć z Alt+F4 oraz z zapisaną mapą i poza edytorem. Zweryfikować także zamykanie podczas testu mapy i w garażu edytora.
