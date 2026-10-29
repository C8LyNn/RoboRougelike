// B01-30 M04-UNITY-INTAKE-001 — optional Editor-only import check.
// CANDIDATE content from exact ORIGINAL #386 FBX; never gameplay code.
// Native Unity 6000.3.14f1 execution is separate receiver G1, NOT producer QA.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace B0130.M04UnityIntake
{
    public static class M04CandidateImportCheck
    {
        private const string Fbx = "Assets/B0130M04Intake001/M04-024-4GIMBALS-ORIGINAL.fbx";
        private static readonly string[] PivotNames =
        {
            "N01_GIMBAL_PIVOT", "N02_GIMBAL_PIVOT",
            "N03_GIMBAL_PIVOT", "N04_GIMBAL_PIVOT"
        };

        [MenuItem("Tools/B01/M04-024/Prepare and check candidate FBX")]
        public static void PrepareAndCheck()
        {
            ModelImporter importer = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("M04-UNITY-INTAKE-001 FAIL: exact candidate FBX not imported");
                return;
            }

            // Mutate ONLY this new dedicated candidate model when user opts in.
            bool dirty = importer.animationType != ModelImporterAnimationType.Generic
                         || !importer.importAnimation || importer.globalScale != 1f;
            if (dirty)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                importer.globalScale = 1f;
                importer.SaveAndReimport();
            }
            VerifyImported();
        }

        [MenuItem("Tools/B01/M04-024/Check candidate FBX (read only)")]
        public static void CheckOnly()
        {
            VerifyImported();
        }

        public static bool VerifyImported()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (model == null)
            {
                Debug.LogError("M04-UNITY-INTAKE-001 FAIL: candidate GameObject unavailable");
                return false;
            }
            Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
            MeshFilter[] meshes = model.GetComponentsInChildren<MeshFilter>(true);
            if (meshes.Length != 105)
            {
                Debug.LogError("M04-UNITY-INTAKE-001 FAIL: expected 105 imported mesh components, got " + meshes.Length);
                return false;
            }

            foreach (string pivot in PivotNames)
            {
                int found = 0;
                foreach (Transform node in nodes)
                    if (node.name == pivot) found++;
                if (found != 1)
                {
                    Debug.LogError("M04-UNITY-INTAKE-001 FAIL: pivot count " + pivot + "=" + found);
                    return false;
                }
            }

            bool oneCompleteClip = false;
            int inspectedClips = 0;
            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__")) continue;
                inspectedClips++;
                // The actual original Blender M04 take has frame 1 (rest),
                // frame 31 (+12deg) and frame 91 (-12deg) at 30 FPS.
                // A rotation binding alone can be CONSTANT and previously
                // triggered a false-green. Prove real opposite-phase motion.
                if (clip.length <= 0f) continue;
                float forwardTime = clip.length * 0.25f;
                float reverseTime = clip.length * 0.75f;
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                HashSet<string> movingPivots = new HashSet<string>();
                foreach (EditorCurveBinding binding in bindings)
                {
                    // Only real rotation values, never translation or scale.
                    string property = binding.propertyName ?? "";
                    bool rotation = property.StartsWith("m_LocalRotation")
                                    || property.StartsWith("localEulerAngles");
                    if (!rotation) continue;
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) continue; // Unknown/missing: fail closed.
                    float rest = curve.Evaluate(0f);
                    float forward = curve.Evaluate(forwardTime);
                    float reverse = curve.Evaluate(reverseTime);
                    float firstDelta = forward - rest;
                    float oppositeDelta = reverse - rest;
                    // A real +/-12 degree nozzle has about 0.1045 change
                    // in quaternion X/Y, or about 12 in Euler degrees.
                    // A minimum of 0.015 excludes unchanged/near-flat curves.
                    if (!(Math.Abs(firstDelta) > 0.015f
                        && Math.Abs(oppositeDelta) > 0.015f
                        && firstDelta * oppositeDelta < 0f))
                        continue;
                    string path = binding.path ?? "";
                    foreach (string pivot in PivotNames)
                        if (path == pivot || path.EndsWith("/" + pivot))
                            movingPivots.Add(pivot);
                }
                if (movingPivots.Count == PivotNames.Length)
                    oneCompleteClip = true;
            }
            if (!oneCompleteClip)
            {
                Debug.LogError("M04-UNITY-INTAKE-001 FAIL: no SINGLE clip proves +/- gimbal motion of all 4 pivots; inspected "
                               + inspectedClips + " clips");
                return false;
            }

            Debug.Log("M04-UNITY-INTAKE-001 PASS (native Editor inspection): 105 MeshFilters, 4 named pivots, "
                      + "one common +/- opposite-phase moving clip. Visual/UV/URP/PhysX NOT validated.");
            return true;
        }
    }
}
#endif
