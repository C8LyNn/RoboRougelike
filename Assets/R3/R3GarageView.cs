using UnityEngine;
using System.Linq;
using RoboRougelike.R1;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    // A right-button click removes; moving at least four pixels changes it
    // into an orbit gesture. Losing focus or opening UI cancels either.
    public sealed class R3GaragePointerGesture
    {
        const float DragThreshold=4f;
        public bool Pressed { get; private set; }
        bool dragging;
        float distance;
        public void Press(){Pressed=true;dragging=false;distance=0;}
        public bool Drag(Vector2 delta)
        {
            if(!Pressed)return false;
            distance+=delta.magnitude;if(distance>=DragThreshold)dragging=true;
            return dragging;
        }
        public bool Release(bool inside)
        {
            bool remove=Pressed&&!dragging&&inside;
            Cancel();return remove;
        }
        public void Cancel(){Pressed=false;dragging=false;distance=0;}
    }

    public static class R3GarageRotationInput
    {
        public static bool TryStep(KeyCode key,out Vector3Int step)
        {
            switch(key)
            {
                case KeyCode.Q:step=new Vector3Int(0,-1,0);return true;
                case KeyCode.E:step=new Vector3Int(0,1,0);return true;
                case KeyCode.R:step=new Vector3Int(1,0,0);return true;
                case KeyCode.F:step=new Vector3Int(-1,0,0);return true;
                case KeyCode.Z:step=new Vector3Int(0,0,-1);return true;
                case KeyCode.X:step=new Vector3Int(0,0,1);return true;
                default:step=default;return false;
            }
        }
    }

    public sealed class R3GarageView : MonoBehaviour
    {
        public static Rect Viewport => new Rect(0.215625f,0.10f,0.7765625f,0.78f);
        public static readonly Rect PortraitOpening=new Rect(24,76,720,500);
        public static readonly Rect BuildArea=new Rect(276,86,994,562);
        public static readonly Rect ActivityOverlay=new Rect(350,80,580,126);
        public static readonly Rect LegendOverlay=new Rect(350,418,560,29);
        public static readonly Rect WeaponGroupsOverlay=new Rect(350,452,560,76);
        public static Rect ViewportForScreen(int width,int height)
        {
            if(width<=0||height<=0)return new Rect(0,0,0,0);
            if(height>width)
            {
                float portraitScale=Mathf.Min(width/768f,height/1024f);
                float x=(width-768*portraitScale)/2, y=(height-1024*portraitScale)/2;
                return new Rect((x+PortraitOpening.x*portraitScale)/width,
                    (height-y-PortraitOpening.yMax*portraitScale)/height,
                    PortraitOpening.width*portraitScale/width,PortraitOpening.height*portraitScale/height);
            }
            float scale=Mathf.Min(width/1280f,height/720f);
            float left=(width-1280*scale)/2, bottom=(height-720*scale)/2;
            var viewport=Viewport;
            return new Rect((left+viewport.x*1280*scale)/width,(bottom+viewport.y*720*scale)/height,
                viewport.width*1280*scale/width,viewport.height*720*scale/height);
        }
        private R1RunController run;
        private Camera view;
        private P12PipelineSafeCamera follow;
        private R3VisualPalette palette;
        private GameObject previewRoot,marker;
        private readonly System.Collections.Generic.HashSet<Renderer> payloadRenderers=new System.Collections.Generic.HashSet<Renderer>();
        private System.Collections.Generic.List<R3VisibleMountPoint> availableMountPoints;
        private R3VehicleAssembly mountPointsAssembly;
        private R3PartKind mountPointsKind;
        private R3ArmorShape mountPointsArmorShape;
        private R3WheelStyle mountPointsWheelStyle;
        private R4WeaponKind mountPointsWeapon;
        private int mountPointsFootprintRevision;
        private Vector3Int mountPointsRotation;
        private int mountPointsMask;
        private bool mountPointsSelection;
        private readonly RaycastHit[] hits=new RaycastHit[512];
        private float yaw=35,pitch=32,distance=9,previousNear;
        private Vector3 pivotLocal;
        private bool orbitDragging,isOpen,arenaWasActive,raycastSaturated;
        private readonly R3GaragePointerGesture rightGesture=new R3GaragePointerGesture();
        private GameObject hiddenArena,workshopSpace;
        private Renderer workshopFloor;
        // A separate, non-gameplay workspace: safely away from arenas, authored maps,
        // projectiles and other physical entities which remain at ground level.
        private static readonly Vector3 WorkshopOrigin=new Vector3(0,500,0);
        private R3PartKind shownKind;
        private R4WeaponKind shownWeapon;
        private int shownFootprintRevision;
        private R41GarageTool shownTool;
        private Vector3Int shownRotation;
        private int shownContacts=-1;
        private bool shownMirrored;
        private R3WheelStyle shownWheelStyle;
        private R3ArmorShape shownArmorShape;
        private bool shownWheelMirrored;
        private readonly R3PartState previewWheel=new R3PartState(default,R3PartKind.Wheel);
        public Vector3Int SelectedRotation {get;private set;}
        public int SelectedContactMask=>R3VehicleModel.PlacementContacts(MountKind,Owned);
        public R3ArmorShape MountArmorShape=>Owned?.ArmorShape??R3ArmorShape.Cube;
        public Vector3Int Cell = new Vector3Int(0,2,1);
        public R3PartKind SelectedPart = R3PartKind.Armor;
        public R4WeaponKind SelectedWeapon;
        public string SelectedInventoryId;
        public bool UseInventory;
        public string Message = "LPM — montaż; PPM — zdejmij; przeciągnij PPM — obrót widoku.";
        public R41GarageTool Tool { get; private set; }
        public bool HasTarget { get; private set; }
        bool pointerSurfaceRejected;
        public Transform Preview => marker!=null?marker.transform:null;
        public bool PreviewValid => HasTarget&&PlacementIssue==null;
        static bool MarkerVisible(bool hasTarget,R41GarageTool tool,bool needsOwned,bool hasOwned)=>
            hasTarget&&(tool!=R41GarageTool.Mount||!needsOwned||hasOwned);
        public float Distance => distance;
        private R3PartState Owned => run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==SelectedInventoryId);
        public bool HasMountSelection=>!(UseInventory||!string.IsNullOrEmpty(SelectedInventoryId))||Owned!=null;
        public System.Collections.Generic.IReadOnlyList<R3VisibleMountPoint> AvailableMountPoints
        {
            get
            {
                var assembly=run.Player!=null?run.Player.GetComponent<R3VehicleAssembly>():null;
                int mask=SelectedContactMask;bool selected=HasMountSelection;
                var armor=Owned?.ArmorShape??R3ArmorShape.Cube;
                var wheel=Owned?.WheelStyle??R3WheelStyle.Offroad;
                var weapon=MountWeapon;
                int footprintRevision=CandidateFootprintRevision;
                if(availableMountPoints==null||mountPointsAssembly!=assembly||mountPointsKind!=MountKind||
                   mountPointsRotation!=SelectedRotation||mountPointsMask!=mask||mountPointsSelection!=selected||
                   mountPointsArmorShape!=armor||mountPointsWheelStyle!=wheel||mountPointsWeapon!=weapon||
                   mountPointsFootprintRevision!=footprintRevision)
                {
                    var candidate=new R3PartState(Vector3Int.zero,MountKind){
                        RotationQuarterTurns=SelectedRotation,ContactMask=mask,
                        ArmorShape=armor,WheelStyle=wheel,Weapon=weapon,FootprintRevision=footprintRevision};
                    availableMountPoints=HasMountSelection?
                        R3BuildValidation.PotentialMountPoints(assembly,candidate):new System.Collections.Generic.List<R3VisibleMountPoint>();
                    mountPointsAssembly=assembly;mountPointsKind=MountKind;
                    mountPointsRotation=SelectedRotation;mountPointsMask=mask;mountPointsSelection=selected;
                    mountPointsArmorShape=armor;mountPointsWheelStyle=wheel;mountPointsWeapon=weapon;
                    mountPointsFootprintRevision=footprintRevision;
                }
                return availableMountPoints;
            }
        }
        public R3PartKind MountKind => Owned?.Kind??SelectedPart;
        public R4WeaponKind MountWeapon => Owned?.Weapon??SelectedWeapon;
        public int CandidateFootprintRevision=>Owned?.FootprintRevision??
            R3VehicleModel.NewPurchaseFootprint(MountKind,MountWeapon,R3WheelStyle.Offroad);
        public R3PartState CandidatePart()
        {
            var candidate=Owned?.Copy()??new R3PartState(Cell,MountKind){Weapon=MountWeapon};
            candidate.Cell=Cell;candidate.RotationQuarterTurns=SelectedRotation;candidate.ContactMask=SelectedContactMask;
            candidate.FootprintRevision=CandidateFootprintRevision;
            if(HasTarget&&candidate.Kind==R3PartKind.Wheel&&R3VehicleModel.HasSideMount(candidate.WheelStyle)&&run?.VehicleModel!=null)
                candidate.ContactMask=run.VehicleModel.SideWheelContactAt(candidate);
            return candidate;
        }
        public System.Collections.Generic.IEnumerable<Vector3Int> CandidateOccupiedCells()=>
            R3VehicleModel.OccupiedCells(CandidatePart());
        public string PlacementIssue => (UseInventory||!string.IsNullOrEmpty(SelectedInventoryId))&&Owned==null?
            "Wybierz kolejną część w magazynie. Zakup wymaga świadomego wyboru typu części.":!HasTarget?"Najedź na ścianę bloku pojazdu.":
            pointerSurfaceRejected?"Nie można zamontować na wskazanej powierzchni: brak zgodnego punktu styku.":
            Owned?.Health<=0?"Zniszczony moduł wymaga odbudowy przed montażem.":
            Owned!=null?run.VehicleModel.OwnedPlacementError(Owned.Id,Cell,SelectedRotation):
            run.VehicleModel.PlacementError(Cell,MountKind,false,MountWeapon,SelectedRotation,SelectedContactMask);
        public void Initialize(R1RunController owner,Camera camera,P12PipelineSafeCamera cameraFollow,R3VisualPalette colors)
        {run=owner;view=camera;follow=cameraFollow;palette=colors;}
        public void Open()
        {
            if(!isOpen)previousNear=view.nearClipPlane;isOpen=true;
            HideArena();EnsureWorkshopSpace();
            // Combat/range can end with the body tilted, spinning or far from spawn.
            // Normalize its world frame once on entry, before sockets or ghost are built.
            run.Player.MoveTo(WorkshopOrigin+Vector3.up*.7f,Quaternion.identity);
            // Rigidbody interpolation can leave Transform at its pre-teleport pose
            // while simulation is paused; synchronize the visible and physics frames.
            run.Player.transform.SetPositionAndRotation(WorkshopOrigin+Vector3.up*.7f,Quaternion.identity);
            var body=run.Player.GetComponent<Rigidbody>();
            if(body!=null&&!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            follow.enabled=false;view.nearClipPlane=0.05f;view.rect=ViewportForScreen(Screen.width,Screen.height);
            foreach(var part in run.Player.GetComponent<R3VehicleAssembly>().Parts)
                if(part!=null&&part.Operational&&part.GunPivot!=null&&part.transform.IsChildOf(run.Player.transform))part.GunPivot.localRotation=Quaternion.identity;
            Physics.SyncTransforms();
            R3BuildValidation.SetGarageSockets(run.Player.GetComponent<R3VehicleAssembly>(),true);
            if(!Application.isBatchMode){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            HasTarget=false;pointerSurfaceRejected=false;Tool=R41GarageTool.Mount;UseInventory=true;SelectedInventoryId=null;SelectedRotation=default;
            RefreshPreview();FrameVehicle();
        }
        public void Close()
        {
            if(run.Player!=null)R3BuildValidation.SetGarageSockets(run.Player.GetComponent<R3VehicleAssembly>(),false);
            if(previewRoot!=null){previewRoot.SetActive(false);Destroy(previewRoot);}
            previewRoot=null;marker=null;payloadRenderers.Clear();availableMountPoints=null;mountPointsAssembly=null;HasTarget=false;raycastSaturated=false;CancelPointerGesture();
            DestroyWorkshopSpace();
            RestoreArena();
            if(isOpen){view.nearClipPlane=previousNear;view.rect=new Rect(0,0,1,1);follow.enabled=true;}
            isOpen=false;
        }
        private void HideArena()
        {
            var arena=run.Arena?.Layout?.gameObject;
            if(hiddenArena!=arena)
            {
                RestoreArena();hiddenArena=arena;arenaWasActive=arena!=null&&arena.activeSelf;
            }
            if(hiddenArena!=null)hiddenArena.SetActive(false);
        }
        private void RestoreArena()
        {
            if(hiddenArena!=null)hiddenArena.SetActive(arenaWasActive);
            hiddenArena=null;
        }
        private void EnsureWorkshopSpace()
        {
            if(workshopSpace!=null)return;
            workshopSpace=new GameObject("Garaż — osobna przestrzeń");
            workshopSpace.transform.position=WorkshopOrigin;
            workshopFloor=WorkshopSurface("Podłoga warsztatu",new Vector3(0,-.1f,0),new Vector3(80,.2f,80),new Color(.19f,.22f,.24f));
            var wallColor=new Color(.12f,.15f,.17f);
            WorkshopSurface("Ściana północna",new Vector3(0,8,39),new Vector3(80,16,.3f),wallColor);
            WorkshopSurface("Ściana południowa",new Vector3(0,8,-39),new Vector3(80,16,.3f),wallColor);
            WorkshopSurface("Ściana wschodnia",new Vector3(39,8,0),new Vector3(.3f,16,80),wallColor);
            WorkshopSurface("Ściana zachodnia",new Vector3(-39,8,0),new Vector3(.3f,16,80),wallColor);
        }
        private Renderer WorkshopSurface(string name,Vector3 localPosition,Vector3 localScale,Color color)
        {
            var surface=GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name=name;surface.transform.SetParent(workshopSpace.transform,false);
            surface.transform.localPosition=localPosition;surface.transform.localScale=localScale;
            // Decoration only: it must never interfere with the canonical physical
            // placement gate or the cursor's vehicle-part raycasts.
            surface.GetComponent<Collider>().enabled=false;
            var renderer=surface.GetComponent<Renderer>();renderer.sharedMaterial=palette.Parts[(int)R3PartKind.Wheel];
            var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",color);renderer.SetPropertyBlock(tint);
            return renderer;
        }
        private void DestroyWorkshopSpace()
        {
            if(workshopSpace==null)return;
            workshopSpace.SetActive(false);Destroy(workshopSpace);workshopSpace=null;workshopFloor=null;
        }
        public void Orbit(float degrees){yaw+=degrees;}
        public void Tilt(float degrees){pitch=Mathf.Clamp(pitch+degrees,-65,75);}
        public void ToggleUndersideView(){pitch=pitch<0?32:-40;}
        public void Zoom(float amount){distance=Mathf.Clamp(distance+amount,3,22);}
        public void RotateSelected(int axis,int quarterTurns)
        {
            if(axis<0||axis>2||quarterTurns==0)return;
            var step=axis==0?new Vector3Int(quarterTurns,0,0):
                axis==1?new Vector3Int(0,quarterTurns,0):new Vector3Int(0,0,quarterTurns);
            SelectedRotation=R3MountingGrid.Normalize(SelectedRotation+step);
            Message="Obrót: X "+SelectedRotation.x*90+"° / Y "+SelectedRotation.y*90+
                "° / Z "+SelectedRotation.z*90+"°. Dopasuj punkty styku.";
            RefreshMarker();
        }
        public void SetTool(R41GarageTool tool){Tool=tool;HasTarget=false;pointerSurfaceRejected=false;RefreshMarker();}
        public void ChooseInventory(string id)
        {
            UseInventory=true;SelectedInventoryId=id;var part=Owned;
            if(part!=null){SelectedPart=part.Kind;SelectedWeapon=part.Weapon;
                SelectedRotation=part.RotationQuarterTurns;}
            SetTool(R41GarageTool.Mount);
        }
        public void Select(Vector3Int cell){Cell=cell;HasTarget=true;pointerSurfaceRejected=false;RefreshMarker();}
        private void UpdatePivot()
        {
            var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(run.Player.transform)).ToArray();
            var bounds=new Bounds(run.Player.transform.position,Vector3.zero);bool first=true;
            foreach(var part in parts)foreach(var renderer in part.GetComponentsInChildren<Renderer>())
            {
                // Frame physical visuals, not world-space beam/trail/effect bounds.
                if(!renderer.enabled||renderer is LineRenderer||renderer is TrailRenderer||renderer is ParticleSystemRenderer)continue;
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
            }
            foreach(var part in run.VehicleModel.Parts.Where(p=>p.Health<=0))
                foreach(var cell in R3VehicleModel.OccupiedCells(part))
                    bounds.Encapsulate(run.Player.transform.TransformPoint((Vector3)cell*R41PartGeometry.Grid));
            pivotLocal=run.Player.transform.InverseTransformPoint(bounds.center);
        }
        public void FrameVehicle()
        {
            UpdatePivot();var size=Vector3.zero;
            foreach(var part in run.VehicleModel.Parts)
            foreach(var cell in R3VehicleModel.OccupiedCells(part))
            {var delta=(Vector3)cell*R41PartGeometry.Grid-pivotLocal;size=Vector3.Max(size,new Vector3(Mathf.Abs(delta.x),Mathf.Abs(delta.y),Mathf.Abs(delta.z)));}
            float viewportAspect=view.pixelWidth/(float)Mathf.Max(1,view.pixelHeight);
            float narrowViewport=Mathf.Clamp(0.94f/Mathf.Max(0.3f,viewportAspect),1,1.8f);
            distance=Mathf.Clamp(size.magnitude*2.6f*narrowViewport,4.5f,22);
        }
        public void RefreshPreview()
        {
            availableMountPoints=null;mountPointsAssembly=null;
            if(previewRoot!=null){previewRoot.SetActive(false);Destroy(previewRoot);}
            previewRoot=new GameObject("Podgląd garażu");previewRoot.transform.SetParent(run.Player.transform,false);marker=null;payloadRenderers.Clear();
            foreach(var part in run.VehicleModel.Parts.Where(p=>p.Health<=0))
            {
                var plan=CreateGhost(part.Cell,part.Kind,part.Weapon,palette.Missing,true,
                    part.RotationQuarterTurns,part.ContactMask);
                plan.name="Utracony plan "+part.Cell;plan.AddComponent<R41MissingPlan>().Part=part;
                var collider=plan.AddComponent<BoxCollider>();collider.isTrigger=true;
                if(R3VehicleModel.UsesWideFootprint(part.Kind)){collider.center=new Vector3(R3BonusModuleGeometry.Grid*.5f,0,R3BonusModuleGeometry.Grid*.5f);
                    collider.size=new Vector3(R3BonusModuleGeometry.Width,R3BonusModuleGeometry.Height,R3BonusModuleGeometry.Width);}
            }
            UpdatePivot();RefreshMarker();
        }
        private void ShowOccupiedCells()
        {
            // Keep the construction grid for visible blocks and missing plans.
            // Replaced modules already show their real shape; a cubic outline
            // would visually recreate the old placeholder around that model.
            float half=R41PartGeometry.Grid*.5f;
            var corners=new[]{new Vector3(-half,-half,-half),new Vector3(half,-half,-half),
                new Vector3(half,-half,half),new Vector3(-half,-half,half),
                new Vector3(-half,half,-half),new Vector3(half,half,-half),
                new Vector3(half,half,half),new Vector3(-half,half,half)};
            var path=new[]{0,1,2,3,0,4,5,1,5,6,2,6,7,3,7,4};
            var points=new Vector3[path.Length];for(int i=0;i<path.Length;i++)points[i]=corners[path[i]];
            var assembly=run.Player.GetComponent<R3VehicleAssembly>();
            var visibleParts=run.VehicleModel.Parts.Where(p=>{
                var mounted=assembly.Parts.FirstOrDefault(m=>m!=null&&m.Id==p.Id);
                var renderer=mounted!=null?mounted.GetComponent<Renderer>():null;
                return p.Health<=0||renderer==null||renderer.enabled;
            });
            foreach(var cell in visibleParts.SelectMany(R3VehicleModel.OccupiedCells).Distinct())
            {
                var zone=new GameObject("W27 occupied cell");zone.transform.SetParent(previewRoot.transform,false);
                zone.transform.localPosition=(Vector3)cell*R41PartGeometry.Grid;
                var line=zone.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;
                line.SetPositions(points);line.startWidth=line.endWidth=.024f;line.sharedMaterial=palette.GridOccupied;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            }
        }
        private GameObject CreateGhost(Vector3Int cell,R3PartKind kind,R4WeaponKind weapon,
            Material material,bool plan=false,Vector3Int rotation=default,
            int contacts=R3MountingGrid.AllFaces)
        {
            var shape=kind==R3PartKind.Armor&&!plan?MountArmorShape:R3ArmorShape.Cube;
            var item=shape!=R3ArmorShape.Cube&&R41PartGeometry.ArmorShapeAvailable(shape)?
                R41PartGeometry.CreateMountingBlock(shape):GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name=plan?"Plan":"Ghost części";
            item.transform.SetParent(previewRoot.transform,false);
            item.transform.localPosition=(Vector3)cell*R41PartGeometry.Grid;
            item.transform.localRotation=R3MountingGrid.Rotation(rotation);
            item.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            item.GetComponent<Renderer>().sharedMaterial=material;
            if(R3VehicleModel.UsesWideFootprint(kind))R3BonusModuleGeometry.ConfigureCube(item,false);
            var collider=item.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(kind==R3PartKind.Armor&&!plan&&Tool==R41GarageTool.Mount)R41PartGeometry.TryAttachArmor(item.transform,Owned?.ArmorShape??R3ArmorShape.Cube);
            if(!R3VehicleModel.UsesWideFootprint(kind)&&(Tool==R41GarageTool.Mount||plan))
            {
                bool wheelMirror=kind==R3PartKind.Wheel&&R4WheelArt.MirrorForMount(run.VehicleModel,
                    new R3PartState(cell,R3PartKind.Wheel){RotationQuarterTurns=rotation,ContactMask=contacts});
                var payload=R41PartGeometry.Payload(item.transform,kind,weapon,material,false,out var muzzle,Owned?.WheelStyle??R3WheelStyle.Offroad,wheelMirror);
                if(kind==R3PartKind.Gun&&CandidateFootprintRevision==2)
                    R41PartGeometry.ConfigureGunScale2(item,payload,weapon);
                // Reuse the player's model and axis adapter, but keep the
                // original ghost geometry and do not create physical colliders.
                if(!plan&&Tool==R41GarageTool.Mount)
                {
                    if(kind==R3PartKind.Gun&&payload!=null)R4WeaponArt.TryAttach(item.transform,payload,muzzle,weapon,CandidateFootprintRevision==2);
                    else if(kind==R3PartKind.Wheel&&payload!=null)R4WheelArt.TryAttach(item.transform,payload,wheelMirror,Owned?.WheelStyle??R3WheelStyle.Offroad);
                    else if(kind==R3PartKind.Suspension)R4SuspensionArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.Booster)R4BoosterArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.JumpDrive)R4JumpArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.Blink)R4BlinkArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.Gyroscope)R4GyroArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.MagneticGrapple)R4GrappleArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.HoverEngine)R4HoverArt.TryAttach(item.transform);
                    else if(kind==R3PartKind.OmniDrive)R4OmniArt.TryAttach(item.transform,cell.x>0);
                    else if(kind==R3PartKind.CrawlerTrack)R4TracksArt.TryAttach(item.transform,cell.x>0);
                    else if(kind==R3PartKind.IndependentSteering)
                    {
                        R4IndependentSteeringArt.TryAttach(item.transform);
                    }
                }
            }
            if(!plan&&Tool==R41GarageTool.Mount)
                foreach(var renderer in item.GetComponentsInChildren<Renderer>())
                    if(renderer.transform!=item.transform)payloadRenderers.Add(renderer);
            // The spatial overlay displays only contacts actually paired with
            // a compatible supporting face; do not mark unused faces green.
            return item;
        }
        private static void AddPortMarkers(Transform parent,R3PartKind kind,int contacts,Material mat)
        {
            var offsets=R3VehicleModel.OccupiedCells(Vector3Int.zero,kind).ToArray();
            var occupied=new System.Collections.Generic.HashSet<Vector3Int>(offsets);
            float scale=parent.localScale.x;
            foreach(var offset in offsets)
                foreach(var direction in R3MountingGrid.FaceDirections)
                {
                    if(occupied.Contains(offset+direction)||(contacts&R3MountingGrid.FaceBit(direction))==0)
                        continue;
                    var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    dot.name="Punkt mocowania "+direction;
                    dot.transform.SetParent(parent,false);
                    dot.transform.localPosition=((Vector3)offset+(Vector3)direction*.5f)*R41PartGeometry.Grid/scale;
                    dot.transform.localScale=Vector3.one*(0.13f/scale);
                    dot.GetComponent<Renderer>().sharedMaterial=mat;
                    var c=dot.GetComponent<Collider>();c.enabled=false;Destroy(c);
                }
        }
        public void RefreshMarker()
        {
            if(previewRoot==null||run.VehicleModel==null)return;
            var wheelStyle=Owned?.WheelStyle??R3WheelStyle.Offroad;
            var armorShape=Owned?.ArmorShape??R3ArmorShape.Cube;
            previewWheel.Cell=Cell;previewWheel.RotationQuarterTurns=SelectedRotation;previewWheel.ContactMask=SelectedContactMask;
            bool wheelMirrored=MountKind==R3PartKind.Wheel&&R4WheelArt.MirrorForMount(run.VehicleModel,previewWheel);
            if(marker==null||shownKind!=MountKind||shownWeapon!=MountWeapon||shownFootprintRevision!=CandidateFootprintRevision||
               shownTool!=Tool||shownRotation!=SelectedRotation||shownContacts!=SelectedContactMask||
               (MountKind==R3PartKind.Wheel&&(shownWheelStyle!=wheelStyle||shownWheelMirrored!=wheelMirrored))||
               (MountKind==R3PartKind.Armor&&shownArmorShape!=armorShape)||
               ((MountKind==R3PartKind.OmniDrive||MountKind==R3PartKind.CrawlerTrack)&&shownMirrored!=(Cell.x>0)))
            {
                if(marker!=null){marker.SetActive(false);Destroy(marker);}
                payloadRenderers.Clear();
                marker=CreateGhost(Cell,MountKind,MountWeapon,palette.Valid,false,
                    SelectedRotation,SelectedContactMask);
                shownKind=MountKind;shownWeapon=MountWeapon;shownTool=Tool;shownRotation=SelectedRotation;
                shownContacts=SelectedContactMask;shownMirrored=Cell.x>0;
                shownWheelStyle=wheelStyle;shownWheelMirrored=wheelMirrored;shownFootprintRevision=CandidateFootprintRevision;
                shownArmorShape=armorShape;
            }
            bool needsOwned=UseInventory||!string.IsNullOrEmpty(SelectedInventoryId);
            marker.SetActive(MarkerVisible(HasTarget,Tool,needsOwned,Owned!=null));
            marker.transform.localPosition=(Vector3)Cell*R41PartGeometry.Grid;
            var material=Tool==R41GarageTool.Mount?(PreviewValid?palette.Valid:palette.Invalid):palette.Selection;
            foreach(var renderer in marker.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=material;
                renderer.sharedMaterials=materials;
            }
            foreach(var plan in previewRoot.GetComponentsInChildren<R41MissingPlan>(true))
                foreach(var renderer in plan.GetComponentsInChildren<Renderer>(true))renderer.enabled=Tool!=R41GarageTool.Mount;
        }
        public static Vector3Int LocalFace(Transform vehicle,Vector3 worldNormal)
        {
            var n=vehicle.InverseTransformDirection(worldNormal);var absolute=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
            if(absolute.x>=absolute.y&&absolute.x>=absolute.z)return n.x<0?Vector3Int.left:Vector3Int.right;
            if(absolute.y>=absolute.z)return n.y<0?Vector3Int.down:Vector3Int.up;
            return n.z<0?Vector3Int.back:Vector3Int.forward;
        }
        public bool Point(Ray ray)
        {
            if(run.Progress?.State!=R1RunState.Garage||run.Transitioning)return false;
            Physics.SyncTransforms();
            int count=Physics.RaycastNonAlloc(ray,hits,100,~0,QueryTriggerInteraction.Collide);
            // PhysX does not guarantee ordering when its fixed buffer fills.
            // A partial hit list cannot safely authorize removal or mounting.
            if(count>=hits.Length)
            {
                raycastSaturated=true;HasTarget=false;pointerSurfaceRejected=false;
                Message="Nie można wskazać części: zbyt wiele obiektów pod kursorem.";
                RefreshMarker();return false;
            }
            if(raycastSaturated){raycastSaturated=false;Message="Możesz ponownie wskazać część.";}
            float nearest=float.PositiveInfinity;R3MountedPart target=null;R41MissingPlan missing=null;Vector3 normal=default;
            RaycastHit nearestHit=default;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.distance>=nearest)continue;
                var plan=hit.collider.GetComponent<R41MissingPlan>();var part=hit.collider.GetComponentInParent<R3MountedPart>();
                if(plan!=null&&plan.transform.IsChildOf(previewRoot.transform)){missing=plan;target=null;}
                else if(part!=null&&part.Operational&&part.transform.IsChildOf(run.Player.transform)){target=part;missing=null;}
                else continue;
                nearest=hit.distance;normal=hit.normal;nearestHit=hit;
            }
            HasTarget=target!=null||missing!=null;
            pointerSurfaceRejected=false;
            if(Tool==R41GarageTool.Mount&&target!=null&&
                (!ResolveSocketFace(ray,ref nearestHit)||
                 !R3BuildValidation.ValidateHit(run.Player.GetComponent<R3VehicleAssembly>(),nearestHit)))
            {
                Cell=target.Cell+LocalFace(run.Player.transform,normal);
                pointerSurfaceRejected=true;RefreshMarker();return false;
            }
            if(Tool==R41GarageTool.Mount&&target!=null)normal=nearestHit.normal;
            if(HasTarget)
            {
                var nearestCell=target!=null?target.Cell:missing.Part.Cell;
                var hitPart=target!=null?run.VehicleModel.Parts.FirstOrDefault(p=>p.Id==target.Id):
                    missing?.Part;
                if(hitPart!=null&&R3VehicleModel.UsesWideFootprint(hitPart.Kind))
                {
                    Vector3 local=run.Player.transform.InverseTransformPoint(
                        nearestHit.point-nearestHit.normal*.02f);
                    nearestCell=R3VehicleModel.OccupiedCells(hitPart)
                        .OrderBy(c=>((Vector3)c*R41PartGeometry.Grid-local).sqrMagnitude).First();
                }
                var face=LocalFace(run.Player.transform,normal);
                var outside=nearestCell+face;
                if(Tool==R41GarageTool.Mount&&R3VehicleModel.UsesWideFootprint(MountKind))
                {
                    var candidates=R3VehicleModel.OccupiedCells(Vector3Int.zero,MountKind,
                            SelectedRotation).Select(offset=>outside-offset).ToArray();
                    Cell=candidates.FirstOrDefault(candidate=>run.VehicleModel.PlacementError(
                        candidate,MountKind,Owned!=null,MountWeapon,SelectedRotation,
                        SelectedContactMask)==null);
                    // If no candidate is valid, show an invalid ghost rather than moving
                    // it silently elsewhere.
                    if(!candidates.Any(c=>c==Cell&&run.VehicleModel.PlacementError(
                        c,MountKind,Owned!=null,MountWeapon,SelectedRotation,SelectedContactMask)==null))
                        Cell=candidates[0];
                }
                else Cell=Tool==R41GarageTool.Mount?outside:nearestCell;
            }
            RefreshMarker();return HasTarget;
        }
        bool ResolveSocketFace(Ray ray,ref RaycastHit hit)
        {
            // The selectable patch is a thin trigger. A grazing ray may hit
            // its side, whose PhysX normal is unrelated to the visible face.
            // Project onto the registered patch only, and require the cursor
            // to land inside its displayed bounds from the front.
            var hitCollider=hit.collider;
            var socket=AvailableMountPoints.FirstOrDefault(p=>p.Surface==hitCollider);
            if(socket.Surface==null)return false;
            var normal=socket.Rotation*Vector3.forward;
            if(Vector3.Dot(ray.direction,normal)>=-.00001f)return false;
            var plane=new Plane(normal,socket.Position);
            if(!plane.Raycast(ray,out float distance)||distance<0)return false;
            var point=ray.GetPoint(distance);
            var local=Quaternion.Inverse(socket.Rotation)*(point-socket.Position);
            float half=R3BuildValidation.GarageSocketSize*.5f+.0001f;
            if(Mathf.Abs(local.x)>half||Mathf.Abs(local.y)>half)return false;
            hit.point=point;hit.normal=normal;
            return true;
        }
        private static string MountedStackFeedback(int remaining,bool usable)=>usable?
            "Zamontowano. Wybierz miejsce dla kolejnej części.":remaining>0?
            "Zamontowano. Pozostałe części są zniszczone — napraw je w magazynie.":
            "Zamontowano. Brak kolejnych części tej grupy — kup lub odzyskaj nowe.";
        public bool ClickTarget()
        {
            if(!HasTarget||run.Progress?.State!=R1RunState.Garage||run.Transitioning)return false;
            bool result;
            if(Tool==R41GarageTool.Mount)
            {
                string error=PlacementIssue;if(error!=null){Message=error;return false;}
                var mounted=Owned;
                result=mounted!=null?run.GarageMount(SelectedInventoryId,Cell,SelectedRotation):
                    MountKind==R3PartKind.Gun?run.GarageBuyWeapon(Cell,MountWeapon,SelectedRotation):
                    run.GarageBuy(Cell,MountKind,SelectedRotation);
                Message=result?"Zamontowano dokładnie w miejscu podglądu.":"Montaż nieudany.";
                if(result&&mounted!=null)
                {
                    // Keep building from the same owned stack, without purchasing or
                    // silently substituting a different weapon or bonus.
                    var remaining=run.VehicleModel.Inventory.Where(p=>p.Kind==mounted.Kind&&
                        (p.Kind!=R3PartKind.Gun||(p.Weapon==mounted.Weapon&&p.FootprintRevision==mounted.FootprintRevision))&&
                        (p.Kind!=R3PartKind.Wheel||(p.WheelStyle==mounted.WheelStyle&&p.FootprintRevision==mounted.FootprintRevision))&&
                        (p.Kind!=R3PartKind.Armor||p.ArmorShape==mounted.ArmorShape)&&
                        (p.Kind!=R3PartKind.BonusModule||p.UpgradeId==mounted.UpgradeId)).ToArray();
                    var next=remaining.Where(p=>p.Health>0).OrderByDescending(p=>p.Health).FirstOrDefault();
                    SelectedInventoryId=next?.Id;
                    Message=MountedStackFeedback(remaining.Length,next!=null);
                }
            }
            else if(Tool==R41GarageTool.Repair)
            {result=run.GarageRepair(Cell);Message=result?"Naprawiono wskazaną część.":"Brak uszkodzeń, Złomu lub sprawnego połączenia.";}
            else if(Tool==R41GarageTool.Remove)
            {result=run.GarageRemove(Cell);Message=result?"Część wróciła do magazynu w obecnym stanie.":"Chroniony rdzeń, most lub pełny magazyn.";}
            else {result=run.VehicleModel.At(Cell)!=null;Message=result?"Stan części i koszt naprawy są widoczne w panelu garażu.":"Nie znaleziono części.";}
            if(result&&Tool==R41GarageTool.Remove)HasTarget=false;
            RefreshMarker();return result;
        }
        public static bool InBuildArea(Vector2 screen,int width,int height)
        {
            if(width<=0||height<=0)return false;
            if(height>width)
            {
                float portraitScale=Mathf.Min(width/768f,height/1024f);
                var portraitOffset=new Vector2((width-768*portraitScale)/2,(height-1024*portraitScale)/2);
                var portraitPoint=(new Vector2(screen.x,height-screen.y)-portraitOffset)/portraitScale;
                return PortraitOpening.Contains(portraitPoint);
            }
            float scale=Mathf.Min(width/1280f,height/720f);var offset=new Vector2((width-1280*scale)/2,(height-720*scale)/2);
            var point=(new Vector2(screen.x,height-screen.y)-offset)/scale;
            var viewport=Viewport;
            var opening=new Rect(viewport.x*1280,(1-viewport.yMax)*720,viewport.width*1280,viewport.height*720);
            return BuildArea.Contains(point)&&opening.Contains(point);
        }
        public bool ClickScreen(Vector2 screen)
        {return InBuildArea(screen,Screen.width,Screen.height)&&Point(view.ScreenPointToRay(screen))&&ClickTarget();}
        private static R41GarageTool? ToolShortcut(KeyCode key,bool modified)
        {
            if(modified)return null;
            switch(key)
            {
                case KeyCode.Alpha1:case KeyCode.Keypad1:return R41GarageTool.Mount;
                case KeyCode.Alpha2:case KeyCode.Keypad2:return R41GarageTool.Inspect;
                case KeyCode.Alpha3:case KeyCode.Keypad3:return R41GarageTool.Repair;
                case KeyCode.Alpha4:case KeyCode.Keypad4:return R41GarageTool.Remove;
                default:return null;
            }
        }
        public bool RotatePointed(Ray ray,Vector3Int turns)
        {
            var prior=Tool;Tool=R41GarageTool.Inspect;
            try
            {
                bool done=Point(ray)&&run.GarageRotate(Cell,turns);
                Message=done?"Obrócono wskazaną część.":"Nie można obrócić: połączenie, miejsce lub kolizja.";
                return done;
            }
            finally {Tool=prior;RefreshMarker();}
        }
        public void HandleGuiPointer(Event current,Vector2 rawTopLeft)
        {
            if(run.Progress?.State!=R1RunState.Garage||run.Transitioning||run.Paused||run.QuitRequested)
            {CancelPointerGesture();return;}
            var screen=new Vector2(rawTopLeft.x,Screen.height-rawTopLeft.y);bool inside=InBuildArea(screen,Screen.width,Screen.height);
            if(current.type==EventType.KeyDown)
            {
                var tool=ToolShortcut(current.keyCode,current.shift||current.control||current.alt||current.command);
                if(tool.HasValue){if(Tool!=tool.Value)SetTool(tool.Value);current.Use();return;}
            }
            if(current.type==EventType.KeyDown&&current.keyCode==KeyCode.U)
            {ToggleUndersideView();current.Use();return;}
            if(current.type==EventType.KeyDown&&(Tool==R41GarageTool.Mount||current.shift))
            {
                if(R3GarageRotationInput.TryStep(current.keyCode,out var turns))
                {
                    if(current.shift&&inside)RotatePointed(view.ScreenPointToRay(screen),turns);
                    else if(!current.shift&&Tool==R41GarageTool.Mount)
                    {
                        int axis=turns.x!=0?0:turns.y!=0?1:2;
                        RotateSelected(axis,axis==0?turns.x:axis==1?turns.y:turns.z);
                    }
                    current.Use();return;
                }
            }
            if(inside&&current.type==EventType.MouseDown&&current.button==1)
            {rightGesture.Press();current.Use();return;}
            if(current.type==EventType.MouseDrag&&current.button==1&&rightGesture.Pressed)
            {
                if(rightGesture.Drag(current.delta)){orbitDragging=true;Orbit(current.delta.x*.4f);Tilt(-current.delta.y*.3f);}
                current.Use();return;
            }
            if(current.type==EventType.MouseUp&&current.button==1&&rightGesture.Pressed)
            {
                bool remove=rightGesture.Release(inside);orbitDragging=false;
                if(remove)
                {
                    var prior=Tool;Tool=R41GarageTool.Remove;ClickScreen(screen);
                    if(Tool!=prior)SetTool(prior);
                }
                current.Use();return;
            }
            if(!inside)return;
            if(current.type==EventType.ScrollWheel){Zoom(current.delta.y*0.4f);current.Use();return;}
            if(current.type==EventType.Repaint&&!orbitDragging)Point(view.ScreenPointToRay(screen));
            if(current.type==EventType.MouseDown&&current.button==0&&!orbitDragging)
            {
                // Direct actions avoid switching tools for each isolated edit.
                var prior=Tool;
                if(current.shift)Tool=R41GarageTool.Remove;
                else if(current.control||current.command)Tool=R41GarageTool.Repair;
                ClickScreen(screen);
                if(Tool!=prior)SetTool(prior);
                current.Use();
            }
        }
        public void CancelPointerGesture(){rightGesture.Cancel();orbitDragging=false;}
        private void OnApplicationFocus(bool focused){if(!focused)CancelPointerGesture();}
        private void LateUpdate()
        {
            if(!isOpen||run.Progress?.State!=R1RunState.Garage||run.Player==null)return;
            view.rect=ViewportForScreen(Screen.width,Screen.height);
            var pivot=run.Player.transform.TransformPoint(pivotLocal);var rotation=Quaternion.Euler(pitch,yaw,0);
            view.transform.SetPositionAndRotation(pivot+rotation*Vector3.back*distance,rotation);
            if(workshopFloor!=null)workshopFloor.enabled=view.transform.position.y>=WorkshopOrigin.y+.01f;
        }
        private void OnDestroy(){DestroyWorkshopSpace();RestoreArena();if(previewRoot!=null)Destroy(previewRoot);}
    }
}
