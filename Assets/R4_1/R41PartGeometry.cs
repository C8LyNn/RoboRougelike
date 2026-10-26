using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.R4_1
{
    // Both the actual assembly and its ghost use these exact local transforms.
    public static class R41PartGeometry
    {
        public const float Grid=0.55f, BlockSize=0.52f;
        private static GameObject Primitive(PrimitiveType type,Transform parent,Material material,bool solid,string name)
        {
            var item=GameObject.CreatePrimitive(type);item.name=name;item.transform.SetParent(parent,false);
            item.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){var collider=item.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);}
            return item;
        }
        public static Transform Payload(Transform block,R3PartKind kind,R4WeaponKind weapon,Material material,bool solid,out Transform muzzle)
        {
            muzzle=null;
            if(kind>=R3PartKind.Booster)
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
                wheel.localPosition=new Vector3(0,-0.5f,0);wheel.localRotation=Quaternion.Euler(0,0,90);
                wheel.localScale=new Vector3(1.5f,0.35f,1.5f);
                if(solid){
                    // A short CapsuleCollider becomes a sphere under this axle
                    // scale and penetrates the supporting armor by ~10cm.
                    // Use the existing rendered cylinder as its convex shape.
                    var capsule=wheel.GetComponent<Collider>();capsule.enabled=false;Object.Destroy(capsule);
                    var tire=wheel.gameObject.AddComponent<MeshCollider>();
                    tire.sharedMesh=wheel.GetComponent<MeshFilter>().sharedMesh;tire.convex=true;
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
