# Praca nad RoboRougelike

Silnik pozostaje Unity 6000.3.14f1. `main` przechowuje zweryfikowane wydania, `develop` służy integracji. Nie tworzymy pustych gałęzi na przyszłość.

## Gałąź i PR

```bash
git switch develop
git pull --ff-only origin develop
git switch -c feature/opis-zmiany
# implementacja, testy, przegląd git diff
git add <konkretne-pliki>
git commit -m "feat: opis zmiany"
git push -u origin feature/opis-zmiany
```

Na GitHub otwórz Pull Request **feature/... → develop**. Opisz problem, zachowanie po zmianie, zakres zmienionych scen/assetów, faktycznie uruchomione testy i ograniczenia. Sprawdź zakładkę Checks. Integracja do `main` wymaga osobnego zaakceptowanego PR z `develop` i aktualnych wyników QA; agent nie scala ani nie wydaje gry samoczynnie.

Po scaleniu zaakceptowanego PR: zamknij Unity dla danego checkoutu, zapisz własną pracę i wykonaj `git switch develop`, `git pull --ff-only origin develop`. Jeśli pull zgłasza rozbieżność, zatrzymaj aktualizację i przeanalizuj historię; nie używaj force push, reset --hard ani clean -fdx.

## Lokalne sprawdzenie

Aktywuj Unity w Unity Hub i zamknij edytor tej kopii. W PowerShell, z katalogu repozytorium:

```powershell
python -m unittest discover -s Tools/CI/tests -v
python Tools/CI/validate_repository.py
.\Tools\CI\Run-Unity.ps1 -Task EditMode
.\Tools\CI\Run-Unity.ps1 -Task PlayMode
.\Tools\CI\Run-Unity.ps1 -Task Build
```

Jeżeli Unity/Python są w innym miejscu, przekaż `-UnityPath <ścieżka-do-Unity.exe>` i `-Python <ścieżka-do-python.exe>`. Używaj dokładnie 6000.3.14f1. Wyniki mają osobny identyfikator uruchomienia, XML, log i kod procesu. Brak XML, zero przypadków, pominięty/nieudany test lub niezerowy kod kończą polecenie błędem. `Build` buduje grę i oddzielny wariant QA istniejącą metodą; nie publikuje release. Otwórz główną scenę i sprawdź zachowanie w Play po zmianach gameplayu.

## GitHub Actions

Na stronie repozytorium → Actions wybierz CI-01/02/03/04, uruchomienie i job. Czerwony preflight aktywacji oznacza **Unity NOT RUN**, a nie regresję gameplayu. Po właściwym wykonaniu pobierz artefakt XML/logów. Zielony CI-01 potwierdza strukturę repozytorium, nie fizykę. Konfigurację i ograniczenia opisuje [CI_SETUP](CI_SETUP.md). Nie zastępuj brakujących wyników starym XML.

## Równoległa praca ChatGPT i Codex

Każde zadanie ma własną gałąź i osobny checkout/worktree; koordynator przydziela właściciela plików. Nie uruchamiaj dwóch edytorów dla jednego projektu ani nie zapisuj Assets, gdy drugi agent wykonuje testy w tej kopii. Zmiany scen, prefabów, InputSystem i ProjectSettings rezerwuj dla jednego zadania naraz. Nigdy nie kopiuj Library pomiędzy różnymi aktywnymi checkoutami.

Przenoś asset razem z jego `.meta`; zachowuj GUID. Do scalania YAML scen używaj narzędzia UnityYAMLMerge dostarczonego z tym samym edytorem i ręcznie sprawdź scenę. Przy konflikcie nie regeneruj meta i nie wybieraj masowo jednej strony. W PR podaj bazowy SHA, listę własnych plików i testy, aby kolejny agent wiedział, jaki stan został sprawdzony.

## Granice akceptacji

Bez osobnej zgody: brak zmiany wersji Unity, usuwania/odbudowy scen lub GUID-ów, destructive Git, force push, publikacji release/buildów, dodawania płatnych zależności, sekretów, zmiany licencji lub automatycznego merge do main. Nie importuj MAP-SYSTEM R3–R6 ani VFX-001 v1–v5 w ramach migracji. Następna funkcja wymaga osobnej gałęzi, weryfikacji licencji i PR. Nie podłączaj publicznych PR do stałego runnera na prywatnym komputerze.
