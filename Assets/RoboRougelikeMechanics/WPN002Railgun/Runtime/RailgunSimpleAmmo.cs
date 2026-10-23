using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    // Optional demo reservoir. Integrate production ammo/energy through IRailgunShotResourceBank.
    public sealed class RailgunSimpleAmmo : MonoBehaviour, IRailgunShotResourceBank
    {
        [SerializeField, Min(0)] private int rounds = 12;
        public int Rounds => rounds;
        public void SetRounds(int count) => rounds = Mathf.Max(0, count);
        public bool TryConsumeShot() { if (rounds == 0) return false; rounds--; return true; }
        public void RefundShot() { if (rounds < int.MaxValue) rounds++; }
    }
}
