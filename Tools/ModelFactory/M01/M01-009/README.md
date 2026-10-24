# MODEL-01 — M01-009: Trójsegmentowa wyrzutnia rakiet

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M01-009.json`; atomowy CREATE `87850e99939d4405edd99c1a1efff1e0d2bbaa9b` z 2026-10-24 18:49:33.950 UTC.  
**Zakres:** wyłącznie nowy wizualny model uzbrojenia, bez WPN-004 i bez ingerencji w mechanikę rakiet.

## Konstrukcja i estetyka

Trzy oddzielne **kwadratowe porty wyrzutni** są ułożone poziomo. Każdy ma zamkniętą kwadratową ramkę z rzeczywistym otworem, wewnętrzną ceramiczną wargę, rakietę w komorze, własną przednią pokrywę z retiklem i własny mechanizm zawiasowy. Korpus to otwarty szkielet grafitowo-tytanowy z bocznymi ceramicznymi okładzinami, miedzianymi akcentami, oznaczeniami bezpieczeństwa i turkusowymi sygnałami na pokrywach. Wewnątrz komór są widoczne czerwono-białe pociski oraz podpory. **8 materiałów**: Graphite, Titanium, Ivory_Ceramic, Oxidized_Copper, Rubber, Signal_Cyan, Safety_Amber, Warhead_Red.

**Jednostki i pivot:** 1 jednostka = 1 m. `MOUNT_TOP_SOCKET` ma pozycję (0,0,0), +Y to góra, +Z to przód/wylot, +X to prawa strona. Płyta mocowania ma rzeczywisty kontakt z dolną belką nośną. Brak fizyki i strzelania w tym PR.

**Ruch:** trzy niezależne zawiasy `LID_A_HINGE_X`, `LID_B_HINGE_X`, `LID_C_HINGE_X`. Każda pokrywa obraca się **na zewnątrz** o maksymalnie **−105° względem lokalnej osi X**; otwarcie jest kaskadowe (5 klatek przesunięcia). Animacja 72 klatki @60 fps. Ramki zawiasów i trzpienie są nieruchome, ruchome pokrywy mają osobne grupy wraz z oznaczeniami i nitami. Zamierzone kontakty: zawias–pokrywa, mocowanie–belka, szyny i osłony konstrukcyjne; nie są traktowane jako niezamierzone kolizje.

## Reprodukcja

Samodzielny generator statycznych OBJ/MTL/raportu (bez zależności):
```sh
python3 Tools/ModelFactory/M01/M01-009/generate_m01_009.py --out /tmp/m01-009
python3 Tools/ModelFactory/M01/M01-009/validate_m01_009.py /tmp/m01-009
```

Rzeczywisty Blender (gdy jest zainstalowany):
```sh
blender -b --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M01/M01-009/generate_m01_009.py \
  -- --out /tmp/m01-009-blender --blender
python3 Tools/ModelFactory/M01/M01-009/validate_m01_009.py /tmp/m01-009-blender
```

Generator usuwa tylko kolekcję `MODEL_M01_009` przy ponownym wykonaniu, nie narusza obcych kolekcji, a po eksporcie przywraca zakres klatek, FPS i zaznaczenie. Artefakty: `M01-009_tri_segment_rocket_launcher.obj`, `.mtl`, `.blend`, `_animated.glb`, `_animated.fbx`, `M01-009_qa.json`. Rzeczywiste pliki 3D należy uznać za wyprodukowane **dopiero po pozytywnym uruchomieniu CI i weryfikacji artefaktów**, nie na podstawie samego skryptu.

## QA i granice

- Manifold każdej zamkniętej bryły, orientacja ścian, dodatnia objętość, brak zdegenerowanych trójkątów.
- **129 próbek położenia pokryw** (43 na pokrywę) i test AABB z nieruchomymi przeszkodami, z wyłączeniem zamierzonych kontaktów z zawiasami.
- Niezależne QA odczytuje wygenerowany OBJ i rzeczywistą strukturę GLB, w tym trzy animowane węzły i 8 materiałów.
- Wynik Blender/GLB/FBX i zgodność OBJ/MTL raportuje GitHub Actions; import do Unity 6 i PlayMode **NOT RUN**, dopóki nie ma dowodów.
- Nie zmieniać wspólnych scen, ProjectSettings, prefabów ani WPN-001..006. Integracja w grze to osobne zadanie po review.
