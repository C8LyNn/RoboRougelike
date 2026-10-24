# VFX-001: v4 → v5

## Nowe klasy

- `VfxImpulseEnvelope`: czysta matematyka wygaszenia, redukcji siły z dystansem, limitowania przesunięć i odrzucania NaN/Infinity.
- `VfxMechanicalJolt`: tylko dekoracyjne transformy, `Bind`, `React`, `SetCondition`, `Tick`; przywraca pozycję i rotację po dezaktywacji.
- `VfxCameraImpulseReceiver`: odbiornik impulsów kamery, odległość, skala zdarzenia, ograniczenie liczby, możliwość ręcznego `Tick`, reset na disable.
- `VfxHeatImpactController`: maksymalna liczba nakładek żaru, wygaszenie w czasie, osobny shader, brak colliderów/Rigidbody.
- `VfxFeedbackOrchestrator`: opcjonalny dispatcher dla v5, emitujący VFX wyłącznie gdy przyjęty przez pool.
- `VFX001HeatGlowURP.shader`: emisyjny, nieteksturowany radialny marker trafienia.

## Zmiany w poprzednich plikach

- `Vfx001AssetBuilder`: nowy materiał `VFX001_HeatGlow` w Resources.
- `VfxMaterialLibrary`: pobieranie i fallback nowego materiału.
- `Vfx001Showcase`: nowe kontrolki, dedykowany visual camera child, pokazy uszkodzeń, poprawiony scroll GUI.

## Rozdzielenie systemów

Prawdziwa część pojazdu nadal odłącza się przez kod gameplayu. VFX nie robi tego samodzielnie. `VfxMechanicalJolt` nie wywołuje `Rigidbody.AddForce`, `MovePosition`, `transform.position` na korzeniu pojazdu ani nie zmienia celowania. `VfxCameraImpulseReceiver` modyfikuje tylko dziecko przeznaczone do efektów optycznych — nie kontroler kamery.

## Ograniczenia

- Gorące nakładki to płaskie quady; na zakrzywionych i nieregularnych blokach wymagają decal projektora lub geometrii konformalnej.
- Dodatkowe renderer-y i `MaterialPropertyBlock` mogą podnieść koszty URP. Stosować selektywnie.
- Wiele skryptów nie powinno modyfikować tego samego `Transform` w `LateUpdate`. Użyj odrębnych węzłów potomnych.
- Skrypt `VfxCameraImpulseReceiver` dołączaj do dedykowanego kontenera kamery. Kamera powinna być jego osobnym dzieckiem.
- `VfxFeedbackOrchestrator` nie ma dostępu do eventów produkcyjnych R6; Codex musi je podłączyć.
- Testy Unity i shader compilation nie były uruchamiane przy tworzeniu ZIP.
