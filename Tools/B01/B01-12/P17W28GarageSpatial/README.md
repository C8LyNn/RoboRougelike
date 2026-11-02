# B01-12 / PASS17 W28 — Garage Spatial 3D Zones / R1

**Type:** ADDITIVE Unity 6 runtime visualizer, NOT another offline prototype. **Producer:** B01-12. **Authority:** existing Codex `R3GarageView` / `R3VehicleModel` at W27 gameplay PR #824 **exact SHA `e03d36f2b86f8c73a067e574b67237a8921f622a`**. **HQ:** #800; requirements: #141. No changes to `Assets/R3/**`, `Assets/WeaponIntegration/**`, Save, ECON, or Codex worktree.

## What is implemented

`B0112GarageSpatialOverlay` auto-creates a read-only observer when the Unity scene loads. In an actual garage with active R3 mounted-part placement ghost, it draws a 3D material-tinted overlay with the five required kinds: RED existing occupied logical footprint and rejected candidate, BLUE protruding wheel/track *visual* bounds, PURPLE weapon *advisory* forward volume, ORANGE mechanism *advisory* moving-zone approximation, GREEN proposed logical cells **ONLY when `R3GarageView.PlacementIssue == null`**. Draw calls use `Graphics.DrawMesh` without new physics colliders, inventory/wallet mutation or own placement rules. OnGUI provides a legend and the exact existing host rejection string. The 0.55m grid and real model `OccupiedCells()` are reused, including wide part rotation.

**Important:** Purple/blue/orange volumes are **non-authoritative approximations**, not swept Collider clearance. The existing R3 placement validator still only checks what it currently checks. This additive UX does NOT meet the full W28 physical *placement enforcement* acceptance criterion; that requires **Codex-owned shared validator** extensions, identical preview/commit validation, and Unity G1. Do not promote this to 'GARAGE 3D P0 COMPLETE'. May need rendering adjustments under URP after actual G1.

## Source QA vs native QA

The source gate verifies exact file SHA256, Git blob SHA-1 and length, unique original new Unity `.meta` GUIDs, no gameplay writing calls, required real Unity API references, five types and the known W27 unchanged Git blob identities (`R3GarageView.cs`, `R3VehicleModel.cs`, `R3MountingGrid.cs`, `R3VehicleAssembly.cs`, `R41PartGeometry.cs`). It DOES NOT compile C# or run Unity. `python Tools/B01/B01-12/P17W28GarageSpatial/test_source.py --require-w27` must run inside the authored PR checkout based on W27. Expected negative: a byte-added copy of authored C# must return code 2 **FAIL_CLOSED**.

Original Unity `Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs` contains **six authored NUnit EditMode cases** (canonical grid, host validator only, invalid target, real wide-rotation footprint, no additional Collider). Those tests MUST actually be run under licensed Unity 6000.3.14f1 before any G1 PASS claim. Author did not run Unity in producer environment. `UNITY_COMPILE=NOT_RUN; UNITY_EDITMODE=NOT_RUN; UNITY_PLAYMODE=NOT_RUN; WINDOWS_PLAYER=NOT_RUN`.

## How to check in the actual garage after B01-40 G0

1. In **disposable** checkout of the exact PR branch, confirm original R3 game files match the pinned W27 SHA. Open activated Unity 6000.3.14f1, import package and run EditMode tests.
2. Launch existing R3 garage scene, choose owned wheel/track/gun and click near vehicle. Red volumes show prior occupancy, the selected proposal is green only when existing validator says valid; invalid proposal turns red and the old placement reason appears in the top legend. Rotate via Q/E/R/F/Z/X and watch volumes update.
3. Test a 2x2x1 wide bonus/shield, gun, wheel and crawler with protrusion. Verify no extra Colliders in the hierarchy and that failure to mount does not change Scrap/IDs. Check Scene/Game render and absence of URP shader warnings.
4. Run full Unity EditMode/PlayMode and Windows Player startup; attach original `Editor.log`, XML and Player.log, commit and build SHA. Manual tests are NOT performed by this package author.

**Non-goals:** no new validator, no bypass of `PlacementIssue`, no new Save/Load/ECON, no model rescale, no replacement garage, no import to owner's actual computer, no merge/release.

## Receiver and rollback

B01-40 formal source-G0 (path/meta/manifest/blobs/ABI/rollback), B01-60 independent actual activated Unity Editor G1, Codex as future renderer/authority integrator (only after G0+owner handoff), B01-00 coordination. Exact overlay/requirements and `W27` SHA need requalification if Codex changes the garage API in later waves.

Safe rollback: abandon unmerged Draft branch or delete **only own newly added files and Unity .meta** within disposable clone. Never delete shared `Assets/Tests/Editor` or foreign empty directories and never touch player Save. If unverified shader or unexpected performance/regression, disable/remove ONLY this observer.

**Stages:** `IMPLEMENTED=YES_SOURCE | TESTED=SOURCE_ONLY | QA_ACCEPTED=NO | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`; `G0_PENDING`, `G1_NOT_RUN`, `CODEX_ACK_NONE`, `GAMEPLAY_SPATIAL_ENFORCEMENT=NOT_IMPLEMENTED_BY_THIS_PACKAGE`.
