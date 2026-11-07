using System;
using System.Linq;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Cosmetic motion inferred from Rigidbody velocity; no forces or contacts here.
    [DisallowMultipleComponent]
    public sealed class R4OmniArt : MonoBehaviour
    {
        Transform hub;
        Transform[] rollers;
        Rigidbody owner;
        float radius,sideSign;
        public static R4OmniArt TryAttach(Transform block,bool mirror)
        {
            if(block==null)return null;
            var old=block.Find("MOV • OmniDrive");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.OmniDrive):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var hub=Array.Find(nodes,t=>t.name=="R4M_OmniDrive_Hub");
            var probe=Array.Find(nodes,t=>t.name=="GroundProbe");
            var rollers=nodes.Where(t=>t.name.StartsWith("R4M_OmniDrive_Roller_",StringComparison.Ordinal)).ToArray();
            if(socket==null||hub==null||probe==null||rollers.Length!=10){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            if(mirror)visual.transform.localScale=Vector3.Scale(visual.transform.localScale,new Vector3(-1,1,1));
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            var view=block.gameObject.AddComponent<R4OmniArt>();view.hub=hub;view.rollers=rollers;
            view.radius=Mathf.Max(.05f,Mathf.Abs(Vector3.Dot(probe.position-hub.position,block.up)));
            view.sideSign=mirror?-1:1;view.owner=block.GetComponentInParent<Rigidbody>(true);
            renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);return view;
        }
        void LateUpdate()
        {
            if(hub==null)return;
            if(owner==null||!transform.IsChildOf(owner.transform))owner=GetComponentInParent<Rigidbody>(true);
            if(owner==null)return;
            var velocity=owner.GetPointVelocity(hub.position);
            float forward=Vector3.Dot(velocity,transform.forward),side=Vector3.Dot(velocity,transform.right);
            if(float.IsNaN(forward)||float.IsInfinity(forward)||float.IsNaN(side)||float.IsInfinity(side))return;
            hub.Rotate(Vector3.right,sideSign*forward/radius*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
            foreach(var roller in rollers)if(roller!=null)roller.Rotate(Vector3.right,sideSign*side/radius*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
        }
    }
}
