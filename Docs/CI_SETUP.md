# CI i lokalny fallback

## Workflow

| ID | Wyzwalanie | Środowisko | Dowód powodzenia |
|---|---|---|---|
| CI-01 Repository Validation | push main/develop, PR do main/develop, ręcznie | ubuntu-24.04 + Python 3 | testy walidatorów, kompletna struktura/GUID/meta/config Git index |
| CI-02 Unity EditMode | push main/develop, PR, ręcznie | GameCI Linux Editor 6000.3.14f1 | rzeczywisty udany proces oraz niepusty XML, każdy przypadek Passed |
| CI-03 Unity PlayMode | niezależnie: push main/develop, PR, ręcznie | GameCI Linux Editor 6000.3.14f1 | analogiczny XML i log PlayMode |
| CI-04 Windows Build Validation | push main/develop, ręcznie | GameCI Linux → Windows x64 Mono | sukces buildera i niepusty EXE; brak publikacji release |

Actions są przypięte do commitów. Test runner używa CLI v0.1.72 i wyłączonej instrumentacji coverage, aby nie zmieniać istniejącego projektu ani nie dodawać pakietów. Artefakty logów/XML mają retencję 7 dni; cache Library nie jest publikowany. Uprawnienia tylko `contents: read`; raportowanie przez artefakty bez dodatkowego zapisu Checks API. Token checkoutu nie pozostaje na dysku.

## Aktywacja — wymagana przed testami chmurowymi

Repozytorium → Settings → Secrets and variables → Actions → New repository secret. Dodaj **UNITY_EMAIL**, **UNITY_PASSWORD** i jeden właściwy wariant: **UNITY_LICENSE** (treść legalnie uzyskanego `.ulf`, jeśli obsługiwany dla Twojej licencji/Unity 6) albo **UNITY_SERIAL** (ważna licencja Pro). Nie dodawaj obu wariantów bez potrzeby. `GITHUB_TOKEN` jest automatyczny i nie trzeba tworzyć PAT do workflow.

Postępuj według [GameCI Activation](https://game.ci/docs/github/activation/) i warunków własnej licencji Unity. Aktywacja lokalna w Hub nie dowodzi, że licencja nadaje się do kontenera. Jeśli Personal w Twoim środowisku nie udostępnia wspieranej aktywacji, pozostaw chmurę BLOCKED i korzystaj z lokalnego runnera PowerShell; nie obchodź licencjonowania i nie kopiuj licencji z cudzej maszyny. Nie umieszczaj `.ulf`, haseł, ALF ani tokenów w Git lub artefaktach. Nie trzeba ujawniać danych logowania agentowi.

Brak danych daje czerwony, jawny preflight **BLOCKED / Unity NOT RUN** i artefakt `activation-status.txt`. Sam fakt obecności danych nie oznacza aktywacji — GameCI musi jeszcze skutecznie uruchomić Unity. Nie ustawiono `continue-on-error`. PR z forka nie otrzymuje sekretów; CI-01 nadal działa, zadania Unity kończą preflight bez wykonania. Zmiany od nieznanych autorów przejrzyj przed dopuszczeniem zaufanego uruchomienia. Brak `pull_request_target` i stałych lokalnych runnerów.

## Zgodność i ograniczenia

Testy matematyki/konfiguracji oraz headless Rigidbody, kolizje i raycasty nie wymagają obrazu i są kandydatami do GameCI Linux. Istniejące EditMode zawiera testy wchodzące w Play Mode, długie próby AI i fizyki; limit joba to 90 min. PlayMode ma osobne 45 min. Nie włączono filtrów wycinających testy. Nazwy niektórych assembly z MOV005/MOV006 zawierają EditMode, lecz Unity klasyfikuje je jako PlayMode; wyników nie przenosimy ręcznie między grupami.

Fizyka i czas wykonania mogą się różnić między Windows i Linux; do pierwszego rzeczywistego zielonego runu Linux zgodność pozostaje **niepotwierdzona**. Grafika/D3D12, obsługa natywnego okna i wizualna jakość wymagają testów Windows. CI-03 to testy PlayMode edytora, nie testy zbudowanego Windows playera.

Projekt używa Mono, więc [GameCI Builder](https://game.ci/docs/github/builder/) może cross-kompilować Windows x64 na standardowym runnerze Linux z modułem Windows Mono. CI-04 nie uruchamia Windows EXE na Linux. W razie braku obrazu 6000.3.14f1/modułu lub ograniczenia aktywacji użyj lokalnego Windows 11 i `Tools/CI/Run-Unity.ps1 -Task Build`. Przyszłe IL2CPP wymaga osobnego hosta/toolchainu Windows; nie zmieniono backendu na potrzeby migracji.

Standardowe GitHub-hosted runnery dla repozytoriów publicznych są [bezpłatne według aktualnej dokumentacji](https://docs.github.com/en/billing/concepts/product-billing/github-actions). Nie skonfigurowano płatnych większych runnerów ani LFS. Największy migrowany plik ma 647260 B, więc LFS nie jest potrzebny. Licencja silnika pozostaje osobnym wymogiem; ustawienia rozliczeń konta nie zostały zmienione.

Po dodaniu sekretów wybierz Actions → odpowiedni workflow → Run workflow → develop. Każdy tryb można uruchomić osobno. Pobierz log i XML; sprawdź liczbę wykonanych testów, a nie wyłącznie kolor joba. Aktualny, rzeczywisty stan migracji i linki do runów: [raport](GITHUB_MIGRATION_AND_CI_REPORT.md).
