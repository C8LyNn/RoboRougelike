using UnityEditor;
using UnityEngine;
using B01.DebrisPlayground001;
using B01.ReattachClearance001;

namespace B01.DockHardening001.Editor
{
    public static class HardenedDockInstaller
    {
        [MenuItem("B01 / PASS17 / W09 Install protected docking demo")]
        public static void Install()
        {
            GameObject host = GameObject.Find("B01-23 Detached Physics Demo");
            if (host == null ||
                host.GetComponent<DebrisRigPlayground>() == null ||
                host.GetComponent<DebrisSafeReattachGate>() == null)
            {
                Debug.LogError("Use an isolated W07 demo scene with original W08 gate installed first.");
                return;
            }
            if (host.GetComponent<HardenedDockDemo>() != null)
            {
                Debug.LogWarning("W09 protected UI already attached; refusing duplicate.");
                return;
            }
            Undo.AddComponent<HardenedDockDemo>(host);
            Debug.Log("W09 protected UI installed on isolated host; play to arm. No canonical game changed.");
        }
    }
}
