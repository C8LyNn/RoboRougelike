// B01-12 / PASS17 W28 / Garage Spatial Readout 001.
// Additive presentation for EXISTING R3GarageView. No extra placement authority.
// This component never calls GarageBuy/Mount/Repair, changes gameplay state,
// changes Collider.enabled, or modifies Save/Load and Scrap.
using System;
using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;

namespace RoboRougelike.B01.GarageSpatialW28
{
    public enum B0112ZoneKind { OccupiedRed, ProtrusionBlue, WeaponPurple, MechanismOrange, ValidGreen, PotentialSocket }

    public readonly struct B0112ZoneBox
    {
        public readonly B0112ZoneKind Kind;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Size;
        public B0112ZoneBox(B0112ZoneKind kind, Vector3 position, Quaternion rotation, Vector3 size)
        { Kind=kind; Position=position; Rotation=rotation; Size=size; }
    }

    /// <summary>
    /// R3 already owns the ONLY placement validator: R3GarageView.PlacementIssue
    /// and the underlying R3VehicleModel.PlacementError/OwnedPlacementError.
    /// This component is an observer and never infers permission from its colored boxes.
    /// "Blue/purple/orange" are APPROXIMATE VISUAL envelopes until Codex supplies
    /// authoritative physical/sweep volumes through the existing garage validator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0112GarageSpatialOverlay : MonoBehaviour
    {
        public const float CellMeters=R41PartGeometry.Grid; // real canonical R3 0.55 m
        public const int MaxBoxes=2048;
        public const string ValidationOwner="R3GarageView.PlacementIssue";

        readonly List<B0112ZoneBox> boxes=new List<B0112ZoneBox>(MaxBoxes);
        readonly Material[] materials=new Material[6];
        R3GarageView garage;
        Mesh cube;
        bool shaderWarned;
        string reason;
        bool visible;
        float nextLookup, nextRefresh;
        R3GarageView lastReadoutSource;
        Vector3Int lastCell,lastRotation;
        R3PartKind lastKind;
        R4WeaponKind lastWeapon;
        R41GarageTool lastTool;
        string lastInventoryId;
        bool lastHasTarget;

        public int VisibleBoxCount => visible?boxes.Count:0;
        public string LastReason => visible?reason:null;
        public bool SourceValidated => visible&&garage!=null&&garage.PreviewValid;
        public bool HasPlacementIssue => visible&&garage!=null&&garage.PlacementIssue!=null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachReadOnlyObserver()
        {
            if(FindObjectOfType<B0112GarageSpatialOverlay>()!=null)return;
            new GameObject("B01-12 Garage 3D Zones (read-only)").AddComponent<B0112GarageSpatialOverlay>();
        }

        static Material NewTint(Color tint)
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null)shader=Shader.Find("Sprites/Default");
            if(shader==null)return null;
            var mat=new Material(shader){name="B01-12 spatial zone visual (runtime only)"};
            if(mat.HasProperty("_Surface"))mat.SetFloat("_Surface",1f);
            if(mat.HasProperty("_SrcBlend"))mat.SetInt("_SrcBlend",5); // SrcAlpha
            if(mat.HasProperty("_DstBlend"))mat.SetInt("_DstBlend",10); // OneMinusSrcAlpha
            if(mat.HasProperty("_ZWrite"))mat.SetInt("_ZWrite",0);
            mat.SetOverrideTag("RenderType","Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue=3000;
            if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",tint);
            if(mat.HasProperty("_Color"))mat.SetColor("_Color",tint);
            return mat;
        }

        void Awake()
        {
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube=primitive.GetComponent<MeshFilter>().sharedMesh;
            primitive.SetActive(false);
            Destroy(primitive);
            materials[(int)B0112ZoneKind.OccupiedRed]=NewTint(new Color(1f,.10f,.12f,.16f));
            materials[(int)B0112ZoneKind.ProtrusionBlue]=NewTint(new Color(.10f,.44f,1f,.20f));
            materials[(int)B0112ZoneKind.WeaponPurple]=NewTint(new Color(.66f,.22f,1f,.20f));
            materials[(int)B0112ZoneKind.MechanismOrange]=NewTint(new Color(1f,.47f,.06f,.20f));
            materials[(int)B0112ZoneKind.ValidGreen]=NewTint(new Color(.13f,1f,.32f,.29f));
            materials[(int)B0112ZoneKind.PotentialSocket]=NewTint(new Color(.08f,.9f,1f,.85f));
        }

        static bool Mechanism(R3PartKind kind) => kind==R3PartKind.Suspension||
            kind==R3PartKind.Booster||kind==R3PartKind.Gyroscope||
            kind==R3PartKind.JumpDrive;
        static bool Protrusion(R3PartKind kind) => kind==R3PartKind.Gun||kind==R3PartKind.Wheel||
            kind==R3PartKind.CrawlerTrack||kind==R3PartKind.OmniDrive;

        public static bool IsAllowedByExistingValidator(bool hasTarget, string placementIssue)
            => hasTarget&&placementIssue==null;
        public static B0112ZoneKind CandidateTint(bool hasTarget,string placementIssue)
            => IsAllowedByExistingValidator(hasTarget,placementIssue)?B0112ZoneKind.ValidGreen:B0112ZoneKind.OccupiedRed;

        void Add(B0112ZoneKind kind,Vector3 position,Quaternion rotation,Vector3 size)
        {
            if(boxes.Count>=MaxBoxes)return;
            if(size.x<=0||size.y<=0||size.z<=0)return;
            boxes.Add(new B0112ZoneBox(kind,position,rotation,size));
        }

        static Transform FindWeaponEndpoint(Transform root)
        {
            if(root==null)return null;
            foreach(var tr in root.GetComponentsInChildren<Transform>(true))
                if(tr.name=="Wylot"||tr.name=="Środek piły")return tr;
            return null;
        }

        void AddFunctionalEnvelope(Transform root,R3PartKind kind,R4WeaponKind weapon,int footprintRevision)
        {
            if(root==null)return;
            if(Protrusion(kind))
            {
                // The rendered wheel/track often extends beyond its logical cell.
                // Renderer bounds are visual guidance, not a Collider/placement proof.
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer is ParticleSystemRenderer||renderer is LineRenderer||!renderer.enabled||
                       renderer.name.StartsWith("Punkt mocowania"))continue;
                    var bounds=renderer.bounds;
                    if(bounds.size.sqrMagnitude>0.01f)
                        Add(B0112ZoneKind.ProtrusionBlue,bounds.center,Quaternion.identity,bounds.size);
                }
            }
            if(kind==R3PartKind.Gun)
            {
                var muzzle=FindWeaponEndpoint(root);
                if(muzzle!=null)
                {
                    // Solid-coloured near-muzzle box marks the hard local exclusion.
                    Add(B0112ZoneKind.WeaponPurple,muzzle.position+muzzle.forward*.24f,
                        muzzle.rotation,new Vector3(.34f,.34f,.48f));
                    // Thin line is information only; never veto placement at long range.
                    Add(B0112ZoneKind.WeaponPurple,muzzle.position+muzzle.forward*1.8f,
                        muzzle.rotation,new Vector3(.04f,.04f,3.2f));
                    if(weapon==R4WeaponKind.RotarySaw)
                        Add(B0112ZoneKind.MechanismOrange,muzzle.position,muzzle.rotation,
                            new Vector3(.28f,1.8f,1.8f)*(footprintRevision==2?2f:1f));
                }
            }
            if(Mechanism(kind))
                Add(B0112ZoneKind.MechanismOrange,root.position,root.rotation,
                    new Vector3(.9f,.9f,1.3f));
        }

        /// <summary>
        /// Called per frame. Never mutates live model, part IDs, inventory or Collider.
        /// Existing validator controls green/red; envelope outlines are only advisory.
        /// </summary>
        public void RebuildReadout(R3GarageView source)
        {
            boxes.Clear();reason=null;visible=false;garage=source;
            if(source==null||source.Preview==null||source.Tool!=R41GarageTool.Mount||!source.HasMountSelection)return;
            var assembly=source.Preview.GetComponentInParent<R3VehicleAssembly>(true);
            if(assembly==null||assembly.Model==null)return;
            var model=assembly.Model;
            var partRoot=assembly.transform;
            var orientation=partRoot.rotation;
            foreach(var point in source.AvailableMountPoints)
                Add(B0112ZoneKind.PotentialSocket,point.Position,point.Rotation,
                    new Vector3(R3BuildValidation.GarageSocketSize,R3BuildValidation.GarageSocketSize,.012f));
            // Available points must remain visible even before the cursor has
            // reached a registered socket; no ghost is shown in this state.
            visible=true;
            if(!source.HasTarget||!source.Preview.gameObject.activeInHierarchy)return;
            // Canonical R3GarageView already renders RED occupied-cell outlines and
            // BLUE selected payloads in the real W27 Player. Do not duplicate them.
            // Put the genuine host-authorized current candidate FIRST so 180-cell
            // Heavy builds cannot starve its green/red indication at MaxBoxes.
            reason=source.PlacementIssue;
            var tint=CandidateTint(source.HasTarget,reason);
            // The actual part ghost already displays green/red. Extra filled
            // cells and unrelated weapon/wheel volumes obscure its geometry.
            // Show green trusted socket faces only near a candidate that the real
            // R3+BUILD001 physical validator has ALREADY accepted; green never
            // grants permission by itself and cannot override a red host rejection.
            if(tint==B0112ZoneKind.ValidGreen)
            {
                var candidate=source.CandidatePart();
                foreach(var cell in R3VehicleModel.OccupiedCells(candidate))
                foreach(var direction in R3MountingGrid.FaceDirections)
                {
                    var support=model.At(cell+direction);
                    if(support==null||support.Health<=0||support.Kind==R3PartKind.Gun||support.Kind==R3PartKind.Wheel||
                       !R3MountingGrid.Compatible(candidate,cell,support,cell+direction))continue;
                    var face=partRoot.TransformPoint(((Vector3)cell+(Vector3)direction*.5f)*CellMeters);
                    var facing=orientation*Quaternion.LookRotation((Vector3)direction,
                        direction.y!=0?Vector3.forward:Vector3.up);
                    Add(B0112ZoneKind.ValidGreen,face,facing,
                        new Vector3(.15f,.15f,.035f));
                }
            }
        }

        void LateUpdate()
        {
            if(garage==null&&Time.unscaledTime>=nextLookup)
            {
                nextLookup=Time.unscaledTime+1f;
                garage=FindObjectOfType<R3GarageView>();
            }
            // No heavy live BUILD001 probe every render frame: immediately refresh
            // on candidate move/rotation/tool/selection, otherwise check dynamic
            // mechanism poses and economy/HP at bounded intervals (10 Hz).
            bool hasTarget=garage!=null&&garage.Preview!=null&&garage.HasTarget;
            bool changed=garage!=lastReadoutSource||hasTarget!=lastHasTarget;
            if(hasTarget)
                changed|=garage.Cell!=lastCell||garage.SelectedRotation!=lastRotation||
                         garage.MountKind!=lastKind||garage.MountWeapon!=lastWeapon||
                         garage.Tool!=lastTool||garage.SelectedInventoryId!=lastInventoryId;
            if(changed||Time.unscaledTime>=nextRefresh)
            {
                RebuildReadout(garage);
                lastReadoutSource=garage;lastHasTarget=hasTarget;
                if(hasTarget){lastCell=garage.Cell;lastRotation=garage.SelectedRotation;
                    lastKind=garage.MountKind;lastWeapon=garage.MountWeapon;
                    lastTool=garage.Tool;lastInventoryId=garage.SelectedInventoryId;}
                nextRefresh=Time.unscaledTime+.10f;
            }
            if(!visible||cube==null)return;
            for(int i=0;i<boxes.Count;i++)
            {
                var box=boxes[i];var material=materials[(int)box.Kind];
                if(material==null)
                {
                    if(!shaderWarned)
                    {Debug.LogWarning("B01-12 Garage zones: URP/Unlit shader unavailable; overlay fails closed.");shaderWarned=true;}
                    continue;
                }
                // Draw only a visual mesh. NO Collider, physics, writes, or raycast obstruction.
                Graphics.DrawMesh(cube,Matrix4x4.TRS(box.Position,box.Rotation,box.Size),material,0);
            }
        }

        void OnGUI()
        {
            if(!visible)return;
            int x=Mathf.RoundToInt(Screen.width*.265f);
            int y=Mathf.RoundToInt(Screen.height*.11f);
            int width=Mathf.Min(Mathf.RoundToInt(Screen.width*.465f),660);
            if(width<220)return;
            string state=!garage.HasTarget?"Najedź na świecący punkt":reason==null?"Zielony: można zamontować":"Czerwony: "+reason;
            GUI.Box(new Rect(x,y,width,57),"CYJAN: punkty styku  |  NIEBIESKI: obrys  |  FIOLET: broń\n"+state);
        }

        void OnDestroy()
        {
            foreach(var material in materials)
            {
                if(material==null)continue;
                if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);
            }
        }
    }
}
