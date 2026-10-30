# B01-24 — SQUAD-PRESSURE-001 v1.0.0 / PASS17 WAVE06

**Dostarczony moduł mechaniki AI:** oryginalny, wykonywalny **C# 9** `SquadPressureScheduler` z izolowanymi danymi scenariuszy, poprawnym `.meta` Unity i sprawdzanym źródłem oryginalnego R3 Codexa. To scheduler **okien akcji specjalnej** oddziału, a nie drugi spawner, system wystrzału, sterowanie pojazdem, wybór celu, przepisanie R5, ani drugi runtime AI. W źródłach istniejącego Codexa `R3EnemyArchetype` ma wartości `Scout, Bruiser, Gunner, Flanker`; wszystkie cztery są faktycznie wspierane.

## Realny, gotowy do wykonania kod

- [`SquadPressure.cs`](SquadPressure.cs): prawdziwy C# 9, bez zależności od UnityEngine, w przestrzeni nazw `RoboRougelike.B0124.SquadPressure`; źródło można skompilować razem z *niezmienionym* `Assets/R3/R3EnemyArchetype.cs`.
- [`SquadPressure.cs.meta`](SquadPressure.cs.meta): oryginalny, stabilny GUID Unity. Tylko Codex / upoważniony odbiorca może skopiować oba pliki do izolowanego `Assets/B0124SquadPressure/`.
- [`scenarios.json`](scenarios.json): 3 autorskie, deterministyczne i wykonywane scenariusze oraz jawne **prowizoryczne** parametry polityki; zawiera pinned Git blob źródła Codexa. Nie jest JSON kompatybilnym `Unity JsonUtility` configiem i nie należy automatycznie ładować go do gry.
- [`Program.cs`](Program.cs): prawdziwy wykonywalny runner C#, *nie* symulacja napisana w JS/Python. `SquadPressureCI.csproj` kompiluje także oryginalny plik Codexa, łącznie z jego enum i `R3EnemyProfiles.InEncounter`. Weryfikowany jest fizyczny Git blob SHA-1 oryginalnego R3 `66cd1ee734b129117b3753d1205469a0496cb673` na docelowym gameplay `203a0805251ed28293888e43f2d49f0df63767c6`.

## Zachowanie mechaniki, dokładne API

```csharp
using RoboRougelike.R3;
using RoboRougelike.B0124.SquadPressure;

var roster = new[] {
    new PressureMember(10, R3EnemyArchetype.Scout),
    new PressureMember(20, R3EnemyArchetype.Gunner)
};
var coordinator = new SquadPressureScheduler(roster, SquadPressurePolicy.Standard());
var observations = new[] {
    new PressureObservation(10, alive: true, targetVisible: true, canExecute: true, distance: 18f),
    new PressureObservation(20, alive: true, targetVisible: true, canExecute: true, distance: 34f)
};
PressurePulse pulse = coordinator.Tick(0, observations);
// pulse.State == Started, pulse.Intent == ScoutMark, pulse.MemberId == 10
```

Per **squad instance**, w monotonicznych tickach symulacji:

1. Przyjmuje komplet obserwacji wszystkich unikalnych ID; odrzuca zły czas, złe ID i NaN/Infinity przed modyfikacją stanu.
2. Rezerwuje **maksymalnie jedno równoczesne** okno akcji specjalnej; w jego czasie zwraca `Active`, po wygaśnięciu `Ended`. Pomiędzy oknami narzuca odstęp, per-role cooldown i sprawiedliwą rotację członków.
3. Zwrotnie mapuje oryginalne archetypy R3: Scout → `ScoutMark`, Gunner → `GunnerVolley`, Bruiser → `BruiserPush`, Flanker → `FlankerPincer`. ScoutTag to **sygnał zdarzenia i bit telemetryczny** dla kolejnego okna, nie wyzwolenie darmowego obrażenia.
4. Kontroluje `Alive`, `TargetVisible`, `CanExecute` (jawny status dostarczany przez przyszłego odbiorcę), oryginalne ID, zasięg i zanik targetu. Zginęcie, utrata zdolności, LOS lub przekroczenie dozwolonego zasięgu **kończy rezerwację przed kolejnym przydziałem**. Nie wydaje rozkazu ruchu bez napędu ani strzału bez broni.
5. Nie wprowadza przyrostu HP, obrażeń, ekonomii, nagrody, friendly-fire, linii widoczności, nowych pocisków ani efektów w grze. To rzeczywista **mechanika arbitrażu**, ale rezultat pozostaje tylko `PressurePulse` do czasu implementacji adaptera przez Codexa.

Parametry `Standard()` (3 ticki okna, 2 odstępu, 12 tagu, per-role cooldown i zasięg) są **danymi badawczymi, nie zatwierdzonym balansem**. Długość ticka nie jest zdefiniowana przez nową paczkę. Odbiorca musi powiązać ticki ze swoim monotonicznym cyklem symulacji i zatwierdzić politykę. Żaden kod Unity `FixedUpdate` nie został zmieniony.

## Rzeczywiste testy i odtwarzanie

Z checkoutu gałęzi PR w katalogu głównym repozytorium z `.NET 8 SDK`:

```sh
dotnet build Research/B01-24/P17/W06-SQUAD-PRESSURE-001/SquadPressureCI.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Research/B01-24/P17/W06-SQUAD-PRESSURE-001/SquadPressureCI.csproj
```

Autorskie testy obejmują zgodność z oryginalnym R3, 4 realne archetypy, policy range, grant/active/end, ScoutMark expiry, pełne 3 scenariusze i ich powtarzalny SHA-256, sprawiedliwość/oddzielność oddziałów, brak rezerwacji po śmierci/utracie możliwości działania, granicę zasięgu, cooldown, mutacje zewnętrznego roster/policy, próby NaN/Infinity, błędnych ID/ticków/duplikatów oraz overflow. Workflow `.github/workflows/b01-24-p17-w06-squad-pressure.yml` automatycznie archiwizuje **oryginalne stdout i exit code CI** dla Windows2025 i Ubuntu24.04. Cały zakres PR to wyłącznie nowy folder `Research/B01-24/P17/W06-SQUAD-PRESSURE-001/**` i jego odrębny workflow.

**Testy Unity 6000.3.14f1 Editor / PlayMode / PhysX / Windows Player oraz faktyczny gameplay = NOT_RUN.** Wynik testów `.NET` nie upoważnia do twierdzenia, że AI gra już w nowy sposób. Nie przeprowadzono natywnych testów animacji/LOS, actual enemy `FixedUpdate`, prędkości ramienia, komunikacji jednostek, percepcji i FPS. To source-level G0 producenta.

## Niezależny odbiór, przyszły Codex i rollback

**B01-40**: potwierdzić oryginalną wersję Git blob `Assets/R3/R3EnemyArchetype.cs`, dokładny HEAD, własny file-scope, cztery role, rzeczywisty CI Windows/Ubuntu, re-run artefaktów. **B01-60**: wykonać niezależne testy property-based sekwencji ticków i rozbrojenia/wrażliwych przypadków (m.in. anulowanie rezerwacji i fairness), a *jeśli* można natywnie importować w Unity, sprawdzić składanie źródła z oryginalnym assembly R3, ID prawdziwych wrogów i destrukcję pod GameObject.

**Codex-only integration plan, dopiero za zgodą B01-00 i po QA:** w izolowanym roboczym checkout zaimportować `SquadPressure.cs` + `.meta` do `Assets/B0124SquadPressure/`, użyć wyłącznie istniejącego `R3EnemyArchetype` i autentycznych nieujemnych stabilnych ID każdej jednostki. Adapter Codexa może przekazywać prawdziwy LOS/ability/target distance, monotoniczny tick i tylko *opcjonalnie* interpretować `Started` jako pozwolenie na konkretną istniejącą taktykę. **Nie zamieniać bieżącego normalnego ataku w przymusowy cooldown**, nie zmieniać sprawdzonych P12/R1/R5, dopóki projekt nie zatwierdzi kształtu rozgrywki. Zachować nieużywany domyślny path AI bez adaptacji. Dowieść natywnego G1 z rzeczywistymi prefab colliderami i PlayMode/Player.

**Rollback:** nie importować lub wyłączyć *jedynie* opcjonalny adapter/przełącznik nowego arbitrażu i usunąć tylko nowy `Assets/B0124SquadPressure/` po kontroli lokalnych edycji. Nie usuwać i nie zmieniać źródła Codexa, nie robić merge samodzielnie, nie zastępować OWNER-ACK. Status: `AUTHOR_SOURCE_TESTED; INDEPENDENT_QA_PENDING; NATIVE_G1_NOT_RUN; CODEX_ACK_PENDING; GAME_NOT_INTEGRATED`.
