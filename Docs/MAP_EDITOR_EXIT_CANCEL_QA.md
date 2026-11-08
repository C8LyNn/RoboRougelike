# Cancel pending map-exit confirmation

Base: `896ea01b` (Draft PR #1116).

Resuming after pause, entering/leaving the map-editor garage, and entering/leaving a map test cancel any pending return-to-menu confirmation. The next return attempt therefore requests confirmation again if the draft is still unsaved. The garage gives that warning priority over its ordinary status text. Normal application quit remains unguarded as described in the preceding QA report.

Verification on 2026-11-08: runtime and editor/test assemblies compiled using Roslyn with local Unity references (exit 0 / 0). The targeted Unity test was extended to pause, resume and verify a fresh confirmation; NOT RUN. Unity, Windows Player and visual/interactive checks: NOT RUN under the lightweight-work constraint. Local ignored compiler logs: `Artifacts/Local/CSharp-MapExitCancel-20261108/`.

Manual follow-up with a disposable map: trigger the warning, resume or return to editing, reopen the menu and confirm the next return attempt warns again. Check the warning is visible in the garage.
