# B01-10 / PASS 17 — R5WB001: game-format encounter workbench

**NOWY SAMODZIELNY PRODUKCYJNY TOOL, nie druga paczka tych samych starć.**

R5WB001 to działający w przeglądarce offline edytor z prawdziwym zapisem danych **`R5Catalog`** / `R1EncounterDefinition`, które aktualny Codex może przekazać do oryginalnego `R5RunPlan`. Narzędzie **nie zmienia gry**, nie uruchamia walki, nie generuje nowego typu AI i nie dotyka `Assets/**`. Pozwala **tworzyć kolejne nowe konfiguracje**, a nie tylko czytać dziesięć gotowych z poprzedniego BAL17-R5-ENCOUNTER-DECK PR #344.

## Uruchomienie

1. Pobierz z PR **`Research/B01-10/P17Workbench/R5Workbench.html`** i otwórz samodzielnie w przeglądarce. To kompletny, niezależny, pojedynczy plik, zawiera identyczną wersję źródłową `R5WorkbenchCore.js`; **nie potrzebuje serwera, pakietów, CDN ani sieci**.
2. Wybierz etap (1–6) i opcję (1–2). Zmień identyfikator, tytuł, trzy oryginalne areny, skład 1–3 wrogów, dozwolone elity/bossa, opcjonalny `AlternativeRoster`, parametry HP/obrażeń/tempa/ruchu i Złom.
3. Kliknij **Zapisz zmiany starcia**. Silnik sprawdza *cały* katalog przed zatwierdzeniem; błędny scenariusz nie nadpisuje danych. Opcjonalna korekta `ThreatBudget` podnosi limit tylko przy autoryzowanej edycji, a nie zmienia obrażeń gry. Interfejs pokazuje oryginalny wariant `SpawnCount` z eskortą, `RiskName` i porównanie progów zagrożenia oraz nagrody.
4. **Eksportuj aktualny R5 JSON** do pola tekstowego i skopiuj albo **Pobierz gotowy plik .json**. Można również wkleić istniejące `Assets/Resources/R5/Encounters.json` lub poprzedni `Research/B01-10/P17/R5_EncounterDeck_BAL17.json` z PR #344 i kliknąć **Importuj JSON z pola**. Narzędzie go weryfikuje i umożliwia edycję, bez wykonywania zapisów do repozytorium.
5. **Cofnij zmianę** odtwarza jeden ze stanów historii (do 30). **Nowy pusty katalog** tworzy sześć bazowych legalnych etapów; nie jest obietnicą zbalansowanej wyprawy ani zgodności z dodatkowymi testami kampanii wymagającymi konkretnych opcji.

## Uruchom niezależne testy źródłowe

W clone gałęzi producenta z Node.js 22+:

```bash
node Research/B01-10/P17Workbench/workbench.test.cjs
```

Oczekiwany wynik: `BAL17-R5WB001 SOURCE_ONLY 41 PASS / 0 FAIL | NATIVE_UNITY_NOT_RUN`.

Wykonano 41/41 na rzeczywistym silniku JS przez V8 (z rzeczywistym tekstem testu z tool call, przy adapterze modułu Node) i 20/20 kroków UI na oryginalnym JS z opublikowanego `.html` w V8 z deterministycznym adapterem DOM. **W WAVE01 nie uruchomiono prawdziwego procesu Node ani przeglądarki — tę lukę usunięto podczas WAVE02 poniżej, przez rzeczywiste GitHub Actions.** Unity i Windows Player nadal nie były uruchomione. Dodatkowo wczytano bez błędów oryginalny wcześniejszy katalog PR #344: 10/10 ofert. Odbiorca powinien sam uruchomić lokalnie prawdziwy Node i przeglądarkę.

## Zgodność z oryginalnym kodem

- Source pin: Codex Draft [PR #310](https://github.com/C8LyNn/RoboRougelike/pull/310) @ `be2c4b6ac63e04eff5b046e50641ec8d1da4837f`; `Assets/R5/R5RunPlan.cs` Git blob `6fde58f1fc621cde6488d9ee81c3510aa729839a`, `Assets/R1/R1RunDefinition.cs` blob `5665e64d3d665c67d352eb5a9809a66fc5136698`, `Assets/R3/R3EnemyArchetype.cs` blob `66cd1ee734b129117b3753d1205469a0496cb673`.
- Export only original `R5Catalog` > `Stages[6]` > `Options[1..2]` > original `R1EncounterDefinition` fields. No invented `SpawnScript`, reinforcement wave, extra currency, runtime reward action, or pass-through of unknown serialized keys.
- `Threat` independently ports the source mathematical expression including one regular Scout escort per non-boss encounter and the original `IsBoss` branch (no escort). `RiskName` recalculated; limits source variables and enums enforced. Matematyczny wynik nie dowodzi realnej walki, trafień, przejezdności, przeciwnika ze specjalną bronią czy fizyki.
- Jeżeli Codex zaktualizuje `R5RunPlan`, należy porównać Git-bloby i przetestować aktualny serializator Unity, **nie przyjmować za pewnik poprzedniego SHA**.

## Odbiór i rollback

**B01-40**: sprawdź ściśle zakres `Research/B01-10/P17Workbench/**`, oryginalny manifest hash i uruchom Node w izolacji. **B01-60**: niezależnie otwórz HTML, wykonaj import PR #344, zmień etap, dodaj opcję, sprawdź ostrzeżenia, eksport, undo i odporny na błędne JSON import.

**Codex**: po przydzieleniu przez HQ, i tylko w **disposable worktree** aktualnej gry, wykonaj kopię oryginalnego `Assets/Resources/R5/Encounters.json`, podmień jedynie **bajty jego treści** na wyeksportowany katalog, **zachowaj `.meta` i GUID**, użyj `JsonUtility.FromJson<R5Catalog>` / `R5RunPlan.Validate`, a następnie oryginalnych natywnych testów oraz rzeczywistego runu (fizyczna walka, LOS, ekonomia, wybory, brak softlocka). Nie podmieniaj danych w aktywnym drzewie Codexa bez zlecenia. **Rollback**: przywróć poprzednie bajty JSON, nie ruszaj `.meta`, sprawdź `git diff` i usuń wyłącznie pliki tymczasowe.

Dodatkowe istniejące testy R5 mogą wymagać dokładnie dwóch opcji na etapie 2 albo innych założeń kampanii. Pusty sześciostopniowy szablon **nie** jest z automatu produkcyjną kampanią. Każdy finalny eksport wymaga osobnego natywnego odbioru.

**PACKAGE_ID** `R5WB001/P17-R1` · **OWNER** B01-10 · **STATUS** `SOURCE_READY / INDEPENDENT_QA_PENDING / CODEX_ACK_PENDING / UNITY_NOT_RUN`. Brak merge do main/develop, brak zmian w CHL/PARTSIZE/LD02 i innych rolach roju.


## WAVE 02 / realne CI na oryginalnym pliku (PASS 17)

Pierwsze 41/41 testów V8 z adapterem zostały uzupełnione **rzeczywistymi testami natywnych procesów Node 22 na Windows i Ubuntu oraz rzeczywistą przeglądarką Chromium**, zamiast nazywania adaptera DOM testem przeglądarki.

**Potwierdzone GitHub Actions** [#40759517141](https://github.com/C8LyNn/RoboRougelike/actions/runs/40759517141), źródła przetestowane na commicie `5a341ceceedb5f118392298f07f2728ba6e92b00`, workflow zakończony **SUCCESS**:

| Zadanie | Wynik rzeczywisty |
| --- | --- |
| Windows 2025, Node 22, `workbench.test.cjs` | **41 PASS / 0 FAIL** |
| Ubuntu 24.04, Node 22, `workbench.test.cjs` | **41 PASS / 0 FAIL** |
| Ubuntu 24.04, Playwright Chromium, `browser.test.cjs` | **27 PASS / 0 FAIL** (po dziewięć na 1280×720, 1920×1080, 720×900) |

**Szczegółowe dowody:** [`QA_W02.json`](QA_W02.json), oryginalne logi workflow, [GitHub Actions artifact #12847242860](https://github.com/C8LyNn/RoboRougelike/actions/runs/40759517141) — **trzy rzeczywiste PNG i raport `QA_BROWSER.json`**. Browser test sprawdza otwarcie przez `file://`, przełączenie etapu, dodanie alternatywy, zapis tytułu/nagrody, odmowę zmiany spoza zakresu bez korupcji danych, eksport/reset/reimport JSON, odmowę błędnego JSON, rzeczywiste pobranie pliku przez przeglądarkę, cofnięcie, brak nieobsługiwanych błędów JS i brak poziomego przepełnienia widoku.

Nowy własny workflow `.github/workflows/b01-10-p17-r5-workbench.yml` dotyczy tylko tej paczki; GitHub CI wywołany na zmianach tej paczki nie dotyka kanonicznych plików Codexa. Można również odtworzyć lokalnie:
```bash
node Research/B01-10/P17Workbench/workbench.test.cjs
npm install --no-save --no-package-lock playwright@1.56.1
npx playwright install chromium
node Research/B01-10/P17Workbench/browser.test.cjs
```
**Uwaga:** powyższe pobranie Playwright dotyczy wyłącznie środowiska QA; sama aplikacja `R5Workbench.html` nadal działa offline. Po edycji wyłącznie manifestu/README nowy SHA PR będzie inny, ale oryginalne źródła `R5WorkbenchCore.js`, `R5Workbench.html`, test Node, Playwright i workflow pozostają tożsame z przetestowanymi Git blob SHA opisanymi w `QA_W02.json`. Nie utożsamiać tego testu z Unity.

**STAN ODBIORU:** Własne realne source/UI tests **GREEN**; **niezależny B01-40/B01-60 QA PENDING**, Codex ACK PENDING, Unity/Windows Player **NOT_RUN**.


## WAVE 03 — realna poprawka auto ThreatBudget, czerwony → zielony test Chromium

**Pakiet nadal jest tą samą aplikacją R5WB001 w Draft #357, nie nowym konkurencyjnym generatorem ani mechaniką Unity.** Oryginalny formularz zawsze podawał w pakiecie zmian aktualne \`ThreatBudget\`, nawet gdy użytkownik pozostawił ten limit bez zmian i zaznaczył \`Automatycznie podnieś limit zagrożenia\`. Przez to oryginalny silnik nie wykonywał auto-korekty i odrzucał legalne zwiększenie trudności.

W3 fix w \`R5Workbench.html\`: jeżeli checkbox jest aktywny **i** pole limitu odpowiada obecnemu zapisanemu limitowi, formularz nie przekazuje \`ThreatBudget\` jako ręcznej nadpisanej wartości. Funkcja \`R5WorkbenchCore.put\` oblicza wtedy budżet na podstawie faktycznie wybranego składu, mnożników i eskorty. **Gdy użytkownik zmienia limit świadomie**, wartość pozostaje w patch i nie jest po cichu podnoszona; zbyt niska wartość powoduje odmowę zapisu. **Po wyłączeniu checkboxa** każda zmiana przekroczenia limitu jest odrzucana. Nie zmieniono \`R5RunPlan.cs\` ani balansowych zasad gry.

**Prawdziwe testy, oryginalne GitHub Actions:**

| Commit / workflow | Windows Node 22 | Ubuntu Node 22 | Chromium 3 widoki |
| --- | --- | --- | --- |
| RED: \`f35809d2321bc3cbd08f0714c56a125480d5738d\`, [40900414206](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900414206) | źródło 41/41 PASS | źródło 41/41 PASS | **33 PASS / 3 FAIL** — dokładnie auto ThreatBudget w każdym widoku |
| GREEN: \`54270e16726be3b92e9f94978a855a03e28e98ac\`, [40900605642](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900605642) | **41/41 PASS** | **41/41 PASS** | **36/36 PASS**, po 12 realnych interakcji na 1280×720, 1920×1080, 720×900 |

**Pobrane oryginalne artefakty:** GREEN [workflow 40900605642](https://github.com/C8LyNn/RoboRougelike/actions/runs/40900605642), artifact **12909511232**, 3 realne PNG + \`QA_BROWSER.json\`. Zarejestrowano również czerwone błędy i faktyczne logi w [\`QA_W03.json\`](QA_W03.json). Test automatycznej korekty przelicza z limitu 12 rzeczywiste zagrożenie **29.440** przy mocniejszym przeciwniku. Nie ma fałszywego deklarowania, że test odegrał całą walkę. Natywne Unity/Player **NOT_RUN**, niezależny B01-60 nadal **PENDING**.

**Odtwarzalność:**
\`\`\`bash
node Research/B01-10/P17Workbench/workbench.test.cjs
npm install --no-save --no-package-lock playwright@1.56.1
npx playwright install chromium
node Research/B01-10/P17Workbench/browser.test.cjs
\`\`\`

### Copy-ready prośba odbioru przez Codexa, po autoryzacji B01-00/B01-40

> **B01-10/R5WB001 WAVE03**: Nie kopiuj samej aplikacji HTML do gry. Otwórz [R5 Workbench Draft #357](https://github.com/C8LyNn/RoboRougelike/pull/357) tylko jako offline producenta oryginalnego \`R5Catalog\`. Sprawdź manifest i aktualny SHA R5 źródła. Jeżeli HQ zaakceptuje konkretny wyeksportowany katalog gry, przełącz się do odizolowanego worktree aktualnego Codexa, zachowaj oryginalne bajty \`Assets/Resources/R5/Encounters.json\` i nieruszony \`.meta\`. Zamień wyłącznie treść katalogu, uruchom natywny \`JsonUtility.FromJson<R5Catalog>\`, \`R5RunPlan.Validate\` i realne starcia. Zapisz raport XML/Editor.log/Player SHA, zgłoś rzeczywisty stan. Przy odrzuceniu danych przywróć oryginalny JSON byte-for-byte, zachowując GUID. **Nie ma autoryzacji do merge, gameplay tuning lub przejęcia source PR bez osobnego ACK.**

**Właściciel handoffu:** B01-40 (weryfikacja pakietu i G0) → B01-60 (niezależny UI / katalog) → B01-00 (kolejność w #79) → Codex (ewentualny import danych). Do tego momentu status **\`PRODUCER_TESTED_READY / INDEPENDENT_QA_PENDING / CODEX_ACK_PENDING / GAME_NOT_INTEGRATED\`**.
