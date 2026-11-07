using System;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Appearance only: VehicleBooster remains the source of force and activation.
    [DisallowMultipleComponent]
    public sealed class R4BoosterArt : MonoBehaviour
    {
        Transform flame, turbine;
        VehicleBooster booster;

        public static R4BoosterArt TryAttach(Transform block)
        {
            if(block==null)return null;
            var old=block.Find("MOV • Booster");
            var scale=block.lossyScale;
            if(old==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Booster):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var flame=Array.Find(nodes,t=>t.name=="Booster_Flame");
            var turbine=Array.Find(nodes,t=>t.name=="R4M_Booster_Turbine");
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var nozzle=Array.Find(nodes,t=>t.name=="Thruster_Nozzle");
            var oldRenderer=old.GetComponent<Renderer>();
            if(flame==null||turbine==null||socket==null||nozzle==null||oldRenderer==null){Destroy(visual);return null;}
            // FBX import rotates source empty axes: its declared mount normal is
            // -forward here, while nozzle.forward remains the exhaust axis.
            var mountNormal=-socket.forward;
            if(Vector3.Cross(nozzle.forward,mountNormal).sqrMagnitude<.0001f){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            var source=Quaternion.LookRotation(nozzle.forward,mountNormal);
            var target=Quaternion.LookRotation(-block.forward,block.up);
            visual.transform.rotation=target*Quaternion.Inverse(source)*visual.transform.rotation;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            flame.localScale=Vector3.one;
            flame.gameObject.SetActive(false);
            var view=block.gameObject.AddComponent<R4BoosterArt>();view.flame=flame;view.turbine=turbine;
            oldRenderer.enabled=false;
            return view;
        }

        void LateUpdate()
        {
            if(booster==null)booster=GetComponent<VehicleBooster>();
            bool active=booster!=null&&booster.isActiveAndEnabled&&booster.IsOperational&&booster.IsBoosting;
            if(flame!=null&&flame.gameObject.activeSelf!=active)flame.gameObject.SetActive(active);
            if(active&&turbine!=null)turbine.Rotate(Vector3.forward,720f*Time.deltaTime,Space.Self);
        }
    }
}
