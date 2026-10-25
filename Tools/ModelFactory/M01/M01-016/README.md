# M01-016 — Rozkładane działo obrotowe

Źródło: katalog ModelFactory V1, kolejka M01. Tylko model wizualny, bez implementacji WPN.

## Projekt
Kompaktowe czterolufowe działo z obracającym się zespołem luf oraz dwoma niezależnie składanymi ramionami bocznymi. Segmentowe ceramiczne osłony ramion, tytanowe prowadnice, grafitowy korpus, miedziane żebra chłodzenia i bursztynowe sygnały. Kolor cyjanowy nie jest jedynym sposobem identyfikacji ruchomych części.

**Układ osi w Blenderze:** Z-up, -Y-forward. **GLB/Unity:** Y-up, +Z-forward (eksport Y-up). 1 jednostka = 1 metr. Punkt `MOUNT_TOP_SOCKET` = (0,0,0) w podstawie. Brak zmian w prefabach, scenach, ProjectSettings i mechanikach WPN.

## Animacja
`ROTARY_DRUM_Y_AXIS`: 4 obroty w 73 klatkach / 60 FPS. `LEFT_FOLD_Z_AXIS` i `RIGHT_FOLD_Z_AXIS`: od 0° do ±30° (klatki 1–26), pozycja otwarta do 50, powrót do 73. Każde ramię ma własny pivot i elementy podrzędne.

## Wykonanie
```bash
blender --background --python Tools/ModelFactory/M01/M01-016/generate_m01_016.py -- --repeat --out Tools/ModelFactory/M01/M01-016/generated
python3 Tools/ModelFactory/M01/M01-016/qa_m01_016.py --out Tools/ModelFactory/M01/M01-016/generated
```
Eksporty: .blend, .glb (animacja), .fbx (animacja), .obj + .mtl (statyczny), _qa.json. Pipeline GitHub Actions uruchamia rzeczywisty Blender i publikuje artefakty dopiero po testach.

## Kryteria
Generator zeruje poprzednią scenę i materiały, po ponownym uruchomieniu weryfikuje liczebność obiektów. Każda siatka musi być zamknięta i bez zdegenerowanych powierzchni. 73 próbki AABB sprawdzają przecięcia pomiędzy zespołami ruchomymi oraz obudową. Elementy w obrębie jednego zespołu mogą celowo stykać się ze sobą. AABB nie dowodzi braku wszystkich kolizji siatek ani nie zastępuje testu Unity. Walidator niezależnie czyta GLB i sprawdza trzy animowane węzły.

## Integracja
Model nie jest jeszcze podłączony do Unity. Import, testy EditMode/PlayMode i ocena wizualna wymagają osobnego etapu QA. PR pozostaje Draft.
