# Changelog VFX001 v1 → v2

## Fixes
- Runtime `.asmdef` przeniesiono do `Runtime/`, aby objąć wszystkie podkatalogi.
- `TransientAutoDestroy` planuje usunięcie dopiero po `Start`, po konfiguracji lifetime.
- Domyślne particle systemy mają własny materiał z proceduralną teksturą alpha.
- Thruster nie skaluje parent/Rigidbody; recoil dotyka wyłącznie modelu lufy.
- DamageTintController używa `_BaseColor`/`_Color`, nie fikcyjnego `_Damage01`.

## New
- `VfxEvent`, `VfxPresetResolver`, `VfxBudgetPolicy`.
- `VfxPoolManager` i `VfxBurstActor`: pooling, cztery warstwy, reset.
- `VfxEventBatcher` (grupowanie tylko zweryfikowanych detached event).
- `ExplosionFeedbackPlayer` z wybuchem, śmiercią rdzenia i pyłem lądowania.
- Shader `ProceduralParticleURP`, Shader `ProceduralFlameURP`.
- Scena demo generowana z menu Unity i moduł przygotowania materiałów.
- EditMode i PlayMode tests.

## Known limitations
Bez wykonania Unity Test Runner w tym środowisku nie można zadeklarować PASS; materiały, playmode, wygląd i integracja do gry wymagają QA przez Codexa.
