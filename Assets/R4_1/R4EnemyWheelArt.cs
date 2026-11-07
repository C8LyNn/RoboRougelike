using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Visual only. The existing damageable cylinder and R6 contacts own physics.
    [DisallowMultipleComponent]
    public sealed class R4EnemyWheelArt : MonoBehaviour
    {
        const float SourceRadius=.39f;
        Transform rotor;
        Renderer source;
        Renderer[] visuals;
        MaterialPropertyBlock tint;
        float radius;

        public static bool TryAttach(Transform wheel,Renderer source,Renderer fallback,float wheelRadius)
        {
            if(wheel==null||wheel.parent==null||source==null||fallback==null||wheelRadius<=0||
               float.IsNaN(wheelRadius)||float.IsInfinity(wheelRadius))return false;
            var parentScale=wheel.parent.lossyScale;var scale=wheel.lossyScale;
            if(!PositiveUniform(parentScale)||!Positive(scale.x)||!Positive(scale.y)||!Positive(scale.z)||
               Quaternion.Angle(wheel.localRotation,Quaternion.Euler(0,0,90))>.01f)return false;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Wheel):null;
            if(prefab==null)return false;
            var art=Instantiate(prefab,wheel,false);
            var nodes=art.GetComponentsInChildren<Transform>(true);
            var rotor=Array.Find(nodes,t=>t.name=="R4M_Wheel_Rotor");
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var renderers=art.GetComponentsInChildren<Renderer>(true);
            if(rotor==null||socket==null||renderers.Length==0||art.GetComponentsInChildren<Collider>(true).Length!=0)
            {Destroy(art);return false;}

            // Enemy wheels are cylinders scaled (.53,.15,.53) and rotated 90°
            // around Z. Cancel the nonuniform axle scale in the rotated basis,
            // keeping the imported wheel in the chassis frame and physical size.
            float desired=wheelRadius*parentScale.x/SourceRadius;
            art.transform.localRotation=Quaternion.Inverse(wheel.localRotation);
            art.transform.localScale=new Vector3(desired/scale.y,desired/scale.x,desired/scale.z);
            art.transform.position+=wheel.position-rotor.position;
            var view=wheel.gameObject.AddComponent<R4EnemyWheelArt>();
            view.rotor=rotor;view.source=source;view.visuals=renderers;view.radius=wheelRadius*parentScale.x;
            view.tint=new MaterialPropertyBlock();
            fallback.enabled=false;
            return true;
        }

        static bool Positive(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f)&&f>.0001f;
        static bool PositiveUniform(Vector3 s)=>Positive(s.x)&&Positive(s.y)&&Positive(s.z)&&
            Mathf.Abs(s.x-s.y)<.0001f&&Mathf.Abs(s.x-s.z)<.0001f;

        void LateUpdate()
        {
            if(source==null||rotor==null)return;
            source.GetPropertyBlock(tint);
            foreach(var item in visuals)if(item!=null)item.SetPropertyBlock(tint);
            var body=GetComponentInParent<Rigidbody>();
            if(body==null)return;
            float speed=Vector3.Dot(body.GetPointVelocity(transform.position),body.transform.forward);
            if(float.IsNaN(speed)||float.IsInfinity(speed))return;
            rotor.Rotate(Vector3.right,speed/radius*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
        }
    }
}
