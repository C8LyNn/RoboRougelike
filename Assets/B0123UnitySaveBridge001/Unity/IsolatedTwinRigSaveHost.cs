using System;
using System.IO;
using UnityEngine;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

namespace B01.UnitySaveBridge001
{
    // Isolated Unity-facing demonstration host. It does NOT patch or wrap private
    // #510 Rig[] / AtomicDockLedger. Never enable it alongside old W07 controllers.
    [DisallowMultipleComponent]
    public sealed class IsolatedTwinRigSaveHost : MonoBehaviour, IWorldFactory
    {
        [SerializeField] private bool buildInitialFixtureOnStart;
        private AtomicWorldSwap coordinator;
        private FileStream slotOwnerLease;
        private string checkpointPath;
        private long runId=1;
        public WorldSwapVerdict LastRestoreVerdict {get;private set;}
        public bool HasWorld => coordinator!=null&&coordinator.HasWorld;
        public bool Quarantined => coordinator!=null&&coordinator.Quarantined;
        public long Generation => coordinator==null?0:coordinator.Generation;
        public bool IsConsumed(int slot,long id) =>
            coordinator!=null&&coordinator.IsConsumed(slot,id);
        private bool CanOperate() => coordinator!=null&&!coordinator.Busy&&PhysicalIdle();
        // The coordinator intentionally holds Busy through both idle checks:
        // checking coordinator.Busy from THIS callback would self-deadlock.
        private bool PhysicalIdle()
        {
            if(!isActiveAndEnabled||slotOwnerLease==null||
               coordinator==null||coordinator.Quarantined||
               transform.position!=Vector3.zero||transform.rotation!=Quaternion.identity||
               transform.lossyScale!=Vector3.one)
                return false;
            // No mixed V2/W07 controllers in the same isolated Unity scene.
            foreach(var component in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(component==null||component==this)continue;
                string type=component.GetType().Name;
                if(type=="SecureDebrisPlaygroundV2"||type=="DebrisRigPlayground")
                    return false;
                var competing=component as IsolatedTwinRigSaveHost;
                if(competing!=null&&competing.isActiveAndEnabled&&competing.HasWorld)
                    return false;
            }
            return true;
        }
        // Long-lived OS slot lease, separate from the short checkpoint CAS lock.
        // Unity OnDisable releases ownership when its physics hierarchy goes inactive.
        private bool AcquireSlot()
        {
            if(slotOwnerLease!=null)return true;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(checkpointPath));
                slotOwnerLease=new FileStream(checkpointPath+".owner",
                    FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                return true;
            }
            catch(IOException){return false;}
            catch(UnauthorizedAccessException){return false;}
        }
        private void ReleaseSlot()
        {
            if(slotOwnerLease==null)return;
            try{slotOwnerLease.Dispose();}
            finally{slotOwnerLease=null;}
        }
        private void Awake()
        {
            checkpointPath=Path.Combine(Application.persistentDataPath,
                "b0123_one_save_host_v2.checkpoint");
            coordinator=new AtomicWorldSwap(this,new FileCheckpointStore(checkpointPath));
            AcquireSlot();
        }
        // A disabled GameObject still owns its old physical/ledger world.
        // Never hand out that same slot to another host until actual destruction.
        private void OnEnable()
        {
            if(slotOwnerLease==null&&!HasWorld&&
               !string.IsNullOrEmpty(checkpointPath))AcquireSlot();
        }
        private void OnDisable(){/* owner still owns its original live physics world */}
        // Component-only Destroy is NOT destruction of owned child GameObjects.
        // Transfer the OS lease to an independent custodian, quarantine the
        // physics roots immediately and release ownership only AFTER Unity
        // physically destroys all previously owned roots at end of frame.
        private void OnDestroy()
        {
            FileStream lease=slotOwnerLease;
            slotOwnerLease=null;
            if(lease==null)return;
            var owned=new System.Collections.Generic.List<GameObject>();
            try
            {
                // This scene contains ONLY roots created by THIS host's TwinWorld.
                // Never recurse into unrelated receiver objects or other scripts.
                foreach(Transform child in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    if(child.parent==transform &&
                       child.gameObject.name=="B01-23 W15 ISOLATED COLD SAVE WORLD")
                        owned.Add(child.gameObject);
                }
                // All physically owned Rigidbody roots must stop simulation BEFORE
                // an external owner can attempt to reacquire the checkpoint slot.
                foreach(GameObject world in owned)world.SetActive(false);
                foreach(GameObject world in owned)
                {
                    if(Application.isPlaying)UnityEngine.Object.Destroy(world);
                    else UnityEngine.Object.DestroyImmediate(world);
                }
                HostWorldLeaseRetirement.HoldUntilAllRootsGone(lease,owned.ToArray());
            }
            catch
            {
                // A failed quiesce/retire MUST NEVER release the same-slot lease.
                // Keep a static process-local owner quarantine until process exit.
                HostWorldLeaseRetirement.FailClosed(lease);
            }
        }

        // Explicit isolated durable slot selection: unlike historical #596,
        // a new run NEVER silently reuses another run's global checkpoint path.
        // Same run + same non-empty slot GUID is stable across process restarts.
        // Call ONLY before first physical Load; no arbitrary filesystem path input.
        public bool TrySelectPersistentRunSlot(long desiredRunId,Guid slotId)
        {
            if(desiredRunId<=0||slotId==Guid.Empty||coordinator==null||
               coordinator.HasWorld||coordinator.Busy||coordinator.Quarantined)
                return false;
            ReleaseSlot();
            checkpointPath=CheckpointSlotPath.ForRun(
                Application.persistentDataPath,desiredRunId,slotId);
            coordinator=new AtomicWorldSwap(this,new FileCheckpointStore(checkpointPath));
            runId=desiredRunId;
            return AcquireSlot();
        }
        // Test-only helper retained for external QA of the same unique slot API.
        public bool TryUseIsolatedNativeTestSlot(Guid testId)
            =>TrySelectPersistentRunSlot(runId,testId);
        private void Start()
        {
            if(buildInitialFixtureOnStart)
                TryRestoreBytes(MakeFixture(runId,1,false,false),runId,1);
        }
        public bool TrySaveBytes(out byte[] bytes)
        {
            bytes=null;
            return CanOperate()&&coordinator.TrySave(runId,PhysicalIdle,out bytes);
        }
        public bool TryRestoreBytes(byte[] bytes,long expectedRun,long minGeneration)
        {
            // Explicit per-run admission: prevent silently restoring an unrelated
            // run into the legacy global default checkpoint file.
            if(expectedRun!=runId || expectedRun<=0)
            {
                LastRestoreVerdict=WorldSwapVerdict.WrongRun;
                return false;
            }
            LastRestoreVerdict=CanOperate()
                ? coordinator.TryLoad(bytes,expectedRun,minGeneration,PhysicalIdle)
                : WorldSwapVerdict.HostBusy;
            if(LastRestoreVerdict==WorldSwapVerdict.Accepted)runId=expectedRun;
            if(coordinator!=null&&coordinator.Quarantined)
                LastRestoreVerdict=WorldSwapVerdict.RollbackQuarantine;
            return LastRestoreVerdict==WorldSwapVerdict.Accepted;
        }
        // Fixture creates original selected #556 bytes, never another schema.
        public static byte[] MakeFixture(long run,long generation,bool lightDetached,
            bool heavyDetached)
        {
            PersistedPart[] parts=new PersistedPart[2];
            for(int i=0;i<2;i++)
            {
                bool detach=i==0?lightDetached:heavyDetached;
                SecureRigSpec spec=SecureRigCatalog.At(i);
                var pos=new Vector3d(spec.RootX+spec.PartOffsetX,
                    1.2+(detach?1:0),0);
                Vector3d velocity=detach?new Vector3d(.25,0,0):new Vector3d(0,0,0);
                parts[i]=new PersistedPart(spec.PartId,FrozenPartCatalog.Owner(i),
                    detach?0:FrozenPartCatalog.Owner(i),
                    FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                    detach?PartDockState.Detached:PartDockState.Attached,
                    detach,!detach,pos,0,0,0,1,velocity,new Vector3d(0,0,0),
                    spec.PartMassKg);
            }
            return SnapshotCodec.Encode(new RunSnapshot(run,generation,parts));
        }

        IPreparedWorld IWorldFactory.Prepare(RunSnapshot snapshot)
            => TwinWorld.Build(snapshot,transform);

        private sealed class TwinWorld : IPreparedWorld
        {
            private GameObject container;
            private readonly GameObject[] owners=new GameObject[2];
            private readonly GameObject[] parts=new GameObject[2];
            private readonly Rigidbody[] bodies=new Rigidbody[2];
            private readonly DebrisImpulseLedger[] ledgers=new DebrisImpulseLedger[2];
            private readonly RunSnapshot record;
            private bool retired;
            private TwinWorld(RunSnapshot snapshot){record=snapshot;}
            private static Vector3 V(Vector3d x) =>
                new Vector3((float)x.X,(float)x.Y,(float)x.Z);
            private static Vector3d D(Vector3 x)=>new Vector3d(x.x,x.y,x.z);

            public static TwinWorld Build(RunSnapshot snapshot,Transform ownerHost)
            {
                if(!SnapshotRules.Valid(snapshot))
                    throw new InvalidOperationException("Invalid original Save contract");
                var world=new TwinWorld(snapshot);
                try
                {
                    world.container=new GameObject("B01-23 W15 ISOLATED COLD SAVE WORLD");
                    world.container.SetActive(false); // no physics while staging
                    world.container.transform.SetParent(ownerHost,false); // owned by host lifecycle
                    for(int i=0;i<2;i++)
                    {
                        var p=snapshot.Parts[i];
                        SecureRigSpec spec=SecureRigCatalog.At(i);
                        if(p.Id!=spec.PartId||p.MassKg!=spec.PartMassKg)
                            throw new InvalidOperationException("Foreign frozen #510 identity");
                        GameObject owner=new GameObject("owner "+p.OwnerId);
                        owner.transform.SetParent(world.container.transform,false);
                        owner.transform.position=new Vector3((float)spec.RootX,0,0);
                        world.owners[i]=owner;
                        GameObject part=GameObject.CreatePrimitive(PrimitiveType.Cube);
                        part.name="original stable module "+p.Id;
                        part.transform.SetParent(p.State==PartDockState.Detached
                            ?world.container.transform:owner.transform,false);
                        part.transform.position=V(p.Position);
                        part.transform.rotation=new Quaternion((float)p.Qx,(float)p.Qy,
                            (float)p.Qz,(float)p.Qw);
                        part.transform.localScale=new Vector3((float)spec.PartWidth,.65f,.65f);
                        part.SetActive(false); // staged body never becomes live before publication
                        Rigidbody body=part.AddComponent<Rigidbody>();
                        body.mass=(float)p.MassKg;
                        body.isKinematic=p.Kinematic;
                        body.useGravity=false;
                        body.constraints=RigidbodyConstraints.FreezeRotation;
                        // Initial inactive-state consistency check needs saved velocities.
                        if(!body.isKinematic)
                        {
                            body.linearVelocity=V(p.LinearVelocity);
                            body.angularVelocity=V(p.AngularVelocity);
                        }
                        world.parts[i]=part;world.bodies[i]=body;
                        var ledger=new DebrisImpulseLedger();
                        if(p.Consumed)
                        {
                            Vector3d zero=new Vector3d(0,0,0);
                            if(!ledger.TryDetach(p.Id,zero,zero,zero,p.MassKg,0,0,
                                out Vector3d impulse)||impulse.MagnitudeSquared!=0)
                                throw new InvalidOperationException("W06 ledger stage failed");
                        }
                        if(ledger.Contains(p.Id)!=p.Consumed)
                            throw new InvalidOperationException("W06 ledger mismatch");
                        world.ledgers[i]=ledger;
                        // Re-enable locally while PARENT STAYS INACTIVE.
                        // Otherwise SetActive(true) on the world root would leave
                        // Rigidbody/Collider permanently inactive (activeSelf=false).
                        part.SetActive(true);
                        if(part.activeInHierarchy)
                            throw new InvalidOperationException("staged physics became live before commit");
                    }
                    return world;
                }
                catch
                {
                    world.Retire();
                    throw;
                }
            }
            public void Activate()
            {
                if(retired||container==null)throw new InvalidOperationException("Retired");
                container.SetActive(true);
                for(int i=0;i<2;i++)
                {
                    var p=record.Parts[i];
                    bodies[i].isKinematic=p.Kinematic;
                    bodies[i].linearVelocity=V(p.LinearVelocity);
                    bodies[i].angularVelocity=V(p.AngularVelocity);
                }
            }
            public void Deactivate()
            {
                if(container!=null)container.SetActive(false);
            }
            public RunSnapshot Capture()
            {
                if(retired)throw new InvalidOperationException("Retired");
                var observed=new PersistedPart[2];
                for(int i=0;i<2;i++)
                {
                    PersistedPart p=record.Parts[i];
                    Transform actual=parts[i].transform;
                    Rigidbody body=bodies[i];
                    bool consumed=ledgers[i].Contains(p.Id);
                    bool detached=actual.parent==container.transform;
                    if(consumed!=detached||body.isKinematic==detached)
                        throw new InvalidOperationException("Physical and W06 ledger split");
                    var rot=actual.rotation;
                    observed[i]=new PersistedPart(p.Id,p.OwnerId,
                        detached?0:p.OwnerId,p.AssetGuid,p.SocketGuid,
                        detached?PartDockState.Detached:PartDockState.Attached,
                        consumed,body.isKinematic,D(actual.position),
                        rot.x,rot.y,rot.z,rot.w,D(body.linearVelocity),
                        D(body.angularVelocity),body.mass);
                }
                return new RunSnapshot(record.RunId,record.Generation,observed);
            }
            public void Retire()
            {
                retired=true;
                if(container==null)return;
                container.SetActive(false);
                if(Application.isPlaying)UnityEngine.Object.Destroy(container);
                else UnityEngine.Object.DestroyImmediate(container);
                container=null;
            }
        }
    }
}

namespace B01.UnitySaveBridge001
{
    // This custodian is NOT part of the canonical #510 game. It survives
    // Destroy(IsolatedTwinRigSaveHost) as a separate transient GameObject,
    // owns the physical OS handle, and never frees it while owned roots remain.
    public sealed class HostWorldLeaseRetirement : UnityEngine.MonoBehaviour
    {
        private System.IO.FileStream lease;
        private UnityEngine.GameObject[] roots;
        private static readonly System.Collections.Generic.List<System.IO.FileStream>
            quarantinedLeases=new System.Collections.Generic.List<System.IO.FileStream>();
        public static void FailClosed(System.IO.FileStream handle)
        {
            if(handle!=null)quarantinedLeases.Add(handle);
        }
        public static void HoldUntilAllRootsGone(System.IO.FileStream handle,
            UnityEngine.GameObject[] ownedRoots)
        {
            if(handle==null)return;
            if(ownedRoots==null||ownedRoots.Length==0)
            {
                handle.Dispose();
                return;
            }
            try
            {
                UnityEngine.GameObject sentinel=new UnityEngine.GameObject(
                    "B01-23 W21 HOST WORLD LEASE ESCROW");
                // The host root may be destroyed this very frame. Standalone
                // custodian is intentionally outside it and survives scene unload.
                UnityEngine.Object.DontDestroyOnLoad(sentinel);
                var guard=sentinel.AddComponent<HostWorldLeaseRetirement>();
                guard.lease=handle;
                guard.roots=ownedRoots;
            }
            catch
            {
                FailClosed(handle);
                throw;
            }
        }
        private void LateUpdate()
        {
            if(lease==null)return;
            foreach(var root in roots)
                if(root!=null)return; // UnityEngine.Object fake-null AFTER Destroy
            System.IO.FileStream done=lease;
            lease=null;
            try{done.Dispose();}
            catch{FailClosed(done);}
            UnityEngine.Object.Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if(lease==null)return;
            // If the custodian itself disappears before the physics hierarchy,
            // preserve the OS exclusion instead of creating duplicate authority.
            foreach(var root in roots)
                if(root!=null){FailClosed(lease);lease=null;return;}
            try{lease.Dispose();}
            catch{FailClosed(lease);}
            finally{lease=null;}
        }
    }
}
