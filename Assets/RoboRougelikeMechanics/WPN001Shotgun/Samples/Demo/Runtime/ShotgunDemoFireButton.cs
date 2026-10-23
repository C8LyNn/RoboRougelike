using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Demo
{
    /// <summary>Demo input independent of old/new Input System. Remove from the game build.</summary>
    public sealed class ShotgunDemoFireButton : MonoBehaviour
    {
        [SerializeField] private ShotgunModule shotgun;
        public void Bind(ShotgunModule module) => shotgun = module;
        private void OnGUI()
        {
            if (shotgun == null) return;
            if (GUI.Button(new Rect(12, 12, 190, 48), "FIRE WPN-001 DEMO")) shotgun.TryFire();
            GUI.Label(new Rect(12, 64, 340, 25),
                "Volleys: " + shotgun.SuccessfulVolleys + " | Last: " + shotgun.LastFailure);
        }
    }
}
