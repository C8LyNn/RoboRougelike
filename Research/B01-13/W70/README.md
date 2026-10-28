# B01-13 / PASS14 — odbiór oryginalnego Windows Player QA dla CV-03 / CV-04 / CV-05

**To narzędzie nie uruchamia gry i nie zastępuje testu z człowiekiem.** Waliduje strukturę *prawdziwych* raportów z istniejących już trzech, dokładnie wskazanych komponentów oryginalnej gry. Kontrola CSV/HTML B01-13 z PASS13 jest kompletna; ten projekt nie dubluje jej.

## Natywne wejście — tylko gdy Codex potwierdzi legalne środowisko i izolację

Codex/inny upoważniony operator w osobnym **Windows Player** uruchamia trzy wbudowane, już istniejące batch-only QA (każdy osobnym procesem i osobnym katalogiem; dokładny EXE, SHA źródeł/builda i archiwum zapisuje w manifeście):

```powershell
$game = "C:\\verified-isolated-player\\RoboRougelike.exe"
$root = "C:\\external-evidence\\B01-13"
New-Item -ItemType Directory -Force "$root\\garage", "$root\\mov009", "$root\\editor" | Out-Null
& $game -batchmode -rrl-garage307-qa "$root\\garage" -logFile "$root\\garage\\Player.log"
$garageExit = $LASTEXITCODE
& $game -batchmode -rrl-suspension-ux-qa "$root\\mov009" -logFile "$root\\mov009\\Player.log"
$movExit = $LASTEXITCODE
& $game -batchmode -rrl-map-editor-qa "$root\\editor" -logFile "$root\\editor\\Player.log"
$editorExit = $LASTEXITCODE
```

**Uwaga:** nazwa EXE i zdatność builda do tych opcji muszą zostać potwierdzone przez operatora; pokazane polecenia są przygotowane do wykonania, a nie wykonane. Trzy kody wyjścia trzeba przepisać jako liczbowe `exit_code` (nie `false`, ani `null`); oryginalne raporty muszą pochodzić z oddzielnych procesów. `-batchmode` testuje API i fizykę tam, gdzie robi to wbudowany test, lecz **nie ocenia czytelności GUI przez użytkownika ani 720/1080 screenshotów**.

**Oryginalne pliki, przypięte do Codex #310 `231005c6d0351945a8ee6684f3856af118f0e94f`:**

| Rola/CV | Plik C#; oryginalny Git blob | Flaga, wynik |
|---|---|---|
| Garage / CV-03 | `Assets/R1/Garage307PlayerQA.cs` — `8a3e07bf374460a8f984bc2415bd134004847e79` | `-rrl-garage307-qa`, `garage307-player-qa.json` |
| MOV009 / CV-04 | `Assets/MOV009Integration/MOV009UxPlayerQA.cs` — `4188573482a49ffa01c717dc22ded3ca76bd3add` | `-rrl-suspension-ux-qa`, `suspension-ux-player-qa.json` |
| Editor / CV-05 | `Assets/MapEditor/MapEditorPlayerQA.cs` — `700895dfcb7516c527c810b69803a8e91f6c4e1b` | `-rrl-map-editor-qa`, `map-editor-player-qa.json` |

**Dokładne porównanie źródła użytego do obecnego Playera z bieżącym Codex HEAD:** `7299dcc5fd63ac042a0ffcd97b69d02fb8744649...231005c6d0351945a8ee6684f3856af118f0e94f` = **1 commit, tylko 11 plików `Docs/SuspensionUx309/**`, zero plików produkcyjnego C#**. Wydany Player był kompilowany ze SHA `7299dcc5...`, ZIP SHA256 `ec3193...`; zewnętrzny operator i tak musi niezależnie zweryfikować fizyczne bajty używanego builda. Nowe kompilacje MUSZĄ mieć nowe piny. Nie należy automatycznie podpinać starego ZIP do przyszłego HEAD.

### Odbiór

Skopiuj `receipt.template.json` jako `receipt.json` do katalogu `$root`; uzupełnij rzetelnie polecenia, pełne 40-hex SHA, wersję Unity, oryginalny ZIP SHA256, numeryczne kody wyjścia i dopiero po prawdziwym ukończeniu trzech procesów `runtime_status = ACTUALLY_EXECUTED`. Wszystkie odnośniki do plików dowodów są relatywne do `$root`.

```bash
python validate_player_receipts.py <EVIDENCE_DIRECTORY> \
  --expected-source-sha <EXACT_COMPILED_SOURCE_SHA_40> \
  --expected-build-sha <EXACT_PLAYER_BUILD_SHA_40>
```

`RECEIPT_STRUCTURE_PASS` = tylko dokumentacja jest poprawna i **self-reported** pola oryginalnych testów są pozytywne. Nie jest to niezależny G1 ani gwarancja autentyczności. B01-60/40 muszą porównać logi i build bytes, wykonać niezależny odbiór, rozdzielić `NOT_RUN` i własny native-gate HQ. Gdy źródło C# zostanie zmodyfikowane przez Codexa, receiver musi sprawdzić różnicę oryginalnych schematów pól i zaktualizować kontrakt na własnej nowej wersji, nie zmyślać wyników.

## Granice i już istniejące wpisy kolejki

- `CV-03` ma już scenariusze krytycznych braków broni/mobilności i grup LPM/PPM. Ta paczka dostarcza **realne nazwy wbudowanych runnerów**, precyzyjne wymagane pola i fail-closed kontrolę dokumentacji. B01-13 gotowy pięciostanowy manifest odtwarzania Light: `Research/B01-13/W66/vehicle_start_presets.json` (git blob `27d786f30e6cb74eb4ddbc2e876dfc674762d9d3`). Player GUI musi osobno przejść osobne widoczne ostrzeżenia braku GUN oraz mobility przy BOTH; sam `Garage307PlayerQA` tego nie mierzy.
- `CV-04` już obejmuje garaż → mapa → garaż i MOV009; oryginalny MOV009 runner bada faktyczny `ForceNewtons > 0`, ale nie percepcję użytkownika.
- `CV-05` obejmuje ghost/scale/map editing; oryginalny runner bada m.in. przejazd `maxZ`, real contact, finish i powrót do edytora, ale nie estetykę/czytelność 720/1080.
- **Badanie z ludźmi:** `HQ GATE D`, uczestnicy, świadoma zgoda i prywatny retencyjny magazyn wciąż **NOT_APPROVED / NOT_RUN**; nie można kierować tego do Codexa jako substytutu uczestników.

## Testy odbiornika (tylko parser / dane fikcyjne)

```sh
python test_receipt_validator.py
```

Wynik źródłowego lokalnego testu: 18/18 sprawdzonych przypadków parsera, w tym błędny SHA, wyjście `false` zamiast integer 0, fałszywy PASS, niekompletne pola, brak logów i traversal. **Zero testów Windows Player przeprowadzonych przez B01-13.**
