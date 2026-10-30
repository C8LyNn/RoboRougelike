// B01-23 / W24 — NEW owner-owned physical implementation for the selected
// #729 ICanonicalRigFactory. Uses real frozen #510 PUBLIC SecureRigCatalog
// and AtomicDockLedger. NO reflection or accesses to frozen PRIVATE Rig[].
// Replaces the old playground only after separately approved scene migration.
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.DetachedSaveContract001;
using B01.CanonicalWorldApi001;

namespace B01.CanonicalRigPortW24
{
    [DisallowMultipleComponent]
    public sealed class CanonicalPhysicalRigPortW24 : MonoBehaviour,
        ICanonicalMutationGate, ICanonicalRigFactory
    {
        private readonly List<GameObject> allOwnedRoots = new List<GameObject>();
        private int unityThread;
        private bool inputAndPhysicsWritersPaused;
        private bool shuttingDown;
        private PhysicalWorld? visible;

        private void Awake(){unityThread=Thread.CurrentThread.ManagedThreadId;}
        // Must be called by the actual owner after disabling all legacy GUI,
        // reattach/detach, combat, AI, FixedUpdate and physics writer routes.
        // This flag alone does NOT prove that frozen #510's own GUI is stopped.
        public bool SetExternalWritersQuiesced(bool paused)
        {
            if(!MainThread()||shuttingDown)return false;
            inputAndPhysicsWritersPaused=paused;
            return true;
        }
        private bool MainThread()=>Thread.CurrentThread.ManagedThreadId==unityThread;
        public bool IsQuiescent()=>MainThread()&&inputAndPhysicsWritersPaused&&!shuttingDown;
        public bool AllOwnedPhysicalRootsDestroyed()
        {
            if(!MainThread())return false;
            foreach(GameObject root in allOwnedRoots)if(root!=null)return false;
            return true;
        }
        private void Own(GameObject game)
        {
            if(game==null)throw new ArgumentNullException(nameof(game));
            allOwnedRoots.Add(game); // before any Rigidbody or Collider creation
        }
        public Rigidbody? VisiblePartBody(int i)
        {
            if(!MainThread()||i<0||i>=FrozenPartCatalog.Slots)return null;
            return visible?.Body(i);
        }
        public bool VisibleConsumedId(int i,long id)
        {
            if(!MainThread()||i<0||i>=FrozenPartCatalog.Slots)return false;
            return visible?.Consumed(i,id)??false;
        }
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snapshot,ColdRestore plan)
        {
            if(!IsQuiescent()||!SnapshotRules.Valid(snapshot)||plan==null||
               plan.Snapshot.RunId!=snapshot.RunId||
               plan.Snapshot.Generation!=snapshot.Generation)
                throw new InvalidOperationException("Physical owner not quiesced or invalid W06 plan");
            // Returns an owning handle BEFORE ANY Unity GameObject allocation.
            return new OwnedStage(this,snapshot,plan);
        }
        private static Vector3 V(Vector3d v)=>new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        private static Vector3d D(Vector3 v)=>new Vector3d(v.x,v.y,v.z);
        private static Quaternion Q(PersistedPart p)=>
            new Quaternion((float)p.Qx,(float)p.Qy,(float)p.Qz,(float)p.Qw);
        private static void ExactFloatWire(PersistedPart p)
        {
            if((double)(float)p.Position.X!=p.Position.X||
               (double)(float)p.Position.Y!=p.Position.Y||
               (double)(float)p.Position.Z!=p.Position.Z||
               (double)(float)p.LinearVelocity.X!=p.LinearVelocity.X||
               (double)(float)p.LinearVelocity.Y!=p.LinearVelocity.Y||
               (double)(float)p.LinearVelocity.Z!=p.LinearVelocity.Z||
               (double)(float)p.AngularVelocity.X!=p.AngularVelocity.X||
               (double)(float)p.AngularVelocity.Y!=p.AngularVelocity.Y||
               (double)(float)p.AngularVelocity.Z!=p.AngularVelocity.Z||
               (double)(float)p.Qx!=p.Qx||(double)(float)p.Qy!=p.Qy||
               (double)(float)p.Qz!=p.Qz||(double)(float)p.Qw!=p.Qw)
                throw new InvalidOperationException("Unrepresentable Unity float physics state; refuse lossy #556 import");
        }
        private static void DestroyTracked(GameObject? go)
        {
            if(go==null)return;
            if(Application.isPlaying)UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        private sealed class OwnedStage : ICanonicalStageOwnership
        {
            private readonly CanonicalPhysicalRigPortW24 port;
            private readonly RunSnapshot planned;
            private readonly ColdRestore ledger;
            private readonly List<GameObject> partialRoots=new List<GameObject>();
            private PhysicalWorld? world;
            private bool prepared;
            public OwnedStage(CanonicalPhysicalRigPortW24 owner,RunSnapshot snap,ColdRestore plan)
            {port=owner;planned=snap;ledger=plan;}
            private GameObject NewInactive(string name,Transform? parent=null)
            {
                // A bare new GameObject has NO physics. Disable BEFORE attaching
                // Rigidbody / Collider; register immediately for orphan recovery.
                var go=new GameObject(name);
                partialRoots.Add(go);port.Own(go);
                go.SetActive(false);
                if(parent!=null)go.transform.SetParent(parent,false);
                return go;
            }
            public ICanonicalStagedWorld PrepareInactive()
            {
                if(prepared||!port.IsQuiescent())throw new InvalidOperationException("Stage is not single-use");
                prepared=true;
                var rigs=new Rig[FrozenPartCatalog.Slots];
                PersistedPart[] parts=planned.Parts;
                for(int i=0;i<rigs.Length;i++)
                {
                    PersistedPart p=parts[i];
                    SecureRigSpec spec=SecureRigCatalog.At(i);
                    if(p.Id!=spec.PartId||p.MassKg!=spec.PartMassKg||
                       ledger.ContainsConsumedId(i,p.Id)!=p.Consumed||
                       ledger.ConsumedCount(i)!=(p.Consumed?1:0))
                        throw new InvalidOperationException("Original #510 catalog and W06 ID mismatch");
                    ExactFloatWire(p);
                    GameObject root=NewInactive("W24 "+spec.Name+" protected root");
                    root.transform.position=new Vector3((float)spec.RootX,0,0);
                    var chassis=NewInactive(spec.Name+" chassis",root.transform);
                    chassis.transform.localPosition=new Vector3(0,1.2f,0);
                    chassis.transform.localScale=new Vector3((float)spec.ChassisWidth,.8f,1);
                    var chassisBox=chassis.AddComponent<BoxCollider>();
                    chassisBox.isTrigger=false;
                    Rigidbody staticBody=chassis.AddComponent<Rigidbody>();
                    staticBody.isKinematic=true;staticBody.useGravity=false;
                    // Detached part is temporarily under an INACTIVE owned root;
                    // it is unparented only when the world is truly published.
                    GameObject part=NewInactive(spec.Name+" guarded body",root.transform);
                    part.transform.localScale=new Vector3((float)spec.PartWidth,.65f,.65f);
                    part.transform.position=V(p.Position);
                    part.transform.rotation=Q(p);
                    var box=part.AddComponent<BoxCollider>();
                    box.isTrigger=false;
                    Rigidbody body=part.AddComponent<Rigidbody>();
                    body.mass=(float)p.MassKg;body.useGravity=false;
                    body.isKinematic=p.Kinematic;
                    body.linearDamping=0;body.angularDamping=0;
                    if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,i,ledger,
                        out AtomicDockLedger originalLedger))
                        throw new InvalidOperationException("Original #510 ledger and frozen #556/W06 bits reject hydration");
                    rigs[i]=new Rig(root,part,body,originalLedger,p);
                }
                world=new PhysicalWorld(port,planned,rigs);
                return world;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {
                // This method is called even if PrepareInactive throws mid-rig.
                // Unity Destroy is DEFERRED in PlayMode. Never lie to the
                // canonical gateway: false = sticky quarantine + lease escrow.
                if(world!=null)world.Retire();
                else foreach(GameObject go in partialRoots)DestroyTracked(go);
                foreach(GameObject go in partialRoots)if(go!=null)return false;
                return true;
            }
        }
        private sealed class Rig
        {
            internal readonly GameObject Root,Part;
            internal readonly Rigidbody Body;
            internal readonly AtomicDockLedger Ledger;
            internal readonly PersistedPart Saved;
            internal Rig(GameObject root,GameObject part,Rigidbody body,
                AtomicDockLedger ledger,PersistedPart saved)
            {Root=root;Part=part;Body=body;Ledger=ledger;Saved=saved;}
        }
        private sealed class PhysicalWorld : ICanonicalStagedWorld
        {
            private readonly CanonicalPhysicalRigPortW24 owner;
            private readonly RunSnapshot planned;
            private readonly Rig[] rigs;
            private bool published,retired;
            // W25: old world may have evolved after its initial #556 restore.
            // Its LIVE velocities must survive a failed competing stage rollback;
            // never reset them from stale immutable Rig.Saved on Reactivate.
            private bool resumePending;
            private readonly Vector3[] resumeLinear=new Vector3[FrozenPartCatalog.Slots];
            private readonly Vector3[] resumeAngular=new Vector3[FrozenPartCatalog.Slots];
            internal PhysicalWorld(CanonicalPhysicalRigPortW24 port,RunSnapshot snap,Rig[] entries)
            {owner=port;planned=snap;rigs=entries;}
            internal Rigidbody? Body(int i)=>published&&!retired?rigs[i].Body:null;
            internal bool Consumed(int i,long id)=>published&&!retired&&rigs[i].Ledger.Contains(id);
            public RunSnapshot Capture()
            {
                if(!owner.MainThread()||retired)throw new InvalidOperationException("World not physical-owner readable");
                var exported=new PersistedPart[rigs.Length];
                for(int i=0;i<rigs.Length;i++)
                {
                    Rig rig=rigs[i];PersistedPart saved=rig.Saved;
                    bool detached=saved.State==PartDockState.Detached;
                    if(rig.Root==null||rig.Part==null||rig.Body==null||
                       rig.Ledger.Busy||rig.Ledger.Contains(saved.Id)!=detached||
                       rig.Ledger.Count!=(detached?1:0)||
                       rig.Body.mass!=(float)saved.MassKg||
                       (published&&(detached
                         ?rig.Part.transform.parent!=null
                         :rig.Part.transform.parent!=rig.Root.transform))||
                       rig.Body.isKinematic!=saved.Kinematic)
                        throw new InvalidOperationException("Physical parent/ledger/mass fork");
                    Vector3 position=rig.Part.transform.position;
                    Quaternion rotation=rig.Part.transform.rotation;
                    // Before first Activate the inactive Rigidbody is not yet
                    // a physics simulation source; saved immutable vectors
                    // remain the exact pending physical target until publish.
                    Vector3 velocity=published?rig.Body.linearVelocity:V(saved.LinearVelocity);
                    Vector3 angular=published?rig.Body.angularVelocity:V(saved.AngularVelocity);
                    exported[i]=new PersistedPart(saved.Id,saved.OwnerId,
                        detached?0:saved.OwnerId,saved.AssetGuid,saved.SocketGuid,
                        saved.State,detached,rig.Body.isKinematic,D(position),
                        rotation.x,rotation.y,rotation.z,rotation.w,
                        D(velocity),D(angular),rig.Body.mass);
                }
                var snap=new RunSnapshot(planned.RunId,planned.Generation,exported);
                if(!SnapshotRules.Valid(snap))throw new InvalidOperationException("Invalid physical W24 captured source");
                return snap;
            }
            public void Activate()
            {
                if(retired||!owner.IsQuiescent())throw new InvalidOperationException("Cannot activate physical world");
                for(int i=0;i<rigs.Length;i++)
                {
                    Rig r=rigs[i];
                    bool detached=r.Saved.State==PartDockState.Detached;
                    // No transient active detached part before old-world
                    // quiescent swap. Root is still inactive here.
                    if(detached){r.Part.SetActive(false);r.Part.transform.SetParent(null,true);}
                    r.Root.SetActive(true);
                    if(!detached)r.Part.SetActive(true);
                }
                for(int i=0;i<rigs.Length;i++)
                {
                    Rig r=rigs[i];
                    if(r.Saved.State==PartDockState.Detached)r.Part.SetActive(true);
                    if(!r.Body.isKinematic)
                    {
                        r.Body.linearVelocity=resumePending?resumeLinear[i]:V(r.Saved.LinearVelocity);
                        r.Body.angularVelocity=resumePending?resumeAngular[i]:V(r.Saved.AngularVelocity);
                    }
                }
                published=true;resumePending=false;owner.visible=this;
            }
            public void Deactivate()
            {
                if(retired)return;
                if(published)
                {
                    // Save the REAL current physics after any movement/damage.
                    // The selected #729 gateway may call Deactivate on the old
                    // world and later Activate it if new Commit fails.
                    for(int i=0;i<rigs.Length;i++)
                    {
                        Rig r=rigs[i];
                        if(r.Body==null)throw new InvalidOperationException("Old Rigidbody lost before rollback snapshot");
                        resumeLinear[i]=r.Body.linearVelocity;
                        resumeAngular[i]=r.Body.angularVelocity;
                    }
                    resumePending=true;
                }
                foreach(Rig r in rigs)
                {
                    if(r.Part!=null)r.Part.SetActive(false);
                    if(r.Root!=null)r.Root.SetActive(false);
                }
                published=false;
                if(ReferenceEquals(owner.visible,this))owner.visible=null;
            }
            public void Retire()
            {
                if(retired)return;
                Deactivate();retired=true;
                foreach(Rig r in rigs)
                {
                    DestroyTracked(r.Part);DestroyTracked(r.Root);
                }
            }
        }
        private void OnDestroy()
        {
            // Not a lease owner. Let the canonical gateway's strong escrow hold
            // the OS owner if OnDestroy preceded proven physical retirement.
            shuttingDown=true;
            visible=null;
            foreach(GameObject root in allOwnedRoots)DestroyTracked(root);
        }
    }
}
