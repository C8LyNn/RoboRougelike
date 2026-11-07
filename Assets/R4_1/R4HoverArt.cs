using System;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // The appearance reads the real pod diagnostics; it never applies lift.
    [DisallowMultipleComponent]
    public sealed class R4HoverArt : MonoBehaviour
    {
        Transform rotor,glow;
        HoverLiftPod pod;
        public static R4HoverArt TryAttach(Transform block)
        {
            if(block==null)return null;
            var old=block.Find("MOV • HoverEngine");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.HoverEngine):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var nozzle=Array.Find(nodes,t=>t.name=="Thruster_Nozzle");
            var rotor=Array.Find(nodes,t=>t.name=="R4M_HoverEngine_Rotor");
            var glow=Array.Find(nodes,t=>t.name=="Hover_ThrustGlow");
            var ring=Array.Find(nodes,t=>t.name=="Hover_PulseRing");
            if(socket==null||nozzle==null||rotor==null||glow==null||ring==null){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            visual.transform.rotation=Quaternion.FromToRotation(-socket.forward,block.up)*visual.transform.rotation;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            // Unity imports this source empty with forward opposite the declared
            // exhaust axis, just as its mount empty uses negative forward for up.
            if(Vector3.Dot(-nozzle.forward,block.up)>-.9f||
               Vector3.Dot(nozzle.position-socket.position,block.up)>=0){Destroy(visual);return null;}
            glow.localScale=Vector3.one;glow.gameObject.SetActive(false);ring.gameObject.SetActive(false);
            var view=block.gameObject.AddComponent<R4HoverArt>();view.rotor=rotor;view.glow=glow;
            renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);return view;
        }
        void LateUpdate()
        {
            if(pod==null)pod=GetComponent<HoverLiftPod>();
            bool lifting=pod!=null&&pod.isActiveAndEnabled&&pod.LastLiftForceNewtons>.001f;
            if(glow!=null&&glow.gameObject.activeSelf!=lifting)glow.gameObject.SetActive(lifting);
            if(lifting&&rotor!=null)rotor.Rotate(Vector3.up,720f*Time.deltaTime,Space.Self);
        }
    }
}
