# M03-001 — Klatka bazowa 1 × 1 × 1 m

**Agent:** MODEL-03 (konstrukcje) · **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1` · **Claim:** `Docs/ModelFactory/claims/M03-001.json` na tej samej gałęzi.

## Co powstało

- `generate_m03_001.py` — kompletny deterministyczny generator Blender Python (`bpy`), projektujący ramę 1 m z 8 narożnikami, 12 belkami, 4 skośnymi rozpórkami, 24 dekoracyjnymi zaślepkami, 4 wskaźnikami świetlnymi, **6 fizycznymi płytami montażowymi**, **8 żebrami** górnego i dolnego portu oraz 6 logicznymi punktami SNAP.
- `M03-001.obj` + `M03-001.mtl` — **rzeczywisty tekstowy model 3D** (statyczny podgląd i fallback do importu Unity), wygenerowany niezależnie od Blendera; 66 zamkniętych elementów, 2032 wierzchołki, 1436 wielokątów, 5 materiałów.
- `M03-001.static-qa.json` — wynik sprawdzenia siatek OBJ, ich domknięcia i wymiarów.
- `validate_static.py` — powtarzalny walidator z biblioteki standardowej Pythona.

**Ważne:** nie jest to nowa mechanika gry. Obiekt nie ma animacji, bo zgodnie z katalogiem jest nieruchomą ramą. Śruby/dekale na węzłach oraz połączenia końcówek rozpórek z odlewami są celowymi stykami/zakładkami; pozostałe elementy mają oddzielne bryły.

## Uruchomienie i eksport FBX w Blenderze

W Blenderze 3.6+ / 4.x:

```bash
blender -b --factory-startup --python Tools/ModelFactory/M03/M03-001/generate_m03_001.py
```

Generator zapisuje `export/M03-001.fbx` i `export/M03-001.manifest.json` obok skryptu. Aby pominąć eksport podczas testu geometrii, ustaw `RRL_SKIP_EXPORT=1`. Aby wskazać folder eksportu, ustaw `RRL_MODEL_OUTPUT` na jego pełną ścieżkę. Ponowne uruchomienie w otwartej scenie usuwa wyłącznie własną kolekcję `RRL_M03_001`.

Dla Unity preferowany jest wygenerowany FBX: zawiera pusty obiekt root w środku bryły i sześć `SNAP_*` w odległości 0,5 m od środka (po jednej osi na ścianę). Importowane skale: 1 jednostka = 1 m; po imporcie sprawdź w Unity położenia SNAP, osie, materiały i kierunki normalnych. `OBJ` jest gotowym **statycznym** zamiennikiem, ale nie przenosi obiektów `SNAP_*` — ich współrzędne zapisano w `M03-001.static-qa.json`. Przestrzeń współrzędnych OBJ jest taka jak w Blenderze (Z w górę); zweryfikuj konwersję osi podczas importu do Unity (Y w górę).

Model przeznaczony do podpięcia pod istniejący system budowy, bez zmian w logice, scenach ani ustawieniach projektu. Kolizje fizyczne i gameplayowe porty montażowe wymagają integracji po osobnym review.

## Walidacja

```bash
python Tools/ModelFactory/M03/M03-001/validate_static.py
```

Walidator sprawdza składnię Pythona generatora, liczbę elementów, kompletność materiałów, wymiary 1 × 1 × 1 m i domknięcie siatki każdego komponentu OBJ. Dodatkowe asercje są w generatorze `bpy` i uruchomią się podczas wykonania Blendera.

**Stan faktyczny:** wygenerowano OBJ/MTL i przeprowadzono walidację strukturalną przy ich budowaniu (domknięcie wszystkich 66 komponentów, zakresy ±0,5 m, 5 materiałów). W tym środowisku **Blender nie jest zainstalowany (NOT RUN)**, **Unity nie jest dostępne (NOT RUN)**, zatem nie ma zweryfikowanego eksportu FBX ani testu importu w Unity. Statyczny walidator Pythona jest do uruchomienia po pobraniu repo; nie deklarujemy jego wykonania w Blenderze.

**Integracja:** dopiero po niezależnym QA i zatwierdzeniu PR. Nie scalaj automatycznie do `develop` ani `main`.

## Poprawka strukturalna V2 — fizyczne porty (24.10.2026)

**Przyczyna:** wersja V1 miała sześć `SNAP_*` (lokalizacja i orientacja), lecz bez fizycznych płyt stykowych na środkach ścian. Górny i dolny SNAP nie miały podpór łączących je ze szkieletem. Port mógł przyciągać moduł logicznie, ale brakowało odpowiedniej reprezentacji konstrukcyjnej.

**Zmiany:** sześć płyt 0,20×0,20×0,06 m na środku każdej ściany; zewnętrzna powierzchnia dokładnie na `±0,5 m`. Każda płyta X/Y styka się z ukośną rozpórką (zachodzenie około 8 mm). Przy Z+/Z− dodano po cztery żebra łączące centralną płytę z belkami obwodowymi. Wszystkie elementy mieszczą się w obrysie 1×1×1 m. Znaczniki `SNAP_*` wskazują odpowiednią płytę poprzez właściwość `rrl_contact_plate`.

**Artefakty:** `generate_m03_001.py`, `M03-001.obj`, `M03-001.mtl`, `M03-001.static-qa.json`, `validate_static.py` i osobny workflow QA odpowiadają wersji V2. OBJ ma 66 zamkniętych części / 2032 wierzchołki / 1436 wielokątów. Sam OBJ nie przenosi pustych obiektów `SNAP_*` — do tego służy generator FBX i współrzędne z manifestu.

**Weryfikacja:** [GitHub Actions run 40096135238 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40096135238) obejmuje składnię generatora, watertight/manifold OBJ, sześć rzeczywistych płyt stykowych, osiem podpór oraz zgodność statycznego manifestu. Późniejsze zmiany dokumentacyjne nie zmieniły geometrii.

**Ograniczenia:** Blender i Unity: **NOT RUN**; fizyka colliderów, połączenie SNAP z systemem budowania, materiały URP i kontrola wizualna wymagają osobnego testu. Bez ingerencji w kod gameplayu i bez automatycznego scalenia na `develop`.

**Integracja:** poprawka przeznaczona do macierzystego Draft PR #39, nie jest nowym zadaniem w katalogu.
