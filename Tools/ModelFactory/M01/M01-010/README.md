# M01-010 — Energetyczny widełkowy projektor

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1` — M01-010, przednie gniazdo, rozwarcie wideł, otwarta brama emisyjna. **Agent:** MODEL-01. **Claim:** `Docs/ModelFactory/claims/M01-010.json` (atomowy CREATE `afa8f92a26d9e9786ba02770e3200ffa7d982505`, 2026-10-24T19:48:10.458Z).

## Wygląd i mechanika wizualna

Dwa masywne grafitowe widełkowe ramiona z ceramicznymi segmentami i miedzianymi przewodnikami, wewnętrzne turkusowe paski emisji, emitery końcowe i otwarta pierścieniowa brama energetyczna między widełkami. Korpus ma skośną ceramiczną osłonę, metalową płytę montażową, miedziane krawędzie i wskaźnik bursztynowy. Wszystkie części są wyłącznie wizualne; nie dodano żadnej mechaniki walki.

**Układ współrzędnych:** 1 jednostka = 1 metr. `MOUNT_FRONT_SOCKET` jest root/pivotem przy przedniej płaszczyźnie przyłączeniowej (0,0,0). `+Z` to przód / kierunek emisji, `+Y` to góra, `+X` prawo. Eksport FBX ma ustawione `axis_forward="-Z"`, `axis_up="Y"`; orientację należy sprawdzić po imporcie do Unity 6.

**Animacja:** `FORK_HINGE_L` i `FORK_HINGE_R` są oddzielnymi węzłami obracającymi się wokół lokalnej osi `Y`. Klatki 1 → 32 → 64: 0° → odpowiednio −21°/+21° → 0°. `Energy_Gate_Outer` pulsuje skalą 1.000 → 1.055 → 1.000. 60 FPS. Obie części wysuwają się na zewnątrz bez zamierzonego kontaktu z obudową. Pierścienie emisji mają pełną fizyczną siatkę, a nie tylko shader.

## Uruchomienie

```bash
blender -b --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M01/M01-010/generate_m01_010.py \
  -- --out /tmp/m01-010

python3 Tools/ModelFactory/M01/M01-010/validate_m01_010.py /tmp/m01-010
```

Generator usuwa jedynie poprzednią kolekcję `M01-010_ENERGY_FORK_PROJECTOR` w bieżącej scenie, więc powtarzanie nie namnaża modeli. Nie dotyka obcych kolekcji. Blender tworzy pliki `M01-010_energy_fork_projector.blend`, `M01-010_energy_fork_projector_animated.glb`, `M01-010_energy_fork_projector_animated.fbx`, `M01-010_energy_fork_projector.obj`, `M01-010_energy_fork_projector.mtl` i `M01-010_qa.json`. Workflow CI po pozytywnym teście zapisuje te realne pliki wyłącznie w `generated/` tej gałęzi oraz jako artefakt.

## Kryteria QA i ograniczenia

- Wszystkie siatki: brak trójkątów zerowej powierzchni, zamknięte krawędzie, dodatnia objętość, obecność materiału.
- 64 próbek animacji; porównanie AABB wszystkich ruchomych elementów z obiektami nieruchomymi oraz między przeciwnymi widełkami. Styki *wewnątrz* tej samej konstrukcji są zamierzone i nie są traktowane jako kolizje.
- Niezależny walidator sprawdza faktyczne nagłówki binarne BLEND/FBX/GLB, liczbę siatek i materiałów w glTF, animację obydwu zawiasów i pulsację bramy oraz wygenerowane OBJ/MTL.
- AABB to konserwatywny broad-phase; nie zastępuje testów Unity, fizyki ani wizualnej inspekcji geometrii w silniku.
- **Unity 6 import, EditMode, PlayMode, gameplay integration: NOT RUN** do czasu wykonania ich w prawdziwym projekcie. Nie modyfikowano `Assets`, wspólnych scen, prefabów, `ProjectSettings` ani WPN-001..006.

Gałąź: `feature/rrl-model-m01-010-energy-fork-projector` (od `develop`). Osobny Draft PR do `develop`. Nie scalać automatycznie.
