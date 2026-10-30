using UnityEditor;
using UnityEngine;

namespace B01.ReattachClearance001.Editor
{
    public static class SafeReattachDemoInstaller
    {
        [MenuItem("B01 / PASS17 / Add safe reattach gate to W07 demo")]
        public static void Install()
        {
            var controller = GameObject.Find("B01-23 Detached Physics Demo");
            if (controller == null ||
                controller.GetComponent<B01.DebrisPlayground001.DebrisRigPlayground>() == null)
            {
                Debug.LogError("W07 original demo controller not found. Open the isolated W07 generated scene first.");
                return;
            }
            if (controller.GetComponent<DebrisSafeReattachGate>() != null)
            {
                Debug.LogWarning("W08 safe gate already installed. No duplicate component added.");
                return;
            }
            Undo.AddComponent<DebrisSafeReattachGate>(controller);
            Debug.Log("W08 isolated, opt-in safe reattach gate added via Undo. Native testing required.");
        }
    }
}
