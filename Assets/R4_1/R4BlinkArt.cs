using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Called only after the existing BlinkDrive reports a successful activation.
    [DisallowMultipleComponent]
    public sealed class R4BlinkArt : MonoBehaviour
    {
        Transform ring,flash;
        float visibleUntil;
        public static R4BlinkArt TryAttach(Transform block)
        {
            if(block==null)return null;
            var old=block.Find("MOV • Blink");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Blink):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var ring=Array.Find(nodes,t=>t.name=="Blink_PhaseRing");
            var flash=Array.Find(nodes,t=>t.name=="Blink_Flash");
            var glow=Array.Find(nodes,t=>t.name=="Blink_ChargeGlow");
            if(socket==null||ring==null||flash==null||glow==null){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            // Blink mounts from the cell's bottom, unlike the downward-facing devices.
            visual.transform.position+=block.TransformPoint(Vector3.down*.5f)-socket.position;
            ring.localScale=Vector3.one;flash.localScale=Vector3.one;
            ring.gameObject.SetActive(false);flash.gameObject.SetActive(false);glow.gameObject.SetActive(false);
            var view=block.gameObject.AddComponent<R4BlinkArt>();view.ring=ring;view.flash=flash;
            renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);return view;
        }
        public void PlayActivation()
        {
            if(!isActiveAndEnabled)return;
            visibleUntil=Time.time+.2f;
            if(ring!=null)ring.gameObject.SetActive(true);
            if(flash!=null)flash.gameObject.SetActive(true);
        }
        void LateUpdate()
        {
            if(Time.time<visibleUntil)return;
            if(ring!=null&&ring.gameObject.activeSelf)ring.gameObject.SetActive(false);
            if(flash!=null&&flash.gameObject.activeSelf)flash.gameObject.SetActive(false);
        }
    }
}
