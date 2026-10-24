# VFX-001 v5 — integracja z RoboRougelike (Unity 6 URP)

## Baseline

Przed integracją potwierdź najnowszy SHA na GitHubie i faktyczny stan integracji MOV-001–MOV-009. Nie opieraj integracji wyłącznie na starym R6. Utwórz branch `feature/vfx001-combat-feedback-v5`, bez edytowania main/develop przed przeglądem.

## Przenoszenie

Skopiuj cały `Assets/RoboRougelikePackages/VFX001`, w tym `.meta`. Z menu Editor wygeneruj materiały URP. **W razie kompilacji CI i Windows dołącz generowane `.mat` w Resources do commita lub użyj równoważnego, zweryfikowanego zarządzania materiałami.**

## Wpięcie do kodu bez mutowania gameplayu

- Przy trafieniu po rozliczeniu HP: `new VfxEvent(VfxEventKind.Hit, worldPoint, worldNormal, material, intensity)` → `VfxFeedbackOrchestrator.Emit(evt)`.
- Przy odłączeniu: do pool zgłoś `VfxEventKind.Detached`; nie odłączaj części drugi raz.
- Przy wybuchu rdzenia: jeden `VfxCoreDestructionDirector.Begin` dla rzeczywistego zdarzenia, plus opcjonalny `VfxCameraImpulseReceiver.Accept`.
- Dla pokazania żaru na powierzchni modułu użyj `VfxHeatImpactController.PlayHeat(worldPoint,normal,intensity,material)`; maksymalnie trzy aktywne quady per moduł (domyślnie).
- Animacja dekoracyjnego mechanizmu: dodaj dziecko bez colliderów i rigidbody; wywołaj `VfxMechanicalJolt.Bind(visualChild)`, następnie `React(intensity, localDirection)` i `SetCondition(damage01,detached)`.
- Kamera: do dekoracyjnego rig-a kamery dodaj dziecko z komponentem `Camera`. Na nadrzędnym rig-u uruchom `VfxCameraImpulseReceiver.Bind(cameraChild)`; nie stosuj na gameplayowym kontrolerze kamery i nie współdziel transformu z inną animacją kamery.

## Priorytety

1. Kompilacja i izolowany showcase.
2. Trafienia i żar na jednym module, bez zmian HP.
3. Animacja uszkodzeń na samym *visual* child, nie na części fizycznej.
4. Impulsy kamery przy ważniejszych eksplozjach.
5. QA wydajności i per-material color logic.

## Regresja

Uruchom wszystkie testy R6/MOV, gry i VFX001. Manualnie sprawdź garaż, poligon, wyskakujące fragmenty, poruszające się pojazdy, reset sceny i pauzę. Mierz zachowanie z efektami i bez efektów, osobno GPU/CPU.
