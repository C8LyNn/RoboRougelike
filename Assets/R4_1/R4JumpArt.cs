using System;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Triggered by the existing activation event or the shared drive group.
    [DisallowMultipleComponent]
    public sealed class R4JumpArt : MonoBehaviour
    {
        Transform ring;
        JumpDrive jump;
        float visibleUntil;

        public static R4JumpArt TryAttach(Transform block)
        {
            if(block==null)return null;
            var old=block.Find("MOV • JumpDrive");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.JumpDrive):null;
            if(prefab==null)return null;
            var visual=Instantiate(prefab,block,false);
            var nodes=visual.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var ring=Array.Find(nodes,t=>t.name=="Jump_BlastRing");
            if(socket==null||ring==null){Destroy(visual);return null;}
            visual.transform.localScale/=scale.x;
            visual.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            ring.localScale=Vector3.one;ring.gameObject.SetActive(false);
            foreach(var node in nodes)if(node.name.StartsWith("Jump_TankGlow_",StringComparison.Ordinal))node.gameObject.SetActive(false);
            var view=block.gameObject.AddComponent<R4JumpArt>();view.ring=ring;renderer.enabled=false;R41PartGeometry.HideMountingShell(block,visual);
            return view;
        }
        void Start(){Bind();}
        void Bind()
        {
            if(jump!=null)return;
            jump=GetComponent<JumpDrive>();
            if(jump!=null)jump.Activated+=OnJump;
        }
        void OnJump(JumpDrive source)
        {
            visibleUntil=Time.time+.2f;
            if(ring!=null)ring.gameObject.SetActive(true);
        }
        public void NotifySharedActivation()=>OnJump(null);
        void LateUpdate()
        {
            if(jump==null)Bind();
            if(ring!=null&&ring.gameObject.activeSelf&&Time.time>=visibleUntil)ring.gameObject.SetActive(false);
        }
        void OnDestroy(){if(jump!=null)jump.Activated-=OnJump;}
    }
}
