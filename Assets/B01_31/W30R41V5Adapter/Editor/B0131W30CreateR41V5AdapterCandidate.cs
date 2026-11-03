#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.W30.Editor
{
    /// <summary>
    /// A separately named candidate ONLY, never replaces the original
    /// R41 AutomaticGun prefab, original source FBX, or Codex weapon authority.
    /// Explicit Unity Editor menu and immutable-fallback safety gate.
    /// </summary>
    public static class B0131W30CreateR41V5AdapterCandidate
    {
        public const string GeneratedFolder =
            "Assets/B01_31/W30R41V5Adapter/Generated";
        public const string CandidatePath =
            GeneratedFolder + "/R41_Turret_v5_ADAPTER_CANDIDATE_ONLY.prefab";

        // Actual producer-v5 Windows FBX bytes previously inspected by native
        // QA. A different file/version must receive a NEW independent contract
        // and explicit producer approval; never silently update old fallback.
        public const string KnownV5FbxSha256 =
            "fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533";

        public static bool IsApprovedImportedModel(GameObject model, out string reason)
        {
            reason = "";
            if (model == null)
            { reason = "NO_SELECTED_R41_V5_FBX"; return false; }
            var path = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrEmpty(path) ||
                !path.EndsWith("/R41_Turret_v5.fbx", StringComparison.OrdinalIgnoreCase))
            { reason = "WRONG_ASSET_NOT_R41_Turret_v5.fbx"; return false; }
            if (!File.Exists(path))
            { reason = "ORIGINAL_V5_FBX_BYTES_NOT_ACCESSIBLE"; return false; }
            // Unity 6000.3 / netstandard-compatible API. Explicitly avoid
            // SHA256.HashData and Convert.ToHexString from incompatible V2 Save.
            string sourceSha;
            using (var reader = File.OpenRead(path))
            using (var hasher = SHA256.Create())
            {
                var digest = hasher.ComputeHash(reader);
                var result = new System.Text.StringBuilder(64);
                foreach (var b in digest) result.Append(b.ToString("x2"));
                sourceSha = result.ToString();
            }
            if (!string.Equals(sourceSha,KnownV5FbxSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                reason = "UNVERIFIED_V5_FBX_SHA256_REQUIRES_NEW_PRODUCER_REVIEW";
                return false;
            }
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || !importer.bakeAxisConversion)
            {
                reason = "FBX_IMPORT_BAKE_AXIS_CONVERSION_REQUIRED_NO_SOURCE_MUTATION";
                return false;
            }
            if (Mathf.Abs(model.transform.localScale.x-1f)>0.0001f ||
                Mathf.Abs(model.transform.localScale.y-1f)>0.0001f ||
                Mathf.Abs(model.transform.localScale.z-1f)>0.0001f)
            { reason = "SOURCE_SCALE_NOT_ONE_REFUSE_GLOBAL_RESCALE"; return false; }
            return true;
        }

        private static Transform FindUnique(Transform root, string exactName)
        {
            Transform found = null;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach(var t in transforms)
            {
                if (t.name != exactName) continue;
                if (found != null)
                    throw new InvalidOperationException("DUPLICATE_FBX_TRANSFORM_" + exactName);
                found = t;
            }
            if(found == null)
                throw new InvalidOperationException("MISSING_FBX_TRANSFORM_" + exactName);
            return found;
        }

        /// <summary>
        /// Build a root candidate in memory only; caller must destroy it after
        /// inspection. On success Yaw and Pitch wrappers rotate physical visuals
        /// and named adapter muzzles; imported marker forwards are NOT trusted.
        /// </summary>
        public static GameObject CreateCandidatePreview(GameObject originalFbx, out string rejection)
        {
            rejection = "";
            if (!IsApprovedImportedModel(originalFbx, out rejection)) return null;
            GameObject root = null, copy = null;
            try
            {
                root = new GameObject("R41_TURRET_V5_ADAPTER_CANDIDATE_ONLY");
                var yaw = new GameObject("R41_ADAPTER_YAW").transform;
                yaw.SetParent(root.transform, false);
                yaw.localPosition = new Vector3(0f,
                    (float)R41V5MuzzleGeometry.PivotHeightMeters, 0f);
                var pitch = new GameObject("R41_ADAPTER_PITCH").transform;
                pitch.SetParent(yaw, false);

                var left = new GameObject("Muzzle_L").transform;
                var right = new GameObject("Muzzle_R").transform;
                left.SetParent(pitch, false);
                right.SetParent(pitch, false);
                left.localPosition = new Vector3(
                    -(float)R41V5MuzzleGeometry.HalfBarrelSpacingMeters,0f,
                    (float)R41V5MuzzleGeometry.BarrelLengthMeters);
                right.localPosition = new Vector3(
                    (float)R41V5MuzzleGeometry.HalfBarrelSpacingMeters,0f,
                    (float)R41V5MuzzleGeometry.BarrelLengthMeters);

                // A separate clone, not an editable imported FBX instance.
                copy = Object.Instantiate(originalFbx);
                copy.name = "SOURCE_R41V5_VISUAL_NOT_GAMEPLAY";
                copy.transform.position=Vector3.zero;
                copy.transform.rotation=Quaternion.identity;
                if (PrefabUtility.IsPartOfPrefabInstance(copy))
                    PrefabUtility.UnpackPrefabInstance(copy,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                if (PrefabUtility.IsPartOfPrefabInstance(copy))
                    throw new InvalidOperationException("ORIGINAL_FBX_COPY_REMAINS_LINKED");

                var originalLeft = FindUnique(copy.transform,"Muzzle_L");
                var originalRight = FindUnique(copy.transform,"Muzzle_R");
                if (!R41V5MuzzleGeometry.ValidateImportedNeutral(
                    originalLeft.position.x,originalLeft.position.y,originalLeft.position.z,
                    originalRight.position.x,originalRight.position.y,originalRight.position.z,
                    out var geometryError))
                    throw new InvalidOperationException(geometryError);

                if (copy.GetComponentsInChildren<Renderer>(true).Length==0 ||
                    copy.GetComponentsInChildren<Joint>(true).Length>0 ||
                    copy.GetComponentsInChildren<MonoBehaviour>(true).Length>0)
                    throw new InvalidOperationException("ORIGINAL_MODEL_HAS_FOREIGN_SCRIPT_JOINT_OR_NO_GEOMETRY");

                // Only cloned VISUAL physics removed. No source prefab/rig edits.
                foreach (var rb in copy.GetComponentsInChildren<Rigidbody>(true))
                    Object.DestroyImmediate(rb);
                foreach (var col in copy.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);
                // Imported v5 has two source animations. They may NOT compete
                // with the new wrapper's yaw/pitch in this static candidate.
                foreach(var anim in copy.GetComponentsInChildren<Animator>(true))
                    anim.enabled = false;
                copy.transform.SetParent(pitch, true);

                float[] yawAngles = {0f,45f,-90f,180f};
                float[] pitchAngles = {0f,-20f,10f};
                foreach(var y in yawAngles)
                foreach(var p in pitchAngles)
                {
                    yaw.localRotation=Quaternion.Euler(0f,y,0f);
                    pitch.localRotation=Quaternion.Euler(p,0f,0f);
                    if(!R41V5MuzzleGeometry.TryPose(y,p,R41V5MuzzleSide.Left,out var l) ||
                       !R41V5MuzzleGeometry.TryPose(y,p,R41V5MuzzleSide.Right,out var r) ||
                       !R41V5MuzzleGeometry.ValidatePosePair(in l, in r) ||
                       Vector3.Distance(left.position,new Vector3((float)l.X,(float)l.Y,(float)l.Z))>.001f ||
                       Vector3.Distance(right.position,new Vector3((float)r.X,(float)r.Y,(float)r.Z))>.001f ||
                       Vector3.Distance(left.forward,new Vector3((float)l.ForwardX,(float)l.ForwardY,(float)l.ForwardZ))>.001f ||
                       Vector3.Distance(right.forward,new Vector3((float)r.ForwardX,(float)r.ForwardY,(float)r.ForwardZ))>.001f ||
                       Vector3.Distance(originalLeft.position,left.position)>.011f ||
                       Vector3.Distance(originalRight.position,right.position)>.011f)
                        throw new InvalidOperationException("YAW_PITCH_WRAPPER_MUZZLES_DIVERGE_" + y + "_" + p);
                }
                yaw.localRotation=Quaternion.identity;
                pitch.localRotation=Quaternion.identity;
                return root;
            }
            catch(Exception ex)
            {
                rejection=ex.Message;
                if (root!=null) Object.DestroyImmediate(root);
                if (copy!=null) Object.DestroyImmediate(copy);
                return null;
            }
        }

        [MenuItem("Tools/B01-31/W30/R41 v5 - Build isolated candidate from selected FBX...")]
        public static void BuildFromSelectionWithExplicitConsent()
        {
            var selected = Selection.activeObject as GameObject;
            if (!IsApprovedImportedModel(selected, out var refusal))
            {
                Debug.LogError("[B01-31 W30] FBX rejected: " + refusal);
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[B01-31 W30] Refuse while Unity is playing.");
                return;
            }
            if (!EditorUtility.DisplayDialog(
                "B01-31 R41 v5 / candidate-only",
                "Creates NEW optional v5 adapter preview/prefab, never replaces original " +
                "AutomaticGun, R41 FBX, source import settings or previous working turret. " +
                "Requires owner-approved disposable Unity 6000.3.14f1 project. Proceed?",
                "Build separate candidate","Cancel"))
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath)!=null ||
                File.Exists(CandidatePath) || File.Exists(CandidatePath+".meta"))
            {
                Debug.LogError("[B01-31 W30] Candidate output already exists; no overwrite.");
                return;
            }
            var candidate = CreateCandidatePreview(selected,out refusal);
            if (candidate==null)
            {
                Debug.LogError("[B01-31 W30] Source failed import/axis/kinematics gate: "+refusal);
                return;
            }
            try
            {
                const string ownerFolder="Assets/B01_31/W30R41V5Adapter";
                if (!AssetDatabase.IsValidFolder(ownerFolder))
                    throw new InvalidOperationException("OWNED_W30_FOLDER_MISSING");
                if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                    AssetDatabase.CreateFolder(ownerFolder,"Generated");
                if (File.Exists(CandidatePath) || File.Exists(CandidatePath+".meta") ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath)!=null)
                    throw new InvalidOperationException("FOREIGN_OUTPUT_COLLISION");
                var saved=PrefabUtility.SaveAsPrefabAsset(candidate,CandidatePath);
                if (saved==null)
                    throw new InvalidOperationException("SAVE_CANDIDATE_FAILED");
                Debug.Log("[B01-31 W30] Isolated candidate CREATED: "+CandidatePath+
                    " | SOURCE FBX and old turret unchanged | NOT APPROVED FOR GAMEPLAY");
            }
            catch(Exception ex)
            {
                // Never delete potentially preexisting or foreign output on error.
                Debug.LogError("[B01-31 W30] Needs manual review: "+ex);
                throw;
            }
            finally {Object.DestroyImmediate(candidate);}
        }
    }
}
#endif
