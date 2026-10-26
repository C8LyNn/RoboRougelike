using System.Collections.Generic;
using System.Linq;
using B01.BUILD001;
using RoboRougelike.R1;
using RoboRougelike.R4_1;
using UnityEngine;
namespace RoboRougelike.R3
{
    public sealed partial class R3BuildValidation
    {
        // Restoration keeps canonical reserved cells/connectivity and payment.
        // This gate only certifies newly recreated geometry, including batches.
        bool ValidateRestoration(IReadOnlyList<R3PartState> requested)
        {
            var dead=requested.Where(p=>p.Health<=0).ToArray();if(dead.Length==0)return true;
            var run=R1RunController.Instance;
            if(run!=null&&ReferenceEquals(run.VehicleModel,model)&&(run.Progress?.State!=R1RunState.Garage||run.Transitioning))return false;
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f)return false;
            var probes=new List<GameObject>();
            try{
                // RepairAffordable mutates canonical health before the single
                // final actor rebuild. Include earlier restored geometry in
                // this transaction even while its live collider is absent.
                var liveIds=new HashSet<string>(assembly.Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(transform)).Select(p=>p.Id));
                var prospective=dead.Concat(model.Parts.Where(p=>p.Health>0&&!liveIds.Contains(p.Id))).GroupBy(p=>p.Id).Select(g=>g.First()).ToArray();
                var replacedIds=new HashSet<string>(prospective.Select(p=>p.Id));
                bool FactoryWheel(R3PartState p)=>model.IsFactoryWheel(p);
                // The shipped factory wheel rows have deliberately intersecting
                // treads on one compound body. Restore that approved group as a
                // compound shape; NEVER apply this exemption to new placement,
                // moved wheels, chassis, foreign bodies or external obstacles.
                var factoryGroup=new HashSet<Collider>();
                foreach(var part in assembly.Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(transform))){
                    var state=model.Parts.FirstOrDefault(p=>p.Id==part.Id);
                    if(state!=null&&FactoryWheel(state))foreach(var c in part.GetComponentsInChildren<Collider>())
                        if(c.enabled&&!c.isTrigger&&c.attachedRigidbody==GetComponent<Rigidbody>())factoryGroup.Add(c);
                }
                foreach(var part in prospective)probes.Add(CreateProbe(part));
                for(int i=0;i<probes.Count;i++)if(FactoryWheel(prospective[i]))foreach(var c in probes[i].GetComponentsInChildren<Collider>())factoryGroup.Add(c);
                Physics.SyncTransforms();
                for(int index=0;index<probes.Count;index++){
                    var probe=probes[index];bool factoryCandidate=FactoryWheel(prospective[index]);
                    var colliders=probe.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
                    var neighbors=new HashSet<Collider>();
                    foreach(var collider in colliders)foreach(var other in Physics.OverlapBox(collider.bounds.center,collider.bounds.extents+Vector3.one*.001f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                        if(!other.transform.IsChildOf(probe.transform)){
                            if(factoryCandidate&&factoryGroup.Contains(other))continue;
                            var old=other.GetComponentInParent<R3MountedPart>();
                            if(old==null||!old.transform.IsChildOf(transform)||old.GetComponentInParent<Rigidbody>()!=GetComponent<Rigidbody>()||!replacedIds.Contains(old.Id))neighbors.Add(other);
                        }
                    var volume=BuildVolumeValidator.Check(new BuildPhysicsEvidence(colliders,neighbors.ToArray(),true));
                    if(volume!=BuildPlacementResult.Valid){TraceRestorationFailure(prospective[index],volume,colliders,neighbors);return false;}
                }
                return true;
            }finally{foreach(var probe in probes)if(probe!=null){probe.SetActive(false);if(Application.isPlaying)Destroy(probe);else DestroyImmediate(probe);}}
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR"),System.Diagnostics.Conditional("B01_UNIFIED_RC2_ACCEPTANCE")]
        void TraceRestorationFailure(R3PartState part,BuildPlacementResult result,Collider[] candidates,HashSet<Collider> neighbors)
        {
            Debug.Log($"[BUILD restoration rejected] {part.Kind}/{part.Id} cell={part.Cell} result={result} root={transform.position} rotation={transform.eulerAngles}");
            foreach(var candidate in candidates)foreach(var other in neighbors){
                bool overlap=Physics.ComputePenetration(candidate,candidate.transform.position,candidate.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth);
                if(!overlap&&!(other is MeshCollider mesh&&!mesh.convex))continue;
                var mounted=other.GetComponentInParent<R3MountedPart>();
                Debug.Log($"[BUILD restoration pair] candidate={candidate.name} at={candidate.transform.position} other={other.name} type={other.GetType().Name} at={other.transform.position} depth={depth:R} oldID={mounted?.Id} sameActor={other.transform.IsChildOf(transform)} sameBody={other.attachedRigidbody==GetComponent<Rigidbody>()}");
            }
        }
        GameObject CreateProbe(R3PartState candidate)
        {
            var probe=GameObject.CreatePrimitive(PrimitiveType.Cube);probe.name="R3 temporary installation geometry";
            probe.transform.SetParent(transform,false);probe.transform.localPosition=(Vector3)candidate.Cell*R41PartGeometry.Grid;
            probe.transform.localRotation=R3MountingGrid.Rotation(candidate.RotationQuarterTurns);probe.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            if(R3VehicleModel.UsesWideFootprint(candidate.Kind))R3BonusModuleGeometry.ConfigureCube(probe,true);
            else {
                var payload=R41PartGeometry.Payload(probe.transform,candidate.Kind,candidate.Weapon,null,true,out _);
                var actual=assembly.Parts.FirstOrDefault(p=>p!=null&&p.Id==candidate.Id&&p.Operational&&p.transform.IsChildOf(transform));
                if(payload!=null&&candidate.Kind==R3PartKind.Gun&&actual?.GunPivot!=null)payload.localRotation=actual.GunPivot.localRotation;
            }
            foreach(var renderer in probe.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            return probe;
        }
        HashSet<Collider> FactoryGroupForReinstallation(R3PartState candidate)
        {
            if(!model.IsFactoryWheel(candidate)||!model.Inventory.Any(p=>p.Id==candidate.Id))return null;
            var result=new HashSet<Collider>();
            foreach(var part in assembly.Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(transform))){
                var state=model.Parts.FirstOrDefault(p=>p.Id==part.Id);
                if(state==null||!model.IsFactoryWheel(state))continue;
                foreach(var c in part.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.attachedRigidbody==GetComponent<Rigidbody>())result.Add(c);
            }
            return result;
        }
    }
}
