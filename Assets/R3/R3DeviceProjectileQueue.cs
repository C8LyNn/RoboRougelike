using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using Vector3=UnityEngine.Vector3;
using Object=UnityEngine.Object;

namespace RoboRougelike.R3
{
    // Owned by the vehicle, not the emitter: already due shots survive losing
    // a drone/turret, just as projectiles already in flight do.
    public sealed class R3DeviceProjectileQueue : MonoBehaviour
    {
        internal sealed class Batch
        {
            public BigInteger Remaining;
            public Transform Owner,Target;
            public Vector3 Origin,Direction;
            public float Damage,Range;
            public Material Material;
            public Collider[] SourceColliders;
            public readonly List<Collider> Siblings=new List<Collider>();
            public LinkedList<Collider> GroupMembers;
            public LinkedListNode<Collider> PruneCursor;
        }
        readonly Queue<Batch> batches=new Queue<Batch>();
        double workBudgetMilliseconds=4d;
        static int budgetFrame=-1;
        static long budgetStart;

        internal void Enqueue(Batch batch)
        {
            if(batch==null||batch.Remaining<=BigInteger.Zero)throw new ArgumentException("A batch must contain shots.",nameof(batch));
            batches.Enqueue(batch);
            Drain();
        }
        void FixedUpdate(){if(Time.timeScale>0)Drain();}
        void OnDisable()
        {
            // Active projectiles retire when their owner is deactivated.
            // Do not resurrect shots still awaiting creation on reactivation.
            if(!gameObject.activeInHierarchy)batches.Clear();
        }
        void Drain()
        {
            if(batches.Count==0)return;
            var run=R1RunController.Instance;
            if(run!=null&&run.Player!=null&&run.Player.transform==transform&&run.EmergencyRescueAttackBlocked)return;
            if(budgetFrame!=Time.frameCount){budgetFrame=Time.frameCount;budgetStart=Stopwatch.GetTimestamp();}
            // Preserve the previous immediate first-shot behavior. Further
            // work is time-budgeted and stays queued instead of being dropped.
            bool attempted=false;
            while(batches.Count>0&&(!attempted||(Stopwatch.GetTimestamp()-budgetStart)*1000d/Stopwatch.Frequency<workBudgetMilliseconds))
            {
                var batch=batches.Peek();
                // Existing shells retire when their owner disappears or is disabled.
                if(batch.Owner==null||!batch.Owner.gameObject.activeInHierarchy){batches.Dequeue();continue;}
                attempted=true;
                Collider shot=null;
                try
                {
                    shot=Spawn(batch);
                    ExcludeSiblings(batch,shot);
                    batch.Remaining-=BigInteger.One;
                    batches.Dequeue();
                    if(!batch.Remaining.IsZero)batches.Enqueue(batch); // Share work among already-pending devices.
                }
                catch(Exception e){if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}UnityEngine.Debug.LogException(e,this);break;}
            }
        }
        static bool Live(Collider col)=>col!=null&&col.enabled&&col.gameObject.activeInHierarchy;
        static void ExcludeSiblings(Batch batch,Collider shot)
        {
            if(batch.GroupMembers!=null)
            {
                // Usually the last shot is still alive and gives O(1) joining.
                // If it expired, locate another member of the same lease.
                var member=batch.GroupMembers.Last;
                while(member!=null)
                {
                    var previous=member.Previous;
                    if(!Live(member.Value))
                    {
                        if(ReferenceEquals(batch.PruneCursor,member))batch.PruneCursor=null;
                        batch.GroupMembers.Remove(member);member=previous;continue;
                    }
                    if(P17PhysicalShell.TryJoinDeviceVolley(member.Value,shot))
                    {
                        batch.GroupMembers.AddLast(shot);
                        PruneGroup(batch);return;
                    }
                    member=previous;
                }
                // If the lease is no longer usable, preserve collisions with
                // every still-live prior shot using ordinary per-pair ignores.
                foreach(var old in batch.GroupMembers)if(Live(old))batch.Siblings.Add(old);
                batch.GroupMembers=null;batch.PruneCursor=null;
            }
            for(int i=batch.Siblings.Count-1;i>=0;i--)
            {
                var sibling=batch.Siblings[i];
                if(!Live(sibling))batch.Siblings.RemoveAt(i);
            }
            if(batch.Siblings.Count==32)
            {
                batch.Siblings.Add(shot);
                if(P17PhysicalShell.TryGroupDeviceVolleyColliders(batch.Siblings))
                {
                    batch.GroupMembers=new LinkedList<Collider>(batch.Siblings);
                    batch.PruneCursor=batch.GroupMembers.First;batch.Siblings.Clear();return;
                }
                batch.Siblings.RemoveAt(batch.Siblings.Count-1);
            }
            foreach(var sibling in batch.Siblings)P17PhysicalShell.IgnoreProjectilePair(shot,sibling);
            batch.Siblings.Add(shot);
        }
        static void PruneGroup(Batch batch)
        {
            // Bound cleanup work per shot instead of repeatedly scanning every
            // member of a large in-flight volley.
            for(int i=0;i<4&&batch.GroupMembers.Count>0;i++)
            {
                var current=batch.PruneCursor??batch.GroupMembers.First;
                var next=current.Next??batch.GroupMembers.First;
                if(!Live(current.Value))
                {
                    if(ReferenceEquals(current,next))next=null;
                    batch.GroupMembers.Remove(current);
                }
                batch.PruneCursor=next;
            }
        }
        static Collider Spawn(Batch shot)
        {
            GameObject shell=null;
            try
            {
                shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="P17_DronePhysicalShell";
                shell.transform.position=shot.Origin;shell.transform.localScale=Vector3.one*.16f;
                if(shot.Material!=null)shell.GetComponent<Renderer>().sharedMaterial=shot.Material;
                var col=shell.GetComponent<Collider>();
                if(shot.SourceColliders!=null)foreach(var source in shot.SourceColliders)if(source!=null)Physics.IgnoreCollision(col,source,true);
                var rb=shell.AddComponent<Rigidbody>();rb.mass=1;rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearVelocity=shot.Direction*38;
                float projectileRange=(float)Math.Min(float.MaxValue,Math.Max(45d,(double)shot.Range+5d));
                float lifetime=(float)Math.Min(float.MaxValue,Math.Max(3d,projectileRange/38d+1d));
                shell.AddComponent<P17PhysicalShell>().Configure(shot.Owner,shot.Damage,lifetime,shot.Direction,shot.Target,projectileRange);
                return col;
            }
            catch{if(shell!=null){shell.SetActive(false);Object.Destroy(shell);}throw;}
        }
    }
}
