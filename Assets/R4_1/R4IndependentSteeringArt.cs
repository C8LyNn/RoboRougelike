using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Static model only: MOV011 and the existing R6 solver own steering.
    public static class R4IndependentSteeringArt
    {
        public static bool TryAttach(Transform block)
        {
            if(block==null)return false;
            var renderer=block.GetComponent<Renderer>();var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return false;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.IndependentSteering):null;
            if(prefab==null)return false;
            var visual=UnityEngine.Object.Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var knuckle=Array.Find(nodes,t=>t.name=="R4M_IndependentSteering_Knuckle");
            if(socket==null||knuckle==null){UnityEngine.Object.Destroy(visual);return false;}
            visual.transform.localScale/=scale.x;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);return true;
        }
    }
}
