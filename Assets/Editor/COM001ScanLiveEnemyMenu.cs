#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R6;

namespace RoboRougelike.EditorTools
{
    // Manual testing only; no modification to the production UI or player input bindings.
    public static class COM001ScanLiveEnemyMenu
    {
        [MenuItem("Tools/RoboRougelike/COM-001/Scan Visible Enemy Parts (Play Mode)")]
        private static void Scan()
        {
            var run = R1RunController.Instance;
            var player = run != null ? run.Player : null;
            if (player == null)
            {
                Debug.LogWarning("[COM001] No active player. Start an encounter first.");
                return;
            }
            var bridge = player.GetComponent<COM001ProductionScanBridge>();
            if (bridge == null) bridge = player.gameObject.AddComponent<COM001ProductionScanBridge>();
            bridge.TriggerScan();
            Selection.activeGameObject = player.gameObject;
        }

        [MenuItem("Tools/RoboRougelike/COM-001/Scan Visible Enemy Parts (Play Mode)", true)]
        private static bool CanScan() => EditorApplication.isPlaying;
    }
}
#endif
