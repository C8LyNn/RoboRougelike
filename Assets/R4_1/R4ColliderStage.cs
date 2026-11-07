using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Explicit staging only: all new colliders are disabled, and existing ones
    // are never changed. Dispose the stage before disposing its mesh cache.
    public sealed class R4ColliderStage : IDisposable
    {
        readonly GameObject[] children;
        public ReadOnlyCollection<MeshCollider> Colliders {get;private set;}
        public bool IsDisposed {get;private set;}
        private R4ColliderStage(List<GameObject> objects,List<MeshCollider> colliders)
        {children=objects.ToArray();Colliders=Array.AsReadOnly(colliders.ToArray());}
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static void Release(GameObject value)
        {
            if(value==null)return;
            if(Application.isPlaying)UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        public static bool TryCreate(GameObject owner,R4ColliderMeshCache cache,string shapeId,out R4ColliderStage stage,out string error)
        {
            stage=null;error=null;
            if(owner==null||cache==null){error="Missing collider stage owner or cache.";return false;}
            var scale=owner.transform.lossyScale;
            if(!Finite(scale.x)||!Finite(scale.y)||!Finite(scale.z)||scale.x<=.0001f||
               Math.Abs(scale.x-scale.y)>.0001f||Math.Abs(scale.x-scale.z)>.0001f)
            {error="Collider stage requires a positive uniform scale.";return false;}
            if(!cache.TryGet(shapeId,out var meshes,out error))return false;
            var objects=new List<GameObject>();var colliders=new List<MeshCollider>();
            try
            {
                foreach(var mesh in meshes)
                {
                    if(mesh==null)throw new InvalidOperationException("Missing shared mesh.");
                    var child=new GameObject("R4 collider "+mesh.name);objects.Add(child);
                    child.layer=owner.layer;child.transform.SetParent(owner.transform,false);
                    child.transform.localPosition=Vector3.zero;child.transform.localRotation=Quaternion.identity;
                    float inverse=1f/scale.x;child.transform.localScale=new Vector3(inverse,inverse,inverse);
                    var collider=child.AddComponent<MeshCollider>();collider.enabled=false;
                    collider.convex=true;collider.sharedMesh=mesh;colliders.Add(collider);
                }
                stage=new R4ColliderStage(objects,colliders);return true;
            }
            catch(Exception exception)
            {
                foreach(var collider in colliders)if(collider!=null)collider.enabled=false;
                foreach(var child in objects)Release(child);
                error="Collider staging failed: "+exception.Message;return false;
            }
        }
        public void Dispose()
        {
            if(IsDisposed)return;
            IsDisposed=true;
            foreach(var collider in Colliders)if(collider!=null)collider.enabled=false;
            foreach(var child in children)Release(child);
        }
    }
}
