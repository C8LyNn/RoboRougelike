using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    // Optional standalone counter for testing/demos. Do not replace R6 inventory with this.
    [DisallowMultipleComponent]
    public sealed class ShotgunSimpleAmmo : MonoBehaviour, IShotgunShotResourceBank
    {
        [SerializeField, Min(0)] private int rounds = 12;
        [SerializeField] private bool infiniteAmmo;
        public int Rounds => rounds;
        public bool InfiniteAmmo => infiniteAmmo;
        public void Configure(int startingRounds, bool infinite)
        {
            rounds = Mathf.Max(0, startingRounds);
            infiniteAmmo = infinite;
        }
        public bool TryConsumeShot()
        {
            if (infiniteAmmo) return true;
            if (rounds <= 0) return false;
            rounds--;
            return true;
        }
        public void RefundShot()
        {
            if (!infiniteAmmo && rounds < int.MaxValue) rounds++;
        }
    }
}
