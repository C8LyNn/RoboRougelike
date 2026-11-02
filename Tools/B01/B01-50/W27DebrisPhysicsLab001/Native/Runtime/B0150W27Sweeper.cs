using UnityEngine;
namespace B01.Performance.W27DebrisLab
{
    public sealed class B0150W27Sweeper : MonoBehaviour
    {
        private Rigidbody body; private Vector3 origin;
        private float radius, frequency, phase;
        public void Configure(Rigidbody rb, Vector3 localOrigin, float distance, float hz)
        { body=rb; origin=localOrigin; radius=distance; frequency=hz; }
        private void FixedUpdate()
        {
            if (body==null || !body.isKinematic) return;
            phase += Time.fixedDeltaTime * frequency * 6.2831853f;
            body.MovePosition(origin + Vector3.right * (Mathf.Sin(phase) * radius));
        }
    }
}
