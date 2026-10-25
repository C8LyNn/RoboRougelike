# M05-015 — Mechaniczny chwytak cargo

**Katalog:** M05 / M05-015, visual-only. **Mocowanie:** tył ramy.
**Claim:** `Docs/ModelFactory/claims/M05-015.json` na `feature/rrl-model-factory-queue-v1`.
**Claim CREATE commit:** `bb83e5b6d71b309e7069dd936c5978ed682c6208`.

## Konstrukcja
Trójpalczasty chwytak rozmieszczony promieniowo co 120° na centralnym
korpusie. Każdy palec ma oddzielny zawias, ceramiczną osłonę, gumową
powierzchnię chwytu, dekoracyjny tłok i wskaźnik emisyjny. Obudowa:
grafit i tytan; akcenty: miedź, ostrzegawczy pomarańcz, cyjanowa emisja.

- Jednostki: **1 Blender unit = 1 m**.
- Punkt montażu: `M05_015_MOUNT_ORIGIN` w `(0,0,0)`.
- Kierunek chwytu: lokalne **+Y**; pion: **+Z**.
- Pivots: `M05_015_FINGER_PIVOT_0..2`, oś obrotu **lokalne +Z**.
- Ruch: otwarte **-12°**, zamknięte **+16°**; klatki 1, 20, 40, 60 (30 FPS).
- Trzy palce i ich osie są elementami wizualnymi, bez nowej logiki chwytania.
- Dodatkowe markery: cztery `MOUNT_BOLT` i `CARGO_GRIP_CENTRE`.

## Generowanie
```bash
blender --background --factory-startup --python Tools/ModelFactory/M05/M05-015/generate_m05_015.py -- --out /tmp/m05-015
```
Tworzy `M05-015.glb`, `M05-015.blend`, `qa.json`; FBX jest opcjonalny.\nBlender 5.0.1 z pakietu Ubuntu ma wadliwy eksporter FBX, więc w tej wersji\nraport `qa.json` zawiera `fbx_export=BLOCKED_BLENDER_5_0_1` i nie powstaje FBX.\nW innych wersjach skrypt podejmuje próbę eksportu FBX i raportuje wynik.
Bez `--out` tworzy model i wykonuje kontrolę wewnątrz Blendera,
ale nie zapisuje plików. Generator usuwa wyłącznie własną kolekcję,
odtwarza ją dwukrotnie i sprawdza stabilność liczby siatek, wierzchołków
i materiałów. Obce obiekty pozostają nietknięte.

## QA i ograniczenia
Blenderowy test kontroluje 60 klatek animacji i konserwatywny odstęp
pomiędzy centrami nakładek palców. **Nie zastępuje pełnego mesh-vs-mesh
collision testu** ani ręcznego przeglądu wizualnego. Zawiasy i łożyska
zawierają celowe zazębienie geometryczne (wał wewnątrz tulei).
Import do Unity, prefab, URP i EditMode/PlayMode: **NOT RUN**, dopóki
nie istnieje dowód rzeczywistego wykonania.

Nie modyfikować `Assets/`, scen, `ProjectSettings` ani gameplayu.
