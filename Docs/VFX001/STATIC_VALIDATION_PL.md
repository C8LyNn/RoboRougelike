# VFX-001 v4 — walidacja statyczna

Testowano: istnienie plików .meta, unikalność GUID, JSON asmdef, obecność tagów URP/HLSL, orientacyjne zbilansowanie nawiasów C#, struktura ZIP i SHA-256. Wyniki JSON: `STATIC_VALIDATION_RESULT.json`.

**NIE wykonano kompilacji C#, Unity Test Runner ani Windows build.** Przygotowane przypadki testowe wymagają uruchomienia w rzeczywistej instalacji Unity 6000.3.14f1.

Szczególnie wymagają manualnej kontroli: warianty cząsteczek 3D, poprawny shader ArmorScar, zachowanie quadów na ścianach nachylonych, wygląd płomienia/dymu, pooling, współpraca z R6/MOV.
