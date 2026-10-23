using UnityEngine;
using RoboRougelike.Mechanics.Rockets;

namespace RoboRougelike.Mechanics.Rockets.Demo
{
    // DEMO ONLY: never use as R6 production HP, splitting or salvage.
    public sealed class DemoRocketBlock : MonoBehaviour, IRocketDamageReceiver
    {
        [SerializeField] private float hitPoints = 80f;
        public float RemainingHP => hitPoints;
        public void ReceiveRocketDamage(float damage, Vector3 point, Vector3 impulse, GameObject attacker)
        {
            hitPoints = Mathf.Max(0f, hitPoints - damage);
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb != null && !rb.isKinematic && impulse.sqrMagnitude > 0f)
                rb.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            if (hitPoints <= 0f) Destroy(gameObject);
        }
    }
}
