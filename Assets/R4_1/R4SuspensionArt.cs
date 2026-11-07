using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // The model is decorative; MOV009 owns support, force and damage state.
    public static class R4SuspensionArt
    {
        public static bool TryAttach(Transform block)
        {
            if(block==null)return false;
            var piston=block.Find("Tłok aktywnego zawieszenia");
            var oldRenderer=piston!=null?piston.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(oldRenderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return false;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.Suspension):null;
            if(prefab==null)return false;
            var visual=UnityEngine.Object.Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var arm=Array.Find(nodes,t=>t.name=="R4M_Suspension_Arm");
            if(socket==null||arm==null){UnityEngine.Object.Destroy(visual);return false;}
            visual.transform.localScale/=scale.x;
            visual.transform.position+=block.position-socket.position;
            oldRenderer.enabled=false;
            return true;
        }
    }
}
