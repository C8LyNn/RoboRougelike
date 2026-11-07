using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Appearance only. The original emitter and rope remain responsible for gameplay.
    public static class R4GrappleArt
    {
        public static bool TryAttach(Transform block)
        {
            if(block==null)return false;
            var old=block.Find("MOV • MagneticGrapple");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return false;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.MagneticGrapple):null;
            if(prefab==null)return false;
            var visual=UnityEngine.Object.Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var muzzle=Array.Find(nodes,t=>t.name=="Grapple_Muzzle");
            var cable=Array.Find(nodes,t=>t.name=="R4M_MagneticGrapple_Cable");
            var glow=Array.Find(nodes,t=>t.name=="Grapple_MagnetGlow");
            if(socket==null||muzzle==null||cable==null||glow==null||
               Vector3.Cross(muzzle.forward,-socket.forward).sqrMagnitude<.0001f){UnityEngine.Object.Destroy(visual);return false;}
            visual.transform.localScale/=scale.x;
            var source=Quaternion.LookRotation(muzzle.forward,-socket.forward);
            var target=Quaternion.LookRotation(block.forward,block.up);
            visual.transform.rotation=target*Quaternion.Inverse(source)*visual.transform.rotation;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            // The game already draws the actual attached rope; keep the FBX cable hidden.
            cable.gameObject.SetActive(false);glow.gameObject.SetActive(false);
            renderer.enabled=false;return true;
        }
    }
}
