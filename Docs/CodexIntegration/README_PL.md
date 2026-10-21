# RoboRougelike — przygotowanie 12 paczek pod Codex (R1, 2026-10-21)

Repozytorium: `C8LyNn/RoboRougelike`. Unity: `6000.3.14f1`. Ostatni obserwowany `develop` SHA: `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Zawsze odśwież HEAD przed pracą.

**Pakiet źródłowy:** `RRL_CODEX_READY_ALL_PACKAGES_R1.zip` (SHA-256 `bbbc3bdf74ad1e4445fc5ca231eddc60f189090cc68104e6a23e1b1f9469407b`), przygotowany jako osobny, pobieralny artefakt. **Nie jest automatycznie dostępny w GitHubie**: Codex musi dostać ZIP lub ścieżkę do jego rozpakowanego katalogu. Nie odtwarzaj kodu źródłowego z samych raportów.

## Wynik audytu źródeł

Zidentyfikowano 18 oryginalnych ZIP; wszystkie przechodzą porównanie manifestów SHA-256. **12 paczek gotowych do izolowanego importu i dalszej implementacji**:

- MAP-R6 (R3/R4/R5 są wersjami referencyjnymi, nie wolno nakładać ich na R6).
- VFX-001-v5 (zastępuje v2/v3/v4; nie importować wszystkich wersji).
- WPN-001 Shotgun, WPN-002 Railgun, WPN-003 Beam Cutter, WPN-004 Rocket Launcher, WPN-005 Rotary Saw, WPN-006 EMP Emitter.
- DEF-001 Directional Shield, DEF-002 Point Defense.
- COM-001 Structural Scan.
- MOV-011 Independent Steering.

MOV-001…009 są już zintegrowane i nie wymagają ponownego importu. Dla MOV-010 brak odnalezionych źródeł — WAIT / PACKAGE_UNAVAILABLE.

Kit zawiera `verify_and_stage.py`, `registry.json`, 12 nowych `packages/<ID>/CODEX_TASK_PL.md`, `docs/CURRENT_GITHUB_CONTRACTS_PL.md`, `docs/INTEGRATION_ORDER_PL.md`, `docs/QUALITY_GATES_PL.md`, `docs/CODEX_MASTER_DISPATCH_PL.md` i źródłowe ZIP. Importer odmawia nadpisywania istniejących plików, kolizji Unity GUID i pracy w nieczystej/produkcyjnej gałęzi.

## Istotne prawdziwe punkty integracji

- `Assets/R3/R3VehicleModel.cs`, `R3VehicleAssembly.cs`, `R3GarageView.cs`: części, HP, koszt, montaż, brak osobnego inventory/HP.
- `Assets/R4/R4WeaponDefinition.cs`: obecnie tylko Basic/Automatic/Heavy, `Get()` ogranicza indeks do 0..2. Dodanie nowych enumów bez rejestru broni **nie zadziała**.
- `Assets/Pack/P12_PipelineSafeDamageable.cs` i fizyczne pociski: jedna ścieżka obrażeń, obowiązkowy adapter DEF001 przed HP; WPN002 penetracja bloków.
- `Assets/R6/R6VehicleDrive.cs` i `Assets/MOVSeriesIntegration/MOVSeriesVehicleAdapter.cs`: nie tworzyć drugiego solvera RigidBody dla MOV011.
- `Assets/R5/R5Arena.cs`, `R5RunPlan.cs`, `Assets/R6/R6TestRange.cs`: MAP-R6 najpierw jako izolowane demo i evidence, dopiero później w Gauntlet.
- `Assets/R6/R6CombatFeedback.cs`: VFX-v5 przez zdarzenia bez zmiany obrażeń/kolizji/fizyki.

**Przed zależnymi integracjami rozstrzygnij Draft PR #1** (jedna faza 0–3 nagród; naprawy; 4 przeciwników; debris), aktualnie bez potwierdzonego QA Unity. Nie nadpisuj tej pracy, nie zakładaj 522/522 PASS dla nowych funkcji.

## Weryfikacja

Z kitu: `python verify_and_stage.py --verify-all`; potem tylko w czystej gałęzi `feature/*`: `python verify_and_stage.py --package WPN-001 --repo <CHECKOUT>` (dry-run), ewentualnie `--apply` po przeglądzie. Po imporcie Unity musi wygenerować brakujące NOWE .meta tam, gdzie paczka ich nie zawiera. Nie modyfikuj GUID już istniejących.

**Import ≠ integracja.** Wymagane: kompilacja Unity, testy paczki EditMode/PlayMode, rzeczywiste adaptery garaż/HP/pociski/AI/łupy/HUD, poligon, regresja MOV001–009, Windows build i manual QA. Tylko osobne Draft PR do `develop`; bez automatycznego merge/release.

MAP-R6: offline 34/34 Python PASS, Unity / przejazdy Rigidbody NOT RUN. Zob. w oryginalnym ZIP właściwy dokument `Docs/R6_CODEX_HANDOFF_PL.md`, nie starszy handoff R5 o tej samej ogólnej nazwie.
