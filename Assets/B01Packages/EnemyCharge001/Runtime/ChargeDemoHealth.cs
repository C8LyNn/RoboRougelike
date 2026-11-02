using UnityEngine;

namespace B01.EnemyCharge001
{
    // Demonstration-only health + team adapter. Never replaces canonical game HP.
    public sealed class ChargeDemoHealth : MonoBehaviour, IChargeDamageSink, IChargeTeamIdentity
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [SerializeField] int teamId = 0;

        public float Health { get; private set; }
        public int Hits { get; private set; }
        public int ChargeTeamId => teamId;

        void Awake() { Health = maxHealth; }
        public void SetTeamId(int value) { teamId = value; }

        public void ReceiveChargeDamage(float amount, Vector3 point, GameObject instigator)
        {
            if (amount <= 0f || Health <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            Hits++;
        }
    }
}
