# Map editor unsaved-draft menu coverage

Base: `311d7a8b` (Draft PR #1115).

The existing return-to-menu confirmation now lives at the common `RestartRun` boundary when a map-editor session has an unsaved draft. The editor, map-test pause, ordinary pause, and the workshop garage display the pending confirmation. The confirmation is bound to the draft snapshot and a changed draft requires a fresh confirmation. Normal menu returns outside a map-editor session remain unchanged.

Explicit limitation: quitting the game, including native window close/Alt+F4 and the quit buttons, is not intercepted and may discard an unsaved map. No claim is made about those paths.

Verification on 2026-11-08: runtime and editor/test assemblies compiled using Roslyn against local Unity references (exit 0 / 0). Added a focused Unity test for first warning in the map-editor garage and changed-draft reconfirmation; NOT RUN. Unity, Player, actual menu transitions, controller input and layout: NOT RUN under the current lightweight-work constraint. Local ignored evidence: `Artifacts/Local/CSharp-MapExitCoverage-20261108/`.

Manual follow-up with a disposable map: edit without saving; try return to menu from the editor, map-test pause and editor garage, separately. In each case verify the first action warns and keeps the session; the second returns. Edit again between attempts and verify a fresh warning. Also check an unchanged saved map and a normal non-editor pause return. Visually inspect the warning in each layout.
