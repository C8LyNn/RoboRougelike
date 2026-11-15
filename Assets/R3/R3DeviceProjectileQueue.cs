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
                    foreach(var sibling in batch.Siblings)if(sibling!=null)P17PhysicalShell.IgnoreProjectilePair(shot,sibling);
                    batch.Siblings.Add(shot);
                    batch.Remaining-=BigInteger.One;
                    if(batch.Remaining.IsZero)batches.Dequeue();
                }
                catch(Exception e){if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}UnityEngine.Debug.LogException(e,this);break;}
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
