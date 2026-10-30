// The only reauthored test adapter. Uses UNMODIFIED original W06 DebrisImpulseLedger.
using B01.DebrisRepulse001;
using UnityEngine;
namespace B01.DebrisRepulse001
{
    public sealed class DebrisRepulsionEmitter : Component
    {
        private readonly DebrisImpulseLedger ledger=new DebrisImpulseLedger();
        public int AppliedCount=>ledger.Count;
        public bool Contains(long id)=>ledger.Contains(id);
        public bool RearmAfterReattach(long id)=>ledger.RearmAfterReattach(id);
        public void ClearForNewRun()=>ledger.ClearForNewRun();
        public bool TryApplyDetachImpulse(long id,Rigidbody body,Vector3 centre)
        {
            if(body==null || body.isKinematic) return false;
            Vector3 pos=body.transform.position;
            Vector3 vel=body.linearVelocity;
            return ledger.TryDetach(id,new Vector3d(centre.x,centre.y,centre.z),
                new Vector3d(pos.x,pos.y,pos.z),new Vector3d(vel.x,vel.y,vel.z),
                body.mass,2.5,4,out Vector3d _);
        }
    }
}
