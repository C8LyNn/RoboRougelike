#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R6;

namespace RoboRougelike.EditorTools
{
    // Adds optional passive recording ONLY to the actual running vehicle.
    // Does not edit scenes, prefabs, garage data or physics.
    public static class MapR6AttachRealVehicleEvidence
    {
        [MenuItem("Tools/RoboRougelike/MAP R6/Attach Recorder To Live Player")]
        private static void Attach()
        {
            var player = R1RunController.Instance?.Player;
            if (player == null || player.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning("MAP-R6: no live player Rigidbody. Enter Play and start a test drive or encounter.");
                return;
            }
            var bridge = player.GetComponent<MapR6ProductionEvidenceBridge>();
            if (bridge == null) bridge = player.gameObject.AddComponent<MapR6ProductionEvidenceBridge>();
            Selection.activeGameObject = bridge.gameObject;
            EditorGUIUtility.PingObject(bridge);
            Debug.Log("MAP-R6: passive recorder attached. Use Recorder > Export evidence JSON in Inspector. This is NOT a route completion PASS.");
        }

        [MenuItem("Tools/RoboRougelike/MAP R6/Attach Recorder To Live Player", true)]
        private static bool CanAttach() => EditorApplication.isPlaying;
    }
}
#endif
