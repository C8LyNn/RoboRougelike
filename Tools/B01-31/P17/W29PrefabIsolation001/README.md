# B01-31 | PASS17 W29 | Real W24 prefab detach before visual physics stripping

**Parent:** exact W28 [Draft #845](https://github.com/C8LyNn/RoboRougelike/pull/845) @ `2508f533d3ed8c31f581e491afb2494ab7fa774c`; requires W27 #820, W25 #788, W24 #770. Original gameplay P12 & Codex source unchanged. This is a **direct improvement to existing W27 native scene generator**, not a competing gameplay or visual system.

### Implemented production C# fix

The existing manual scene builder `Assets/B01_31/W27ArmorLiveDemo/Editor/B0131W27CreateArmorLiveDemoScene.cs` now calls `CreateDetachedVisualClone(originalW24Prefab, disposableScene)` before original W28 `StripVisualOnlyPhysics`. New clone uses real Unity `Object.Instantiate`, moves **only that clone** into the approved additive disposable scene, and if linked calls `PrefabUtility.UnpackPrefabInstance(..., Completely, AutomatedAction)`. If the clone remains prefab-linked it **fails closed**, deletes only that newly instantiated clone and never alters source or user scene. Original W24 prefab asset and three OBJ meshes remain untouched. Real scene runtime continues to use original P12 Damageable, existing W25 visual binder, and actual W24 three mesh variants.

W29 tightens W28 full preflight: reject **any other MonoBehaviour**, including missing-script null entries and children of inactive armor variants, besides the one original W24 visual controller. On bad input, **no collider/Rigidbody is removed**. Existing W28 logic still strips all physics ONLY on the detached visual clone after successful validation, not on the actual P12 gameplay rigidbody, garage collider, part ID or Save.

### Native tests written; Unity G1 NOT_RUN

New `Assets/B01_31/W29PrefabIsolation/Tests/EditMode/B0131W29DetachedArmorPrefabTests.cs` includes **five real Unity Test Framework EditMode NUnit tests**. They require the **actual approved W24 generated prefab** (three original OBJ meshes; native import prerequisite), not a cube-only mock. Verify original AssetDatabase asset dependency hash unchanged, detached copy not linked to source prefab, original model renderer count and all three serialized visual states preserved after unpack, added physical Collider/Rigidbody only stripped from copy including inactive, foreign extra script rejected before mutation, and bad Scene/source cannot produce a clone.

The original W28 .NET8 source harness compiles FIVE VERBATIM original C# files (P12 Gameplay, W24, W25, W27 GUI and W29-mutated W27 Editor) and exercises 37 original-code assertions; **UnityEngine/Editor & R3/R6 dependencies explicitly synthetic**, NOT native Unity. Original W27 managed source 25 assertions repeated. The new GitHub Actions workflow checks both Ubuntu and Windows2022; Windows independently reads **actual original Ubuntu physical changed-source ZIP** without rebuilding it. The independent verifier opens ZIP and confirms 16 exact source paths, five owned original Unity `.meta` GUIDs and 8 deliberately corrupt source/path/GUID negative cases. Two full ZIP exports must produce identical SHA-256.

### Acceptance / integration gating

B01-30/40 must formally review the single actual W27 editor replacement, inherited W28 sanitizer, original B01-31 changed test-shim paths, new 5 .meta GUIDs, and exact W24/W25 dependencies in an approved disposable checkout. B01-60 must import W24->W25->W27->W28->W29 in an owner-approved **Unity 6000.3.14f1 Windows** project. Execute W29 five actual EditMode NUnit cases and previous W28 EditMode + W27 PlayMode suites, manually generate scene, capture Editor.log, NUnit XML and real imported 3D before/after evidence. Without execution: **NATIVE G1 NOT_RUN**. Do not present source PASS as fully integrated game or real Windows Player.

No unapproved merge, Codex handoff or #79 duplicate offer. B01-00 decides after independent G0. W29 DOES NOT solve Saw #828, EMP #839, five-color garage, one-owner Save #826, economic softlock #812 or R41 turret exporter. Those stay with their respective owners.

**Risk:** MEDIUM (native prefab/nested Unity hierarchy behaviour unverified); **rollback:** discard only W29 own 12 new source paths and restore only 4 owner-managed source files from exact parent SHA. Never delete original W24, P12, any foreign/even-empty dirs or active gameplay files.