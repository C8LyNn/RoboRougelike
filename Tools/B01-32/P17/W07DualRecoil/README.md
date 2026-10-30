# B01-32 DUAL-RECOIL-ANIM-001 — PASS17/WAVE07

**Original isolated dual-channel 3D barrel recoil** for Unity 6 URP. Unlike existing WheelWake, impact/decal, Salvage Beacon or Repair Weld, this is an animated mechanical 3D *position change of two weapon barrels*, with independent left/right shots. Purely visual: **no ammunition, projectile logic, Rigidbody force, damage, weapon cooldown, inventory, save or canonical Codex gameplay code**.

## Real implementation
- **DualRecoilCore.cs** is actual compiled standalone C#9; a critically damped, analytic recovery calculation for two independently triggered lanes. `Fire(RecoilLane.Left/Right, intensity)` accepts finite positive values, clamps intensity and rapid-fire travel to 0.28 world units. `Advance(dt)` handles time-based decay; `Clear()` and `SetVisualsEnabled(false)` reset stale positions and forbid new shots until re-enabled.
- **B0132DualRecoilVisual.cs** is actual authored Unity C#9; at prefab awake it generates **two real face-normalized 3D cuboid meshes** under the isolated GameObject via `MeshFilter`/`MeshRenderer` and uses real `Transform.localPosition` updates after `PlayLeftShot` or `PlayRightShot`. No physical Collider or Rigidbody ever created. Mesh/material are destroyed with component, avoiding leaked allocations. No external FBX/model or copyrighted assets.
- `Assets/B0132DualRecoil/Resources/B0132_DualRecoil.prefab` is actual serialized Unity GameObject with the original script-GUID link. ZIP contains **17** actual file paths including `.meta` (11 GUIDs), isolated runtime & test asmdef and **2 authored strict Unity PlayMode tests**.
- **8 actual 512×512 PNG preview states** and a 2048×1024 contact sheet come from **compiled original C# sample JSON**, not screenshots of the Unity renderer.

## How to reproduce
Windows & Ubuntu Actions run `dotnet run -c Release --project Tools/B01-32/P17/W07DualRecoil/CoreTests/CoreTests.csproj -- build/DualRecoilW07/frames.json`, original `SyntaxTests`, `python Tools/B01-32/P17/W07DualRecoil/render_samples.py --output build/DualRecoilW07`, `python Tools/B01-32/P17/W07DualRecoil/build_import.py --output build/DualRecoilW07`, and the exact `verify_import.py`. Final Actions artifact includes immutable ZIP and checksum receipt.

## Receiver / safe import
1. **B01-40** downloads exact-SHA physical artifact, checks nested `B0132-DualRecoil-UnityImport.zip` SHA256 and all original 17 ZIP paths, 11 .meta GUIDs, prefab script, against **current** Codex Unity tree for case-insensitive path/GUID collisions, validates rollback.
2. **B01-60** on an *authorized licensed* disposable Unity **6000.3.14f1 URP** checkout imports the whole `Assets/B0132DualRecoil/` subtree including root folder `.meta`. Load `Resources/B0132_DualRecoil.prefab`; verify two 3D meshes with **no colliders**, 720/1080 screenshot and actual left/right recoil. Run two original `DualRecoilPlayModeTests` and attach Test Runner XML and Editor.log / exit codes. This native Unity step is **NOT_RUN** by producer CI.
3. **B01-00** may later schedule #79 for Codex after independent QA; Codex alone owns any presentation-only link from its actual existing LMB/RMB weapon fire events to `PlayLeftShot`/`PlayRightShot`. No such integration is presumed.

**Rollback:** remove only `Assets/B0132DualRecoil/` and its sibling root `.meta` plus any future Codex-owned optional integration adapter. No canon source/save migration. **Draft only. No merge without owner approval.**

**Gates:** producer .NET+source binary G0 may PASS; Unity compiler, PlayMode and GPU visual G1 **NOT_RUN** without genuine runtime evidence; independent B01-40/60 **PENDING**, Codex **NOT_SENT/ACK_PENDING**, G2 HOLD. Govern by HQ [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).