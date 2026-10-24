# M01-003 — Podwójny emiter łuku (dual arc emitter)

**Agent:** MODEL-01 (M01). **Zakres:** nowy model wizualny do Unity 6, bez logiki broni. **Źródło:** [katalog V1](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md).

## Rezerwacja
- ID: `M01-003`
- Nazwa z katalogu: **Podwójny emiter łuku**
- Atomowy CREATE: [commit `3b181c7`](https://github.com/C8LyNn/RoboRougelike/commit/3b181c7aad22d972071bb5ef6e338c370456597d)
- Plik: `Docs/ModelFactory/claims/M01-003.json` na **oddzielnej gałęzi kontrolnej**, nie w tym PR.

## Geometria, materiały i osie
- Skala: 1 jednostka = 1 metr; Y-up, +Z-forward; punkt montażu górnego gniazda: `MOUNT_TOP_SOCKET` w `(0,0,0)`.
- Dwa odseparowane rdzenie: lewy cyjan, prawy fiolet; osobne pierścienie miedziane, ceramiczne osłony, grafitowy korpus, bursztynowe oznaczenia.
- Siedem materiałów, w tym dwa emisyjne. Każda bryła jest oddzielną zamkniętą siatką trójkątów; połączenia między stałymi bryłami mogą celowo zachodzić na siebie jak w mechanicznym zespole.
- `SHIELD_SLIDER_L` i `SHIELD_SLIDER_R` są osobnymi rodzicami animowanych osłon; ruch lokalny X: odpowiednio -0,115 m i +0,115 m.
- Animacja referencyjna 31 klatek @30 FPS: zamknięte → szybkie rozsunięcie → utrzymanie → zamknięcie. Minimalny poziomy odstęp od stałego korpusu wynosi 0,020 m; osłony przesuwają się wyłącznie na zewnątrz, więc nie wchodzą w rdzenie. Dolne prowadnice są poniżej osłon.
- Emisja nie jest wymagana do odczytania bryły; materiał bazowy, ceramika i miedź pozostają czytelne bez bloom.

## Pliki
- `generate_m01_003.py`: powtarzalny generator Python. Z Blender `bpy` tworzy animowane FBX/GLB, bez Blendera tworzy rzeczywisty statyczny OBJ/MTL oraz JSON.
- `qa_m01_003.py`: niezależny test `--self-test`, topologii i eksportu OBJ/MTL bez bibliotek zewnętrznych.
- `.github/workflows/m01-003-asset-qa.yml`: CI generuje i udostępnia **rzeczywiste** statyczne pliki OBJ/MTL z manifestem jako artefakt workflow.

## Uruchomienie
```bash
python3 Tools/ModelFactory/M01/M01-003/qa_m01_003.py
python3 Tools/ModelFactory/M01/M01-003/generate_m01_003.py --out build/M01-003
blender --background --python Tools/ModelFactory/M01/M01-003/generate_m01_003.py -- --out build/M01-003
```

**Unity:** statyczny OBJ/MTL importuje się bez importera glTF; do GLB potrzebny importer, FBX jest natywny. Animacja nie jest mechaniką strzelania. Do wykorzystania ruchu należy kontrolować hierarchię shield sliders; nie zmieniono prefabów ani skryptów gameplay.

## Granice weryfikacji
- Wykonanie na CI: sprawdź [przebiegi M01-003](https://github.com/C8LyNn/RoboRougelike/actions/workflows/m01-003-asset-qa.yml).
- Blender FBX/GLB: **NOT RUN**, dopóki nie zostanie wykonany Blender.
- Unity import/Play Mode, wizualny przegląd animacji i finalna kontrola interpenetracji w silniku: **NOT RUN**.
- Analiza prześwitów jest konserwatywna (AABB w skrajnych klatkach); nie zastępuje wizualnej kontroli wszystkich elementów złożenia.
- Nie scalać PR do `develop` bez importu do Unity i oględzin.
