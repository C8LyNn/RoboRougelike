# Map editor: confirmation before discarding unsaved work

Base: `6137a460` (Draft PR #1112).

Loading an existing map over a draft that differs from the last successful save/load requires a second click. The confirmation is tied to the target path and the current draft snapshot: a subsequent edit or a different name requires a new confirmation. A failed load leaves the saved baseline unchanged. Loading without unsaved changes does not require confirmation. Returning to the main menu is not covered by this change.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn against existing local Unity references (exit 0 / 0). A focused Unity test was added for an edited draft, repeated editing while confirmation is pending, confirmed load, and subsequent clean load. The test was NOT RUN: Unity and Windows Player remain deferred under the current lightweight-work constraint. Local ignored compiler logs: `Artifacts/Local/CSharp-MapLoadConfirmation-20261108/`.

Manual follow-up: create and save a map, change it, choose Wczytaj once and verify the draft is preserved; choose again to load. Repeat after editing between clicks and after changing the name. Check the warning and button fit in the editor.
