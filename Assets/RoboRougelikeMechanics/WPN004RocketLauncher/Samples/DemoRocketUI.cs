using UnityEngine;
using RoboRougelike.Mechanics.Rockets;

namespace RoboRougelike.Mechanics.Rockets.Demo
{
    // OnGUI input is independent of the old Input Manager; optional test-only UI.
    public sealed class DemoRocketUI : MonoBehaviour
    {
        [SerializeField] private RocketLauncher launcher;
        [SerializeField] private RocketAmmoMagazine ammo;
        private string message = "Ready";
        public void Bind(RocketLauncher value, RocketAmmoMagazine magazine)
        {
            launcher = value; ammo = magazine;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 250, 135), GUI.skin.box);
            GUILayout.Label("WPN-004 Rocket Launcher demo");
            GUILayout.Label("Ammo: " + (ammo != null ? ammo.Rounds.ToString() : "infinite"));
            if (GUILayout.Button("FIRE ROCKET") && launcher != null)
                message = launcher.TryFire().ToString();
            GUILayout.Label(message);
            GUILayout.EndArea();
        }
    }
}
