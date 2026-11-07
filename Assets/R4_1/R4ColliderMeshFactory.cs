using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    // Explicit, opt-in construction only. This does not attach colliders or alter gameplay.
    // The caller owns the returned Mesh objects and must destroy them when finished.
    public static class R4ColliderMeshFactory
    {
        public static bool TryCreate(string json,string shapeId,out Mesh[] meshes,out string error)
        {
            meshes=Array.Empty<Mesh>();error=null;
            if(!R4ColliderBundle.IsTrustedSource(json)){error="Untrusted collider bundle.";return false;}
            var created=new List<Mesh>();
            try
            {
                // Deserialize the exact text that was just hashed; never trust a
                // separately supplied or previously mutable parsed object.
                var bundle=JsonUtility.FromJson<R4ColliderBundle>(json);
                if(bundle==null){error="Collider bundle could not be parsed.";return false;}
                if(!bundle.Validate(out error))return false;
                R4ColliderShape shape=null;
                foreach(var candidate in bundle.shapes)if(candidate.id==shapeId){shape=candidate;break;}
                if(shape==null){error="Unknown collider shape.";return false;}
                foreach(var piece in shape.pieces)
                    if(piece.positions.Length/3>65535){error="Collider mesh exceeds supported vertex count.";return false;}
                foreach(var piece in shape.pieces)
                {
                    var mesh=new Mesh();created.Add(mesh);mesh.name=piece.name;
                    var vertices=new Vector3[piece.positions.Length/3];
                    for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(piece.positions[i*3],piece.positions[i*3+1],piece.positions[i*3+2]);
                    mesh.vertices=vertices;mesh.triangles=(int[])piece.indices.Clone();mesh.RecalculateBounds();
                }
                meshes=created.ToArray();return true;
            }
            catch(Exception exception)
            {
                foreach(var mesh in created)if(mesh!=null)
                {
                    if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);
                    else UnityEngine.Object.DestroyImmediate(mesh);
                }
                error="Collider mesh creation failed: "+exception.Message;return false;
            }
        }
    }
}
