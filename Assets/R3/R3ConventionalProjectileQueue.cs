using System;
using System.Collections.Generic;
using System.Diagnostics;
using RoboRougelike.R1;
using RoboRougelike.R4;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoboRougelike.R3
{
    // Bonus shells remain owned by the vehicle even if the physical gun is lost.
    public sealed class R3ConventionalProjectileQueue : MonoBehaviour
    {
        readonly struct Sibling
        {
            public readonly Collider Collider;
            public readonly ulong Launch;
            public Sibling(Collider collider,P17PhysicalShell shell){Collider=collider;Launch=shell.LaunchSequence;}
            public bool Live
            {
                get
                {
                    if(Collider==null||!Collider.enabled||!Collider.gameObject.activeInHierarchy)return false;
                    var shell=Collider.GetComponent<P17PhysicalShell>();
                    return shell!=null&&shell.LaunchSequence==Launch&&shell.IsActiveProjectile;
                }
            }
        }
        sealed class Batch
        {
            public int Remaining;
            public P17PhysicalShell.ConventionalSnapshot Shot;
            public readonly List<Sibling> Siblings=new List<Sibling>();
            public LinkedList<Sibling> Group;
            public LinkedListNode<Sibling> Cursor;
        }
        readonly Queue<Batch> batches=new Queue<Batch>();
        double workBudgetMilliseconds=4d;
        static int budgetFrame=-1;
        static long budgetStart;

        internal void Enqueue(P17PhysicalShell primary,int count)
        {
            if(count<=0)return;
            if(primary==null)throw new ArgumentNullException(nameof(primary));
            var batch=new Batch{Remaining=count,Shot=primary.CaptureConventional()};
            var collider=primary.GetComponent<Collider>();
            if(collider!=null)batch.Siblings.Add(new Sibling(collider,primary));
            batches.Enqueue(batch);Drain();
        }
        void FixedUpdate(){if(Time.timeScale>0)Drain();}
        void OnDisable(){if(!gameObject.activeInHierarchy)batches.Clear();}
        void Drain()
        {
            if(batches.Count==0)return;
            var run=R1RunController.Instance;
            if(run!=null&&run.Player!=null&&run.Player.transform==transform&&run.EmergencyRescueAttackBlocked)return;
            if(budgetFrame!=Time.frameCount){budgetFrame=Time.frameCount;budgetStart=Stopwatch.GetTimestamp();}
            bool attempted=false;
            while(batches.Count>0&&(!attempted||(Stopwatch.GetTimestamp()-budgetStart)*1000d/Stopwatch.Frequency<workBudgetMilliseconds))
            {
                var batch=batches.Peek();
                if(batch.Shot.Owner==null||!batch.Shot.Owner.gameObject.activeInHierarchy){batches.Dequeue();continue;}
                attempted=true;P17PhysicalShell shot=null;
                try
                {
                    shot=Spawn(batch.Shot);ExcludeSiblings(batch,shot);
                    batch.Remaining--;if(batch.Remaining==0)batches.Dequeue();
                }
                catch(Exception e){if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}UnityEngine.Debug.LogException(e,this);break;}
            }
        }
        static P17PhysicalShell Spawn(P17PhysicalShell.ConventionalSnapshot data)
        {
            P17PhysicalShell shell=null;
            try
            {
                shell=P17PhysicalShell.RentConventional();var item=shell.gameObject;item.name="P17_PlayerPhysicalShell";
                item.transform.SetPositionAndRotation(data.Position,Quaternion.identity);item.transform.localScale=data.Scale;
                var renderer=item.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=data.Material!=null?data.Material:shell.DefaultMaterial;
                var col=item.GetComponent<SphereCollider>();col.enabled=true;col.isTrigger=false;
                var origin=item.GetComponent<R4ProjectileOrigin>();origin.Weapon=data.Weapon;origin.PartId=data.PartId;
                var body=item.GetComponent<Rigidbody>();body.mass=data.Mass;body.useGravity=false;body.isKinematic=false;
                body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                item.SetActive(true);body.angularVelocity=Vector3.zero;body.linearVelocity=data.Velocity;
                if(data.Hybrid)shell.ConfigureHybrid(data.Owner,data.Kinetic,data.Blast,data.Lifetime,data.Direction,data.Target,data.Range);
                else shell.Configure(data.Owner,data.Damage,data.Lifetime,data.Direction,data.Target,data.Range);
                return shell;
            }
            catch{if(shell!=null){shell.gameObject.SetActive(false);Object.Destroy(shell.gameObject);}throw;}
        }
        static void ExcludeSiblings(Batch batch,P17PhysicalShell projectile)
        {
            var col=projectile.GetComponent<Collider>();var shot=new Sibling(col,projectile);
            if(batch.Group!=null)
            {
                var node=batch.Group.Last;
                while(node!=null)
                {
                    var previous=node.Previous;
                    if(!node.Value.Live)
                    {if(ReferenceEquals(batch.Cursor,node))batch.Cursor=null;batch.Group.Remove(node);node=previous;continue;}
                    if(P17PhysicalShell.TryJoinVolley(node.Value.Collider,col))
                    {batch.Group.AddLast(shot);PruneGroup(batch);return;}
                    node=previous;
                }
                foreach(var old in batch.Group)if(old.Live)batch.Siblings.Add(old);
                batch.Group=null;batch.Cursor=null;
            }
            for(int i=batch.Siblings.Count-1;i>=0;i--)if(!batch.Siblings[i].Live)batch.Siblings.RemoveAt(i);
            if(batch.Siblings.Count==32)
            {
                var colliders=new List<Collider>(33);foreach(var sibling in batch.Siblings)colliders.Add(sibling.Collider);colliders.Add(col);
                if(P17PhysicalShell.TryGroupVolleyColliders(colliders))
                {batch.Siblings.Add(shot);batch.Group=new LinkedList<Sibling>(batch.Siblings);batch.Cursor=batch.Group.First;batch.Siblings.Clear();return;}
            }
            foreach(var sibling in batch.Siblings)P17PhysicalShell.IgnoreProjectilePair(col,sibling.Collider);
            batch.Siblings.Add(shot);
        }
        static void PruneGroup(Batch batch)
        {
            for(int i=0;i<4&&batch.Group.Count>0;i++)
            {
                var current=batch.Cursor??batch.Group.First;var next=current.Next??batch.Group.First;
                if(!current.Value.Live){if(ReferenceEquals(current,next))next=null;batch.Group.Remove(current);}
                batch.Cursor=next;
            }
        }
    }
}
