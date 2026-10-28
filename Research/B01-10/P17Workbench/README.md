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

Wykonano 41/41 na rzeczywistym silniku JS przez V8 (z rzeczywistym tekstem testu z tool call, przy adapterze modułu Node) i 20/20 kroków UI na oryginalnym JS z opublikowanego `.html` w V8 z deterministycznym adapterem DOM. **Nie uruchomiono procesu Node na checkoutcie, rzeczywistej przeglądarki, Unity ani Windows Player.** Dodatkowo wczytano bez błędów oryginalny wcześniejszy katalog PR #344: 10/10 ofert. Odbiorca powinien sam uruchomić lokalnie prawdziwy Node i przeglądarkę.

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
