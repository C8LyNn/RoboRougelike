using UnityEngine;
using RoboRougelike.Mechanics.PointDefense;

namespace RoboRougelike.Mechanics.PointDefense.Tests
{
    public sealed class PointDefenseTestThreat : MonoBehaviour, IPointDefenseThreat
    {
        public int faction = 2;
        public float health = 1f;
        public Vector3 speed = new Vector3(0, 0, -5);
        public int interceptCalls;
        public int FactionId { get { return faction; } }
        public bool IsActiveThreat { get { return health > 0f && isActiveAndEnabled; } }
        public Transform AimTransform { get { return transform; } }
        public Vector3 Velocity { get { return speed; } }
        public bool TryIntercept(float damage)
        {
            interceptCalls++;
            health -= Mathf.Max(0, damage);
            return health <= 0;
        }
    }
}
