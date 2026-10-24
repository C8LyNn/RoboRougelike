# MODEL-05 / M05-011 — Reaktor miniaturowy w klatce

**Źródło przydziału:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi
`feature/rrl-model-factory-queue-v1`. Wyłącznie model wizualny.
**Rezerwacja:** `Docs/ModelFactory/claims/M05-011.json`, commit
`45a417d8efdee12e8747eb3425dccacceeeb8724`.
**Agent:** MODEL-05. **Czas:** 2026-10-24T20:51:34.275Z.

## Projekt wizualny

Miniaturowy reaktor z nieruchomym jasnym rdzeniem cyjanowym,
koncentrycznym obracającym się pierścieniem z miedzianą bieżnią i
12 płetwami, fioletowym śladem emisji, czterema przeszklonymi oknami,
ośmioma słupkami klatki, dolnym kołnierzem i górnym radiatorem.
Oznaczenia ostrzegawcze są bursztynowe. Konstrukcja ma **9 materiałów**:
Graphite, Titanium, Copper, Ceramic, Gasket, EnergyCyan, EnergyViolet,
StatusAmber, ShieldGlass. Emisja jest tylko uzupełnieniem materiałów.

**Mocowanie:** wewnątrz ramy. 1 jednostka = 1 metr.
Lokalny punkt montażowy: `MOUNT_FRAME_ORIGIN=(0,0,0)`, na dolnej
płaszczyźnie płyty. Cztery pomocnicze `MOUNT_FRAME_NP/NN/PP/PN`
na `(±0.235,±0.235,0)`. Oś pionowa modelu w Blenderze: +Z.
Eksport FBX: `axis_forward="-Z", axis_up="Y"` do Unity.

**Animacja:** jedyny rotor `ROTOR_Z_PIVOT` o środku
`(0,0,0.46)` obraca się o 360° wokół lokalnej osi +Z w klatkach
1/16/31/46/61, 60 FPS, interpolacja liniowa kwaternionów.
Pod rotorem jest 30 ruchomych siatek: dwie bieżnie, 12 płetw,
12 indeksów emisyjnych i 4 miedziane nity. Rdzeń i klatka pozostają
nieruchome. To animacja prezentacyjna, **nie** nowa mechanika energii.

**Bezpieczeństwo ruchu:** QA bada współrzędne każdego wierzchołka
ruchomego w 61 klatkach, a nie wyłącznie osie brył. Ruchoma geometria
ma pozostać co najmniej 60 mm od rdzenia o promieniu 125 mm
i co najmniej 10 mm od obliczeniowej wewnętrznej granicy klatki
o promieniu 350 mm. Test sprawdza też granice wysokości.
Są to prześwity analityczne, nie zastępują przeglądu wizualnego.

## Uruchomienie

```bash
blender -b -t 2 --python Tools/ModelFactory/M05/M05-011/qa_blender_m05_011.py
blender -b -t 2 --python Tools/ModelFactory/M05/M05-011/generate_m05_011.py -- --out build/M05-011
```

Generator tworzy `M05-011_caged_mini_reactor.glb`,
`M05-011_caged_mini_reactor.fbx`,
`M05-011_caged_mini_reactor.blend` oraz `M05-011_qa.json`.
Workflow: `.github/workflows/ci-model-m05-011.yml`, artefakt
`M05-011-caged-reactor-GLB-FBX-BLEND-QA` (14 dni).

## Minimalne QA

- **Skala i mocowanie:** metr, pivot na dolnej płaszczyźnie, 5 markerów.
- **Wygląd:** 9 różnych materiałów, ceramika, szkło i dwie emisje.
- **Ruch:** 61 klatek rzeczywistego obrotu z kontrolą wierzchołków.
- **Powtarzalność:** dwa wywołania w jednym Blenderze; brak namnażania
  obiektów, siatek i materiałów; obcy obiekt zostaje nienaruszony.
- **Eksport:** faktyczne FBX/GLB/BLEND oraz odczyt animacji, materiałów,
  markerów i liczby siatek z binarnego GLB.
- **Unity:** import, URP i PlayMode wymagają osobnego uruchomienia;
  bez takiego dowodu stan pozostaje **NOT RUN**.

Żadnych zmian w scenach, prefabach, ProjectSettings, kodzie rozgrywki
ani innych paczkach modeli. Wymagana kontrola wizualna w Unity 6
przed integracją. PR musi pozostać Draft.
