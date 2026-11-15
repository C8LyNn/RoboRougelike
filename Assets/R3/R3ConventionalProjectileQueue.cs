using System;
using System.Collections.Generic;
using System.Diagnostics;
using RoboRougelike.R1;
using RoboRougelike.R4;
using RoboRougelike.WPN002Railgun;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using Object=UnityEngine.Object;
using BigInteger=System.Numerics.BigInteger;

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
            public bool IsRailgun;
            public RailgunShot RailShot;
            public float RailDamageScale;
            public Material RailMaterial;
            public string RailPartId;
            public bool IsShotgun;
            public P17PhysicalShell.ConventionalSnapshot[] Pellets;
            public int[] PelletRemaining;
            public int NextPellet;
            public BigInteger ShotgunRemaining;
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
        internal void EnqueueRailgun(RailgunShot shot,int count,float damageScale,Material material,string partId)
        {
            if(count<=0)return;
            if(shot.OwnerRoot==null||shot.OwnerBody==null)throw new ArgumentException("A rail volley needs an owner and body.",nameof(shot));
            batches.Enqueue(new Batch{Remaining=count,IsRailgun=true,RailShot=shot,RailDamageScale=damageScale,RailMaterial=material,RailPartId=partId});
            Drain();
        }
        internal void EnqueueShotgun(IReadOnlyList<GameObject> primaries,IReadOnlyList<int> requested)
        {
            if(primaries==null||requested==null||primaries.Count==0||primaries.Count!=requested.Count)
                throw new ArgumentException("Shotgun pellets and bonus requests must match.");
            var batch=new Batch{IsShotgun=true,Pellets=new P17PhysicalShell.ConventionalSnapshot[primaries.Count],PelletRemaining=new int[primaries.Count]};
            bool grouped=true;
            for(int i=0;i<primaries.Count;i++)
            {
                var shell=primaries[i]!=null?primaries[i].GetComponent<P17PhysicalShell>():null;
                if(shell==null||requested[i]<0)throw new ArgumentException("A live primary and non-negative request are required.");
                batch.Pellets[i]=shell.CaptureConventional();batch.PelletRemaining[i]=requested[i];batch.ShotgunRemaining+=requested[i];
                var collider=shell.GetComponent<Collider>();if(collider==null)throw new ArgumentException("A pellet needs a collider.");
                batch.Siblings.Add(new Sibling(collider,shell));grouped &= shell.HasVolleyLayer;
            }
            if(batch.ShotgunRemaining.IsZero)return;
            batch.Shot=batch.Pellets[0];
            if(grouped){batch.Group=new LinkedList<Sibling>(batch.Siblings);batch.Cursor=batch.Group.First;batch.Siblings.Clear();}
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
                var owner=batch.IsRailgun?(batch.RailShot.OwnerRoot!=null?batch.RailShot.OwnerRoot.transform:null):batch.Shot.Owner;
                if(owner==null||!owner.gameObject.activeInHierarchy){batches.Dequeue();continue;}
                attempted=true;P17PhysicalShell shot=null;
                try
                {
                    if(batch.IsRailgun)shot=SpawnRailgun(batch);
                    else
                    {
                        int pellet=-1;
                        if(batch.IsShotgun)
                        {
                            for(int i=0;i<batch.PelletRemaining.Length;i++)
                            {int index=(batch.NextPellet+i)%batch.PelletRemaining.Length;if(batch.PelletRemaining[index]>0){pellet=index;break;}}
                            if(pellet<0)throw new InvalidOperationException("Shotgun bonus count is inconsistent.");
                        }
                        shot=Spawn(batch.IsShotgun?batch.Pellets[pellet]:batch.Shot);ExcludeSiblings(batch,shot);
                        if(batch.IsShotgun)
                        {batch.PelletRemaining[pellet]--;batch.ShotgunRemaining-=BigInteger.One;batch.NextPellet=(pellet+1)%batch.PelletRemaining.Length;}
                    }
                    bool finished;
                    if(batch.IsShotgun)finished=batch.ShotgunRemaining.IsZero;
                    else{batch.Remaining--;finished=batch.Remaining==0;}
                    batches.Dequeue();
                    if(!finished)batches.Enqueue(batch); // Do not starve another already-queued weapon.
                }
                catch(Exception e){if(shot!=null){shot.gameObject.SetActive(false);Object.Destroy(shot.gameObject);}UnityEngine.Debug.LogException(e,this);break;}
            }
        }
        static P17PhysicalShell SpawnRailgun(Batch batch)
        {
            P17PhysicalShell projectile=null;var shot=batch.RailShot;
            try
            {
                projectile=P17PhysicalShell.RentConventional();var shell=projectile.gameObject;shell.name="WPN002 physical rail";
                shell.transform.SetPositionAndRotation(shot.Origin,Quaternion.LookRotation(shot.Direction));shell.transform.localScale=Vector3.one*shot.Spec.Radius*2;
                var collider=shell.GetComponent<Collider>();collider.enabled=true;collider.isTrigger=true;
                var renderer=shell.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=batch.RailMaterial!=null?batch.RailMaterial:projectile.DefaultMaterial;
                var body=shell.GetComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.Discrete;
                if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
                var origin=shell.GetComponent<R4ProjectileOrigin>();origin.Weapon=R4WeaponKind.Railgun;origin.PartId=batch.RailPartId;
                double speed=shot.InitialVelocity.magnitude;
                double lifetime=speed>0&&!double.IsInfinity(speed)&&!double.IsNaN(speed)?Math.Max(30d,(double)shot.Spec.Range/speed+1d):30d;
                shell.SetActive(true);projectile.Configure(shot.OwnerRoot.transform,0,(float)Math.Min(float.MaxValue,lifetime),shot.Direction,null,shot.Spec.Range);
                projectile.ConfigureRailgun(shot,batch.RailDamageScale);return projectile;
            }
            catch{if(projectile!=null){projectile.gameObject.SetActive(false);Object.Destroy(projectile.gameObject);}throw;}
        }
        static P17PhysicalShell Spawn(P17PhysicalShell.ConventionalSnapshot data)
        {
            P17PhysicalShell shell=null;
            try
            {
                shell=P17PhysicalShell.RentConventional();var item=shell.gameObject;item.name=data.Weapon==R4WeaponKind.Shotgun?"WPN001 physical pellet":"P17_PlayerPhysicalShell";
                item.transform.SetPositionAndRotation(data.Position,data.Rotation);item.transform.localScale=data.Scale;
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
