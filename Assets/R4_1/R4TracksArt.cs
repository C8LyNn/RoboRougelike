using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Samples the supplied visual-only track loop from actual Rigidbody motion.
    [DisallowMultipleComponent]
    public sealed class R4TracksArt : MonoBehaviour
    {
        const float LinkPitchMeters=.0517f;
        GameObject visual;
        AnimationClip travel;
        Rigidbody owner;
        float cycle;
        public static R4TracksArt TryAttach(Transform block,bool mirror)
        {
            if(block==null)return null;
            var old=block.Find("MOV • CrawlerTrack");var renderer=old!=null?old.GetComponent<Renderer>():null;
            var scale=block.lossyScale;
            if(renderer==null||scale.x<=.0001f||float.IsNaN(scale.x)||float.IsInfinity(scale.x)||
               Mathf.Abs(scale.x-scale.y)>.0001f||Mathf.Abs(scale.x-scale.z)>.0001f)return null;
            var catalog=Resources.Load<R4MovementArtCatalog>("R4Art/MovementCatalog");
            var prefab=catalog!=null?catalog.ModelFor(R3PartKind.CrawlerTrack):null;
            if(prefab==null||catalog.CrawlerTravel==null||catalog.CrawlerTravel.length<=0)return null;
            var model=Instantiate(prefab,block,false);
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var socket=Array.Find(nodes,t=>t.name=="Socket_Mount");
            var link=Array.Find(nodes,t=>t.name=="R4M_CrawlerTrack_Link_01");
            if(socket==null||link==null){Destroy(model);return null;}
            model.transform.localScale/=scale.x;
            if(mirror)model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(-1,1,1));
            model.transform.position+=block.TransformPoint(Vector3.up*.5f)-socket.position;
            var view=block.gameObject.AddComponent<R4TracksArt>();view.visual=model;view.travel=catalog.CrawlerTravel;
            view.owner=block.GetComponentInParent<Rigidbody>(true);renderer.enabled=false;return view;
        }
        void LateUpdate()
        {
            if(visual==null||travel==null)return;
            if(owner==null||!transform.IsChildOf(owner.transform))owner=GetComponentInParent<Rigidbody>(true);
            if(owner==null)return;
            float speed=Vector3.Dot(owner.GetPointVelocity(transform.position),transform.forward);
            if(float.IsNaN(speed)||float.IsInfinity(speed))return;
            cycle=Mathf.Repeat(cycle+speed*Time.deltaTime/LinkPitchMeters,1f);
            travel.SampleAnimation(visual,cycle*travel.length);
        }
    }
}
