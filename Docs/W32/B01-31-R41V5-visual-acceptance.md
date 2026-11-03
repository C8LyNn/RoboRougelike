# B01-31 — P17/W32 · R41 v5 visual exploration + non-destructive acceptance

**Kind:** original schematic concept + QA handoff; **not** a rendered or imported v5 FBX, Unity gameplay feature, or alternative turret system.  
**Own parent:** [B01-31 W31 Draft #904](https://github.com/C8LyNn/RoboRougelike/pull/904) at `ba92a52a9bb73fb63b17e94a3af4a199868ef92e`. This continues [W30 #893](https://github.com/C8LyNn/RoboRougelike/pull/893) without changing either producer branch.  
**Canonical game:** Codex [#873](https://github.com/C8LyNn/RoboRougelike/pull/873) → [#877](https://github.com/C8LyNn/RoboRougelike/pull/877) at the latest independently selected SHA; B01-31 **does not** own integration, gameplay firing authority, Save or ECON.  
**Artifact:** [W32 schematic SVG](B01-31-R41V5-visual-exploration.svg), entirely new original artwork, viewable in a GitHub browser and deliberately outside Unity `Assets/`.

## 1. What is real evidence, and what is an art proposal?

**Existing source contract, verified in GitHub on 2026-11-03:**

| Property | Value | Authoritative file |
|---|---|---|
| Grid | `0.55 m` | `Assets/B01_31/W30R41V5Adapter/R41V5MuzzleGeometry.cs` |
| Yaw/pitch pivot (neutral, root-local) | `(0, 0.135, 0) m` | same |
| Left muzzle (neutral, root-local) | `(-0.060, 0.135, +0.700) m` | same |
| Right muzzle (neutral, root-local) | `(+0.060, 0.135, +0.700) m` | same |
| Two muzzle forward vectors | `+Z` after yaw/pitch wrapper | same |
| Producer FBX expected at | `Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx` | `R41V5AssetPaths.cs` |
| Exact external source FBX SHA-256 | `fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533` | original W31 importer |
| Unity import | `ModelImporter.bakeAxisConversion == true`, source local scale `(1,1,1)` | original W31 importer |
| Fallback | original working R41/AutomaticGun preserved | #893, #904 owner scope |

These numbers describe the **adapter contract**, not measured body dimensions, blade/weapon collision envelopes, projectile rate, or the missing physical FBX mesh. The top-view diagram is a didactic scale sketch; the hero is an original stylized concept and **must never be used as proof that Claude v5 has this silhouette or materials**.

**Art direction proposed, not implemented:** a compact graphite housing, cyan muzzle/selection accents and amber mechanical service highlights. These hues are only a material study; the existing `Assets/R3/R3VisualPalette.cs` and its valid/invalid/missing/selected gameplay colors retain authority. In particular, do not convert physical build-validity red/green into decorative status lighting.

## 2. Player-facing visual criteria for a future real prefab

1. **Readable direction:** at garage preview distance, both physical barrel tips should read as one aimed weapon, with distinct left/right muzzle origins and a clear +Z forward. No fake muzzle alignment by changing the original physics or weapon logic.
2. **Mechanical ownership:** yaw/pitch wrapper may animate the **visual clone**, but core attachment, part HP, collider eligibility, projectile origins, cadence, AI and damage remain on the canonical R3/R4/R41 game systems.
3. **Damage readability:** neutral, damaged and unusable states must remain distinguishable from the garage's valid/invalid placement overlays. An accent material alone must not convey functional status.
4. **Mount clearance:** actual rendered barrel/body may not visually disappear into armor, saw disc or nearby gun; use the canonical physical 5-zone / mounted-weapon acceptance, never a visual-only collision override.
5. **Stable fallback:** original turret survives rejected SHA, unsafe import hierarchy, wrong transform, absent FBX, missing script, native failure and denied owner integration. No automatic prefab swap.
6. **Readability test scope:** compare 1920×1080 and 1280×720 garage frames, both small and fully assembled vehicles, neutral rotation and 12 yaw/pitch combinations; record **actual** Unity screenshots before claiming clarity or performance.

**Legacy compatibility hazard:** original `Assets/R4_1/R41PartGeometry.cs` builds an Automatic gun with **one** `Wylot` transform at local `+Z 1.8 m`, while the isolated v5 adapter exposes **two** muzzle markers at `+Z 0.70 m`. This is *not* equivalent runtime mounting. The owner/integrator must explicitly design and test whether original firing remains single-origin or alternates/uses two verified physical origins. B01-31 does not select a fire pattern, emit extra shots, or bypass R3 collision checks.

## 3. Exact gated intake, no implicit approvals

| Gate | Required measurable evidence | Owner | Current state |
|---|---|---|---|
| W32 concept integrity | SVG and document present; known constants/labels; original fallback explicitly preserved | B01-31 | SOURCE_ART_ONLY |
| B01-31 source self-G0 | #904 at exact `ba92a52`, W31 source/zip receipt and cross-OS tests | B01-31 | Existing #904 source PASS (not W32 native) |
| Formal independent G0 | exact #893→#904→W32 ancestry, scope, unchanged gameplay, FBX SHA contract, original 12-path/2-GUID W31 closure | B01-30 / B01-40 | PENDING |
| Native 1: safe Editor probes | four W31 preclone tests, with `Editor.log` and NUnit XML, **no external FBX required** | B01-60 | NOT_RUN |
| Native 2: actual producer asset | SHA-256 exact original FBX physically available, import/bake verification, four inherited W30 real-FBX tests; actual mesh/markers/kinematics/screenshots | B01-60 | NOT_RUN · FBX EXTERNAL |
| Native 3: canonical gameplay | exact accepted canonical #877 successor, real robot mount and 2 actual physical muzzle trajectories, meaningful obstruction cases, existing original fallback | Codex / B01-60 | NOT_RUN |
| Windows x64 extracted Player | Unity 6000.3.14f1 build, EXE + complete data, extracted cold boot, real `Player.log`, SHA manifest, combat/garage smoke | B01-60 / Codex | NOT_RUN |
| G2 / merge / release | owner decision + formal G0/G1, no conflicting PR/code/fallback regression | B01-00 / Codex | HOLD · NOT AUTHORIZED |

Until these are independently accepted: `DESIGNED=W32_SCHEMATIC_ONLY | IMPLEMENTED=NO_NEW_GAMEPLAY | QA_ACCEPTED=NO | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`.

## 4. Failure modes and non-destructive rollback

- **No real FBX:** record `FBX_ABSENT`, do not silently generate a cube in its place or count `Inconclusive` as native success.
- **Wrong FBX SHA / path / importer setting:** reject original v5 source preflight, keep original R41.
- **Source has a MonoBehaviour, Joint, missing/duplicate muzzle, or no Renderer:** reject **before cloning** via original #904 gate; do not relax this for visual preview.
- **Safe clone but real mounts/line-of-fire fail:** do not call the art package production-ready; preserve old turret. Any weapon authority changes are a separate authorized Codex task.
- **Unity/license unavailable:** report `NOT_RUN`, do not infer Unity PASS from portable .NET or Python source tests.

Rollback for **this W32 package** is simply to omit its two `Docs/W32` files in an approved independent checkout. Do not delete other files, even empty directories, touch the producer FBX, overwrite #904 or #877, replace AutomaticGun, merge or release.

## 5. Executed W32 checks and ownership boundary

- Created one **new SVG** and this **new text handoff** only, with no Unity `Assets/` or runtime edits.
- SVG local source invariants checked: concept disclaimer; both muzzle coordinates in derived callouts; pivot/grid values; no external image dependency; single SVG root; native HOLD/fallback states. These are **static checks, not a rendered screenshot or Unity compile**.
- Required downstream verification: read back the exact GitHub file/branch, inspect the change list relative to #904, and preserve full exact SHA provenance.
- No fresh Unity Editor process, Blender/FBX, real image-from-FBX, NUnit, Windows build, ZIP or Player process executed for W32.

**Decision requested only after gates:** whether the dual visual muzzle adapter will be used for actual alternating shots, simultaneous shots (subject to existing weapon cadence), or stay cosmetic. Until then, the old canonical AutomaticGun remains authoritative.
