using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Purely visual: R6WheelContacts still reads the original tire collider.
    [DisallowMultipleComponent]
    public sealed class R4WheelArt : MonoBehaviour
    {
        const float RadiusMeters=.39f;
        Transform rotor;
        Rigidbody owner;

        public static R4WheelArt TryAttach(Transform block,Transform physicalWheel)
        {
            if(block==null||physicalWheel==null)return null;
            var scale=block.lossyScale;
            if(float.IsNaN(scale.x)||float.IsInfinity(scale.x)||float.IsNaN(scale.y)||float.IsInfinity(scale.y)||
                float.IsNaN(scale.z)||float.IsInfinity(scale.z)||scale.x<=.0001f||
                Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Wheel):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var spinning=Array.Find(visual.GetComponentsInChildren<Transform>(true),t=>t.name=="R4M_Wheel_Rotor");
            var socket=Array.Find(visual.GetComponentsInChildren<Transform>(true),t=>t.name=="Socket_Mount");
            var renderer=physicalWheel.GetComponent<Renderer>();
            if(spinning==null||socket==null||renderer==null){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            var view=block.gameObject.AddComponent<R4WheelArt>();view.rotor=spinning;
            view.owner=block.GetComponentInParent<Rigidbody>(true);renderer.enabled=false;
            return view;
        }

        void LateUpdate()
        {
            if(rotor==null)return;
            if(owner==null||!transform.IsChildOf(owner.transform))owner=GetComponentInParent<Rigidbody>(true);
            if(owner==null)return;
            var direction=Vector3.Cross(rotor.right,transform.up);
            if(direction.sqrMagnitude<.0001f)return;
            float speed=Vector3.Dot(owner.GetPointVelocity(rotor.position),direction.normalized);
            if(float.IsNaN(speed)||float.IsInfinity(speed))return;
            rotor.Rotate(Vector3.right,speed/RadiusMeters*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
        }
    }
}
