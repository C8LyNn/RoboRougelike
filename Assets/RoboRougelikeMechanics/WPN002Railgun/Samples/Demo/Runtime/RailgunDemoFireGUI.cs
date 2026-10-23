using UnityEngine;

namespace RoboRougelike.WPN002Railgun.Demo
{
    public sealed class RailgunDemoFireGUI : MonoBehaviour
    {
        [SerializeField] private RailgunModule weapon;
        public void Bind(RailgunModule gun) => weapon = gun;
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUI.Box(new Rect(12, 12, 310, 100), "WPN-002 Railgun — isolated demo");
            if (GUI.Button(new Rect(24, 42, 130, 40), "FIRE RAILGUN") && weapon != null)
                weapon.TryFire();
            GUI.Label(new Rect(163, 50, 148, 38), weapon == null ? "No weapon" :
                "Shots: " + weapon.SuccessfulShots + " / " + weapon.LastFailure);
        }
    }
}
