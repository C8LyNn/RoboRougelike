#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RogueMachine.P12PipelineSafe.Editor
{
    [InitializeOnLoad]
    public static class P12PipelineSafeAutoCleanup
    {
        private const string SessionKey = "RogueMachine_P12_PipelineSafe_CleanupDone";

        private static readonly string[] ObsoletePrefixes =
        {
            "P11_OriginLock",
            "P10_StableRig",
            "P09_Groundlock",
            "P08_CleanBoot",
            "P07_ReturnFire",
            "P06_Sightline",
            "P05_BreachTest",
            "P04_Trackframe"
        };

        static P12PipelineSafeAutoCleanup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("Tools/Rogue Machine/[12] Pipeline Safe/CLEAN OLD PACK FILES")]
        public static void ManualCleanup()
        {
            RunCleanup(true);
        }

        private static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            RunCleanup(false);
        }

        private static void RunCleanup(bool verbose)
        {
            int removed = 0;
            string root = Application.dataPath.Replace("\\", "/");

            string[] allFiles;
            try
            {
                allFiles = Directory.GetFiles(
                    Application.dataPath,
                    "*",
                    SearchOption.AllDirectories
                );
            }
            catch
            {
                return;
            }

            foreach (string absolute in allFiles)
            {
                string fileName = Path.GetFileName(absolute);

                bool obsolete = false;

                foreach (string prefix in ObsoletePrefixes)
                {
                    if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        obsolete = true;
                        break;
                    }
                }

                if (fileName == "PACK_06_README.txt" ||
                    fileName == "PACK_05_README.txt" ||
                    fileName == "PACK_04_README.txt")
                {
                    obsolete = true;
                }

                if (!obsolete)
                    continue;

                string normalized = absolute.Replace("\\", "/");

                if (!normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;

                string assetPath = "Assets" + normalized.Substring(root.Length);

                if (assetPath.Contains("P12_PipelineSafe"))
                    continue;

                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    removed++;

                    if (verbose)
                        Debug.Log("[Pack 12] Removed obsolete file: " + assetPath);
                }
            }

            string[] oldGeneratedFolders =
            {
                "Assets/RogueMachineGenerated_P11_OriginLock",
                "Assets/RogueMachineGenerated_P10_StableRig",
                "Assets/RogueMachineGenerated_P09_Groundlock",
                "Assets/RogueMachineGenerated_P08_CleanBoot",
                "Assets/RogueMachineGenerated_P07_ReturnFire",
                "Assets/RogueMachineGenerated_P06_Sightline",
                "Assets/RogueMachineGenerated_P05_BreachTest",
                "Assets/RogueMachineGenerated_P04_Trackframe"
            };

            foreach (string folder in oldGeneratedFolders)
            {
                if (AssetDatabase.IsValidFolder(folder) &&
                    AssetDatabase.DeleteAsset(folder))
                {
                    removed++;

                    if (verbose)
                        Debug.Log("[Pack 12] Removed obsolete folder: " + folder);
                }
            }

            AssetDatabase.Refresh();

            if (verbose || removed > 0)
            {
                Debug.Log(
                    $"[Pack 12 Pipeline Safe] Cleanup complete. Removed {removed} obsolete item(s)."
                );
            }
        }
    }
}
#endif
