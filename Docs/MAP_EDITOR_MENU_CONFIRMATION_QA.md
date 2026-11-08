# Map editor: confirmation on its menu button

Base: `347962bb` (Draft PR #1113).

The map editor's own "Powrót do menu" button now asks for a second click when the draft differs from its last successful save/load. The confirmation is bound to the current map snapshot; an edit or save cancels it. This protects that specific button only: the pause menu, native window close and exiting from a map test or garage are NOT covered and may still discard unsaved map edits.

Verification on 2026-11-08: runtime C# compiled using Roslyn and existing local Unity references (exit 0). Unity, Player and interactive exit/visual checks were NOT RUN under the current lightweight-work constraint. Local ignored log: `Artifacts/Local/CSharp-MapEditorMenuConfirm-20261108/runtime.log`.

Manual follow-up: change a draft, click the editor's menu button once and verify it remains open, change the map again and confirm a fresh warning is required, then test the second click. Repeat after saving and verify immediate return. Keep a backup of maps during testing.
