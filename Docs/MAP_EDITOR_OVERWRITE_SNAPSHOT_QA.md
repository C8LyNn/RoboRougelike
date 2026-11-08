# Map editor overwrite confirmation bound to draft

Base: `5408e990` (Draft PR #1114).

An overwrite confirmation now applies to both the target path and the draft snapshot. If the draft changes after the first click, saving requests a fresh confirmation. Normal editor edits also clear the pending button state. The existing atomic file replacement and naming rules are unchanged.

Verification on 2026-11-08: runtime and editor/test assemblies compiled using Roslyn with existing local Unity references (exit 0 / 0). The existing focused Unity test was extended to edit the draft between clicks and check that the original file remains unchanged until reconfirmation; it was NOT RUN. Unity, Windows Player and actual UI are deferred under the current lightweight-work constraint. Local ignored logs: `Artifacts/Local/CSharp-MapOverwriteSnapshot-20261108/`.

Manual follow-up: save a map, edit and click Zapisz to request overwrite, edit again, and click Zapisz. The first click after the second edit should request confirmation again; the on-disk file must remain unchanged until the following click. Keep a backup during testing.
