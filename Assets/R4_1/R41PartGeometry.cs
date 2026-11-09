using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.R4_1
{
    // Both the actual assembly and its ghost use these exact local transforms.
    public static class R41PartGeometry
    {
        public const float Grid=0.55f, BlockSize=0.52f;
        // Conservative first integration: only the two full source sockets are
        // active. Partial triangular faces never support guns or wheels.
        public const int WedgeFullContacts=(1<<3)|(1<<5);
        private static readonly System.Collections.Generic.Dictionary<R3ArmorShape,Mesh> armorMeshes=new System.Collections.Generic.Dictionary<R3ArmorShape,Mesh>();
        private static readonly System.Collections.Generic.Dictionary<R3ArmorShape,int> armorContacts=new System.Collections.Generic.Dictionary<R3ArmorShape,int>();
        public static string ArmorCatalogId(R3ArmorShape shape)=>shape switch {
            R3ArmorShape.Wedge=>"R4B_Wedge",R3ArmorShape.HalfBlock=>"R4B_HalfBlock",
            R3ArmorShape.Pyramid=>"R4B_Pyramid",R3ArmorShape.RoundEdge=>"R4B_RoundEdge",
            R3ArmorShape.HalfWedge=>"R4B_HalfWedge",R3ArmorShape.Dome=>"R4B_Dome",_=>null};
        private static string ArmorPrefab(R3ArmorShape shape)=>shape switch {
            R3ArmorShape.Cube=>"R4Art/ArmorCube",R3ArmorShape.Wedge=>"R4Art/ArmorWedge",
            R3ArmorShape.HalfBlock=>"R4Art/ArmorHalfBlock",R3ArmorShape.Pyramid=>"R4Art/ArmorPyramid",
            R3ArmorShape.RoundEdge=>"R4Art/ArmorRoundEdge",R3ArmorShape.HalfWedge=>"R4Art/ArmorHalfWedge",
            R3ArmorShape.Dome=>"R4Art/ArmorDome",_=>null};
        public static int FullArmorContacts(R3ArmorShape shape)
        {
            if(armorContacts.TryGetValue(shape,out var cached))return cached;
            var id=ArmorCatalogId(shape);if(id==null||!R4MountDefinitionAdapter.TryCreate(id,true,out var definition))return 0;
            int mask=0;foreach(var socket in definition.Sockets)mask|=R3MountingGrid.FaceBit(socket.Face);
            armorContacts[shape]=mask;return mask;
        }
        private static Mesh ArmorMesh(R3ArmorShape shape)
        {
            if(armorMeshes.TryGetValue(shape,out var known)&&known!=null)return known;
            var id=ArmorCatalogId(shape);var source=Resources.Load<TextAsset>("R4Art/BlockColliders");
            if(id==null||source==null||!R4ColliderMeshFactory.TryCreate(source.text,id,out var meshes,out _))return null;
            if(meshes.Length!=1){foreach(var mesh in meshes)if(mesh!=null)Object.Destroy(mesh);return null;}
            var result=meshes[0];var vertices=result.vertices;
            // Block transforms have the legacy .52 scale; source vertices are meters.
            for(int i=0;i<vertices.Length;i++)vertices[i]/=BlockSize;
            result.vertices=vertices;result.RecalculateBounds();result.name=id+" physical collider";
            armorMeshes[shape]=result;return result;
        }
        public static bool ArmorShapeAvailable(R3ArmorShape shape)=>FullArmorContacts(shape)!=0&&ArmorMesh(shape)!=null&&
            Resources.Load<GameObject>(ArmorPrefab(shape))!=null;
        public static bool WedgeAvailable=>ArmorShapeAvailable(R3ArmorShape.Wedge);
        public static GameObject CreateMountingBlock(R3ArmorShape shape)
        {
            if(shape==R3ArmorShape.Cube)return GameObject.CreatePrimitive(PrimitiveType.Cube);
            if(!ArmorShapeAvailable(shape))throw new System.InvalidOperationException("R4 armor geometry is unavailable or invalid: "+shape);
            var mesh=ArmorMesh(shape);var item=new GameObject("R4 "+shape);item.AddComponent<MeshFilter>().sharedMesh=mesh;
            item.AddComponent<MeshRenderer>();var collider=item.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;
            return item;
        }
        public static void ConfigureWheelMount(GameObject block,R3WheelStyle style)
        {
            if(!R3VehicleModel.HasForkMount(style))return;
            var box=block.GetComponent<BoxCollider>();if(box==null)throw new System.InvalidOperationException("Missing wheel mounting collider.");
            // The small fork has a compact upper hub rather than the former
            // full cell cube, which would touch ground before the small tire.
            box.center=new Vector3(0,.38f,0);box.size=new Vector3(.6f,.24f,.6f);
        }
        // The mounting cube remains the collider and damage owner; only its
        // legacy renderer disappears once replacement art is ready.
        public static void HideMountingShell(Transform block,GameObject visual)
        {
            var renderer=block!=null?block.GetComponent<Renderer>():null;
            if(renderer!=null)renderer.enabled=false;
            R4ReplacementDamageVisual.Attach(visual,block);
        }
        // The authored cube replaces appearance only: original HP and collider remain authoritative.
        public static bool TryAttachArmorCube(Transform block)=>TryAttachArmor(block,R3ArmorShape.Cube);
        public static bool TryAttachArmor(Transform block,R3ArmorShape shape)
        {
            if(block==null)return false;
            var renderer=block.GetComponent<Renderer>();var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return false;
            var path=ArmorPrefab(shape);
            if(path==null)return false;
            var prefab=Resources.Load<GameObject>(path);if(prefab==null)return false;
            var visual=Object.Instantiate(prefab,block,false);
            var renderers=visual.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0||visual.GetComponentsInChildren<Collider>(true).Length!=0){Object.Destroy(visual);return false;}
            visual.transform.localScale/=scale.x;
            foreach(var part in renderers)
            {
                var materials=part.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                    if(materials[i]!=null&&materials[i].name.Contains("Armor_Gunmetal"))materials[i]=renderer.sharedMaterial;
                part.sharedMaterials=materials;
            }
            HideMountingShell(block,visual);return true;
        }
        private static GameObject Primitive(PrimitiveType type,Transform parent,Material material,bool solid,string name)
        {
            var item=GameObject.CreatePrimitive(type);item.name=name;item.transform.SetParent(parent,false);
            item.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){var collider=item.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);}
            return item;
        }
        // The enlarged guns scale 2x around the underside of the mounting
        // block. The solid base and physical barrel proxy use that same frame.
        public static void ConfigureGunScale2(GameObject block,Transform pivot,R4WeaponKind weapon)
        {
            if(block==null)return;
            var box=block.GetComponent<BoxCollider>();
            if(box!=null){box.size=new Vector3(1.98f,1.06154f,1.98f);box.center=new Vector3(0,.03077f,0);}
            if(pivot!=null)
            {
                pivot.localPosition=Vector3.up*((.135f*2f+.26f)/BlockSize);pivot.localScale=Vector3.one*2f;
                // Distances measured from the source model's pitch axis to its
                // muzzle in Unity. The pivot's 2x scale supplies the final 2x.
                float sourceLength=weapon==R4WeaponKind.Automatic?.70f:weapon==R4WeaponKind.Heavy?.60f:
                    weapon==R4WeaponKind.RocketLauncher?.382f:weapon==R4WeaponKind.Shotgun?.45f:
                    weapon==R4WeaponKind.Railgun?.84f:weapon==R4WeaponKind.BeamCutter?.44f:.66f;
                float localLength=sourceLength/BlockSize;
                var muzzle=pivot.Find("Wylot");if(muzzle!=null)muzzle.localPosition=Vector3.forward*localLength;
                foreach(Transform barrel in pivot)
                {
                    if(!barrel.name.StartsWith("Lufa",System.StringComparison.Ordinal))continue;
                    if(weapon==R4WeaponKind.EmpEmitter)
                    {
                        foreach(var collider in barrel.GetComponentsInChildren<Collider>())collider.enabled=false;
                        continue;
                    }
                    var position=barrel.localPosition;position.z=localLength*.5f;
                    if(weapon==R4WeaponKind.Automatic)position.x=Mathf.Sign(position.x)*(.06f/BlockSize);
                    barrel.localPosition=position;
                    var size=barrel.localScale;size.z=localLength;barrel.localScale=size;
                }
            }
        }
        public static Transform Payload(Transform block,R3PartKind kind,R4WeaponKind weapon,Material material,bool solid,out Transform muzzle,R3WheelStyle wheelStyle=R3WheelStyle.Offroad,bool mirrorWheel=false)
        {
            muzzle=null;
            if(kind>=R3PartKind.Booster&&kind<=R3PartKind.CrawlerTrack)
            {
                // Geometry belongs to this real HP block and shared body, never an extra weapon.
                var type=kind==R3PartKind.Blink||kind==R3PartKind.Gyroscope?PrimitiveType.Sphere:
                    kind==R3PartKind.CrawlerTrack?PrimitiveType.Cube:PrimitiveType.Cylinder;
                var device=Primitive(type,block,material,solid,"MOV • "+kind).transform;
                device.localPosition=Vector3.zero;device.localScale=Vector3.one*.85f;
                if(kind==R3PartKind.Booster||kind==R3PartKind.MagneticGrapple)
                {
                    device.localRotation=Quaternion.Euler(90,0,0);device.localPosition=Vector3.forward*(kind==R3PartKind.Booster?-.6f:.6f);
                    device.localScale=new Vector3(.65f,.4f,.65f);
                }
                if(kind==R3PartKind.HoverEngine||kind==R3PartKind.JumpDrive)
                {device.localPosition=Vector3.down*.45f;device.localScale=new Vector3(1,.16f,1);}
                if(kind==R3PartKind.CrawlerTrack)
                {device.localPosition=Vector3.down*.55f;device.localScale=new Vector3(.85f,.5f,1.8f);}
                if(kind==R3PartKind.OmniDrive)
                {device.localPosition=Vector3.down*.4f;device.localRotation=Quaternion.Euler(0,0,90);device.localScale=new Vector3(.9f,.18f,.9f);}
                if(kind==R3PartKind.Gyroscope)
                {
                    var ring=Primitive(PrimitiveType.Cylinder,block,material,false,"Pierścień żyroskopu").transform;
                    ring.localScale=new Vector3(1.15f,.05f,1.15f);ring.localRotation=Quaternion.Euler(45,0,20);
                }
                return null;
            }
            if(kind==R3PartKind.Suspension)
            {
                // Decorative piston; the mounting block remains the physical collider.
                var piston=Primitive(PrimitiveType.Cylinder,block,material,false,"Tłok aktywnego zawieszenia").transform;
                piston.localPosition=new Vector3(0,-.55f,0);piston.localScale=new Vector3(.3f,.45f,.3f);
                return null;
            }
            if(kind==R3PartKind.Wheel)
            {
                var wheel=Primitive(PrimitiveType.Cylinder,block,material,solid,"Koło").transform;
                // The supplied side wheel's axle is offset outside its port.
                // Place the physical axle there as well so its actual side socket
                // remains centered against the neighboring block face.
                float axle=R3VehicleModel.WheelAxleOffset(wheelStyle);
                wheel.localPosition=R3VehicleModel.HasForkMount(wheelStyle)?new Vector3(0,-R3VehicleModel.ForkAxleDrop(wheelStyle)/BlockSize,0):
                    new Vector3((mirrorWheel ? -axle : axle)/BlockSize,0,0);
                if(wheelStyle==R3WheelStyle.CasterSlick)wheel.localPosition+=Vector3.forward*(.075f/BlockSize);
                wheel.localRotation=Quaternion.Euler(0,0,90);
                float diameterScale=R3VehicleModel.WheelRadius(wheelStyle)/(.5f*BlockSize);
                wheel.localScale=new Vector3(diameterScale,R3VehicleModel.HasSizedSideWheel(wheelStyle)||R3VehicleModel.HasForkMount(wheelStyle) ? R3VehicleModel.WheelWidth(wheelStyle)/(2*BlockSize) : .35f,diameterScale);
                if(solid){
                    // A short CapsuleCollider becomes a sphere under this axle
                    // scale and penetrates the supporting armor by ~10cm.
                    // Use the existing rendered cylinder as its convex shape.
                    var capsule=wheel.GetComponent<Collider>();capsule.enabled=false;Object.Destroy(capsule);
                    var tire=wheel.gameObject.AddComponent<MeshCollider>();
                    tire.sharedMesh=wheel.GetComponent<MeshFilter>().sharedMesh;tire.convex=true;
                    if(wheelStyle==R3WheelStyle.TwinRoad)
                    {
                        tire.enabled=false;Object.Destroy(tire);
                        // Two independent tread shapes share one wheel module and one R6 drive unit.
                        foreach(float sign in new[]{-1f,1f})
                        {
                            var tread=new GameObject(sign<0?"Lewa opona":"Prawa opona");
                            tread.transform.SetParent(wheel,false);
                            tread.transform.localPosition=Vector3.up*(sign*.09f/(R3VehicleModel.WheelWidth(wheelStyle)*.5f));
                            var shape=tread.AddComponent<MeshCollider>();
                            shape.sharedMesh=wheel.GetComponent<MeshFilter>().sharedMesh;shape.convex=true;
                        }
                    }
                }
                return wheel;
            }
            if(kind!=R3PartKind.Gun)return null;
            var pivot=new GameObject("Obrót broni").transform;pivot.SetParent(block,false);pivot.localPosition=Vector3.up*0.6f;
            if(weapon==R4WeaponKind.RotarySaw)
            {muzzle=new GameObject("Środek piły").transform;muzzle.SetParent(pivot,false);muzzle.localPosition=Vector3.forward*1.65f;SawRotor(muzzle,material);return pivot;}
            bool heavy=weapon==R4WeaponKind.Heavy,automatic=weapon==R4WeaponKind.Automatic,shotgun=weapon==R4WeaponKind.Shotgun;
            var barrel=Primitive(PrimitiveType.Cube,pivot,material,solid,"Lufa").transform;
            barrel.localPosition=Vector3.forward*(heavy?1.1f:0.85f);
            barrel.localScale=heavy?new Vector3(0.48f,0.4f,2):automatic?new Vector3(0.13f,0.13f,1.7f):new Vector3(0.2f,0.2f,1.4f);
            if(automatic||shotgun)
            {
                var second=Object.Instantiate(barrel,pivot);barrel.localPosition+=Vector3.left*0.18f;second.localPosition+=Vector3.right*0.18f;
            }
            muzzle=new GameObject("Wylot").transform;muzzle.SetParent(pivot,false);muzzle.localPosition=Vector3.forward*(heavy?2.2f:automatic?1.8f:1.65f);
            return pivot;
        }
        public static Transform SawRotor(Transform center,Material material)
        {
            var existing=center.Find("WPN005 visual rotor");if(existing!=null)return existing;
            var rotor=Primitive(PrimitiveType.Cylinder,null,material,false,"WPN005 visual rotor").transform;
            rotor.SetPositionAndRotation(center.position,center.rotation*Quaternion.Euler(0,0,90));
            rotor.localScale=new Vector3(1.8f,.14f,1.8f);rotor.SetParent(center,true);return rotor;
        }
    }
    public enum R41GarageTool { Mount, Inspect, Repair, Remove }
    public sealed class R41MissingPlan : MonoBehaviour { public R3PartState Part; }
}
