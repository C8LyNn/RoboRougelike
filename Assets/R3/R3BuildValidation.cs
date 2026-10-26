using System.Collections.Generic;
using System.Linq;
using B01.BUILD001;
using RoboRougelike.R1;
using RoboRougelike.R4_1;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Receiver policy only. R3 remains the sole inventory/wallet/HP owner.
    public sealed partial class R3BuildValidation : MonoBehaviour
    {
        R3VehicleAssembly assembly;
        R3VehicleModel model;
        readonly Dictionary<string,BuildDefinition> definitions=new Dictionary<string,BuildDefinition>();
        readonly BuildSocketRegistry registry=new BuildSocketRegistry();
        readonly List<GameObject> socketFaces=new List<GameObject>();
        GameObject sockets;

        public static void Bind(R3VehicleAssembly actual,R3VehicleModel canonical)
        {
            var gate=actual.GetComponent<R3BuildValidation>()??actual.gameObject.AddComponent<R3BuildValidation>();
            gate.assembly=actual;gate.model=canonical;
            canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate):"Brak rzeczywistego pojazdu do walidacji montażu.");
            canonical.SetRestorationGate(parts=>gate!=null&&gate.ValidateRestoration(parts));
        }
        BuildDefinition Definition(R3PartState part)
        {
            string key=part.Kind+"/"+part.ContactMask;
            if(definitions.TryGetValue(key,out var known))return known;
            var role=part.Kind==R3PartKind.Core?BuildRole.Core:part.Kind==R3PartKind.Armor?BuildRole.Armor:
                part.Kind==R3PartKind.Wheel?BuildRole.Wheel:part.Kind==R3PartKind.Gun?BuildRole.Gun:
                part.Kind==R3PartKind.Suspension?BuildRole.Suspension:BuildRole.Device;
            var cells=R3VehicleModel.OccupiedCells(Vector3Int.zero,part.Kind).ToArray();
            var occupied=new HashSet<Vector3Int>(cells);var patches=new List<BuildSocket>();
            foreach(var cell in cells)
            foreach(var face in R3MountingGrid.FaceDirections)
            {
                if(occupied.Contains(cell+face)||(part.ContactMask&R3MountingGrid.FaceBit(face))==0)continue;
                var kind=role==BuildRole.Wheel?BuildSocketKind.Axle:role==BuildRole.Gun?BuildSocketKind.WeaponBase:
                    role==BuildRole.Suspension?BuildSocketKind.SuspensionBase:BuildSocketKind.Structural;
                patches.Add(new BuildSocket(cell,face,kind,new Vector2(.47f,.47f)));
                // The canonical suspension is a structural mounting block as well as a base.
                if(role==BuildRole.Suspension)patches.Add(new BuildSocket(cell,face,BuildSocketKind.Structural,new Vector2(.47f,.47f)));
            }
            var bonds=new List<BuildCellBond>();
            foreach(var a in cells)foreach(var b in cells)
                if((a-b).sqrMagnitude==1&&System.Array.IndexOf(cells,a)<System.Array.IndexOf(cells,b))bonds.Add(new BuildCellBond(a,b));
            known=new BuildDefinition("R3/"+key,role,cells,patches.ToArray(),role!=BuildRole.Wheel&&role!=BuildRole.Gun,bonds.ToArray());
            definitions.Add(key,known);return known;
        }
        List<BuildInstalledPart> Installed()=>model.Parts.Select(p=>{
            var actual=assembly.Parts.FirstOrDefault(a=>a!=null&&a.Id==p.Id);
            bool alive=p.Health>0&&actual!=null&&actual.Operational&&actual.transform.IsChildOf(transform)&&
                actual.GetComponentsInChildren<Collider>().Any(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.attachedRigidbody==GetComponent<Rigidbody>());
            return new BuildInstalledPart(p.Id,Definition(p),new BuildPose(p.Cell,p.RotationQuarterTurns),alive);
        }).ToList();

        public string Validate(R3PartState candidate)
        {
            if(assembly==null||model==null||candidate==null)return "Brak autorytatywnego pojazdu.";
            var run=R1RunController.Instance;
            if(run!=null&&ReferenceEquals(run.VehicleModel,model)&&
                (run.Progress?.State!=R1RunState.Garage||run.Transitioning))return "Montaż jest dostępny tylko w garażu.";
            if((transform.lossyScale-Vector3.one).sqrMagnitude>.000001f)return "Niezgodna fizyczna skala siatki pojazdu.";
            var item=new BuildInstalledPart(candidate.Id,Definition(candidate),new BuildPose(candidate.Cell,candidate.RotationQuarterTurns),candidate.Health>0);
            var installed=Installed();
            var logical=BuildPlacementRules.Check(installed,item,out _);
            if(logical!=BuildPlacementResult.Valid)return Explain(logical);
            GameObject probe=null;
            try
            {
                // Exact existing R3 cube/wide/payload geometry; no controller, HP, body or forces.
                probe=CreateProbe(candidate);
                var candidateColliders=probe.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
                Physics.SyncTransforms();var neighbors=new HashSet<Collider>();
                var factoryGroup=FactoryGroupForReinstallation(candidate);
                // Allocating broadphase returns ALL candidates; no fixed buffer/truncation PASS.
                foreach(var collider in candidateColliders)
                foreach(var other in Physics.OverlapBox(collider.bounds.center,collider.bounds.extents+Vector3.one*.001f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    if(!other.transform.IsChildOf(probe.transform)&&(factoryGroup==null||!factoryGroup.Contains(other)))neighbors.Add(other);
                var result=BuildPlacementRules.CheckForInstallation(installed,item,
                    new BuildPhysicsEvidence(candidateColliders,neighbors.ToArray(),true),out _);
                return result==BuildPlacementResult.Valid?null:Explain(result);
            }
            finally
            {
                if(probe!=null){probe.SetActive(false);if(Application.isPlaying)Destroy(probe);else DestroyImmediate(probe);}
            }
        }
        static string Explain(BuildPlacementResult result)=>result==BuildPlacementResult.ColliderPenetration?
            "Fizyczny obrys części przenika istniejący collider.":result==BuildPlacementResult.NoCompatibleSocket?
            "Brak zgodnego, połączonego z rdzeniem mocowania. Koło i broń nie są podporą konstrukcji.":
            "Montaż odrzucony: "+result;

        public static bool ValidateHit(R3VehicleAssembly actual,RaycastHit hit)
        {
            var gate=actual!=null?actual.GetComponent<R3BuildValidation>():null;
            if(gate==null||gate.sockets==null)return false;
            var part=hit.collider!=null?hit.collider.GetComponentInParent<R3MountedPart>():null;
            var state=part!=null?gate.model.Parts.FirstOrDefault(p=>p.Id==part.Id):null;
            if(state==null||!part.Operational||!part.transform.IsChildOf(actual.transform))return false;
            // Preserve the genuine hit collider/ID; transform only its point and normal
            // into the same canonical grid frame used by source BuildPose geometry.
            var local=hit;local.point=actual.transform.InverseTransformPoint(hit.point);
            local.normal=actual.transform.InverseTransformDirection(hit.normal).normalized;
            return BuildSocketRaycast.ValidateHit(local,part.transform,part.Id,gate.Definition(state),
                new BuildPose(state.Cell,state.RotationQuarterTurns),Vector3.zero,gate.registry,out _)==BuildHitResult.Valid;
        }
        public static void SetGarageSockets(R3VehicleAssembly actual,bool visible)
        {
            var gate=actual!=null?actual.GetComponent<R3BuildValidation>():null;if(gate==null)return;
            if(!visible){gate.registry.Clear();foreach(var face in gate.socketFaces)if(face!=null){face.SetActive(false);Destroy(face);}gate.socketFaces.Clear();if(gate.sockets!=null){gate.sockets.SetActive(false);Destroy(gate.sockets);}gate.sockets=null;return;}
            SetGarageSockets(actual,false);gate.sockets=new GameObject("R3 authorized structural sockets");gate.sockets.transform.SetParent(actual.transform,false);
            foreach(var part in actual.Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(actual.transform)))
            {
                var state=gate.model.Parts.FirstOrDefault(p=>p.Id==part.Id);if(state==null)continue;
                var definition=gate.Definition(state);if(!definition.SupportsOtherParts)continue;
                for(int index=0;index<definition.Sockets.Length;index++)
                {
                    var socket=definition.Sockets[index];if(socket.Kind!=BuildSocketKind.Structural&&socket.Kind!=BuildSocketKind.Axle)continue;
                    var face=new GameObject("Authorized socket "+index);gate.socketFaces.Add(face);face.transform.SetParent(part.transform,false);
                    face.transform.localPosition=((Vector3)socket.Cell+(Vector3)socket.Face*.5f)*R41PartGeometry.Grid/part.transform.localScale.x;
                    face.transform.localRotation=Quaternion.LookRotation((Vector3)socket.Face,socket.Face.y!=0?Vector3.forward:Vector3.up);
                    var box=face.AddComponent<BoxCollider>();box.isTrigger=true;
                    box.size=new Vector3(socket.HalfSize.x*2,socket.HalfSize.y*2,.012f/R41PartGeometry.Grid)*R41PartGeometry.Grid/part.transform.localScale.x;
                    var marker=face.AddComponent<BuildMountSurface>();marker.PartInstanceId=part.Id;marker.SocketIndex=index;
                    if(!gate.registry.Register(box,part.transform,part.Id,definition,index))
                        Debug.LogError("[BUILD receiver] Cannot register actual structural socket "+part.Id+"/"+index);
                }
            }
            Physics.SyncTransforms();
        }
    }
}
