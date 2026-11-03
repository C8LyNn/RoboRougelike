# B01-60 — COURSE-DRIVE-BROWSER-QA-001 / PASS 17 WAVE 02

**独立ne / niezależne QA** działającego pliku HTML z PR #365. Paczka zawiera **realny wykonywalny test Chromium**, nie kolejną prezentację lub źródłowe testy Node należące do autora. Wykonuje sześć interakcyjnych testów UI, klawiatury, trasy i rozdzielczości oraz rygorystyczny test integralności. Funkcje gospodarza zawierają 18 map wyprodukowanych przez oryginalny C#.

## Pochodzenie i własność
- Producent B01-20: [PR #365](https://github.com/C8LyNn/RoboRougelike/pull/365), dokładnie `1a98ea3e1fc26c889c100aa1f554c804c3a47663`; źródło bazowe #331.
- **Nie edytowano żadnego pliku producenta**, `Assets/**`, aktualnej gry Codexa, CHL ani PARTSIZE.
- QA branch jest stosowany na niezmienionym HEAD producenta; PR ma base `b01-20/p17-course-drive-interactive`, aby nie wyświetlać jako własnych jego plików.
- Test wymaga oryginalnych blobów źródłowych i oryginalnego SHA256 pojedynczego zbudowanego HTML. Gdy źródło nie jest identyczne, kończy się FAIL zamiast testować inny prototyp.

## Odtworzenie
W izolowanym checkoutcie tej gałęzi, z Git, Python 3.11, .NET SDK 8 i Playwright Chromium:

```sh
dotnet run --project Research/B01-20/P17/MAPGEN-COURSE/MapgenCourseP17.csproj --configuration Release -- Research/B01-20/P17/MAPGEN-COURSE/presets.json p17-source-maps
python Research/B01-20/P17/MAPGEN-COURSE/verify_courses.py p17-source-maps
python Research/B01-20/P17/COURSE-DRIVE-001/build_viewer.py p17-source-maps
python -m pip install playwright==1.56.0
python -m playwright install chromium
python Tools/B01/COURSE-DRIVE-BROWSER-QA-001/browser_qa.py --html p17-source-maps/COURSE-DRIVE-001.html --output qa-receipts/receipt.json --screenshots qa-receipts/screens
```

Właściwy CI uruchamia całość na rzeczywistym hostowanym runnerze i zachowuje `receipt.json` oraz cztery PNG (320×720, 768×1024, 1280×720, 1920×1080). **Interakcyjne testy w prawdziwej przeglądarce**: 18 map × 2 pojazdy, sterowanie W, pauza, spacja, R, Light/Heavy autopilot przez checkpointy, brak HTTP requestów i nieobsłużonych wyjątków, brak horizontal overflow na wybranych rozdzielczościach.

## Kryteria i decyzje
- Zaliczenie pojedynczego testu nie oznacza Native Unity/PhysX, pełnej kontroli kolizji, human playtestu lub gotowości map do gry.
- Wykryty FAIL => reprodukcja wraz z JSON/screenshotami i przekazanie do B01-20; **nie zmieniaj plików producenta w QA**.
- `SOURCE/CHROMIUM` może zostać odebrane przez B01-40; `G1(Unity)=NOT_RUN`, `G2=HOLD`, `CODEX_ACK=PENDING`.
- Gdy producent stworzy kolejną rewizję, wymaga ona nowego jawnie przypiętego SHA i retestu. Nie aktualizuj po cichu referencji.

## Import i rollback
Narzędzie jest opcjonalnym zestawem QA, a nie importowaną mechaniką Unity. Codex może przejrzeć wyniki jako preflight oryginalnych torów, ale nie kopiować HTML/JS do gry. Rollback usuwa tylko `Tools/B01/COURSE-DRIVE-BROWSER-QA-001/**` i dedykowany workflow. **Bez merge do main/develop bez zgody właściciela.**
