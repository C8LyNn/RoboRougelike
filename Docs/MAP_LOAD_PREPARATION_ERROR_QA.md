# Map load preparation error feedback

Base: `58591c79` (Draft PR #1117).

Errors while validating/enumerating the map directory or preparing the current draft for confirmation are now caught before loading; the editor reports the error and cancels the pending confirmation. This does not change map storage or parsing and does not mutate the draft in the error handler.

Verification on 2026-11-08: runtime C# compiled with Roslyn against local Unity references (exit 0). Actual inaccessible-directory errors, Unity, Windows Player and UI were NOT RUN under the current lightweight-work constraint. Local ignored log: `Artifacts/Local/CSharp-MapLoadError-20261108/runtime.log`.

Manual follow-up in an isolated test profile: make the map directory inaccessible, attempt loading with an edited draft, and check that the error is displayed and the draft remains unchanged. Restore permissions afterward. Do not change access to the real player's map directory for this test.
