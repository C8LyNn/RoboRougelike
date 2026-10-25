# M01-018 — Armata kinetyczna z przeciwwagą

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Zakres:** wyłącznie wizualny model 3D dla Unity 6; nie zmienia mechanik WPN.

## Sylwetka

Ciężka, długa lufa z rzeczywistym otworem przelotowym, szeroki pancerny korpus z ceramicznymi bocznymi osłonami, miedziane pierścienie i żebra chłodzenia. Charakterystyczny **otwarty tylny mechanizm**: dwie odsłonięte prowadnice, poprzeczka i przesuwna centralna przeciwwaga z segmentami wolframowymi. Pomarańczowe ostrzegawcze oznaczenia, turkusowe punkty stanu, bez opierania wyglądu wyłącznie na emisji.

## Materiały i współrzędne

Siedem materiałów: Graphite, Titanium, CeramicIvory, Copper, Tungsten, SafetyOrange, EmissiveCyan. 1 Blender unit = 1 metr. W Blenderze Z-up, lufa skierowana w -Y. Eksport do GLB/FBX: Y-up, +Z forward. Pivot montażowy `MOUNT_TOP_SOCKET` w (0,0,0). Pivot ruchu `COUNTERWEIGHT_SLIDE_Y_AXIS` w (0,0.79,0.66) względem mountu. **Oś przesuwu:** lokalna +Y w Blenderze; skok 0,22 m. Animacja 61 klatek przy 60 FPS, 3 cykle przesuwu z interpolacją liniową.

## Generowanie

```bash
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-018/generate_m01_018.py -- --repeat --out Tools/ModelFactory/M01/M01-018/generated
python3 Tools/ModelFactory/M01/M01-018/qa_m01_018.py --out Tools/ModelFactory/M01/M01-018/generated
```

Generator zeruje scenę, siatki, materiały i akcje przed kolejnym przebiegiem; `--repeat` testuje powtarzalność. QA bada zamknięcie siatek, brak zdegenerowanych ścian, objętość, prześwit AABB ruchome–nieruchome w 61 klatkach i poprawność plików binarnych. Zamierzone kontakty wewnątrz jednego zespołu nie są traktowane jako kolizje.

## Eksport i ograniczenia

Oczekiwane pliki po udanym Blender QA: `.blend`, animowany `.glb`, `.fbx`, statyczny `.obj` + `.mtl`, `_qa.json`. GitHub Actions publikuje artefakt oraz próbuje umieścić pliki na wyłącznej gałęzi. **Nie należy deklarować ich istnienia, dopóki workflow rzeczywiście ich nie wygeneruje.** AABB nie jest ciągłym testem kolizji trójkątów; import do Unity 6, EditMode/PlayMode, ręczna ocena wizualna i gameplay wymagają oddzielnych testów. Nie zmieniać wspólnych scen, prefabów, ProjectSettings ani WPN-001..006.
