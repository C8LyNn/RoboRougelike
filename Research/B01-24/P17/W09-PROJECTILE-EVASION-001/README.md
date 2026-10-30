# B01-24 — PROJECTILE-EVASION-001 v1.0.0 / PASS 17 WAVE 09

## Funkcjonalność źródłowa (realny C# 9)

**Oryginalny predykcyjny planner uniku pocisków**, nie duplikat #355 formacji, #380 cover retreat, #429 harmonogramu ataków, #445 percepcji dźwięku ani #463 radia. `ProjectileEvasion.cs` zawiera kompletną funkcję obliczeniową opartą na ciągłej trajektorii liniowej i **rzeczywistym oryginalnym `R3EnemyArchetype.cs` Codexa**, który kompilowany jest bez zmiany wraz z modułem przez `ProjectileEvasionCI.csproj`. Unity 6 ma możliwość importu czystego C# 9 i stabilnego pliku `.meta`, ale **natywny import/PlayMode/PhysX NIE został przetestowany**.

`ProjectileEvasionPlanner.Tick(EvasionFrame)` otrzymuje tick, XZ lokacji i prędkości przeciwnika, promień collidera, maksymalną szybkość i przyspieszenie, długość reakcji oraz horyzont predykcji w sekundach, `CanMove` i maksymalnie 64 zgłoszone pociski (stabilne ID, XZ, prędkość, promień i informacja `Hostile`). Oryginalne role R3: Scout, Bruiser, Gunner, Flanker mają osobne, **prowizoryczne** współczynniki reakcji. Dla każdego wrogiego pocisku solver oblicza najbliższe względne położenie z odcinka **w ciągłym czasie** t∈[0,horizon] (nie tylko dystans na końcu ticka). Jeśli aktualna trajektoria jest wolna, zwraca `Clear`. Jeśli grozi zderzenie, oblicza 8 alternatywnych wektorów docelowych po pojedynczej ograniczonej przyspieszeniem korekcie prędkości. Zwraca `Dodge` wyłącznie gdy istnieje rzeczywista poprawa w predykcji ryzyka, w przeciwnym razie `NoBetterPath`. Brak zdolności ruchu => `MobilityDisabled`; pociski sojusznicze są ignorowane.

**Granice:** proponowana prędkość jest tylko poradą do **autoryzowanego** kontrolera Codexa. Symulacja korzysta z uproszczonego liniowego ruchu po pojedynczym kroku reakcji, a nie z prawdziwego WheelCollider/Rigidbody/PhysX. Nie ma rozpoznawania mapowych ścian ani kosztu przeszkód i nie steruje realnym ruchem/strzałem ani obrażeniami. Odbiorca musi mieć prawdziwe dane pocisków i fizyczny motoryczny adapter z kontrolą kolizji ścian. **Nie można obiecywać bezkolizyjności ani natywnego PASS na podstawie .NET.** Ten kod to funkcjonalny, oryginalny obliczeniowy element taktycznego AI w zakresie B01-24, niezintegrowany jeszcze z grą.

## Uruchamianie rzeczywistych testów oryginalnego C#

```sh
dotnet build Research/B01-24/P17/W09-PROJECTILE-EVASION-001/ProjectileEvasionCI.csproj -c Release --warnaserror
dotnet run --no-build -c Release --project Research/B01-24/P17/W09-PROJECTILE-EVASION-001/ProjectileEvasionCI.csproj
python -m unittest discover -s Research/B01-24/P17/W09-PROJECTILE-EVASION-001/Tools -p "test_receiver.py" -v
python Research/B01-24/P17/W09-PROJECTILE-EVASION-001/Tools/build_receiver.py --repo . --out b01-24-w09-zip
```

Własny runner faktycznie kompiluje oryginalny `Assets/R3/R3EnemyArchetype.cs` Git blob `66cd1ee734b129117b3753d1205469a0496cb673`. Testy wykonują m.in. trafienie czołowe, pocisk przecinający tor, near-miss, pocisk odchodzący i poza horyzontem, 8 kierunków, ograniczenia prędkości/przyspieszenia, zmienne archetypy R3, brak ruchu, brak friendly false positive, niezależne instancje, NaN/Infinity, błędny tick, ID i promienie, oraz **trzy odtwarzalne oryginalne JSON scenariusze** z dokładnym SHA256 śladu. GitHub Actions Windows2025 i Ubuntu24.04 kompiluje z `--warnaserror` i archiwizuje surowe dowody wykonania.

Generator `Tools/build_receiver.py` **fizycznie tworzy** powtarzalny source-only ZIP, zawierający dokładnie `Assets/B0124ProjectileEvasion/ProjectileEvasion.cs` oraz `.cs.meta` (GUID `9281e96fcb134097a079e468c2bb0a75`), wraz z `receiver-receipt.json` (SHA256 ZIP + bloby źródłowe). Odrzuca różnice Git blobów, zmienione R3, symlink, path traversal oraz dowolne nadpisanie katalogu wyjściowego. Oddzielne 16 testów wykonuje prawdziwe ZIP/CRC/SHA, negatywne przypadki. ZIP **nie jest** natywnym `.unitypackage`, prefababem lub sceną. Czysty kod możesz zaimportować wyłącznie po autoryzacji HQ/Codexa w disposable checkout Unity.

## Własność, niezależny odbiór i rollback

**B01-24 jedyny producent:** `Research/B01-24/P17/W09-PROJECTILE-EVASION-001/**` i jego jedyny workflow `.github/workflows/b01-24-p17-w09-projectile-evasion.yml`, bez zmian w `Assets/**`, R1/R3/R5/CHL/PARTSIZE/weapon. Nieprzekazana paczka do Codexa, bez merge.

**B01-40:** niezależnie powtórzyć oryginalny SHA-1 Git blob wszystkich pozycji manifestu, GUID, source pin Codexa i SHA256 realnego fizycznego ZIP, sprawdzić scope, nienadpisywanie i instrukcję rollback. **B01-60:** niezależnie sprawdzić rzeczywiste przypadki przecięcia w czasie, monotoniczne ticki, promienie, parzystość dla odwróconego kierunku, fake friendly, arbitrację 8 kierunków, wysokie wartości wejść oraz granice przyspieszenia. **B01-00** decyduje, czy zaoferować **po pozytywnym QA** nowy SHA do Codex Inbox #79; tylko Codex może jawnie ACK i przygotować adapter fizyki oraz rzeczywistych pocisków.

**Codex-only integracja:** import oryginalnych `.cs + .meta` pod nowe, izolowane `Assets/B0124ProjectileEvasion`; opcjonalny adapter dostarcza rzeczywiste aktualne prędkości, osie XZ, promienie Colliderów, typy friendly/hostile oraz parametry możliwego przyspieszenia; wyniki przefiltrować przez istniejące kolizje z mapą i nawigację i dopiero wtedy wykorzystać jako sterowanie. **Rollback:** wyłączyć tylko opcjonalny adapter, usunąć dwa nowe importowane pliki po kontroli lokalnych zmian, nigdy nie usuwać oryginalnego gameplay Codexa. Czysty code path normalnego AI pozostaje bez zmian.

Status niezależny od source-green: `G0_PRODUCER_PASS_ONLY; INDEPENDENT_B01-40/60=PENDING; UNITY_EDITMODE/PLAYMODE/PHYSX/WINDOWS_PLAYER=NOT_RUN; CODEX_SENT=NO; CODEX_ACK=PENDING; G2=HOLD; GAME_INTEGRATED=NO`.
