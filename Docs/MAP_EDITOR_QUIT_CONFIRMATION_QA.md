# Unsaved map: confirmation when quitting from pause

Base: `b7ff03f2` (Draft PR #1121).

The pause-menu quit button and its keyboard/controller action request confirmation when the current map draft is unsaved. Returning to the main menu and quitting are separate actions: confirming one does not authorize the other, and a changed draft requires a fresh confirmation. Resuming and normal editor transitions retain the cancellation behavior from earlier work. This does not alter map saves or gameplay.

Scope: native X and Alt+F4 still close normally without a prompt; the exceptional error-screen quit button also remains unchanged. Those paths may still discard an unsaved map. The confirmation was not added to `Application.wantsToQuit`.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn using existing local Unity references (exit 0 / 0). Nine offline reflection checks of the compiled decision function passed, covering clean/dirty snapshots, both actions, action switches and subsequent edits. These do not exercise Unity, the GUI, input devices, or process shutdown. A targeted Unity test was added but NOT RUN. Windows Player and human interaction: NOT RUN under the current request for lightweight work without PC control. Local ignored logs: `Artifacts/Local/CSharp-MapQuitConfirm-20261108/`.

Manual follow-up: on a disposable map, edit and open pause in both editor and map test. Select quit once; verify the warning, then switch to return-to-menu and verify a fresh confirmation is required. Resume and verify the warning resets. Test keyboard, mouse and controller. In a separate process verify that a confirmed quit exits; do not use a valuable unsaved map for X/Alt+F4 tests.
