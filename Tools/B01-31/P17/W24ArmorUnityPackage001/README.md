# B01-31 P17/W24 — Actual Unity .unitypackage of original three armor states + visual-only prefab composer

**PACKAGE_ID:** `B01-31-W24-NATIVE-ARMOR-UNITYPACKAGE-001/P17-W24-R1` / [HQ #731](https://github.com/C8LyNn/RoboRougelike/issues/731).

## Closing a real W23 integration dependency

The earlier [W23 #740](https://github.com/C8LyNn/RoboRougelike/pull/740) gave three **source-verified 756 actual total triangle** armor OBJ files with original six-material MTL and exact glTF PBR sidecar; however W23 distributed them only via GitHub Actions ZIP. W24 makes an **actually physically serialized standard Unity `.unitypackage` TAR/GZIP**, containing original immutable three W23 OBJ files, common MTL, exact PBR JSON, two original 1800×625 PNG source reviews, **real Unity C# visual component and an Editor helper**, with **13 full Unity assets/folders in GUID-scoped `asset`/`asset.meta`/`pathname` members**. It can be manually imported into a disposable Unity project without any third-party glTF plugin.

W24 **also commits the actual original native MonoBehaviour** `B0131W24ArmorDamageVisual` under `Assets/B01_31/W24ArmorStates/` and a native Unity Editor MenuItem. Unlike earlier sources, the actual visual state switch is implemented: `ConfigureVisuals(GameObject ok,damaged,destroyed)` and `SetVisualPhase(Ok/Damaged/Destroyed)` ensure **exactly one visual GameObject is active**, reject invalid enum/null/duplicate refs, do not mutate gameplay parts, HP, Rigidbody, essential Core, free victory rebuild reward, 65% price, one-shot loaner, IDs, Save or disk. External gameplay may feed **read-only visual state** after its own trusted authority checks. No invention of native combat integration.

The original Editor command is **OPT-IN ONLY**, no `InitializeOnLoad`, no auto-executing installer, no delete:
`Tools > B01-31 > W24 > Create Isolated Armor Visual Prefab...`
It requires typed user click/Unity confirmation, verifies **all three ORIGINAL W23 exact SHA256 from disk BEFORE any prefab writes**, independently checks `AssetDatabase.LoadAssetAtPath<GameObject>` import success for each OBJ, and only creates the new, collision-guarded path `Assets/B01_31/W24ArmorStates/Generated/B0131_W24_ArmorVisual_ONLY.prefab`. If destination is present, it refuses to overwrite. This creates a visual-only 3-state GameObject composition, **not actual game owner prefab integration**. Any import collision or missing Unity importer stops before creating the prefab. Never installs onto the user's PC automatically.

## Real physical reproduction/verification

The original W23 [Actions #41132293604](https://github.com/C8LyNn/RoboRougelike/actions/runs/41132293604) [artifact #13012614601](https://github.com/C8LyNn/RoboRougelike/actions/runs/41132293604/artifacts/13012614601) contains exact `B0131W23/*`. [W23 source SHA](https://github.com/C8LyNn/RoboRougelike/pull/740) 48f376a3ca07998c33483ecb5ede3d5706f1381d and original OBJ SHA pinned in `manifest.json`. All 3 actual OBJ must match original W23 204/264/288 faces; any other .obj rejected. This code never regenerates or mutates W23 original meshes.

```bash
python Tools/B01-31/P17/W24ArmorUnityPackage001/build_unitypackage.py --repo . --original original-W23 --output build/W24
python Tools/B01-31/P17/W24ArmorUnityPackage001/verify_unitypackage.py --repo . --original original-W23 --output build/W24
dotnet run -c Release --project Tools/B01-31/P17/W24ArmorUnityPackage001/Harness/VisualManagedHarness.csproj
```

**Original independent verifier** never imports builder; it unpacks actual gzip/TAR bytes and verifies exact **13 path+GUID/meta Unity assets**, actual source bytes of 3 OBJ+MTL+JSON+2 PNG and real 2 C# files, original source GUID uniqueness, deterministic receipt, no foreign or `../` traversal, rejects **eight deliberately corrupted Unity tar-member/GUID/path/source cases**. Ubuntu rebuilds physical `.unitypackage`+receipt twice and requires byte-identical SHA. Windows2022 independently downloads physical Ubuntu-created Unitypackage and the original W23 artifact, then reruns receiver **WITHOUT calling creator**. A real .NET8 executable **compiles the actual original native Unity C# MonoBehaviour source with an explicitly synthetic UnityEngine stub** and tests 12 genuine visual state/negative branches. This is source/managed QA, emphatically **NOT Unity Editor/PhysX/native QA**.

## Receiver-native handoff, current block and rollback

**B01-30/40** should inspect original source SHA, unique GUIDs and actual 13-entry Unitypackage in **disposable approved scratch**, not unreviewed project install. **B01-60** owns operator-confirmed legally activated local Unity 6000.3.14f1 Windows G1: import via `Assets > Import Package > Custom Package...` in disposable user-reviewed project, manually click above MenuItem, verify three visually distinct states, physical model import, normal/pivot/material/URP, exact Editor.log/NUnit XML. MTL only approximates PBR; six original glTF PBR factors are in JSON, Unity shader setup may require separate reviewed mapping. Until it truly executes: **NATIVE_G1_NOT_RUN**, no engine PASS.

**Frozen Save owner decisions stay with designated agents:** selected sole #749 candidate pending owner decision vs #753/#741; runtime #690, API #729, #712/#669 and frozen #510/#556/#596 untouched. This art visual component cannot fix .owner/.lease Save authority, Rig[]/AtomicDockLedger or ColdRestart; no original source joins/merges. Approved one-free-per-win, one loaner, 65% rebuild and fail-only-nonfunctional policy are **not** implemented by the visual-only component. Codex currently unavailable: no ACK, integration time not measured.

**Six lifecycle fields:** `IMPLEMENTED=YES_SOURCE | TESTED=PENDING_EXACT_CI | QA_ACCEPTED=NO_FORMAL | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`. Source-format G0 pending, B01-30/40 formal G0 pending, native Unity G1 NOT_RUN, G2 HOLD.

**Safe rollback:** no automatic installation. Any user-authorized disposable import could remove only W24 under isolated `Assets/B01_31/W24ArmorStates` after verifying no foreign modifications and preserving any foreign even-empty directory; never remove W22/W23 original, other agents assets or frozen game. Editor helper never deletes files; incomplete new files require human review.
