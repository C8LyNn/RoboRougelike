# M02-007 — Para rolek podporowych (MODEL-02)

**Katalog:** `M02-007`, kolejka M02, tylko nowy asset wizualny dla Unity 6 RoboRougelike.
**Claim CREATE:** `ee3e62e560c374e694f8500c22e8eb8bf5b92de8` z 2026-10-24T16:49:39Z, `Docs/ModelFactory/claims/M02-007.json` na `feature/rrl-model-factory-queue-v1`.

## Konstrukcja i układ współrzędnych

- **Pivot mocowania:** `(0,0,0)` w płaszczyźnie spodu ramy `Z=0` w Blenderze, jednostki w metrach. Eksporter glTF zmienia orientację na Y-up; nie należy arbitralnie przestawiać pivota.
- **Jazda:** +X; osie rolek +Y. Dwa niezależne zespoły nośne o środkach `(-0.34,0,-0.405)` i `(0.34,0,-0.405)` m.
- **Rolki:** promień nominalny gumy 0.155 m, maksymalny promień bieżnika 0.166 m, szerokość opony 0.176 m. Po 16 osobnych klocków bieżnika na rolkę, osadzonych 5 mm w gumie. Rzeczywiste piasty pierścieniowe z otworem R=0.029 m i wałem R=0.018 m (11 mm luzu).
- **Zawieszenie:** dwa niezależne suwaki, zakres 0–40 mm pionowo; po dwa amortyzatory na rolkę. Górna obudowa w cyjanie i bursztynowa opaska, chromowany tłoczyskowy suwak oraz bursztynowy pierścień dolny. Rurka Rinner=0.022 m, tłok R=0.015 m (7 mm luzu). Zewnętrzne ramiona podpierają wał poza obrysem gumy.
- **Hierarchia:** `M02-007_MOUNT_Z0` → `M02-007_SUSP_L/R` → `M02-007_ROLLER_L/R_ROTATE_Y`; wały i uchwyty należą do suwaków, a obręcze, piasty i gumowe bieżniki do rotorów. Oba koła obracają się w tę samą stronę.
- **Animacja:** 61 klatek przy 30 FPS: dwa pełne obroty (360° każde) i naprzemienne ugięcie suwaków. Ruch nie zmienia geometrii stałych części i nie wymaga zmian w fizyce gry.
- **Materiały:** 8 odrębnych PBR: gunmetal, stal, brąz, grafitowa guma, grafitowy bieżnik, cyjan, bursztyn, chrom. Kolor nie jest uzależniony od emissive.

## Artefakty i uruchomienie

`generate_m02_007.py` — samowystarczalny generator w Pythonie 3.10+, który zawsze może wygenerować OBJ/MTL i raport. W Blenderze 4.2+/5.x dodatkowo tworzy bibliotekę `.blend` z własną kolekcją i animowany `.glb`. Powtórne uruchomienie usuwa tylko obiekty z własnej kolekcji.

```bash
python3 Tools/ModelFactory/M02/M02-007/generate_m02_007.py --self-test
python3 Tools/ModelFactory/M02/M02-007/generate_m02_007.py --out build/M02-007
blender --background --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M02/M02-007/generate_m02_007.py -- --out build/M02-007
```

Dedykowany `.github/workflows/ci-m02-007.yml` uruchamia Python, Blender, eksport GLB, niezależną inspekcję binarnego glTF 2.0 i zapisuje wygenerowane pliki **wyłącznie na własnej gałęzi** `feature/rrl-model-m02-007-support-roller-pair`. Nie scala PR.

## QA — bramki

`--self-test` wymaga zamkniętych brył o dodatniej objętości, bez degeneracji i bez krawędzi nie-manifold. Analizuje 81 położeń amortyzatorów × 72 kąty obrotu = **5832 konfiguracje** (analityczne obwiednie radialne i osiowe, nie pełna detekcja trójkąt–trójkąt). Weryfikuje prześwity wał/piasta, tłok/obudowa, rolka/ramy, osłony/ramiona i rozstaw rolek.

**Status Blender/GLB/BLEND:** tylko prawdziwy log CI może potwierdzić PASS. **FBX:** NOT RUN; animowany GLB wymaga zgodnego importera glTF w Unity, OBJ jest statyczny. **Unity EditMode/PlayMode, import i kontrola wizualna w grze:** NOT RUN. Nie ma zmian w scenach, prefabach, ProjectSettings, gameplayu ani cudzych paczkach.

**Integracja Codex:** po zatwierdzeniu jakości można pobrać wygenerowany GLB i podłączyć rig z dwoma rotorami oraz suwakami; osobny adapter w Unity musi mapować ruch i skalę. Generator nie jest sam w sobie integracją Unity.
