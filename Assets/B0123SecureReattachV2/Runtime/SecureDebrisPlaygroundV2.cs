using System;
using System.Collections.Generic;
using UnityEngine;
using B01.DebrisRepulse001;

namespace B01.SecureReattachV2
{
    /// <summary>
    /// V2 SAFE REPLACEMENT for the W07 demo, not an add-on to unsafe W07.
    /// Every exposed dock request, including legacy-named Reattach, is guarded.
    /// No public method can invoke unguarded CommitDock.
    /// Does not mutate canonical Codex robot/garage/camera/parts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SecureDebrisPlaygroundV2 : MonoBehaviour
    {
        private sealed class Rig
        {
            public SecureRigSpec Spec;
            public GameObject Root;
            public GameObject Part;
            public Rigidbody Body;
            public BoxCollider Collider;
            public DebrisRepulsionEmitter Emitter;
            public Vector3 LocalMount;
            public bool Detached;
        }
        private readonly Rig[] rigs = new Rig[2];
        private readonly Collider[] candidates = new Collider[SecureDockPolicy.Capacity];
        private readonly List<DockHit> contacts = new List<DockHit>(SecureDockPolicy.Capacity);
        private GuardedDockCommand gate;
        private bool built;
        [SerializeField, Range(0.1f, 50f)] private float maximumReach = 3f;
        [SerializeField, Range(0f, .05f)] private float allowedPenetration = .005f;
        public DockResult LastDockResult => gate == null ? DockResult.InvalidInput : gate.LastResult;

        private void Awake()
        {
            gate = new GuardedDockCommand(InspectDock, CommitDock);
        }
        private void Start()
        {
            if (!built) BuildDemo();
        }
        public void BuildDemo()
        {
            if (built) ResetForNewRun();
            for (int i=0;i<rigs.Length;i++)
            {
                SecureRigSpec s=SecureRigCatalog.At(i);
                GameObject root=new GameObject(s.Name+" / protected root");
                root.transform.SetParent(transform,false);
                root.transform.position=new Vector3((float)s.RootX,0,0);
                GameObject chassis=GameObject.CreatePrimitive(PrimitiveType.Cube);
                chassis.name=s.Name+" chassis";
                chassis.transform.SetParent(root.transform,false);
                chassis.transform.localPosition=new Vector3(0,1.2f,0);
                chassis.transform.localScale=new Vector3((float)s.ChassisWidth,.8f,1);
                Rigidbody chassisBody=chassis.AddComponent<Rigidbody>();
                chassisBody.isKinematic=true; chassisBody.useGravity=false;

                GameObject part=GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name=s.Name+" guarded detachable body";
                part.transform.SetParent(root.transform,false);
                Vector3 mount=new Vector3((float)s.PartOffsetX,1.2f,0);
                part.transform.localPosition=mount;
                part.transform.localScale=new Vector3((float)s.PartWidth,.65f,.65f);
                Rigidbody body=part.AddComponent<Rigidbody>();
                body.mass=(float)s.PartMassKg;
                body.isKinematic=true; body.useGravity=false;
                body.linearDamping=0; body.angularDamping=0;
                body.constraints=RigidbodyConstraints.FreezeRotation;
                DebrisRepulsionEmitter emitter=root.AddComponent<DebrisRepulsionEmitter>();
                rigs[i]=new Rig { Spec=s,Root=root,Part=part,Body=body,
                    Collider=part.GetComponent<BoxCollider>(),Emitter=emitter,
                    LocalMount=mount,Detached=false };
            }
            built=true;
        }

        public Rigidbody PartBody(int index) => RequireRig(index).Body;
        public bool IsDetached(int index) => RequireRig(index).Detached;
        public long PartId(int index) => RequireRig(index).Spec.PartId;
        public int AppliedCount(int index) => RequireRig(index).Emitter.AppliedCount;

        public bool Detach(int index)
        {
            Rig rig=RequireRig(index);
            if(rig.Detached) return false;
            rig.Part.transform.SetParent(null,true);
            rig.Body.isKinematic=false;
            rig.Body.linearVelocity=new Vector3((float)rig.Spec.InitialSpeed,0,0);
            Vector3 ownerCentre=rig.Root.transform.position+new Vector3(0,1.2f,0);
            if(!rig.Emitter.TryApplyDetachImpulse(rig.Spec.PartId,rig.Body,ownerCentre))
            {
                rig.Body.linearVelocity=Vector3.zero;
                rig.Body.isKinematic=true;
                rig.Part.transform.SetParent(rig.Root.transform,false);
                rig.Part.transform.localPosition=rig.LocalMount;
                return false;
            }
            rig.Detached=true;
            return true;
        }

        // ALL PUBLIC docking aliases and the GUI route through one guarded object.
        public bool Reattach(int index) => DockGate().Reattach(index);
        public bool TrySafeReattach(int index) => DockGate().TrySafeReattach(index);
        public bool ReattachFromGui(int index) => DockGate().ReattachFromGui(index);

        private GuardedDockCommand DockGate()
        {
            if(gate == null) gate = new GuardedDockCommand(InspectDock,CommitDock);
            return gate;
        }

        private DockResult InspectDock(int index)
        {
            if(!built || index<0 || index>=rigs.Length || rigs[index]==null)
                return DockResult.InvalidInput;
            Rig rig=rigs[index];
            if(!rig.Detached) return DockResult.NotDetached;
            Rigidbody body=rig.Body;
            BoxCollider own=rig.Collider;
            if(body==null || body.isKinematic || own==null || !own.enabled)
                return DockResult.InvalidInput;
            // The replacement fixture is deliberately axis aligned, on a unit-scale root.
            if(rig.Root==null || rig.Part.transform.parent!=null ||
                rig.Root.transform.lossyScale!=Vector3.one ||
                rig.Root.transform.rotation!=Quaternion.identity ||
                rig.Part.transform.rotation!=Quaternion.identity)
                return DockResult.InvalidInput;
            Vector3 worldScale=rig.Part.transform.lossyScale;
            if(!Finite(worldScale.x)||!Finite(worldScale.y)||!Finite(worldScale.z) ||
                worldScale.x<=0||worldScale.y<=0||worldScale.z<=0)
                return DockResult.InvalidInput;
            Vector3 target=rig.Root.transform.TransformPoint(rig.LocalMount);
            Vector3 half=Vector3.Scale(own.size,worldScale)*.5f;
            Vector3 centre=target+Vector3.Scale(own.center,worldScale);
            if(!Finite(half.x)||!Finite(half.y)||!Finite(half.z)||
                half.x<=0||half.y<=0||half.z<=0)
                return DockResult.InvalidInput;
            // All layers; no editable filter; truncated queries cannot authorize docking.
            Physics.SyncTransforms();
            int count=Physics.OverlapBoxNonAlloc(centre,half,candidates,Quaternion.identity,
                SecureDockPolicy.AllLayers,QueryTriggerInteraction.Ignore);
            contacts.Clear();
            for(int i=0;i<count;i++)
            {
                Collider other=candidates[i];
                if(other==null)
                {
                    contacts.Add(new DockHit(false,false,false,double.NaN));
                    continue;
                }
                bool ownCollider=other==own || other.attachedRigidbody==body;
                bool supported=other is BoxCollider || other is SphereCollider ||
                    other is CapsuleCollider || (other is MeshCollider mesh && mesh.convex);
                double depth=0;
                if(!ownCollider && !other.isTrigger && supported)
                {
                    if(Physics.ComputePenetration(own,target,Quaternion.identity,
                        other,other.transform.position,other.transform.rotation,
                        out Vector3 _,out float penetration))
                        depth=penetration;
                }
                contacts.Add(new DockHit(ownCollider,other.isTrigger,supported,depth));
            }
            return SecureDockPolicy.Check(rig.Detached,!body.isKinematic,
                Vector3.Distance(rig.Part.transform.position,target),maximumReach,
                SecureDockPolicy.AllLayers,count,SecureDockPolicy.Capacity,
                allowedPenetration,contacts);
        }

        // The *only* path that mutates body state/parent/ID; it is private.
        private bool CommitDock(int index)
        {
            if(!built || index<0||index>=rigs.Length || rigs[index]==null ||
                !rigs[index].Detached) return false;
            Rig rig=rigs[index];
            rig.Body.linearVelocity=Vector3.zero;
            rig.Body.angularVelocity=Vector3.zero;
            rig.Body.isKinematic=true;
            rig.Part.transform.SetParent(rig.Root.transform,false);
            rig.Part.transform.localPosition=rig.LocalMount;
            rig.Detached=false;
            if(!rig.Emitter.RearmAfterReattach(rig.Spec.PartId))
                throw new InvalidOperationException("Detached ID missing at protected reattach");
            return true;
        }

        public void ResetForNewRun()
        {
            for(int i=0;i<rigs.Length;i++)
            {
                Rig rig=rigs[i];
                if(rig==null)continue;
                if(rig.Part!=null && rig.Part.transform.parent==null)DestroyOwned(rig.Part);
                if(rig.Root!=null)DestroyOwned(rig.Root);
                rigs[i]=null;
            }
            built=false;
        }
        private void OnDestroy()=>ResetForNewRun();
        private Rig RequireRig(int index)
        {
            if(!built || index<0||index>=rigs.Length || rigs[index]==null)
                throw new InvalidOperationException("BuildDemo before using a rig");
            return rigs[index];
        }
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static void DestroyOwned(GameObject go)
        {
            if(Application.isPlaying)Destroy(go);
            else DestroyImmediate(go);
        }
        private void OnGUI()
        {
            if(!built)return;
            GUILayout.BeginArea(new Rect(15,15,425,230),GUI.skin.box);
            GUILayout.Label("B01-23 / SAFE REATTACH V2: NO LEGACY DOCK API");
            for(int i=0;i<2;i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(rigs[i].Spec.Name);
                if(GUILayout.Button("Detach"))Detach(i);
                if(GUILayout.Button("Reattach (GUARDED)"))ReattachFromGui(i);
                GUILayout.EndHorizontal();
                GUILayout.Label("Detached="+rigs[i].Detached);
            }
            if(GUILayout.Button("Reset new run"))
            {
                ResetForNewRun();
                BuildDemo();
            }
            GUILayout.Label("Guard result: "+LastDockResult);
            GUILayout.EndArea();
        }
    }
}
