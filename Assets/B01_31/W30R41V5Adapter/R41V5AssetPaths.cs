using System;
using System.IO;

namespace RoboRougelike.B0131.W30
{
    /// <summary>
    /// Unity AssetDatabase paths are project-relative, NOT process-relative.
    /// Computes exactly the approved imported-v5 path from Application.dataPath
    /// without depending on process CWD and without resolving foreign imports.
    /// This production source is also tested VERBATIM on Windows and Ubuntu.
    /// </summary>
    public static class R41V5AssetPaths
    {
        public const string ExactV5AssetPath =
            "Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx";

        public static bool TryResolveFbxPath(
            string unityAssetsAbsolutePath, string unityAssetPath,
            out string absoluteFbxPath, out string refusal)
        {
            absoluteFbxPath = "";
            refusal = "";
            if (string.IsNullOrWhiteSpace(unityAssetsAbsolutePath) ||
                !Path.IsPathRooted(unityAssetsAbsolutePath))
            { refusal = "UNITY_ASSETS_DIRECTORY_NOT_ABSOLUTE"; return false; }

            // A fixed exact source-relative asset path cannot contain '..',
            // backslashes, symlink-injected subfolders or ambiguous aliases.
            if (!string.Equals(unityAssetPath, ExactV5AssetPath,
                StringComparison.Ordinal))
            { refusal = "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH"; return false; }

            try
            {
                var assetsDirectory = Path.GetFullPath(unityAssetsAbsolutePath);
                if (!string.Equals(
                    new DirectoryInfo(assetsDirectory).Name, "Assets",
                    StringComparison.Ordinal))
                { refusal = "UNITY_ASSETS_ROOT_NAME_MISMATCH"; return false; }

                const string prefix = "Assets/";
                var exactRelative = ExactV5AssetPath.Substring(prefix.Length)
                    .Replace('/', Path.DirectorySeparatorChar);
                var candidate = Path.GetFullPath(
                    Path.Combine(assetsDirectory, exactRelative));

                var rootPrefix = assetsDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;
                if (!candidate.StartsWith(rootPrefix,
                    StringComparison.Ordinal))
                { refusal = "UNEXPECTED_FBXP_PATH_OUTSIDE_UNITY_PROJECT"; return false; }
                absoluteFbxPath = candidate;
                return true;
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is IOException ||
                ex is NotSupportedException)
            {
                refusal = "INVALID_UNITY_PROJECT_ASSET_PATH";
                return false;
            }
        }
    }
}
