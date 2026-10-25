# CAMERA001 changelog
R1 2026-10-25: isolated pure hull-aware camera solver; opt-in Camera001Rig using world collision spherecasts, smoothing and diagnostics; standalone demo scene with Light/Heavy and controls; authored EditMode/PlayMode NUnit suites; Unity .meta/asmdef, installation contract, receiver prompt and evidence ledger.
NO modification of production P12, physics R6, garage, AI, main or develop. Native Unity not yet executed.

## R1.1 — W9 source repair / 2026-10-25
Removed the two invalid direct UnityEngine.TestRunner / UnityEditor.TestRunner references from both CAMERA001 test asmdefs, keeping the sole runtime reference and optionalUnityReferences ["TestAssemblies"]. Added Tools/B01/CAMERA001/verify_asmdefs.py as executable standalone JSON-only regression check. RED: both source asmdefs referenced both runner names. FIX: updated both files. RETEST: 2/2 static JSON configuration assertions PASS on new blobs; no Unity compilation performed.
