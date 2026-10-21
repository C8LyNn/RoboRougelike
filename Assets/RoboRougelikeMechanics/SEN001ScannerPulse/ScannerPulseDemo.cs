using UnityEngine;

namespace RoboRougelike.Mechanics.SEN001
{
    /// <summary>Optional, keybind-free demo. Attach manually next to ScannerPulse in a sandbox scene.</summary>
    [RequireComponent(typeof(ScannerPulse))]
    public sealed class ScannerPulseDemo : MonoBehaviour
    {
        private ScannerPulse scanner;

        private void Awake() => scanner = GetComponent<ScannerPulse>();

        private void OnGUI()
        {
            if (scanner == null) return;
            GUILayout.BeginArea(new Rect(16f, 16f, 340f, 140f), GUI.skin.box);
            GUILayout.Label("SEN-001 / manual scanner demonstration");
            GUILayout.Label("Ready: " + scanner.Ready + " | cooldown: " +
                scanner.RemainingCooldown.ToString("F1") + " s");
            if (GUILayout.Button("Pulse at this object") && scanner.Ready)
                scanner.TryPulse(transform.position);
            GUILayout.Label("Unique hits: " + scanner.LastHits.Count +
                " | revealed: " + scanner.Revealed.Count +
                " | capacity reached: " + scanner.LastPulseTruncated);
            GUILayout.EndArea();
        }

        private void OnDrawGizmosSelected()
        {
            var pulse = GetComponent<ScannerPulse>();
            if (pulse == null) return;
            Gizmos.DrawWireSphere(transform.position, pulse.RadiusMeters);
        }
    }
}
