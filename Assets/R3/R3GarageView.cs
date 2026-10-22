using UnityEngine;
using System.Linq;
using RoboRougelike.R1;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    public sealed class R3GarageView : MonoBehaviour
    {
        public static Rect Viewport => new Rect(0.265f,0.11f,0.465f,0.88f);
        public static readonly Rect BuildArea=new Rect(340,95,595,430);
        private R1RunController run;
        private Camera view;
        private P12PipelineSafeCamera follow;
        private R3VisualPalette palette;
        private GameObject previewRoot,marker;
        private readonly RaycastHit[] hits=new RaycastHit[512];
        private float yaw=35,pitch=32,distance=9,previousNear;
        private Vector3 pivotLocal;
        private bool orbitDragging;
        private R3PartKind shownKind;
        private R4WeaponKind shownWeapon;
        private R41GarageTool shownTool;
        public Vector3Int Cell = new Vector3Int(0,2,1);
        public R3PartKind SelectedPart = R3PartKind.Armor;
        public R4WeaponKind SelectedWeapon;
        public string SelectedInventoryId;
        public bool UseInventory;
        public string Message = "Wybierz część i kliknij powierzchnię pojazdu. PPM — obrót, kółko — zoom.";
        public R41GarageTool Tool { get; private set; }
        public bool HasTarget { get; private set; }
        public Transform Preview => marker!=null?marker.transform:null;
        public bool PreviewValid => HasTarget&&PlacementIssue==null;
        public float Distance => distance;
        private R3PartState Owned => run.VehicleModel.Inventory.FirstOrDefault(p=>p.Id==SelectedInventoryId);
        public R3PartKind MountKind => Owned?.Kind??SelectedPart;
        public R4WeaponKind MountWeapon => Owned?.Weapon??SelectedWeapon;
        public string PlacementIssue => (UseInventory||!string.IsNullOrEmpty(SelectedInventoryId))&&Owned==null?
            "Wybierz kolejną część w magazynie. Zakup wymaga świadomego wyboru typu części.":!HasTarget?"Najedź na ścianę bloku pojazdu.":
            Owned?.Health<=0?"Zniszczony moduł wymaga odbudowy przed montażem.":
            run.VehicleModel.PlacementError(Cell,MountKind,Owned!=null,MountWeapon);
        public void Initialize(R1RunController owner,Camera camera,P12PipelineSafeCamera cameraFollow,R3VisualPalette colors)
        {run=owner;view=camera;follow=cameraFollow;palette=colors;}
        public void Open()
        {
            follow.enabled=false;previousNear=view.nearClipPlane;view.nearClipPlane=0.05f;view.rect=Viewport;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;HasTarget=false;Tool=R41GarageTool.Mount;SelectedInventoryId=null;
            RefreshPreview();FrameVehicle();
        }
        public void Close()
        {
            if(previewRoot!=null){previewRoot.SetActive(false);Destroy(previewRoot);}
            previewRoot=null;marker=null;HasTarget=false;orbitDragging=false;
            view.nearClipPlane=previousNear;view.rect=new Rect(0,0,1,1);follow.enabled=true;
        }
        public void Orbit(float degrees){yaw+=degrees;}
        public void Tilt(float degrees){pitch=Mathf.Clamp(pitch+degrees,5,75);}
        public void Zoom(float amount){distance=Mathf.Clamp(distance+amount,3,22);}
        public void SetTool(R41GarageTool tool){Tool=tool;HasTarget=false;RefreshMarker();}
        public void ChooseInventory(string id)
        {
            UseInventory=true;SelectedInventoryId=id;var part=Owned;
            if(part!=null){SelectedPart=part.Kind;SelectedWeapon=part.Weapon;}
            SetTool(R41GarageTool.Mount);
        }
        public void Select(Vector3Int cell){Cell=cell;HasTarget=true;RefreshMarker();}
        private void UpdatePivot()
        {
            var parts=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(run.Player.transform)).ToArray();
            var bounds=new Bounds(run.Player.transform.position,Vector3.zero);bool first=true;
            foreach(var part in parts)foreach(var renderer in part.GetComponentsInChildren<Renderer>())
            {if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
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
            if(previewRoot!=null){previewRoot.SetActive(false);Destroy(previewRoot);}
            previewRoot=new GameObject("Podgląd garażu");previewRoot.transform.SetParent(run.Player.transform,false);marker=null;
            foreach(var part in run.VehicleModel.Parts.Where(p=>p.Health<=0))
            {
                var plan=CreateGhost(part.Cell,part.Kind,part.Weapon,palette.Missing,true);
                plan.name="Utracony plan "+part.Cell;plan.AddComponent<R41MissingPlan>().Part=part;
                var collider=plan.AddComponent<BoxCollider>();collider.isTrigger=true;
                if(part.Kind==R3PartKind.BonusModule){collider.center=new Vector3(R3BonusModuleGeometry.Grid*.5f,0,R3BonusModuleGeometry.Grid*.5f);
                    collider.size=new Vector3(R3BonusModuleGeometry.Width,R3BonusModuleGeometry.Height,R3BonusModuleGeometry.Width);}
            }
            UpdatePivot();RefreshMarker();
        }
        private GameObject CreateGhost(Vector3Int cell,R3PartKind kind,R4WeaponKind weapon,Material material,bool plan=false)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name=plan?"Plan":"Ghost części";
            item.transform.SetParent(previewRoot.transform,false);item.transform.localPosition=(Vector3)cell*R41PartGeometry.Grid;
            item.transform.localRotation=Quaternion.identity;item.transform.localScale=Vector3.one*R41PartGeometry.BlockSize;
            item.GetComponent<Renderer>().sharedMaterial=material;
            if(kind==R3PartKind.BonusModule)R3BonusModuleGeometry.ConfigureCube(item,false);
            var collider=item.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(kind!=R3PartKind.BonusModule&&(Tool==R41GarageTool.Mount||plan))
                R41PartGeometry.Payload(item.transform,kind,weapon,material,false,out _);
            return item;
        }
        public void RefreshMarker()
        {
            if(previewRoot==null||run.VehicleModel==null)return;
            if(marker==null||shownKind!=MountKind||shownWeapon!=MountWeapon||shownTool!=Tool)
            {
                if(marker!=null){marker.SetActive(false);Destroy(marker);}
                marker=CreateGhost(Cell,MountKind,MountWeapon,palette.Valid);
                shownKind=MountKind;shownWeapon=MountWeapon;shownTool=Tool;
            }
            marker.SetActive(HasTarget);marker.transform.localPosition=(Vector3)Cell*R41PartGeometry.Grid;
            var material=Tool==R41GarageTool.Mount?(PreviewValid?palette.Valid:palette.Invalid):palette.Selection;
            foreach(var renderer in marker.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
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
            float nearest=float.PositiveInfinity;R3MountedPart target=null;R41MissingPlan missing=null;Vector3 normal=default;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.distance>=nearest)continue;
                var plan=hit.collider.GetComponent<R41MissingPlan>();var part=hit.collider.GetComponentInParent<R3MountedPart>();
                if(plan!=null&&plan.transform.IsChildOf(previewRoot.transform)){missing=plan;target=null;}
                else if(part!=null&&part.Operational&&part.transform.IsChildOf(run.Player.transform)){target=part;missing=null;}
                else continue;
                nearest=hit.distance;normal=hit.normal;
            }
            HasTarget=target!=null||missing!=null;
            if(HasTarget)
            {
                var nearestCell=target!=null?target.Cell:missing.Part.Cell;
                if(target!=null&&target.Kind==R3PartKind.BonusModule)
                {
                    // Select the actual face cell, not always the anchor of a 2x2 module.
                    Vector3 local=run.Player.transform.InverseTransformPoint(
                        hits.First(h=>h.collider!=null&&h.collider.GetComponentInParent<R3MountedPart>()==target).point);
                    nearestCell=new Vector3Int(
                        Mathf.Clamp(Mathf.RoundToInt(local.x/R41PartGeometry.Grid),target.Cell.x,target.Cell.x+1),
                        target.Cell.y,
                        Mathf.Clamp(Mathf.RoundToInt(local.z/R41PartGeometry.Grid),target.Cell.z,target.Cell.z+1));
                }
                Cell=missing!=null?missing.Part.Cell:nearestCell+
                    (Tool==R41GarageTool.Mount?LocalFace(run.Player.transform,normal):Vector3Int.zero);
            }
            RefreshMarker();return HasTarget;
        }
        public bool ClickTarget()
        {
            if(!HasTarget||run.Progress?.State!=R1RunState.Garage||run.Transitioning)return false;
            bool result;
            if(Tool==R41GarageTool.Mount)
            {
                string error=PlacementIssue;if(error!=null){Message=error;return false;}
                result=Owned!=null?run.GarageMount(SelectedInventoryId,Cell):MountKind==R3PartKind.Gun?
                    run.GarageBuyWeapon(Cell,MountWeapon):run.GarageBuy(Cell,MountKind);
                Message=result?"Zamontowano dokładnie w miejscu podglądu.":"Montaż nieudany.";
            }
            else if(Tool==R41GarageTool.Repair)
            {result=run.GarageRepair(Cell);Message=result?"Naprawiono wskazaną część.":"Brak uszkodzeń, Złomu lub sprawnego połączenia.";}
            else if(Tool==R41GarageTool.Remove)
            {result=run.GarageRemove(Cell);Message=result?"Żywa część wróciła do magazynu; utracony plan usunięty.":"Chroniony rdzeń, most lub ostatni niezbędny plan.";}
            else {result=run.VehicleModel.At(Cell)!=null;Message="Zaznaczono część na pojeździe. Jej HP i koszt w warsztacie.";}
            if(result&&Tool==R41GarageTool.Remove)HasTarget=false;
            RefreshMarker();return result;
        }
        public static bool InBuildArea(Vector2 screen,int width,int height)
        {
            float scale=Mathf.Min(width/1280f,height/720f);var offset=new Vector2((width-1280*scale)/2,(height-720*scale)/2);
            return BuildArea.Contains((new Vector2(screen.x,height-screen.y)-offset)/scale);
        }
        public bool ClickScreen(Vector2 screen)
        {return InBuildArea(screen,Screen.width,Screen.height)&&Point(view.ScreenPointToRay(screen))&&ClickTarget();}
        public void HandleGuiPointer(Event current,Vector2 rawTopLeft)
        {
            if(run.Progress?.State!=R1RunState.Garage||run.Transitioning)return;
            var screen=new Vector2(rawTopLeft.x,Screen.height-rawTopLeft.y);bool inside=InBuildArea(screen,Screen.width,Screen.height);
            if(current.type==EventType.MouseUp&&current.button==1)orbitDragging=false;
            if(inside&&current.type==EventType.MouseDown&&current.button==1){orbitDragging=true;current.Use();return;}
            // A drag inside the viewport can start orbit even if another GUI pass consumed its initial press.
            if(current.type==EventType.MouseDrag&&current.button==1&&(orbitDragging||inside))
            {orbitDragging=true;Orbit(current.delta.x*0.4f);Tilt(-current.delta.y*0.3f);current.Use();return;}
            if(!inside)return;
            if(current.type==EventType.ScrollWheel){Zoom(current.delta.y*0.4f);current.Use();return;}
            if(current.type==EventType.Repaint&&!orbitDragging)Point(view.ScreenPointToRay(screen));
            if(current.type==EventType.MouseDown&&current.button==0&&!orbitDragging){ClickScreen(screen);current.Use();}
        }
        private void LateUpdate()
        {
            if(run.Progress?.State!=R1RunState.Garage||run.Player==null)return;
            var pivot=run.Player.transform.TransformPoint(pivotLocal);var rotation=Quaternion.Euler(pitch,yaw,0);
            view.transform.SetPositionAndRotation(pivot+rotation*Vector3.back*distance,rotation);
        }
        private void OnDestroy(){if(previewRoot!=null)Destroy(previewRoot);}
    }
}
