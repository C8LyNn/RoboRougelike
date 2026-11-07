using System;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Visual rotor only; the existing GyroStabilizer remains the physics owner.
    [DisallowMultipleComponent]
    public sealed class R4GyroArt : MonoBehaviour
    {
        Transform rotor;
        GyroStabilizer gyro;

        public static R4GyroArt TryAttach(Transform block)
        {
            if(block==null)return null;
            var body=block.Find("MOV • Gyroscope");var ring=block.Find("Pierścień żyroskopu");
            var scale=block.lossyScale;
            if(body==null||ring==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Gyroscope):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var rotor=Array.Find(nodes,t=>t.name=="R4M_Gyroscope_Rotor");
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var bodyRenderer=body.GetComponent<Renderer>();var ringRenderer=ring.GetComponent<Renderer>();
            if(rotor==null||socket==null||bodyRenderer==null||ringRenderer==null){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            var view=block.gameObject.AddComponent<R4GyroArt>();view.rotor=rotor;
            bodyRenderer.enabled=false;ringRenderer.enabled=false;
            return view;
        }

        void LateUpdate()
        {
            if(gyro==null)gyro=GetComponent<GyroStabilizer>();
            if(rotor!=null&&gyro!=null&&gyro.isActiveAndEnabled&&gyro.IsOperational&&gyro.IsRequestedActive)
                rotor.Rotate(Vector3.up,540f*Time.deltaTime,Space.Self);
        }
    }
}
