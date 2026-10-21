# VFX-001 — zmiany v2 → v3

Wersja 3 rozwija *ten sam pakiet* VFX-001 i zachowuje namespace oraz API v2.

## Nowe możliwości
- **VfxShockwaveRing** — proceduralna, animowana fala uderzeniowa dla Explosions/CoreDestroyed, montowana na pożyczonym obiekcie puli.
- **Etapowanie wybuchu** — błysk i iskry natychmiast, odłamki 45 ms później, dym po 110 ms; wszystkie efekty pozostają czysto kosmetyczne.
- **PersistentDamageFxController** — ciągły dym zależny od uszkodzenia; gaśnie po odłączeniu modułu.
- **EngineExhaustTrailController** — emitowanie strumienia w przestrzeni świata ze zmiennym ciągiem i profilem uszkodzenia.
- **GroundContactVfxController** — ślady poślizgu + pył kontrolowane prędkością, poślizgiem i informacją o kontakcie z podłożem.
- **VfxDamageResponse** — klasyfikacja stopni uszkodzeń bez modyfikacji HP gry.
- Dwa kolejne shadery URP, przygotowane w tym samym editorowym generatorze materiałów.
- Rozbudowany demonstrator i testy.

## Naprawy / decyzje
- **Pauza:** pooled bursts żyją w czasie skalowanym (`Time.time`), by przy `timeScale=0` nie wygasały przed cząsteczkami.
- **Bez fizyki VFX:** ring, trail, smoke i marks nie dodają żadnych Rigidbody / Collider ani sił.
- **Kompatybilność:** klasy v2 zachowane; żaden nowy komponent nie podmienia API ruchu, uszkodzeń lub garażu.

## Świadome ograniczenia
- Zawieszenie dymu / płomieni podczas pauzy odpowiada `ParticleSystem` w czasie skalowanym.
- TrailRenderer do śladów ruchu jest demonstracyjny: **w produkcji należy położyć punkty kontaktu przy gruncie**, dodać kontrolę warstw i przetestować nawierzchnie.
- Brak artystycznych flipbooków; efekty są proceduralne.
- Brak testów silnika Unity w środowisku przygotowywania ZIP; Codex musi wykonać import, testy, wizualne QA i build.
