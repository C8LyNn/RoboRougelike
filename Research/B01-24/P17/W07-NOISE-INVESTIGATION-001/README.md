# B01-24 — NOISE-INVESTIGATION-001 v1.0.0 / PASS17 WAVE07

## Dostarczona funkcjonalność

**Oryginalny moduł AI percepcji słuchowej, pamięci celu i przeszukiwania**, oparty o rzeczywiste `R3EnemyArchetype` Codexa. Kod `EnemyInvestigation.cs` jest samodzielnym, kompilowalnym C# 9, kompatybilnym z typami .NET używanymi przez Unity 6; plik `.meta` ma stabilny GUID `b73cae7d75bb42d898dac28c121f7193`.

Działa jako nowa mechanika wyznaczania zamiarów ruchu przeciwnika po usłyszeniu odgłosu albo utracie kontaktu wzrokowego. To **nie** druga implementacja formacji #355, koordynatora okien ataku #429, solvera uniku, systemu celowania, broni ani przepychania oderwanych części. Codex zachowuje wyłączność na rzeczywisty `P12PipelineSafeEnemyTurret`, Rigidbody, system strzelania i wejście do poziomu.

### API i rzeczywiste zachowanie

- `EnemyInvestigation(R3EnemyArchetype.Scout|Bruiser|Gunner|Flanker)` tworzy **oddzielną pamięć dla każdej jednostki**. Wszystkie cztery role korzystają z odmiennej, prowizorycznej czułości słuchu i czasów pamięci.
- `Tick(PerceptionFrame)` przyjmuje monotoniczny tick gry, `Vector2(X,Z)` pozycji przeciwnika, flagę prawdziwej widoczności celu, ostatnią pozycję gracza, prawdziwe `CanMove`, tablicę `NoiseCue(id, tick, worldXZ, strength, occluded)`. Własność `occluded` musi być wyznaczona przez silnik/adapter Codexa, nie jest wnioskowana z samego punktu.
- Każda nowa próbka odgłosu ma amplitudę 0..1, jednorazowe unikalne ID i wygasający timestamp. Siła sygnału maleje z dystansem i jest tłumiona przez przeszkodę. Najsilniejszy świeży sygnał wygrywa; przy remisie najniższe ID, niezależnie od kolejności wejściowej.
- Zauważony gracz ma pierwszeństwo nad hałasem. Po utracie LOS moduł podaje **dokładną ostatnią zaobserwowaną pozycję** i przechodzi z `PursueVisible` do `MoveToClue`. Osiągnięcie wskazówki inicjuje 4 rzeczywiście wyliczane punkty `SearchClue` dookoła niej. Po wygaśnięciu pamięci, upływie limitu przeszukiwania lub odwiedzeniu punktów AI zwraca `Idle`.
- Brak napędu: `Hold`, bez podawania rozkazu ruchu; odzyskanie napędu pozwala wznowić działanie w czasie pamięci. Błędne ID, duplikaty hałasu, ujemne/future ticki, nieskończone/NaN XZ i siły są odrzucane **przed mutacją** stanu.
- Moduł nie wykonuje `FixedUpdate`, nie zmienia transformów/HP, nie nadaje teleportacji, nie steruje bronią ani nawigacją. **Źródłowo działa**, ale nie jest jeszcze mechaniką zaimplementowaną w działającej grze.

### Proste użycie po ewentualnym autoryzowanym imporcie

```csharp
using System.Numerics;
using RoboRougelike.R3;
using RoboRougelike.B0124.Investigation;

var brain = new EnemyInvestigation(R3EnemyArchetype.Scout);
var noise = new NoiseCue(1, 0, new Vector2(10f, 0f), 1f, true);
PerceptionDecision decision = brain.Tick(new PerceptionFrame(
    0, Vector2.Zero, false, Vector2.Zero, true, new[] { noise }));
// Mode = MoveToClue, Source = Noise, Target = XZ(10,0)
```

W Unity host musi przetłumaczyć `Vector3(x,y,z)` na `System.Numerics.Vector2(x,z)` bez mylenia osi oraz użyć własnego bezpiecznego nawigatora (rzeczywiste R5 przeszkody, kolizje, nav, unikanie sojuszników). Ruch może być wykonywany dopiero po natywnej walidacji. `MinimumSignal`, czułość słuchu, 4 punkty przeszukiwania, limity ticków i dystans są **prowizorycznymi danymi autorskimi**, do zatwierdzenia przez projektantów; czas ticka określa Codex.

### Oryginalne, wykonywane źródła i testy

Pliki: `EnemyInvestigation.cs`, `EnemyInvestigation.cs.meta`, trzy jawne ścieżki wejściowe `investigation_scenarios.json`, `Program.cs` i `InvestigationCI.csproj`. Projekt `.csproj` **kompiluje oryginalny, niezmieniony** `Assets/R3/R3EnemyArchetype.cs` z gry Codexa — Git blob `66cd1ee734b129117b3753d1205469a0496cb673` (bazowy gameplay `203a0805251ed28293888e43f2d49f0df63767c6`).

```sh
dotnet build Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/InvestigationCI.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/InvestigationCI.csproj
```

Testy oryginalnego C# obejmują wszystkie 4 role, widoczność, zasięg/cichą próbkę/okluzję, priorytet sygnału, powtórzone eventy, zmianę wskazówki, cykl przeszukiwania, bezruch i powrót napędu, izolację stanu 2 wrogów, odrzucanie źle sformowanych eventów oraz trzy scenariusze (23 **rzeczywiste kroki**), z ich powtarzalnym SHA-256. `.github/workflows/b01-24-p17-w07-noise-investigation.yml` uruchamia `dotnet` z kodem źródłowym na Windows2025 i Ubuntu24.04. **Native Unity Editor, PlayMode/PhysX, WindowsPlayer = NOT_RUN**. Nie utożsamiać sukcesu konsoli .NET z integracją z grą.

### Zakres, odbiór, rollback

Wszystkie nowe pliki należą tylko do `Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/**` oraz osobnego workflow. Oryginalny R3/P12/R1/R5/CHL/PARTSIZE i gameplay Codexa pozostają nietknięte.

**B01-40:** sprawdzić Git bloby w manifeście, obecny docelowy R3 ABI, pełny zakres PR, stabilność GUID i realny wynik CI. **B01-60:** niezależnie sprawdzić event watermark, monotoniczne ticki, pamięć/false positives, degeneracje Float, losowe sekwencje, realną nawigację/WorldLOS dopiero przy natywnym Unity. **B01-00:** sam decyduje, czy włączyć kod do kolejki Codexa; nie jest to automatycznie `CODEX_SENT`.

**Codex-only integracja, dopiero po ACK:** sprawdzić bieżący realny runtime SHA, importować TYLKO `EnemyInvestigation.cs` + `.meta` do nowego `Assets/B0124EnemyInvestigation/`, opcjonalnie podłączyć bezpieczny adapter percepcji i istniejącą nawigację, zachować domyślną ścieżkę przeciwnika gdy funkcja wyłączona. Testy natywne: rzeczywista widoczność przez R5 geometry, odgłosy broni i kolizji, wiele wrogów, brak teleportów, brak przechodzenia przez ściany i bez uszkodzenia broni/AI. Zmiany `P12PipelineSafeEnemyTurret` może wykonywać **wyłącznie Codex**. Rollback: wyłączyć adapter i usunąć tylko dodatkowe pliki B0124, po sprawdzeniu kopii lokalnie edytowanych plików. No merge i no Game G2 claim.


## WAVE07 — fizyczny ZIP odbiorczy dla Codexa i niezależnego QA

Po sprawdzeniu oryginalnego manifestu `Tools/build_receiver.py` buduje deterministyczny, **rzeczywisty ZIP bajtów** dwóch oryginalnych plików Unity (`EnemyInvestigation.cs`, `EnemyInvestigation.cs.meta`) w dedykowanym `Assets/B0124EnemyInvestigation/`; dołącza `receiver-receipt.json` z SHA-256 archiwum i SHA256 każdego wpisu. Komenda **odmawia nadpisania** istniejącego katalogu odbioru i odrzuca zmianę dowolnego przypiętego bloba, oryginalnego R3, GUID lub próbę symlinka/traversal. Nigdy nie kopiuje ani nie zmienia canonical `Assets/**`.

```sh
python -m unittest discover -s Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/Tools -p "test_receiver.py" -v
python Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/Tools/build_receiver.py --repo . --out b01-24-W07-source-zip
```

16 testów oryginalnego pakowacza wykonuje rzeczywiste operacje ZIP, CRC, bajtową deterministyczność i celowe błędne źródła na osobnych, tymczasowych katalogach. CI buduje osobne artefakty do pobrania na Windows2025 i Ubuntu24.04; można porównać ich **wewnętrzny** ZIP SHA-256 (nie należy utożsamiać go z zewnętrznym archiwum GitHub Actions).

To ZIP dwóch oryginalnych plików importu C#, **nie** `.unitypackage`, prefab ani scena. Wewnętrzny folder `Assets/B0124EnemyInvestigation` to wyłącznie docelowa ścieżka opcjonalnego *przyszłego* importu autoryzowanego przez Codexa. Natywne Unity pozostaje NOT_RUN nawet po utworzeniu prawdziwego archiwum.
