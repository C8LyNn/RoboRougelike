# B01-11 P17 W24 RECOVERYHUD001/R1

NEW original Unity C#9 read-only HUD component, not a garage simulator. RecoveryNoticeBuilder maps strictly host-supplied RunId, SlotId, Epoch, single-owner, world and durable-commit proofs to clear Polish text. RecoveryNoticePanel : MonoBehaviour renders those texts in four inspector-connected UnityEngine.UI.Text fields. OnDisable clears stale ready text. NO Save, wallet, battle, loaner or repair write actions.

Approved W24 economy represented as text: one free rebuild per authentic victory only when owner-supplied flag says available; parts destroyed in victory N wait for NEXT distinct victory; 65%-of-purchase paid quote displayed verbatim from owner (no invented integer rounding); ONE weaker loaner offered only if Core alive, normal build genuinely nonfunctional, next eligible challenge, one-shot not spent, and verified host explicitly authorizes offer; functional builds remain deployable even with optional broken parts. UI is NOT a combat or authorization gate.

REAL UNITY WIRING (future authorized disposable Unity6 scene only): drag new Assets/B01Packages/RECOVERYHUD001 package with all .meta files, attach RecoveryNoticePanel to a GameObject with Canvas, assign heading/explanation/detail/severityLabel Text references in inspector. Canonical host must call Present(trustedRead, expectedRun, expectedSlot, expectedEpoch) from main thread AFTER exclusive true V3 Save/physical read. UI flags are caller supplied and forgeable; real authority must verify separately. No game action buttons.

Original direct-source QA: python Tools/B01/B01-11/P17W24RecoveryHud001/verify_package.py ; dotnet run --configuration Release --project Tools/B01/B01-11/P17W24RecoveryHud001/RecoveryHud.Tests.csproj

Actual original runtime C# source is compiled in .NET8 with explicitly synthetic UnityEngine/UnityEngine.UI stubs; this is SOURCE QA, NOT Unity Editor/PhysX. Positive/negative tests cover authority/stale ID, uncertainty, dead Core, optional parts, pending next win, loaner once, host price/rounding, UI text/clear and 150 generated variants. Independent formal G0 and activated Unity Windows G1 pending.

Source starts immutable main 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1. No edits of frozen #510/#556/#596/#642/#647/#690, canonical #729 or selected #749; #753/#741 not imported. No merge or foreign files. Package does NOT choose Save owner. Receiver B01-20/B01-22 UI+Economy, B01-23/62 canonical session, B01-40 physical G0, B01-61/41 independent QA, B01-60 native G1, B01-00 integration.

Gates: SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; LIVE_GAME_SAVE=NOT_APPLICABLE; GAME_INTEGRATED=NO; CODEX_ACK=NONE; G2=HOLD; INTEGRATOR_FEEDBACK=NOT_MEASURED.
Safe rollback: close this Draft; any later authorized scratch import remove only original package files after asserting no foreign modifications; never delete shared Assets folders.
