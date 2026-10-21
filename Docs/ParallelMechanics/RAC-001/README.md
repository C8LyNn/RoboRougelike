# RAC-001 — pakiet niezależnej mechaniki (bootstrap R0)

Powiązane zadanie: https://github.com/C8LyNn/RoboRougelike/issues/8

## Stan

Wyścig na czas z checkpointami w kolejności; odczytuje wyłącznie Transform uczestnika, nie modyfikuje sterowania.

- Unity: **6000.3.14f1**. Pakiet działa dopiero po świadomym umieszczeniu komponentu na obiekcie w osobnej scenie testowej lub zewnętrznym bezkolizyjnym adapterze.
- **Nie jest automatycznie podłączony do istniejącej sceny Gauntlet**; import kodu nie oznacza integracji gameplay.
- Repo nie zawiera dowodów wykonanych testów Unity tego pakietu: **NOT RUN**. Niezbędne kolejne kroki: EditMode/PlayMode, demo opt-in, bezpieczny adapter, CI, Play test w głównej scenie.
- Zero zmian w scenie 1, plikach R1/R3/R4/R6, istniejących assetach, MOV, ProjectSettings, Codex PR #1, MAP/COM/VFX.

## Weryfikacja i dalsze etapy

Zobacz `Docs/DEVELOPMENT_WORKFLOW.md`. Po dołożeniu testów w ramach własnego katalogu używaj `Tools/CI/Run-Unity.ps1` na osobnym worktree z właściwą licencją Unity. Raportuj liczbę wykonanych przypadków, logi, SHA i blokery w issue. Brak uruchomienia oznaczaj **NOT RUN**, nigdy PASS.

## Bezpieczny zakres

Wyłącznie `Assets/RoboRougelikeMechanics/RAC001CheckpointRace/**` i `Docs/ParallelMechanics/RAC-001/**` oraz metadane katalogu `Assets/RoboRougelikeMechanics/RAC001CheckpointRace.meta`. DRAFT PR do `develop`, bez auto-merge. Zachowaj GUID przy dalszych zmianach.
