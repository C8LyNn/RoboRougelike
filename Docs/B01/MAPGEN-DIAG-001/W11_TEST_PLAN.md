# W11 independent differential: plan and gates
**Pinned:** R3.2 `003bced54480d509ffc0d49d26daf87160a8d609` → R3.3 `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`.

Run from repository root:
```bash
python Tools/B01/MAPGEN-DIAG-001/w11_compare.py w11-mapgen-diff-evidence
```
Requires git history containing both exact commits, Python 3 and .NET SDK 8. The script reads pinned Git bytes via `git show SHA:path` into two temporary source trees, compiles and executes **two independent .NET builds** of original unchanged MapGenData/Planner/RouteValidator plus existing producer synthetic UnityEngine types. Does NOT compile MapGenBuilder or call Unity's real PhysX, URP or player. It compares geometry hash and route code across 33 case IDs; each actual revision has its own per-case JSON, separate console log, and one comparison JSON per ID. No rerun of prior 1013 or W10 27 source cases.

Test matrix: 12 original R6 seeds 7/31/83 × Arena/Course × actual measured Light 2.980 / Heavy 5.180 width with nominal conservative vehicle length 3.2 / 5.8; 7 historical Heavy normal2/7/31 hard0/2/7/19; 5 narrow Light10m seeds 2/12/14/22/28 in both nominal and measured widths; 4 nominal profile controls for seed7. Nominal vs measured Light10m is a **negative profile acceptance test**, not a demand to allow invalid width. For each map, source geometry hash, route code, floor segment X/Z overlaps, closest ramp/boundary lateral X-gap, rotated ramp bounding-box vertical penetration into floor and ramp width minus hull width are recorded. These AABB measures cannot prove actual 3D contact/traversal.

Independent URP **STATIC** inspection detects explicit Lit/Simple Lit Shader.Find fallback, sharedMaterial assignment, MaterialPropertyBlock colour, material cleanup and authored Unity shader-name test. Does not prove shader inclusion in Windows Player or correct GPU pixels; native 720p/1080p and shader stripping remains B01-60/Codex QA.

Expected status: no geometric-hash changes because all 3 pinned C# geometry sources have identical bytes; 5 measured Light10m source rejects with `IMPOSSIBLE_COURSE_PROFILE` at 2.980m are expected. If this prediction fails, the script emits a per-case FAIL and CI exits non-zero. CI artifacts uploaded even on failure. **No source ownership transfer, no merge/G2.**
