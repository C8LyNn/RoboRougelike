using UnityEngine;

namespace B01.DebrisRepulse001
{
    // Opt-in adapter. Canonical robot detachment and identity remain Codex-owned.
    [DisallowMultipleComponent]
    public sealed class DebrisRepulsionEmitter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float desiredSpeed = 2.5f;
        [SerializeField, Min(0f)] private float maxAddedSpeed = 4f;
        private readonly DebrisImpulseLedger ledger = new DebrisImpulseLedger();

        public int AppliedCount => ledger.Count;
        public bool TryApplyDetachImpulse(long stablePartId, Rigidbody body, Vector3 ownerCentre)
        {
            if (body == null || body.isKinematic) return false;
            Vector3 centre = body.worldCenterOfMass;
            Vector3 velocity = body.linearVelocity; // Unity 6 Rigidbody API
            if (!ledger.TryDetach(stablePartId,
                new Vector3d(ownerCentre.x,ownerCentre.y,ownerCentre.z),
                new Vector3d(centre.x,centre.y,centre.z),
                new Vector3d(velocity.x,velocity.y,velocity.z),
                body.mass,desiredSpeed,maxAddedSpeed,out Vector3d impulse))
                return false;
            if (impulse.MagnitudeSquared > 0)
                body.AddForce(new Vector3((float)impulse.X,(float)impulse.Y,(float)impulse.Z),ForceMode.Impulse);
            return true;
        }
        public bool RearmAfterReattach(long stablePartId) => ledger.RearmAfterReattach(stablePartId);
        public void ClearForNewRun() => ledger.ClearForNewRun();
    }
}
