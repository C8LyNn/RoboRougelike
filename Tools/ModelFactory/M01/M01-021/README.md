# M01-021 — Kanister plazmy z dyszą

**Agent:** MODEL-01 / M01 (uzbrojenie wizualne). **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`.

## Zakres

Nowy moduł wizualny kanistra plazmy, bez gameplayu. Wyraźna ciśnieniowa butla, boczne mocowanie, wydrążona ceramiczna dysza, zawór z czteroramiennym pokrętłem, miedziane obejmy, przewody obejściowe i manometr. Materiały: grafit, tytan, ceramika, miedź, izolator, cyjanowy akcent plazmy i bursztynowy wskaźnik.

## Transformacje

- Jednostki: 1 m na jednostkę, Y-up, +Z-forward.
- `MOUNT_SIDE_SOCKET`: (0, 0, 0), rzeczywisty punkt montażowy.
- `VALVE_WHEEL_ROTATE_Z`: pivot (0.82, 0.16, 0), obrót lokalnie wokół osi Z, zakres 0° → 115° → 0°, 73 próbki / 60 FPS.
- Zawór ma osiowy prześwit od nieruchomego trzpienia. Połączenia elementów ruchomych w obrębie jednego zespołu są zamierzone; test AABB dotyczy ruchome–nieruchome.

## Generowanie i walidacja

```bash
python3 Tools/ModelFactory/M01/M01-021/generate_m01_021.py --self-test
python3 Tools/ModelFactory/M01/M01-021/qa_m01_021.py
blender --background --python-exit-code 1 --python Tools/ModelFactory/M01/M01-021/generate_m01_021.py -- --repeat --out Tools/ModelFactory/M01/M01-021/generated
python3 Tools/ModelFactory/M01/M01-021/qa_m01_021.py --out Tools/ModelFactory/M01/M01-021/generated --require-blender
```

Uruchomienie zwykłym Pythonem generuje wyłącznie statyczne OBJ, MTL i JSON QA. Animowane GLB/FBX oraz BLEND powstają **wyłącznie przy rzeczywistym wykonaniu w Blenderze**. Eksporty należy potwierdzić raportem CI i niezależnym walidatorem przed deklarowaniem gotowych assetów.

## Ograniczenia QA

Test próbkowany AABB nie jest ciągłą kolizją trójkąt–trójkąt. Import do Unity 6, testy EditMode/PlayMode, integracja z rozgrywką i wizualna ocena w silniku wymagają osobnego wykonania. Żadnych zmian w WPN-001..006, prefabach, wspólnych scenach ani ProjectSettings.
