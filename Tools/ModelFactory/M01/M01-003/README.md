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
- Animacja referencyjna 31 klatek @30 FPS: zamknięte → szybkie rozsunięcie → utrzymanie → zamknięcie. Minimalny poziomy odstęp osłon od stałego korpusu wynosi 0,020 m; osłony przesuwają się wyłącznie na zewnątrz, więc nie wchodzą w rdzenie. Dolne prowadnice i stopki ślizgowe są połączone konstrukcyjnie; między metalową prowadnicą a stopką pozostaje kontakt na płaszczyźnie bez przenikania.
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

## Poprawka V2 — ciągłe prowadzenie ruchu osłon (24.10.2026)

**Usterka:** w pierwotnym generatorze dwie `Shield_Guide_*` miały szerokość X zaledwie 46 mm i długość w osi Z 475 mm, choć osłony przesuwały się na zewnątrz po osi X o **115 mm**. Dodatkowo prowadnice były odsunięte od stałego korpusu o **33 mm w poziomie** oraz od spodniej powierzchni osłon o **41 mm w pionie** — nie miały rzeczywistej ścieżki nośnej ani elementów ślizgowych.

**Naprawa:** wydłużono obie nieruchome miedziane prowadnice w osi X do **230 mm** (środek |X| = 465 mm), pozostawiając wysokość Y od 245,5 do 280,5 mm i długość Z 475 mm. Ich wewnętrzne końce zachodzą na sztywny korpus grafitowy co najmniej **15 mm na osi X**. Dodano dwie ruchome części `Shield_Slider_Shoe_L/R` o wymiarach 45 × 39 × 350 mm i środku Y = 300 mm. Ich górna powierzchnia jest zakotwiona w osłonie na **3 mm**, dolna leży na górnej powierzchni prowadnicy (zerowa nominalna szczelina pionowa). Obie stopki są dziećmi oryginalnych `SHIELD_SLIDER_L/R`, dlatego podążają z osłonami w całym skoku. Przez cały ruch stopki pozostają w granicach swoich prowadnic, z dodatnim zapasem przy obu krańcach.

**Rzeczywiste QA:** [GitHub Actions 40120310833 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40120310833). Uruchomiono samodzielny generator i niezależny `qa_m01_003.py`, który analizuje **wierzchołki rzeczywistego OBJ**, zgodność wszystkich brył z kodem źródłowym, połączenie prowadnica–korpus, stopka–osłona, styczność prowadnica–stopka i **42 pozycje ruchu** (21 na stronę). Gotowy statyczny model zapisano jako artefakt `M01-003-V2-connected-shield-rails-OBJ-MTL`. Model ma teraz **37 części / 1670 wierzchołków / 3232 trójkąty / 7 materiałów**.

**Ograniczenia:** animacja jest zdefiniowana w źródłowym skrypcie Blendera, ale faktyczne uruchomienie `bpy`, eksport animowanego FBX/GLB i Unity EditMode/PlayMode pozostają **NOT RUN**. Statyczne QA nie potwierdza wizualnej jakości ślizgu, tarcia ani współpracy z istniejącym gameplayem. Nie zmieniono mechanik WPN, scen, wspólnych prefabów ani ProjectSettings. Poprawkę należy zrewidować w istniejącej gałęzi pierwotnego Draft PR #52.
