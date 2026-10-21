# GITHUB MIGRATION AND CI REPORT

2026-10-21, Europe/Warsaw. **MIGRACJA GOTOWA. UNITY W CHMURZE: BLOCKED / NOT RUN (aktywacja).**

## A. Wybór projektu

Wybrano **Prototypy/RoboRougelike-MOV001-009**. Osobna kopia migracyjna: **Prototypy/RoboRougelike-GitHub**. Prywatne ścieżki konta systemowego pozostają lokalnie. [Porównanie 12 snapshotów](MIGRATION_SOURCE_SELECTION.md) obejmuje pięć głównych kopii i siedem historycznych backupów, zawartość Assets/Packages/ProjectSettings oraz dowody testów/buildów. Wszystkie używają **Unity 6000.3.14f1 (d68c3f99a318)**; URP 17.3.0.

MOV001–009 miało zakończoną integrację i 522/522 testów; świeża kopia potwierdziła ten wynik. Starsze R6 miało 96 testów, MOV009 160; POSTR6-Multi ma identyczne 335 plików source/config jak MOV009 i nie zawiera nowych integracji. Wyboru dokonano przez zawartość i QA, nie datę katalogu.

Zachowano sześć spotkań Gauntlet, garaż 3D, budowę pojazdu, salvage/inwentarz, zniszczalne moduły, istniejące bronie/AI/elity/bossa oraz Booster, JumpDrive, Blink, Gyro, Omni, MagneticGrapple, Hover, Tracks i ActiveSuspension. **Nie importowano MAP-SYSTEM R3–R6, VFX-001 v1–v5, WPN001/002, DEF001 ani MOV010/011.**

Wszystkie **627** plików source/config, w tym **336 `.meta`**, zachowano; nie zmieniono GUID-ów, runtime C#, asmdefów, scen, manifestu/locka ani wersji silnika. [Manifest SHA256](MIGRATION_SOURCE_MANIFEST.json) opisuje publikowane źródła. Trzy różnice prywatności/infrastruktury względem oryginału: ProjectSettings.asset — puste cloudProjectId/organizationId; Layout.wlt — usunięta ścieżka autora szablonu; UnityConnectSettings.asset — Unity automatycznie zmienił m_Enabled z 1 na 0 po odłączeniu konta. Zachowano ten przetestowany stan. Oryginalna kopia pozostała bajtowo niezmieniona; żadnej kopii nie usunięto.

## B. GitHub

Repozytorium **PUBLICZNE**: [C8LyNn/RoboRougelike](https://github.com/C8LyNn/RoboRougelike). Remote: `https://github.com/C8LyNn/RoboRougelike.git`.

| Referencja publikacji zweryfikowanej gry | SHA |
|---|---|
| Pierwszy commit | `2e3f213f82480b65494a143f27c0b3b51b127871` |
| main po pierwszym pushu | `2e3f213f82480b65494a143f27c0b3b51b127871` |
| develop utworzony z main i wypchnięty | `2e3f213f82480b65494a143f27c0b3b51b127871` |

Zamykający commit dokumentacji zachowuje identyczne źródła i CI. Końcowe referencje są dostępne przez [main](https://github.com/C8LyNn/RoboRougelike/commits/main) i [develop](https://github.com/C8LyNn/RoboRougelike/commits/develop); dokładne SHA po ostatnim pushu zapisano lokalnie w `Artifacts/Migration/publication-receipt.json` i podano w końcowej odpowiedzi. Commit nie może zawierać własnego hasha; powyższy stały SHA identyfikuje bazę gry sprawdzoną pełnym QA.

Operacje: przed Git sprawdzono brak otwartej instancji Unity/niezapisanej sceny, zapisano snapshoty hashów i utworzono osobną kopię. Audyt przed historią; git init -b main, origin, publiczny autor noreply, commit. Ponownie sprawdzono pusty remote; push main, branch develop main, push develop. Następnie commit i zwykły push dokumentacji do obu gałęzi. Bez reset --hard, clean -fdx, force push, kasowania referencji, pustych feature branches lub merge przyszłych funkcji. Świeży klon z GitHuba przeszedł walidację i 16 testów narzędzi.

Pierwszy commit: **648 plików, ok. 2.17 MB**. Najważniejsze pliki: Assets + meta; Packages/manifest.json i packages-lock.json; ProjectSettings; README; .gitignore Unity 6; .gitattributes zachowujące dokładne bajty; cztery workflow; Tools/CI/Run-Unity.ps1; walidatory i fixture tests; Docs/DEVELOPMENT_WORKFLOW.md, CI_SETUP.md, noty licencyjne i dowody migracji. Raport oraz snapshot Actions dochodzą w commicie dokumentacji.

## C. Bezpieczeństwo publikacji

Sprawdzono wszystkie publikowane pliki: sygnatury tokenów GitHub/OpenAI/AWS/Slack, kluczy prywatnych, przypisań sekretów, .env/.ulf/.alf/keystore, lokalnych home paths, prywatnych e-maili i identyfikatorów. **Brak sekretów lub niedozwolonych binariów w publikowanym zestawie.** Korekty prywatności wykonano przed pierwszym commitem; nic tajnego nie trafiło tymczasowo do historii. Git używał zapisanej autoryzacji bez publikacji tokenu/hasła. Niezależny przegląd nie wykazał Important/Critical usterek.

Nie znaleziono komercyjnych modeli, audio, paczek Asset Store ani zamkniętych DLL. Geometria i materiały powstają z istniejących skryptów/prymitywów Unity. Jedyny PNG (URP.png, 24069 B) odpowiada bajtowo zainstalowanemu szablonowi URP; zachowano jego oryginalną notę Unity Companion License. Oficjalne pakiety UPM pobierane są z rejestru, z własnymi licencjami. [Pochodzenie i warunki](THIRD_PARTY_NOTICES.md). Nie nadano nowej licencji open source autorskiej grze.

Wykluczono Library, Temp, Obj, Logs, UserSettings, Build/Builds, MemoryCaptures, IDE/cache, pliki aktywacji/sekretów, stare lokalne raporty/logi/backupy/ZIP-y, dane Google Drive i materiały nowych niezintegrowanych paczek. Surowe nowe XML/logi/buildy zachowano lokalnie w ignorowanych katalogach; publiczny skrót ma hashe dowodów. Największy plik: scena **647260 B**. **LFS niepotrzebny** — nie włączono LFS ani płatnych zasobów. Brak blokera publikacji; odrębny bloker CI dotyczy aktywacji silnika.

## D. Rzeczywisty stan Actions

Poniższe runy rzeczywiście wykonano dla pierwszego commita; sprawdzono statusy, kroki i logi. CI-01: 16 fixture tests PASS, 648 śledzonych plików, 0 błędów. CI-02/03/04: preflight zakończony kodem 1, kroki Unity skipped, **brak wykonanych testów/builda w chmurze**. Artefakty activation-status.txt istnieją (potwierdzono listy artefaktów develop). To jawny BLOCKED, nie wynik testów gry.

| Workflow | Gałąź | Wynik | Unity | Link |
|---|---|---|---|---|
| CI-01 Repository Validation | develop | PASS | nie dotyczy | [run 39653931616](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653931616) |
| CI-02 Unity EditMode | develop | FAIL preflight / BLOCKED | NOT RUN | [run 39653931919](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653931919) |
| CI-03 Unity PlayMode | develop | FAIL preflight / BLOCKED | NOT RUN | [run 39653931694](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653931694) |
| CI-04 Windows Build Validation | develop | FAIL preflight / BLOCKED | NOT RUN | [run 39653931565](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653931565) |
| CI-01 Repository Validation | main | PASS | nie dotyczy | [run 39653930146](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653930146) |
| CI-02 Unity EditMode | main | FAIL preflight / BLOCKED | NOT RUN | [run 39653930187](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653930187) |
| CI-03 Unity PlayMode | main | FAIL preflight / BLOCKED | NOT RUN | [run 39653930188](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653930188) |
| CI-04 Windows Build Validation | main | FAIL preflight / BLOCKED | NOT RUN | [run 39653929969](https://github.com/C8LyNn/RoboRougelike/actions/runs/39653929969) |

CI-01 działa bez Unity na push main/develop i PR. CI-02/03 uruchamiają niezależne pełne zestawy, przypinają 6000.3.14f1 i wymagają rzeczywiście udanego procesu oraz niepustego XML, gdzie każdy przypadek Passed. Nie wycięto testów fizyki, nie ma continue-on-error. CI-04 przygotowuje Windows x64 Mono przez cross-kompilację GameCI na Linux; nie uruchamia Windows playera ani nie publikuje release. Akcje przypięto do SHA; test runner CLI v0.1.72, coverage wyłączone, artefakty 7 dni.

Wymagane sekrety: **UNITY_EMAIL**, **UNITY_PASSWORD** i właściwy wariant **UNITY_LICENSE** (Personal, jeśli wspierany dla Twojej licencji/Unity 6) lub **UNITY_SERIAL** (Pro). Settings → Secrets and variables → Actions. GITHUB_TOKEN jest automatyczny; workflow nie wymaga własnego PAT. Nie kopiowano lokalnej licencji do Git/artefaktów i nie obchodzono aktywacji. Instrukcje i oficjalne źródła: [CI_SETUP](CI_SETUP.md).

Zgodność fizyki z Linux GitHub-hosted pozostaje **niepotwierdzona**, dopóki testy nie zostaną rzeczywiście wykonane po aktywacji. Testy matematyki/headless Rigidbody/raycast/input fixtures są kandydatami do Linux; nie oznacza to PASS. D3D12, natywne okno i wizualna jakość wymagają Windows. Brak natywnych pluginów platformowych. Jeśli aktywacja/obraz/moduł Windows Mono uniemożliwia chmurę, użyj lokalnego skryptu Windows 11. Nie podłączono prywatnego stałego runnera do publicznych PR; forki nie dostają sekretów. Standardowe publiczne runnery są bezpłatne według aktualnej dokumentacji GitHub; nie zmieniono ustawień rozliczeń.

## E. Świeże lokalne Unity QA

Windows 11, zainstalowany Unity 6000.3.14f1 z Windows Build Support. Wykonano `Tools/CI/Run-Unity.ps1 -Task All` na świeżej kopii, bez przenoszenia Library. Import i kompilacja poprawne. Główną scenę Assets/scene 1.unity sprawdzono przez pełny zestaw integracyjny i build; SHA256 nadal `f2d5ec5c0d3481285e2a10711e539c92c8e64405b0e2585578ebcb78a209c9d5`. SampleScene pozostaje zachowanym szablonem.

| Kontrola | Wykonano | PASS | FAIL | Skipped | Kod procesu |
|---|---:|---:|---:|---:|---:|
| EditMode | 346 | 346 | 0 | 0 | 0 |
| PlayMode | 176 | 176 | 0 | 0 | 0 |
| Walidatory Python | 16 | 16 | 0 | 0 | 0 |

QA: **17:55:30–18:20:53 CEST**. Przypadki EditMode: 17:57:25–18:17:58, PlayMode: 18:18:13–18:18:45. Pełne nazwy przypadków odpowiadają poprzedniej zweryfikowanej bazie MOV001–009. Zachowano wszystkie 96 R6 i 160 MOV009 — są podzbiorem 522, bez podwójnego liczenia. Nie wykryto regresji w wykonanych testach.

Windows retail: **Succeeded, 102817044 B**, 0 błędów, 4 istniejące ostrzeżenia o nieużywanych polach. Osobny QA: **Succeeded, 102826935 B**, 0 błędów, 4 ostrzeżenia. Oba mają poprawny PE AMD64, native build process exit 0. QA używa osobnego define i katalogu. Nie publikowano żadnego buildu/release. Nowego ręcznego/obrazowego QA playera podczas migracji nie wykonano; wcześniejszy visual QA służył wyborowi bazy.

Surowe dowody lokalnie: `Artifacts/Local/20261021T155530Z-c5a84f984ec043d4a864c94d574f1e88/` — EditMode.xml/log/exitcode, PlayMode.xml/log/exitcode, Build.log/exitcode i znaczniki czasu procesu. [Przenośny skrót z hashami i porównaniem regresji](Verification/Migration/local-qa-summary.json). Unikalny katalog nie pozwala pomylić starego XML z nową próbą. Świeży klon z GitHuba ponownie przeszedł walidator struktury i 16 testów narzędzi.

## F. Następne kroki

Gotowe: kompletny publiczny projekt, stabilne main/develop, lokalne testy/buildy, cztery workflow i dokumentacja współpracy ChatGPT/Codex. Wszystkie oryginały zachowane, brak nowego gameplayu w migracji.

Interwencja właściciela: skonfiguruj wspieraną aktywację Unity CI, uruchom osobno CI-02/03/04 na develop, pobierz log/XML i potwierdź rzeczywiste wykonanie. Zielony CI-01 ani lokalny Windows PASS nie oznaczają PASS fizyki Linux.

Pierwszy przyszły PR: wybierz funkcję; `git switch develop`, `git pull --ff-only`, `git switch -c feature/opis`; implementacja, testy, audyt nowych zasobów; push gałęzi i PR do develop. MAP/VFX wymagają osobnego zakresu. Bez automatycznych merge do main, zmiany Unity lub publikacji release. [Praca równoległa i zapobieganie konfliktom](DEVELOPMENT_WORKFLOW.md).
