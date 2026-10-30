# B01-32-REPAIR-WELD-VFX-001 · PASS 17 / WAVE 06
**Original functional cosmetic Unity LineRenderer weld arc and 8 procedural sparks, no sprite flipbook.** This is a distinct repair-action VFX, not B01-32 WheelWake, salvage/reward beacon, part damage decals, muzzle/trails, impact or detach.

The exact **original C#9** `WeldVisualCore` implements explicit **Idle → Welding → Cooling → Idle** state transitions, monotonic visual progress, finite/invalid input rejection, deterministic seeded spark lines, 0.65-second cooldown and cancellation with no stale lines. The original Unity `B0132RepairWeldVfx` creates and reuses 9 `LineRenderer` objects (one central electric-blue/gold arc and eight orange weld sparks). It requires no shader files/texture imports or physics; it only uses Unity rendering APIs. Public opt-in methods are `BeginWeld(uint seed)`, `SetVisualProgress(float normalized)`, `FinishWeld()`, `CancelWeld()`, `SetVisualsEnabled(bool enabled)`.

**No gameplay integration**: This package never changes HP, damaged parts, inventory, repair costs, loot, physics, save data, or a real repair event. Only Codex may call it after selecting the current authoritative repair lifecycle. No new adapter or event guessed. No canonical game changes.

## Physical Unity package + preview
GitHub Actions produces `B0132-RepairWeld-UnityImport.zip` containing actual `Assets/B0132RepairWeld/` original C# runtime, prefab `Resources/B0132_RepairWeld.prefab`, source `.meta` with deterministic GUIDs and isolated runtime/PlayMode asmdefs. Binary artifact also contains seven original **512×512 transparent PNG** diagnostic frames drawn **from the actual compiled C# frame samples**, not claimed as Unity GPU screenshot; source receipt, SHA-256 and manifest. Unity prefab is a script-bearing GameObject: nine actual LineRenderers are instantiated in `Awake` and pooled. The package itself is not hooked to Codex gameplay.

**Two original authored Unity PlayMode acceptance cases** require native licensed Unity 6000.3.14f1 URP, import exact physical ZIP into an authorized disposable checkout, verify prefab loads nine real LineRenderers and cancel clears all; second tests VFX OFF/ON and cooling transitions. Require original TestRunner XML, Editor.log, screenshots at 720/1080. We explicitly mark Unity native tests `NOT_RUN` in Actions (which executes .NET and real Pillow PNG verification instead).

## Independent QA & installation
1. B01-40 downloads final pinned Actions artifact, verifies original nested Unity ZIP SHA-256 and every `Assets/` file + GUID + case-normalized paths against the actual target game HEAD before any installation.
2. B01-60 in licensed disposable Unity 6000.3.14f1 URP worktree imports **only** `Assets/B0132RepairWeld/` subtree **with .meta files** and runs authored PlayMode tests; observes line shader compatibility, alpha and cleanup; records native evidence. There is no native Unity PASS claim from source CI.
3. B01-00 chooses whether to offer the package to CODEX INBOX #79; Codex ACK is not presumed. Codex owns optional actual repair status→visual adapter on a separately authorized branch.

**Rollback:** remove newly imported `Assets/B0132RepairWeld/` and sibling folder `.meta` only, and any *Codex-owned* optional adapter if installed by Codex; leave existing repair/gameplay systems and saves untouched. The producer only owns isolated `Tools/B01-32/P17/W06RepairWeld/**` + own workflow. Do not merge draft producer PR directly to canonical game.

**Risk / limitations**: Native Unity/URP shader and prefab deserialization remain `NOT_RUN`; compiled original core on .NET8 is not Unity. Confirm the runtime `Sprites/Default` shader availability on licensed Unity target (fallback URP/Unlit), correct 2D plane orientation and visual layering during B01-60 QA.
