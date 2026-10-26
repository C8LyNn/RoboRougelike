# INSTALL_FOR_CODEX — MAPGENWORLD001 1.1.0 / G2 HOLD
**Do not install into Codex's active game before B01-00 per-package G2.** This is a SOURCE_COMPLETE_FOR_QA package, not a deployment authorization.

## Pin / dependency
Producer MAPGEN001 complete R3.4.1 `5be31039411f89f17654b1c332f8692883fa03cc`. Adapter PR own SHA recorded in its GitHub PR; verify full ZIP SHA256 from Actions artifact `SHA256SUMS.txt` and every file in embedded `META-INF/MAPGENWORLD001_MANIFEST.json`. Requires Unity 6000.3.14f1, URP, existing MAPGEN001 Runtime asmdef. No R3, R6, R4, scene or project setting file is shipped/overwritten.

## Preflight (disposable clean checkout ONLY)
1. Record target Git HEAD and `git status --porcelain`; abort if dirty or current live target unknown.
2. Extract ZIP in isolated temporary path, reject path traversal and GUID/path duplicates; run `python3 Tools/B01/MAPGENWORLD001/package_world.py verify <zip>` or independent B01-40 PACKAGE-AUTO #238 dry-run. Compare all SHA256 and file lengths with embedded manifest.
3. Check `Assets/B01Packages.meta` exists in target and preserve **its exact GUID/bytes**. The adapter does not include or overwrite it. Check all package GUIDs are collision-free across target assets; any identical path with different bytes or GUID is a hard conflict; do not delete or auto-overwrite.
4. Confirm pinned MAPGEN001 source + `Resources/MAPGEN001_BlocksURP.shader` and assembly `B01.MAPGEN001.Runtime` present; no speculative fallback if incompatible.
5. Use a *new disposable branch/worktree* from approved target SHA, never the active Codex worktree. Copy only `Assets/B01Packages/MAPGENWORLD001.meta`, `Assets/B01Packages/MAPGENWORLD001/**`. Docs and Tools remain QA packet outside Unity Assets.

## Minimal host wiring — CODEX ONLY AFTER G2
```csharp
// On an opt-in temporary host GameObject (not a replacement R3 vehicle controller):
var adapter = host.AddComponent<B01.MAPGENWORLD001.MapGenWorldAdapter>();
var profile = B01.MAPGEN001.MapVehicleProfile.Light;
profile.Width = measuredVehicleWidthMeters; // From actual assembled R3/R6 Collider envelope, not 2.1
// Fill Length, MinTurnRadius and MaxSlopeDegrees from actual vehicle capabilities.
var request = new B01.MAPGENWORLD001.MapWorldRequest {
    Recipe = B01.MAPGEN001.MapRecipe.CombatArena,
    Seed = 7,
    Config = B01.MAPGEN001.MapGenConfig.Default,
    Vehicle = profile, EnvelopeMeasured = true, ClearancePerSideMeters = .25f
};
if (adapter.TryGenerate(request, out var map, out var reason))
{
    // Receiver-only: use its existing authoritative spawn API at map.Start,
    // follow map.Checkpoints, resolve map.Finish, implement race/boss activity callbacks.
}
else UnityEngine.Debug.LogWarning("WORLD001_REJECTED " + reason);
```
Save/replay opt-in (only **after** package G2):
```csharp
// From an approved generated world:
string gameSaveSeedReceipt = map.ReplayReceipt;
// Restore through the same world-adapter component, without creating a shadow save manager:
if (!adapter.TryReplay(gameSaveSeedReceipt, out var restored, out var replayReason))
    UnityEngine.Debug.LogWarning("MAPGENWORLD001_REPLAY_REJECTED " + replayReason);
```
For `InvalidReplayReceipt` and `ReplayGeometryMismatch`, keep current activity/world unchanged and surface a safe restart choice. Do not override the hash/seed or silently re-roll.

No host API names are guessed here. Robot spawning, wheel traction, checkpoint trigger sizes and activity transitions remain receiver's responsibility. Do not bypass a rejected request. Do not present optional Heavy ramp as mandatory.

## Native QA and transactional rollback
- Editor native compile, six EditMode tests, three PlayMode tests with NUnit XML/log/exit. Demonstration Arena/Course ×7/31/83 × real Light2.980/Heavy5.180, negative measured Light10m seeds2/12/14/22/28; confirm no previous-live-map mutation after rejection; callback order and no duplicates; standalone URP Windows Player visible map.
- Measure actual R6 vehicle footprint and physical clearance; **source .NET PASS does not replace this**. Record source, target and QA SHA.
- On failure, stop Player/Editor; restore disposable worktree branch from exact pre-import commit, remove only new adapter-owned paths (not MAPGEN001 nor shared parent metas), reimport asset database, compare git status to saved preflight; do not overwrite user files; if dirty fail closed. Package importer B01-40 handles journal/rollback.
- G0 source/target, G1 native/independent B01-60, G2 B01-00 per-package. **No merge or approved handoff.**
