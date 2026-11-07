using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // One owner per lifetime. The cache shares meshes but creates no colliders.
    // Consumers must stop using the returned meshes before the owner disposes it.
    public sealed class R4ColliderMeshCache : IDisposable
    {
        R4ColliderBundle bundle;
        readonly Dictionary<string,ReadOnlyCollection<Mesh>> shapes=new Dictionary<string,ReadOnlyCollection<Mesh>>(StringComparer.Ordinal);
        bool disposed;
        private R4ColliderMeshCache(R4ColliderBundle validated){bundle=validated;}

        public static bool TryOpen(string json,out R4ColliderMeshCache cache,out string error)
        {
            cache=null;
            if(!R4ColliderMeshFactory.TryReadBundle(json,out var bundle,out error))return false;
            cache=new R4ColliderMeshCache(bundle);return true;
        }
        public bool TryGet(string shapeId,out IReadOnlyList<Mesh> meshes,out string error)
        {
            meshes=Array.Empty<Mesh>();error=null;
            if(disposed){error="Collider mesh cache is disposed.";return false;}
            if(string.IsNullOrEmpty(shapeId)){error="Unknown collider shape.";return false;}
            if(shapes.TryGetValue(shapeId,out var cached)){meshes=cached;return true;}
            R4ColliderShape shape=null;
            foreach(var candidate in bundle.shapes)if(candidate.id==shapeId){shape=candidate;break;}
            if(shape==null){error="Unknown collider shape.";return false;}
            if(!R4ColliderMeshFactory.TryBuildShape(shape,out var created,out error))return false;
            cached=Array.AsReadOnly(created);shapes.Add(shapeId,cached);meshes=cached;return true;
        }
        public void Dispose()
        {
            if(disposed)return;
            disposed=true;
            foreach(var group in shapes.Values)foreach(var mesh in group)if(mesh!=null)
            {
                if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);
                else UnityEngine.Object.DestroyImmediate(mesh);
            }
            shapes.Clear();bundle=null;
        }
    }
}
