# M02-010 — Wahacz niezależnego zawieszenia

**Agent:** MODEL-02 · **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` z gałęzi `feature/rrl-model-factory-queue-v1` · **Claim CREATE:** `e7da94a9b16d725804385abcebb8461ab0083b86`, 2026-10-24T19:48:04.902Z.

## Konstrukcja

Wahacz trójkątny z dwoma tulejami łożyskowymi po stronie ramy, osobnymi uszami mocowania, przegubem kulowym po stronie piasty oraz zewnętrznym zespołem sprężyna–amortyzator. Stalowy wał osiowy pozostaje nieruchomy, a wahacz obraca się o **±12° wokół osi +X**. Amortyzator teleskopowy ma ruchomy chromowany tłoczyskowy rdzeń; dziewięciozwojowa sprężyna zmienia długość i orientację razem z geometrią wahacza. Zawieszenie jest wyłącznie modelem wizualnym, bez ingerencji w Unity Physics.

**Układ współrzędnych:** Blender X przód/tył, Y bok pojazdu, Z góra. **1 jednostka = 1 metr**. Pivot całego modułu i gniazda pojazdu: `(0,0,0)`. Oś wahacza: `(0,0.130,0)`, kierunek `+X`. Przegub zewnętrzny w spoczynku: `(0,0.638,-0.072)`. Dwie tuleje w osi X mają otwór o promieniu 24 mm, a wał promień 16 mm (8 mm nominalnego luzu promieniowego). Oś mechanizmu i mocowanie są opisane w custom properties root.

**Materiał i czytelność:** 8 odrębnych materiałów (tytan, grafit, guma, miedź, sprężyna bursztynowa, chrom, cyjanowy wskaźnik, jasna ceramika). Geometria jest modelowana bryłami zamkniętymi z opcjonalnymi małymi fazowaniami Blendera, z naciskiem na mechaniczne połączenia, czytelny kontur i odrębność powierzchni.

## Pliki i generowanie

- `generate_m02_010.py` — kompletny generator `bpy`, zapisuje rzeczywisty projekt `M02-010.blend` i animowany `M02-010_animated.glb` po uruchomieniu w Blenderze. `--self-test` oraz `--static-obj` działają w czystym Pythonie bez Blendera.
- `qa_m02_010.py` — niezależny parser wygenerowanego OBJ/MTL i animowanego GLB, sprawdza zamknięte siatki, normalne/winding, spójność manifestu, deterministyczność oraz obecność kanałów transformacji.
- `.github/workflows/ci-m02-010.yml` — izolowane testy statyczne, rzeczywisty Blender z systemowych pakietów Ubuntu i publikacja artefaktów OBJ/MTL/QA oraz BLEND/GLB/QA.

Przykładowe uruchomienia:

```bash
python3 Tools/ModelFactory/M02/M02-010/generate_m02_010.py --self-test
python3 Tools/ModelFactory/M02/M02-010/qa_m02_010.py
blender -b --factory-startup --python Tools/ModelFactory/M02/M02-010/generate_m02_010.py -- --out build/M02-010
python3 Tools/ModelFactory/M02/M02-010/qa_m02_010.py build/M02-010/M02-010_animated.glb
```

## Zakres QA i ograniczenia

Generator sprawdza każdą siatkę pod kątem krawędzi o dokładnie dwóch ścianach, degeneracji i dodatniej objętości. W 97 konfiguracjach analitycznych sprawdza skok wahacza, połączenie tłoczyska z obudową, skok i odstęp zwojów sprężyny, odległość od ramion wahacza oraz luz łożyskowy. Testy analityczne używają konserwatywnych obwiedni, a nie pełnego silnika fizycznego. W Blenderze animacja wykorzystuje grupy `ARM_RIG`, `SPRING_RIG`, `DAMPER_RIG`, `ROD_RIG` z kluczami co 6 klatek, 1–97.

**Wymagana kontrola po eksporcie:** otwarcie BLEND, inspekcja połączeń w skrajnych pozycjach, import GLB do Unity 6 i testy EditMode/PlayMode. Bez uruchomienia testów w Unity raport musi zawierać `NOT RUN`, nie `PASS`. Materiały mogą wymagać ręcznej adaptacji do URP. Pliki generowane przez workflow są artefaktami CI; nie twierdzimy, że zostały zapisane binarnie w gałęzi.

**Bezpieczna integracja:** osobna gałąź i Draft PR do `develop`; bez zmian `main`, `develop`, `Assets`, scen, prefabów, `ProjectSettings` i cudzych paczek.
