using UnityEngine;
namespace B01.Performance.W27DebrisLab
{
    public sealed class B0150W27ActiveControl : MonoBehaviour
    {
        private Rigidbody body; private Vector3 initialPosition;
        public void Configure(Rigidbody rb) { body=rb; initialPosition=rb.position; }
        private void FixedUpdate()
        {
            if (body==null) return;
            // A real actively driven Rigidbody must never be explicitly put to sleep.
            if (body.position.x > initialPosition.x + 6f)
                body.position = initialPosition; // fixture-only reset, not gameplay
            body.linearVelocity = Vector3.right * 4f;
        }
    }
}
