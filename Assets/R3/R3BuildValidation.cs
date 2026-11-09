using System.Collections.Generic;
using System.Linq;
using B01.BUILD001;
using RoboRougelike.R1;
using RoboRougelike.R4_1;
using RoboRougelike.R4;
using RoboRougelike.Weapons.RotarySaw;
using UnityEngine;

namespace RoboRougelike.R3
{
    public readonly struct R3VisibleMountPoint
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Collider Surface;
        public R3VisibleMountPoint(Vector3 position,Quaternion rotation,Collider surface)
        {Position=position;Rotation=rotation;Surface=surface;}
    }
    // Receiver policy only. R3 remains the sole inventory/wallet/HP owner.
    public sealed partial class R3BuildValidation : MonoBehaviour
    {
        // The visible patch and its registered trigger are the same size.
        public const float GarageSocketSize=.18f;
        R3VehicleAssembly assembly;
        R3VehicleModel model;
        readonly Dictionary<string,BuildDefinition> definitions=new Dictionary<string,BuildDefinition>();
        readonly BuildSocketRegistry registry=new BuildSocketRegistry();
        readonly List<GameObject> socketFaces=new List<GameObject>();
        // No per-frame PhysX query arrays; full buffer means blocked, never permitted.
        const int FunctionalQueryCapacity=256;
        readonly Collider[] functionalOverlap=new Collider[FunctionalQueryCapacity];
        readonly RaycastHit[] functionalCast=new RaycastHit[FunctionalQueryCapacity];
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
            string key=part.Kind+"/"+part.ContactMask+"/"+part.ArmorShape+"/"+part.WheelStyle+"/"+part.Weapon+"/"+part.FootprintRevision;
            if(definitions.TryGetValue(key,out var known))return known;
            if(part.Kind==R3PartKind.Armor&&part.ArmorShape!=R3ArmorShape.Cube)
            {
                // Source full patches are the current conservative policy for
                // both construction and module support. Never silently replace
                // an unknown shape with a full-faced cube.
                if(!R4MountDefinitionAdapter.TryCreate(R41PartGeometry.ArmorCatalogId(part.ArmorShape),true,out known))
                    known=new BuildDefinition("invalid/"+key,BuildRole.Armor,System.Array.Empty<Vector3Int>(),
                        System.Array.Empty<BuildSocket>(),false);
                definitions.Add(key,known);return known;
            }
            var role=part.Kind==R3PartKind.Core?BuildRole.Core:part.Kind==R3PartKind.Armor?BuildRole.Armor:
                part.Kind==R3PartKind.Wheel?BuildRole.Wheel:part.Kind==R3PartKind.Gun?BuildRole.Gun:
                part.Kind==R3PartKind.Suspension?BuildRole.Suspension:BuildRole.Device;
            var local=part.Copy();local.Cell=Vector3Int.zero;local.RotationQuarterTurns=Vector3Int.zero;
            var cells=R3VehicleModel.OccupiedCells(local).ToArray();
            var occupied=new HashSet<Vector3Int>(cells);var patches=new List<BuildSocket>();
            foreach(var cell in cells)
            foreach(var face in R3MountingGrid.FaceDirections)
            {
                if(occupied.Contains(cell+face)||(part.FootprintRevision!=0&&cell!=Vector3Int.zero)||
                    (part.ContactMask&R3MountingGrid.FaceBit(face))==0)continue;
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
            // A rotation replaces the same physical part; its old pose must
            // not collide with the proposed pose or count as its own support.
            var installed=Installed().Where(p=>p.PartId!=candidate.Id).ToList();
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
                    if(!other.transform.IsChildOf(probe.transform)&&(factoryGroup==null||!factoryGroup.Contains(other))&&
                        other.GetComponentInParent<R3MountedPart>()?.Id!=candidate.Id)neighbors.Add(other);
                var result=BuildPlacementRules.CheckForInstallation(installed,item,
                    new BuildPhysicsEvidence(candidateColliders,neighbors.ToArray(),true),out _);
                if(result!=BuildPlacementResult.Valid)return Explain(result);
                // Same actual gameplay installation gate is called by preview AND Buy/Mount.
                // Do not authorize a near-barrel or rotating saw envelope that enters own armor.
                // Selected B01-23 #887: reject the reverse case before any payment:
                // new armor must not disable an already mounted functional gun.
                var mountedIssue=ExistingMountedWeaponPlacementIssue(probe,candidateColliders,new HashSet<string>{candidate.Id});
                if(mountedIssue!=null)return mountedIssue;
                return FunctionalWeaponPlacementIssue(candidate,probe);
            }
            finally
            {
                if(probe!=null){probe.SetActive(false);if(Application.isPlaying)Destroy(probe);else DestroyImmediate(probe);}
            }
        }

        // An actual physical near-weapon exclusion, NOT the long-range purple hint.
        // No second writer, no new placement authority: both ghost and the atomic
        // canonical model purchase call Validate() through its existing installationGate.
        string FunctionalWeaponPlacementIssue(R3PartState candidate,GameObject probe)
        {
            if(candidate.Kind!=R3PartKind.Gun)return null;
            // EMP uses area occlusion/teams, not a projectile or rotating disc.
            if(candidate.Weapon==R4WeaponKind.EmpEmitter)return null;
            var pivot=probe.transform.Find("Obrót broni");
            if(pivot==null)return "Nie można określić fizycznej osi lufy.";
            // Existing R41 physical probe hierarchy, not a visual port marker.
            var muzzle=pivot.Find(candidate.Weapon==R4WeaponKind.RotarySaw?"Środek piły":"Wylot");
            if(muzzle==null)return "Brak fizycznie zdefiniowanego wylotu broni.";
            if(candidate.Weapon==R4WeaponKind.RotarySaw)
            {
                var config=new RotarySawConfiguration();
                float extent=Mathf.Sqrt(config.bladeRadius*config.bladeRadius+
                    config.bladeHalfThickness*config.bladeHalfThickness);
                int count=Physics.OverlapSphereNonAlloc(muzzle.position,extent,
                    functionalOverlap,~0,QueryTriggerInteraction.Ignore);
                if(count>=functionalOverlap.Length)
                    return "Fioletowa strefa: przepełnienie bufora sprawdzania piły (montaż zablokowany).";
                for(int i=0;i<count;i++)
                {
                    var other=functionalOverlap[i];
                    if(other==null||!other.enabled||!other.gameObject.activeInHierarchy||
                        other.transform.IsChildOf(probe.transform)||
                        !other.transform.IsChildOf(transform)||other.GetComponentInParent<R3MountedPart>()?.Id==candidate.Id)continue;
                    if(other.bounds.Contains(muzzle.position)||
                       RotarySawContactMath.IsOnCuttingDisc(
                           other.ClosestPoint(muzzle.position)-muzzle.position,
                           muzzle.right,config.bladeRadius,config.bladeHalfThickness,
                           config.hubDeadZone))
                        return "Fioletowa strefa: tarcza piły przecina własny pancerz lub mechanizm.";
                }
                return null;
            }
            var delta=muzzle.position-pivot.position;
            if(delta.sqrMagnitude<.00001f)return "Nieprawidłowy fizyczny odcinek lufy.";
            // Hard near-barrel clearance only, NOT an infinite shot trajectory.
            const float radius=.035f;
            int nearCount=Physics.OverlapSphereNonAlloc(pivot.position,radius,
                functionalOverlap,~0,QueryTriggerInteraction.Ignore);
            if(nearCount>=functionalOverlap.Length)
                return "Fioletowa strefa: przepełnienie bufora osi lufy (montaż zablokowany).";
            for(int i=0;i<nearCount;i++)
            {
                var other=functionalOverlap[i];
                if(other!=null&&other.enabled&&other.gameObject.activeInHierarchy&&
                    other.transform.IsChildOf(transform)&&
                    !other.transform.IsChildOf(probe.transform)&&other.GetComponentInParent<R3MountedPart>()?.Id!=candidate.Id)
                    return "Fioletowa strefa: oś lufy zaczyna się we własnej części.";
            }
            int castCount=Physics.SphereCastNonAlloc(pivot.position,radius,delta.normalized,
                functionalCast,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(castCount>=functionalCast.Length)
                return "Fioletowa strefa: przepełnienie bufora lufy (montaż zablokowany).";
            for(int i=0;i<castCount;i++)
            {
                var collider=functionalCast[i].collider;
                if(collider!=null&&collider.enabled&&
                    collider.transform.IsChildOf(transform)&&
                    !collider.transform.IsChildOf(probe.transform)&&collider.GetComponentInParent<R3MountedPart>()?.Id!=candidate.Id)
                    return "Fioletowa strefa: obracana lufa przecina własną konstrukcję.";
            }
            return null;
        }
        string ExistingMountedWeaponPlacementIssue(GameObject probe,Collider[] proposed,HashSet<string> ignoredOwnerIds=null)
        {
            if(proposed==null||proposed.Length==0)
                return "Brak fizycznego collidera proponowanej części.";
            foreach(var gun in assembly.Parts)
            {
                if(gun==null||gun.Kind!=R3PartKind.Gun||!gun.Operational||
                   !gun.gameObject.activeInHierarchy||!gun.transform.IsChildOf(transform)||
                   gun.Muzzle==null||gun.Weapon==R4WeaponKind.EmpEmitter)continue;
                if(ignoredOwnerIds!=null&&ignoredOwnerIds.Contains(gun.Id))continue;

                if(gun.Weapon==R4WeaponKind.RotarySaw)
                {
                    var config=new RotarySawConfiguration();
                    Vector3 center=gun.Muzzle.position;
                    float extent=Mathf.Sqrt(config.bladeRadius*config.bladeRadius+
                        config.bladeHalfThickness*config.bladeHalfThickness);
                    // Candidate-only conservative bounds prefilter prevents a
                    // global query for each gun on large real vehicles. Real
                    // Collider.ClosestPoint then evaluates the original disc test.
                    foreach(var candidateCollider in proposed)
                    {
                        if(candidateCollider==null||!candidateCollider.enabled||
                           !candidateCollider.gameObject.activeInHierarchy||
                           candidateCollider.bounds.SqrDistance(center)>extent*extent)continue;
                        if(candidateCollider.bounds.Contains(center)||
                           RotarySawContactMath.IsOnCuttingDisc(
                               candidateCollider.ClosestPoint(center)-center,
                               gun.Muzzle.right,config.bladeRadius,
                               config.bladeHalfThickness,config.hubDeadZone))
                            return "Fioletowa strefa: nowa część blokuje tarczę zamontowanej piły.";
                    }
                    continue;
                }

                if(gun.GunPivot==null)
                    return "Nie można zweryfikować osi zamontowanej broni.";
                Vector3 start=gun.GunPivot.position;
                Vector3 end=gun.Muzzle.position;
                Vector3 delta=end-start;
                if(delta.sqrMagnitude<.00001f)
                    return "Nieprawidłowa oś zamontowanej lufy.";
                const float radius=.035f;
                var segmentBounds=new Bounds((start+end)*.5f,
                    new Vector3(Mathf.Abs(delta.x),Mathf.Abs(delta.y),
                        Mathf.Abs(delta.z))+Vector3.one*(2f*radius));
                if(!proposed.Any(c=>c!=null&&c.enabled&&
                    c.bounds.Intersects(segmentBounds)))continue;
                // Same bounded PhysX scratch buffer and saturation refusal as
                // #877. No allocating OverlapSphereAll/SphereCastAll fallback.
                int count=Physics.OverlapCapsuleNonAlloc(start,end,radius,
                    functionalOverlap,~0,QueryTriggerInteraction.Ignore);
                if(count>=FunctionalQueryCapacity)
                    return "Fioletowa strefa: zbyt wiele kolizji przy zamontowanej lufie — montaż odrzucony.";
                for(int i=0;i<count;i++)
                {
                    var hit=functionalOverlap[i];
                    if(hit!=null&&hit.enabled&&
                       hit.gameObject.activeInHierarchy&&
                       hit.transform.IsChildOf(probe.transform))
                        return "Fioletowa strefa: nowa część blokuje zamontowaną lufę.";
                }
            }
            return null;
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
        public static List<R3VisibleMountPoint> PotentialMountPoints(R3VehicleAssembly actual,R3PartState candidate)
        {
            var result=new List<R3VisibleMountPoint>();
            var gate=actual!=null?actual.GetComponent<R3BuildValidation>():null;
            if(gate==null||gate.sockets==null||gate.model==null||candidate==null)return result;
            var states=gate.model.Parts.ToDictionary(p=>p.Id);
            var occupied=new HashSet<Vector3Int>(gate.model.Parts.SelectMany(R3VehicleModel.OccupiedCells));
            var reachable=BuildPlacementRules.Reachable(gate.Installed());
            var definition=gate.Definition(candidate);
            var atOrigin=candidate.Copy();atOrigin.Cell=Vector3Int.zero;
            var offsets=R3VehicleModel.OccupiedCells(atOrigin).ToArray();
            var seen=new HashSet<(Vector3Int,Vector3Int)>();
            foreach(var face in gate.socketFaces)
            {
                if(face==null||!face.activeInHierarchy)continue;
                var marker=face.GetComponent<BuildMountSurface>();var collider=face.GetComponent<Collider>();
                var mounted=face.GetComponentInParent<R3MountedPart>();
                if(marker==null||collider==null||mounted==null||!mounted.Operational||
                    !mounted.transform.IsChildOf(actual.transform)||!reachable.Contains(mounted.Id)||
                    !states.TryGetValue(marker.PartInstanceId,out var state))continue;
                var support=gate.Definition(state);
                if(!gate.registry.TryGet(collider,mounted.transform,mounted.Id,support,out int index))continue;
                var socket=support.Sockets[index];
                var cell=state.Cell+R3MountingGrid.Rotate(socket.Cell,state.RotationQuarterTurns);
                var direction=R3MountingGrid.Rotate(socket.Face,state.RotationQuarterTurns);
                if(!seen.Add((cell,direction)))continue;
                foreach(var offset in offsets)
                {
                    var anchor=cell+direction-offset;
                    var atAnchor=candidate.Copy();atAnchor.Cell=anchor;
                    var candidateCells=R3VehicleModel.OccupiedCells(atAnchor);
                    if(candidateCells.Any(c=>occupied.Contains(c)||System.Math.Abs(c.x)>6||
                        System.Math.Abs(c.z)>7||c.y<0||c.y>5))continue;
                    if(!BuildSocketGeometry.Touches(support,new BuildPose(state.Cell,state.RotationQuarterTurns),
                        definition,new BuildPose(anchor,candidate.RotationQuarterTurns),out _))continue;
                    result.Add(new R3VisibleMountPoint(face.transform.position,face.transform.rotation,collider));
                    break;
                }
            }
            return result;
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
                    box.size=new Vector3(GarageSocketSize,GarageSocketSize,.012f)/part.transform.localScale.x;
                    var marker=face.AddComponent<BuildMountSurface>();marker.PartInstanceId=part.Id;marker.SocketIndex=index;
                    if(!gate.registry.Register(box,part.transform,part.Id,definition,index))
                        Debug.LogError("[BUILD receiver] Cannot register actual structural socket "+part.Id+"/"+index);
                }
            }
            Physics.SyncTransforms();
        }
    }
}
