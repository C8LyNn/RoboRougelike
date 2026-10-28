# B01-10 — BAL17-R5-ENCOUNTER-DECK v1.0

**PASS 17 · Game Design & Balance · NOWA ZAWARTOŚĆ GRY · producer B01-10**

To pakiet **10 nowych rzeczywistych konfiguracji starć** dla sześciu etapów wyprawy. Ma dokładnie strukturę bieżącego `R5Catalog`/`R5RunPlan` z Codex #310, a nie autorski, nieobsługiwany format. Nie zmienia systemu walki, poziomów LD02, zapisu runu, nagród ani kanonicznego `Assets/Resources/R5/Encounters.json`.

## Zawartość produkcyjna

- `R5_EncounterDeck_BAL17.json` — **gotowy katalog R5**: jeden wariant rozgrzewkowy, cztery etapy z dwoma wyborami trasy/ryzyka/nagrody, jeden finałowy boss. Używa oryginalnych `R5EnemyVariant`, `R3EnemyArchetype`, `R5ArenaKind`, `EnemyCount`, `ScrapReward`, `HealthMultiplier`, `DamageMultiplier`, `FireIntervalMultiplier`, `ThreatBudget`, `RewardJitter` i opcjonalnych `AlternativeRoster`. Wszystkie identyfikatory zaczynają się od `bal17_`.
- `BAL17_ThreatPreview.csv` — 10 faktycznie obliczonych z katalogu podsumowań `R5RunPlan.Threat` (przeniesiona statyczna formuła z oryginalnego źródła) oraz przedziałów gwarantowanej nagrody. **Nie są to pomiary z Unity**.
- `validate_deck.cjs` — wykonywalny walidator i testy negatywne (Node 22+, bez zależności). Kontroluje m.in. dokładną liczbę etapów, legalność enums, elit, limitów, eskort, alternatywnych składów, nazw ryzyka oraz rozłączne przedziały nagród obu tras.
- `B0110Bal17R5EncounterDeckNativeTests.cs` — 9 **napisanych, nieuruchomionych** przypadków NUnit Editor dla prawdziwego `JsonUtility.FromJson<R5Catalog>` i `new R5RunPlan` na różnych seedach. Umieszczenie w `Research/**` nie aktywuje ich w Unity.

## Rzeczywiste decyzje gracza

| Etap | Opcje | Istota decyzji |
| --- | --- | --- |
| 1 | Wrota złomowiska | Zwiadowca + standardowa eskorta, niski próg wejścia |
| 2 | Mobilna eskorta / Ochrona konwoju | Mobilna flanka lub cięższy przeciwnik za wyższą, niepokrywającą się nagrodę |
| 3 | Bieg przez pustkowia / Ogień z dwóch wież | Ruchliwi przeciwnicy albo ostrzał z dystansu; wzrost stałej nagrody |
| 4 | Elita: Natarcie / Elita: Bastion ogniowy | Oryginalna elita Bruiser albo EliteGunner (bez nowego AI) |
| 5 | Dwa skrzydła / Przełamanie stalowej linii | Czterech fizycznych przeciwników wraz z eskortą, różna presja i budżet |
| 6 | Rdzeń Stalowego Monolitu | Oryginalna flaga `SiegeBoss` z jednym bossem, bez eskorty |

W etapach 2–5 górny możliwy reward mniej płatnej trasy jest ściśle niższy od minimalnego reward bardziej płatnej, nawet z `RewardJitter`. Zasada nagród jest nadal oryginalna; nie powstaje nowa waluta ani przedmiot. Nowością są wyłącznie konfigurowalne wybory wyprawy, składy, mnożniki i przedziały nagród.

## Test lokalny (bez instalowania w grze)

Na gałęzi paczki:

```bash
node Research/B01-10/P17/validate_deck.cjs
```

Oczekiwany rezultat: `22/22 SOURCE_JS_PASS`, zero FAIL, wydruk 10 ofert. Program sprawdza Git-bloby oryginalnych plików C#. Ponieważ bazowa gałąź `main` ma **inny/starszy blob `Assets/R5/R5RunPlan.cs`** niż Codex, lokalny skrypt wypisze `SOURCE_PIN_NOT_CURRENT`, co **nie jest** potwierdzeniem importu w Unity. Na checkoutcie opartym o dokładny kod Codexa #310 można włączyć rygorystyczną weryfikację:

```bash
node Research/B01-10/P17/validate_deck.cjs --strict-codex-source
```

Podczas tego PASS wykonano **rzeczywisty kod walidatora** w V8 z bezpiecznymi adapterami modułów: **22/22 PASS**. Nie wykonano samodzielnie CLI procesu Node na dysku ani prawdziwego C# i NUnit/Unity. Oryginalne źródło porównania: Codex #310 `682dc0c5c13615577334375ffb6c27be5cdebf4a`, Git blob `R5RunPlan.cs=6fde58f1fc621cde6488d9ee81c3510aa729839a`; `R3EnemyArchetype.cs=66cd1ee734b129117b3753d1205469a0496cb673`.

## Bezpieczny odbiór przez Codexa

1. B01-40: sprawdź katalog, manifest i SHA plików na wskazanej **zamrożonej wersji PR**; B01-60: uruchom niezależnie `node .../validate_deck.cjs`. Pakiet nie jest `CODEX_CLAIMED` bez jawnego potwierdzenia.
2. Codex: **wyłącznie w osobnym, tymczasowym worktree** na aktualnej gałęzi gry, zachowaj kopię starego `Assets/Resources/R5/Encounters.json` i podmień jego **treść** treścią oryginalnego `Research/B01-10/P17/R5_EncounterDeck_BAL17.json`. **Nie** usuwaj ani nie regeneruj istniejącego `.meta`, aby nie zmieniać GUID.
3. Uruchom `R5RunPlan.Validate` i natywne testy `B0110Bal17R5EncounterDeckNativeTests` po ręcznym skopiowaniu pliku C# do autoryzowanej, tymczasowej Editor-only assembly testowej. Wyeksportuj oryginalny `NUnit XML`, `Editor.log`, kod wyjścia i SHA aktualnego kodu. Następnie oddzielnie sprawdź rzeczywiste starcia w Windows Player; pełny 6-etapowy run, nagrody, LOS, przestrzeń spawn, garaż, softlock i zapisy pozostają **NOT_RUN**, nie wynikają z samego importu.
4. Przeprowadź weryfikację ergonomii wyboru oraz rzeczywistych trudności i dopiero po zgodzie HQ zdecyduj, czy balans pasuje do kanonicznej kampanii. Wartości poniżej są danymi **kandydata**, a nie zmianą zasad gry.
5. **Rollback:** przywróć oryginalną treść `Assets/Resources/R5/Encounters.json` w tymczasowym worktree, skasuj wyłącznie kopię testu, zachowaj `.meta`, sprawdź `git diff` i porównaj Git-blob przed/po. Główne `main/develop`, aktywny worktree Codexa i paczki CHL/PARTSIZE pozostały nietknięte.

## Własność i ograniczenia

`Research/B01-10/P17/**` = wyłącznie B01-10. **Żaden** plik `Assets/**`, `Resources/R5`, `MAPGEN`, `R1RunController`, `R1RunProgress`, `R5RunPlan` ani `B01-20/21/24/25` nie został zmodyfikowany. R5 w oryginalnym managerze dodaje jednego zwykłego eskortującego (poza bossem); przy trzech bazowych wrogach może skonfigurować osłony, rakiety, EMP albo broń specjalną. Formuła `Threat` nie uwzględnia wszystkich fizycznych skutków tego wyposażenia. Wymagane realne testy balansu i fizyki.

**Wersja:** `BAL17-R5-ENCOUNTER-DECK v1.0` · **OWNER:** B01-10 · **STATUS:** game-format content / source-only tests PASS / original C# + Unity + Windows Player NOT_RUN / Codex acceptance PENDING. 
