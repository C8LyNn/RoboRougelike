using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R6
{
    public sealed class R6DebrisRelease : MonoBehaviour
    {
        readonly List<(Collider a,Collider b)> ignored=new List<(Collider,Collider)>();
        float deadline;
        public int PendingPairs => ignored.Count;
        public static void Release(Rigidbody body,Rigidbody owner,Vector3 velocity,Vector3 spin,float mass)
        {
            body.mass=Mathf.Max(1,mass);body.linearVelocity=velocity;body.angularVelocity=spin;
            body.maxDepenetrationVelocity=R6Tuning.Current.MaxDepenetrationSpeed;
            body.linearDamping=.1f;body.angularDamping=.5f;
            if(owner==null)return;
            var grace=body.gameObject.AddComponent<R6DebrisRelease>();
            grace.deadline=Time.time+R6Tuning.Current.DebrisGraceSeconds;
            foreach(var a in body.GetComponentsInChildren<Collider>())
                foreach(var b in owner.GetComponentsInChildren<Collider>())
                {
                    if(a==null||b==null||a==b||a.isTrigger||b.isTrigger||a.attachedRigidbody==b.attachedRigidbody||!a.bounds.Intersects(b.bounds))continue;
                    if(!Overlaps(a,b))continue;
                    Physics.IgnoreCollision(a,b,true);grace.ignored.Add((a,b));
                }
        }
        static bool Overlaps(Collider a,Collider b)=>Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out _);
        void FixedUpdate()
        {
            for(int i=ignored.Count-1;i>=0;i--)
            {
                var pair=ignored[i];
                if(pair.a!=null&&pair.b!=null&&Time.time<deadline&&Overlaps(pair.a,pair.b))continue;
                if(pair.a!=null&&pair.b!=null)Physics.IgnoreCollision(pair.a,pair.b,false);
                ignored.RemoveAt(i);
            }
            if(ignored.Count==0)Destroy(this);
        }
        void OnDestroy(){foreach(var pair in ignored)if(pair.a!=null&&pair.b!=null)Physics.IgnoreCollision(pair.a,pair.b,false);}
    }
}
