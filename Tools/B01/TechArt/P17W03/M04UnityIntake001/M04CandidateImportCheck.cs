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
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                HashSet<string> animatedPivots = new HashSet<string>();
                foreach (EditorCurveBinding binding in bindings)
                {
                    // Translation-only movement does NOT count as nozzle tilt.
                    bool rotation = binding.propertyName.StartsWith("m_LocalRotation")
                                    || binding.propertyName.StartsWith("localEulerAngles");
                    if (!rotation) continue;
                    string path = binding.path ?? "";
                    foreach (string pivot in PivotNames)
                        if (path == pivot || path.EndsWith("/" + pivot))
                            animatedPivots.Add(pivot);
                }
                if (animatedPivots.Count == PivotNames.Length)
                    oneCompleteClip = true;
            }
            if (!oneCompleteClip)
            {
                Debug.LogError("M04-UNITY-INTAKE-001 FAIL: no SINGLE imported clip animates all 4 pivots; inspected "
                               + inspectedClips + " clips");
                return false;
            }

            Debug.Log("M04-UNITY-INTAKE-001 PASS (native Editor inspection): 105 MeshFilters, 4 named pivots, "
                      + "one common rotation-animated clip. Visual/UV/URP/PhysX NOT validated.");
            return true;
        }
    }
}
#endif
