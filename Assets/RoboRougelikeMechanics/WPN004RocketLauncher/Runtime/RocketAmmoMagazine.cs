using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    [DisallowMultipleComponent]
    public sealed class RocketAmmoMagazine : MonoBehaviour, IRocketAmmoSource
    {
        [SerializeField, Min(0)] private int rounds = 10;
        public int Rounds => rounds;
        public void SetRounds(int value) => rounds = Mathf.Max(0, value);
        public bool TrySpendAmmo(int units)
        {
            if (units < 0 || units > rounds) return false;
            rounds -= units;
            return true;
        }
        public void RefundAmmo(int units)
        {
            if (units > 0) rounds = rounds > int.MaxValue - units ? int.MaxValue : rounds + units;
        }
    }
}
