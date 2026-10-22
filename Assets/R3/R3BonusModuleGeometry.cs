using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    /// <summary>
    /// One damageable/mesh/collider spanning four cells on the horizontal XZ plane.
    /// Three non-colliding structural sockets expose the footprint to the existing P12
    /// flood-fill without creating extra inventory items or separate destructible panels.
    /// </summary>
    public static class R3BonusModuleGeometry
    {
        public const float Grid=0.55f;
        public const float Height=0.52f;
        public const float Width=Grid*2f-0.03f;
        private static Mesh sharedShape;
        public static void ConfigureCube(GameObject cube,bool solid)
        {
            if(cube==null)return;
            var filter=cube.GetComponent<MeshFilter>();
            if(filter!=null)
            {
                if(sharedShape==null)
                {
                    sharedShape=Object.Instantiate(filter.sharedMesh);
                    sharedShape.name="Bonus 1x2x2 mesh (single physical part)";
                    var vertices=sharedShape.vertices;
                    for(int i=0;i<vertices.Length;i++)
                        vertices[i]=new Vector3(vertices[i].x*Width+Grid*.5f,
                            vertices[i].y*Height,vertices[i].z*Width+Grid*.5f);
                    sharedShape.vertices=vertices;
                    sharedShape.RecalculateBounds();
                }
                filter.sharedMesh=sharedShape;
            }
            cube.transform.localScale=Vector3.one;
            var box=cube.GetComponent<BoxCollider>();
            if(box!=null)
            {
                box.center=new Vector3(Grid*.5f,0,Grid*.5f);
                box.size=new Vector3(Width,Height,Width);
                box.enabled=solid;
            }
        }
        public static void AddStructuralSockets(Transform anchor)
        {
            var offsets=new[]{Vector3Int.right,Vector3Int.forward,
                Vector3Int.right+Vector3Int.forward};
            foreach(var offset in offsets)
            {
                var socket=new GameObject("Bonus footprint socket "+offset);
                socket.transform.SetParent(anchor,false);
                socket.transform.localPosition=(Vector3)offset*Grid;
                // Every socket is part of ONE physical bonus. It has no renderer
                // or collider, so projectiles cannot hit it separately.
                var guard=socket.AddComponent<P12PipelineSafeDamageable>();
                guard.Configure(100000f,false);
                socket.AddComponent<P12_PipelineSafePlayerBlock>();
            }
        }
    }
}
