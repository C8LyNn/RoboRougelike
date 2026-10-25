import json,pathlib
root=pathlib.Path.cwd();out=root/'Docs/B01_RC1';s=json.loads((out/'native-summary.json').read_text())
a,b=s['windows']['FinalMapgen720'],s['windows']['FinalMapgen1080'];base=s['artifacts']['BASE'];exp=s['artifacts']['MAPGEN_EXPERIMENTAL']
f=s['native']['FinalNative'];assert f['EditMode']['passed']==35 and f['PlayMode']['passed']==423
text=f'''# B01 / Integration RC1 — Windows x64

**Dwa kompletne grywalne buildy są dostępne. MAPGEN jest podłączony do rzeczywistej gry w osobnym wariancie eksperymentalnym. G2 pozostaje HOLD; nie wykonano merge ani produkcyjnego wdrożenia.** Wszystkie wyniki poniżej pochodzą z tego cyklu.

## Fundament i dokładne rewizje

| Element | SHA |
|---|---|
| RC1 BASE / OBJ #210 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| PHY #125, już w historii OBJ | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| MAPGEN R3.4.1 / #237 | `5be31039411f89f17654b1c332f8692883fa03cc` |
| B01-21 MAPGENWORLD001 / #243 | `de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7` |
| PACKAGE-AUTO / #238 | `402863ba3c15f515a4bbb0cbc6af99db743c5c32` |
| Import paczek | `5cf2fd81e1b163f5b5463201584180f24df220f3` |
| Pełna regresja przed osłoną API | `8ac3a814b692a380033323b188747fe106314054` |
| Test RED pustego Player | `ebadd88320520dee6506839abd7f66a3f60ba66b` |
| **Końcowy kod gry, buildy i końcowe testy** | **`86eb7bb77e6bfc7787f781ea1ccda547b8a4f643`** |

Gałąź `integration/b01-rc1-experimental`, nowy sandbox `C:\\Users\\kubap\\Desktop\\Prototypy\\RoboRougelike-B01-RC1`, utworzony PACKAGE-AUTO z dokładnego OBJ. [Początkowy stan worktree](initial-worktrees.json). Poprzednie checkouty pozostały nienaruszone. Unity **6000.3.14f1**, URP **17.3.0**, Packages/ProjectSettings/scena/Pack/R3/R6 bez zmian. Późniejszy commit raportu i narzędzi nie zmienia kodu Playerów.

## Co znajduje się we wspólnej wersji

BASE zachowuje istniejący fundament: trzy pojazdy, sześć etapów Gauntletu, garaż/poligon, fizyczne części i bonusy1×2×2, obrót XYZ/punkty styku, obrażenia/odpadanie części, odzysk/naprawy/progresję; grounded R6/Core recovery; wcześniej zintegrowane WPN-001–006, DEF-001/002, COM-001, SEN-001, MOV-011 i wcześniejsze napędy, osłony/zagrożenia/VFX; OBJ-001: reaktor600HP i trzy fale. Nie oznacza to ręcznego przejścia każdej broni w tym cyklu.

EXP dodaje opcjonalną **CombatArena generowaną według seeda**, korzystającą z tego samego spawnera, HP, AI, fizyki, nagród i garażu. Używa oryginalnego `MapGenWorldAdapter` B01-21 i `MapGenBuilder`. Obrys pochodzi z colliderów rzeczywistego pojazdu. Nieobsługiwany napęd/etap z ponad trzema wrogami/zbyt duży obrys nie uruchamia trybu. Pozostałe trasy i reaktor nadal działają. W czasie jazdy nie przypisuje się pozycji ani prędkości Rigidbody; początkowy spawn korzysta z istniejącego przepływu gry.

Zmiany istniejącej gry ograniczają się do trzech plików R1: kontroler wybiera świat i sprząta go, spawner przyjmuje opcjonalne pozycje, HUD udostępnia przyciski. Nowy `R1GeneratedArena` łączy interfejsy; nie tworzy nowego generatora/solvera/ekonomii. Po wygranej, przegranej i restarcie znika mapa, wraca wcześniejszy teren. QA fixture jest wyłączony z dostarczanych buildów retail.

Nie importowano nowych CAMERA, BUILD, RAC ani LEGENDARY. Course pozostaje kodem/testami producenta, **bez osobnej aktywności**. Reaktor nie został umieszczony na mapie MAPGEN. Osłony generatora są statyczne; zniszczalne osłony i strefy zagrożeń pozostają na dotychczasowych arenach.

## Integralność i rollback

[MAPGEN verify](mapgen-source-verification.json): oryginalny ZIP89258B/63wpisy, 62hashe/22GUID, SHA256 `2337cb85c090f194a7e10474e73bf3b6aa69f86eeabc74b3204a59d3a60bc623`. Pełne Unity-owned assets przeszły [preflight](mapgen-target-preflight.json), staging, rzeczywisty rollback36plików i ponowny staging. Parent `Assets/B01Packages.meta` nie istniał w bazie; przyjęto oryginalny GUID, zachowany byte-for-byte podczas importu adaptera.

Adapter: oryginalny ZIP140144B/67plików/14GUID, SHA256 `e5970c8f1f13837f9fe43e3d575c5ff4dc3a0dfbc1eb0174dbf08699aee1a491`; weryfikator producenta faktycznie wykonał `verify`. Jego manifest `files` i celowy brak wspólnego parent meta nie są wprost obsługiwane konfiguracją #238. Nie zmieniano ZIP-a ani narzędzia: zastosowano udokumentowany import tylko owned assets, z dodatkowym porównaniem Git/SHA/GUID, ekskluzywnymi zapisami i jawnym journalem. Rzeczywisty rollback23plików i ponowny staging przeszły.

[Końcowa integralność](source-integrity.json): 35plików MAPGEN+23adaptera+parent meta zgodne bajtowo z pinami, 832GUID bez kolizji, autorytatywne ścieżki niezmienione. [Automatyczny rollback końcowej wersji](rollback-proof-final.txt) w nowym checkoutcie odwrócił pięć commitów i uzyskał **całe drzewo identyczne z BASE**; lokalny HEAD `52e3af002e9de2a1a95f4081df31e161704fab2b`. Nie zaliczano tego jako nowego testu Unity. Oryginalna integracja, archiwa i współdzielone gałęzie pozostały nietknięte.

## Rzeczywiste wyniki

| Etap | Wynik | Dowód |
|---|---|---|
| BASE kompilacja/release Windows | PASS, native0/0, build0błędów/4ostrzeżenia | [Proces](BaseNative/Build-process.json), log.gz obok |
| BASE EditMode celowany | **60/60 PASS**, native0 | [XML](BaseNative/EditMode.xml) |
| BASE pełny PlayMode | **401/401 PASS**, native0 | [XML](BaseNative/PlayMode.xml) |
| Pierwsze testy hosta RED/GREEN |4FAIL brak wejścia, potem10PASS; GREEN był working tree | [RED](HostRed/EditMode.xml), [GREEN](HostGreen/EditMode.xml) |
| Pełna regresja `8ac3a81` przed osłoną API | **1055/1055 Edit+423/423 Play PASS**, native0/0 | [Edit](PreGuardNative/EditMode.xml), [Play](PreGuardNative/PlayMode.xml) |
| Test braku Player RED |1oczekiwany FAIL: false/true, native2 | [XML](GuardRed/EditMode.xml) |
| Końcowa kompilacja i oba buildy `86eb7bb` | **PASS**, native0, release0błędów/4ostrzeżenia, QA0/4 | [Proces](FinalNative/Build-process.json), log.gz obok |
| **Końcowy EditMode: całe MAPGEN+adapter+host** | **35/35 PASS**, native0 | [XML](FinalNative/EditMode.xml) |
| **Końcowy pełny PlayMode** | **423/423 PASS**, native0 | [XML](FinalNative/PlayMode.xml) |
| Końcowy Windows MAPGEN720/1080 | **PASS**, po20kontroli, oba native0 | [720](FinalMapgen720/rc1-native-result.json), [1080](FinalMapgen1080/rc1-native-result.json) |
| Końcowy Windows OBJ/standardowa walka | **PASS**,21różnych nazw kontroli, native0 | [JSON](FinalReactor/obj001-native-result.json) |
| UI retail BASE720/1080 i EXP720/1080 |PASS w opisanym niżej zakresie; UI EXP przed osłoną | Foldery BaseUI/ExperimentalUI |
| Zamknięcia sterowane UI | **FAIL C0000005**, BASE i EXP | process.json w folderach UI |
| PC1/PC2, pełna wyprawa człowieka | **NOT_RUN** | Nie deklarowano PASS |

Przegląd tylko do odczytu znalazł drobny brak: `StartRun(); ChooseGeneratedArena()` mogło mierzyć obrys bez fizycznego Player. Dodany test wykazał błędne dopuszczenie; naprawiono je wyłącznie warunkiem `Player != null`. Końcowe35Edit obejmuje24generatora+6adaptera+5hosta; pełne423Play wykonano ponownie. Nie powtarzano1021niezmienionych legacy Edit po tej izolowanej osłonie. **1055/423 jest wynikiem rewizji przed osłoną, nie końcowego SHA.** Recenzent potwierdził poprawkę i brak nowych blokujących regresji; to nie odbiór B01-60/G2.

Wcześniejszy GREEN ma w oryginalnym process.source HEAD `bc46c8a` plus niezatwierdzony wówczas host w working tree. Zachowano te metadane; nie przedstawia się go jako czystego commita RED ani końcowej akceptacji. Pierwszy szeroki BASE Edit przerwano z powodu błędnego30min watchdoga runnera (wcześniejszy pełny przebieg trwał34,5min): [native-1/brak XML](BaseInterrupted/receiver-interruption.json), **NOT_RUN jako kompletny zestaw**. Runner poprawiono do istniejących90min CI; asercji i limitów symulacji nie zmieniano.

NUnit jest rozdzielone od Windows/API. OBJ zapisuje powtarzane kontrole podczas fal:364wpisy/21różnych nazw, nie364niezależne testy. MAPGEN20obserwacji obejmuje dwukrotną kontrolę shadera. Nie dolicza się ich do NUnit. Sukces wrappera Playera otrzymał jawny exit0 po tym, jak parent błędnie potraktował niezdefiniowany LASTEXITCODE jako porażkę; pierwotny native0/20PASS zachowano i nie powtarzano tej udanej próby.

## Jazda, combat i granice dowodów

Końcowy Heavy ma obrys5,180×2,114×4,920m, początkowo139części/12kół/1176kg. Fixture wyłącza kontroler wejścia i podaje ograniczone żądania siły do **jedynego R6**; Rigidbody/grawitacja/collidery są rzeczywiste. 720p: **{a['netHorizontalMeters']:.3f}m netto XZ**, {a['distance3DIncludingSettling']:.3f}m skumulowane3D;1080p: **{b['netHorizontalMeters']:.3f}m XZ**, {b['distance3DIncludingSettling']:.3f}m3D. 3D zawiera osiadanie po spawnie. Kontakty: {a['supportedSamples']}/{a['contactSamples']} oraz {b['supportedSamples']}/{b['contactSamples']} zapytań z przynajmniej jednym kołem, max12; bez gwarancji ciągłego styku każdego koła.

Realne pociski uszkodziły gracza ({a['playerHpLost']:.2f}/{b['playerHpLost']:.2f}HP) i wroga (40HP). **Następnie wygraną jawnie wywołano obrażeniem Core przez API.** Sprawdzono rzeczywisty salvage,1bonus+część wroga, garaż, naprawę po oddzielnym jawnym urazie fixture2HP, montaż łupu, mapę seed31, kontrolowaną porażkę, restart i zwolnienie materiału. To nie zwycięstwo człowieka. OBJ fixture również ustawia aktorów do kontrolowanego testu AI i niszczy Core; nie służy za dowód jazdy.

UI BASE720: wybórLight, przygotowanie, obrót garażu, reaktor, widoczny strzał LPM/cooldown, realne obrażenia reaktora, pauza, porażka i restart **przyciskiem**. BASE1080: Standard, montaż83→84części/złom600→595, obrótY0→90°, poligon i powrót z zachowaną częścią. EXP720: Light/menu/garaż/mapa seed2026, naturalna utrata uzbrojenia20→14części i restart. EXP1080: Heavy/mapa/LPM/cooldown, utrata koła od AI i pauza. **Te retail UI captures pochodzą z8ac3a81; końcowe86eb7bb ma dodatkowo sprawdzony tylko guard, nowe Windows API/GPU i ponowne uruchomienie finalnego wypakowanego EXE.** Krótkie syntetyczne W/Enter nie potwierdziły raw InputSystem; sprzętowa długa jazda i pełna wyprawa człowieka NOT_RUN.

Zachowano **{sum(x['visualEvidenceValid'] for x in s['screenshots'])} poprawnych rzeczywistych PNG**. UI captures są skalowane DPI z ramką; finalne QA ma natywne1280×720/1920×1080. [Finalna jazda/ogień720](FinalMapgen720/03-real-drive-projectile-damage.png), [regeneracja po nagrodach1080](FinalMapgen1080/06-regenerated31.png). Shader producenta `B01/MAPGEN001/BlocksURP`, bez override; szara podłoga/brązowe osłony bez różowego error shadera. Pierwotne9czarnych PNG ukrytego okna zachowano jako **INVALID dowód renderowania**, wraz z jego nativeC0000005. Widoczne próby po odłączeniu klienta UI mają prawdziwy render i exit0, bez zmiany asercji.

## Stabilność i pozostałe bramki

Sprawdzone przejścia menu/garaż/walka/rewards/kolejna mapa/przegrana/restart oraz poligon nie zawieszały gry. Zaakceptowane scenariusze Windows mają puste listy błędów. Regenerate usuwa poprzedni root, Clear przywraca teren, restart zwalnia materiał.

**C0000005 pozostaje nierozwiązane.** Cztery retail zamknięcia z klientem UI oraz pierwsze ukryte QA zakończyły się-1073741819. Po resecie klienta UI widoczne scenariusze MAPGEN/OBJ w obu kolejnych rewizjach zamykały się native0. To zależność od warunków obserwacji, nie dowód ostatecznej przyczyny Unity/UIA/środowiska lub naprawy gry. W pierwotnym QA zmieniła się też widoczność okna; nie udaję pełnego eksperymentu przyczynowego. Zamknięcie człowieka końcowego EXP bez UIA: NOT_RUN.

Profile PC1/PC2, pełny benchmarkFPS, Course jako aktywność, arbitralne konstrukcje i pełna wyprawa człowieka nie zostały zaliczone. Rzeczywisty host Windows/D3D11 raportuje RTX5080. B01-60 musi niezależnie odebrać integrację; B01-40/B01-00 zatwierdzają handoff/G2. To oraz ograniczenia odbioru ręcznego blokują formalny produkcyjny merge.

## Pliki, hashe i uruchomienie

| Wariant | ZIP | Rozmiar | SHA256 |
|---|---|---|---|
| BASE | `{base['zip']}` |{base['zipBytes']}B |`{base['zipSha256']}` |
| **Końcowy MAPGEN** | `{exp['zip']}` |{exp['zipBytes']}B |`{exp['zipSha256']}` |

Użyj **folderu Final dla EXP**. Wcześniejszy eksperymentalny kandydat8ac3a81 pozostał zachowany w katalogu nadrzędnym. ZIP-y zawierają EXE, UnityPlayer.dll, Data, MonoBleedingEdge i wszystkie pozostałe pliki. Sprawdzono CRC, każdy hash po wypakowaniu i faktyczne uruchomienie aplikacji. [Receipt BASE](RoboRougelike_B01_RC1_BASE_Windows_x64-receipt.json), [receipt finalnego EXP](RoboRougelike_B01_RC1_MAPGEN_EXPERIMENTAL_Windows_x64-receipt.json) podają dokładne EXE i manifesty. Uruchom `RoboRougelike.exe` w odpowiednim `*-VerifiedExtract`, pozostawiając wszystkie foldery/DLL obok.

Wspólny launcher EXE ma SHA256`17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`. Różnią się **rzeczywiste biblioteki i dane**: Assembly-CSharp.dll BASE`{base['gameAssemblySha256']}`, EXP`{exp['gameAssemblySha256']}`. QA fixture nie ma w retail. Wcześniejsze audyty binarne zachowano przed późniejszą przebudową QA; końcowe foldery mają audyty końcowej rewizji.

EXP: wybierz pojazd/seed → szeroki przycisk **RC1 EKSPERYMENT** nad kartami → garaż → **MAPGEN: arena • seed...** nad pojazdem → walka → standardowe nagrody → garaż. Kolejna wygenerowana walka jest dostępna, jeśli etap mieści się w limicie spawnów; pozostałe przechodzą zwykłą trasą. BASE: standardowy Gauntlet/garaż/reaktor. Natychmiastowy rollback użytkowy: uruchom zachowany BASE.

Rollback źródła: `Tools/B01RC1/Rollback-RC1.ps1 -Repo <checkout> -Target <nowy-katalog>` tworzy nowy worktree/reverty, sprawdza zgodność gry z dokładnym BASE i zachowuje oryginał. Nie nadpisuje istniejącego celu. Reprodukcja końcowych testów: `Tools/B01RC1/Run-RC1Native.ps1 -Variant Experimental -Filter 'MapgenRC1IntegrationTests;B01.MAPGEN001.Tests;B01.MAPGENWORLD001.Tests' -FilterEditOnly` z repo root. Omiń filtr, aby uruchomić także niezmienione legacy Edit. Jawne `-rc1-native-output`/`-obj001-native-output` działają wyłącznie w osobnym QA Playerze.

Pełne oryginały logów pozostają w Artifacts/Local. Publiczne `.log.gz` pomijają identyfikatory maszyny/połączeń i końcowe białe znaki; XML/JSON/CSV/PNG są zachowane. [Zbiorczy manifest](native-summary.json) rozdziela rewizje, NUnit, obserwacje Windows, przerwania i artefakty. Końcowe zadanie obejmuje Draft PR i raport HQ; bez merge/G2 i bez następnej paczki.
'''
(out/'RC1_INTEGRATION_QA.md').write_text(text,encoding='utf-8')
print('Final report written from actual validated receipts')
