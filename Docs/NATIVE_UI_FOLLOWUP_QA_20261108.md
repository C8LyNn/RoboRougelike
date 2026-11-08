# Celowane QA Unity — garaż i aktywności, 8 listopada 2026

Gra: baza `801a8c91889c6f256cb7b3edb80f36276887288b` (PR #1149). Poprawka wyłącznie testu: `5d971b83f28587356cfc3b348b1520aa00af2f01`. Testy 5 i 31 przypadków wykonano na tym SHA; pierwsze 12 po poprawce pliku testowego w drzewie roboczym 801a, identycznej z poprawką w 5d971b83. Unity 6000.3.14f1, batchmode, nographics, Windows; bez sterowania komputerem użytkownika.

| Zestaw | Wynik | Kod procesu | Dowód |
| --- | --- | --- | --- |
| Komunikaty uruchamiania, nagrody, elementy UI | 12/12 PASS | 0 | [XML](QA/NativeUI-20261108/Targeted-retest.xml), [proces](QA/NativeUI-20261108/Targeted-retest-process.json) |
| Wyścig, trasa, porażka, nagroda | 5/5 PASS | 0 | [XML](QA/NativeUI-20261108/Activity-flows.xml), [proces](QA/NativeUI-20261108/Activity-flows-process.json) |
| Katalog i narzędzia garażu | 31/31 PASS | 0 | [XML](QA/NativeUI-20261108/Garage-targeted.xml), [proces](QA/NativeUI-20261108/Garage-targeted-process.json) |

Łącznie 48 przypadków przeszło. To wybrane testy **EditMode**, nie pełny zestaw PlayMode; niektóre testy UnityTest wchodzą w Play Mode. Przepływy aktywności uruchamiają rzeczywiste obiekty Unity, ale zwycięstwo, porażka i inne zdarzenia są wywoływane przez API testowe. Nie zastępuje to rozgrywki człowieka ani testu Windows Playera. Garaż sprawdzono pod kątem wyszukiwania, posiadanych części i wraków, zakupów, napraw, wyboru i skrótów w granicach przypadków zapisanych w XML. Nie oznacza to automatycznego zaliczenia wszystkich testów z kolejki.

Pierwszy właściwy przebieg zwrócił 11/12 i kod 2: `FailedActivityLaunchDisplaysCurrentReasonWithoutReusingOldMessage` zgłosił NullReferenceException po przejściu do Play Mode. Przyczyną była przechwycona przez lambdę zmienna testowa, której obiekt nie zachował się po przeładowaniu domeny. Poprawka używa bieżącej instancji kontrolera; zachowano asercje i nie zmieniono logiki gry. [Pierwotny XML](QA/NativeUI-20261108/Initial-failed.xml), [proces](QA/NativeUI-20261108/Initial-failed-process.json).

Wcześniejsza próba w sandboxie zakończyła się przed testami awarią edytora po przekroczeniu czasu inicjalizacji licencji, kod `0xE0000001`; XML nie powstał. [Dane procesu](QA/NativeUI-20261108/Sandbox-crash-process.json). Kolejne uruchomienia w środowisku z dostępem do lokalnej usługi licencyjnej zakończyły testy. Logi zawierają lokalne dane środowiska i pozostają w `Artifacts/Local/QA-LastUiTasks-801a8c91-20261108/`, `QA-LastUiTasks-FixtureFix-20261108/`, `QA-ActivityRewardFlows-5d971b83-20261108/` i `QA-GarageTargeted-5d971b83-20261108/`. Unity Test Framework zapisał też własny `TestResults.xml` w lokalnym katalogu danych aplikacji; wybrane testy nie zapisywały checkpointu gracza.

## Pozostałe ograniczenia i kontrola ręczna

- **NOT RUN:** Windows Player, ręczna jazda i sterowanie, wygląd i czytelność ekranów, zrzuty, pełna osobna sesja PlayMode. Nie przygotowano buildu: użytkownik poprosił o build dopiero na osobne żądanie.
- Pozostają niewykonane pozostałe przypadki z [kolejki QA](PENDING_TARGETED_QA_20261108.md), w szczególności dotyczące zapisu mapy, broni, tarczy, energii i ostrzeżeń konstrukcji, o ile nie są wymienione w powyższych XML.
- Gdy będzie dostępny odpowiedni build i użytkownik zechce przeprowadzić próbę: w garażu sprawdzić wyszukiwanie bez polskich znaków, filtr posiadanych części i wraki, zakup pięciu sztuk, koszt naprawy grupowej oraz skróty narzędzi; następnie sprawdzić czytelność komunikatu blokady aktywności.
- Rozegrać wyścig lub trasę i sprawdzić ekran zwycięstwa bez łupów oraz porażkę i powrót do menu. Ocenić rzeczywisty układ i działanie myszy oraz klawiatury przy 1280×720 i 1920×1080. Nie modyfikować istniejących zapisów użytkownika na potrzeby prób bez zapewnienia ich izolacji.
