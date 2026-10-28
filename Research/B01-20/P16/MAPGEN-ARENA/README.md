# B01-20 P16 — trzy proceduralne warianty aren MAPGEN

**Status:** izolowane dane i narzędzie weryfikacji; **NIE** zintegrowana gra, scena Unity ani nowa paczka gameplay.

Trzy konfiguracje oryginalnego generatora MapGenPlanner.Plan(CombatArena, seed, config, profile):

| Wariant | Density | Difficulty | Cele rozgrywki |
| --- | ---: | ---: | --- |
| P16-ARENA-A-OPEN | 25 | 0.25 | Otwarte flanki, szybki manewr |
| P16-ARENA-B-COVER | 60 | 0.55 | Więcej bocznych osłon, gra na dystans |
| P16-ARENA-C-DENSE | 90 | 0.85 | Gęste osłony, ryzykowne flankowanie |

Każdy preset ma **6 seedów**: 7,31,83,109,202,407. Kod wywołuje oryginalny C# generatora i walidatora dla Light i Heavy = 36 oryginalnych przebiegów źródłowych i 6 negatywów. Wymaga 18 unikatowych hashy geometrii i powtarzalnych wyników. Wynik: 18 opisów MapBlueprint JSON wygenerowanych przez oryginalny kod na .NET8, oraz results.json. Format diagnostyczny MapBlueprint **nie jest** MapDocument v2 i nie służy do bezpośredniego wczytania przez edytor.

## Oryginalne zależności i własność

Bazowy immutable MAPGEN R3.4.1 [Draft #237](https://github.com/C8LyNn/RoboRougelike/pull/237) SHA **5be31039411f89f17654b1c332f8692883fa03cc**. Projekt C# włącza BEZ MODYFIKACJI źródła:
- Assets/B01Packages/MAPGEN001/Runtime/MapGenData.cs
- Assets/B01Packages/MAPGEN001/Runtime/MapGenPlanner.cs
- Assets/B01Packages/MAPGEN001/Runtime/MapGenRouteValidator.cs
- Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs (wyłącznie zastępcza matematyka Unity do offline C#).

Nowe ścieżki WYŁĄCZNIE Research/B01-20/P16/MAPGEN-ARENA/** i dedykowany workflow. Nie zmieniono MAPGEN, zamrożonego CHL #285, plików B01-21/B01-25, Codexa ani main/develop. B01-20 produkuje dane, B01-40/60 odbierają niezależnie.

## Odtwarzalne C# source-only testy i generacja

Na odizolowanym checkout tej gałęzi (stack na MAPGEN #237):

    dotnet run --project Research/B01-20/P16/MAPGEN-ARENA/MapgenArenaP16.csproj --configuration Release -- Research/B01-20/P16/MAPGEN-ARENA/presets.json p16-arena-output
    python Research/B01-20/P16/MAPGEN-ARENA/validate_blueprints.py p16-arena-output

GitHub Actions Windows+Ubuntu archiwizuje 18 źródłowo wygenerowanych blueprintów i results.json. Original C# Plan/Validate: deterministic regeneration, two vehicle reference profiles, seed diversity, three non-overlapping enemy spawns, 12m clear route, 6 invalid input negatives. Python ponownie odczytuje i sprawdza każdy eksport.

**To NIE są próby Unity/PhysX/Rigidbody.** Oryginalne wcześniejsze natywne MAPGEN R3.4.1 [QA #241](https://github.com/C8LyNn/RoboRougelike/pull/241) testowało inne przypadki. Nie wolno kopiować starego natywnego PASS na te 18 nowych presetów. Native=NOT_RUN, wymagane zatwierdzenie HQ.

## Odbiór Codex / B01-40

Po akceptacji Codex w faktycznie zgodnym buildzie może podać gotowe presety do już istniejącego MapGenBuilder.Generate(CombatArena, seed, config, profile), bez nowego generatora i tylko w autoryzowanym zakresie. **Nie próbować importować diagnostycznego MapBlueprint JSON jako MapDocument v2** — adapter nie jest częścią tej pracy. B01-40 sprawdza dokładne SHA czterech oryginalnych plików oraz fizyczne wyjścia Actions.

Native acceptance: co najmniej 3 seedy na wariant, obie realne konfiguracje pojazdów, fizycznie wolne spawny i przejazdy, kolizje i grunt, reset/regen bez pozostałości, 720p/1080p; rzeczywiste XML/logi, Unity6000.3.14f1 i nezależna B01-60. G0/G1/G2/HQ oraz jedyny integrator Codex pozostają obowiązujące.

## Manifest / ograniczenia

Wejście: presets.json, oryginalny MAPGEN #237 SHA, recipe CombatArena. Wyjście: 18 JSON + results.json i geometria-hash w realnym artefakcie Actions. Generator ma stałe 64×64m i prosty, szeroki 12m korytarz centralny — różnice wariantów dotyczą **osłon bocznych**, a nie nowego układu tras, spawnów, AI czy mechaniki wygranej. Nie deklarujemy nowego contentu zintegrowanego z grą.
