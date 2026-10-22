using UnityEngine;
using System.Linq;

namespace RoboRougelike.R3
{
    /// <summary>
    /// One damageable/mesh/collider spanning four cells on the horizontal XZ plane.
    /// All four grid cells resolve to the same physical owner in the P12 graph.
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
        public static void AddStructuralSockets(Transform anchor,R3PartState state)
        {
            // Retain the W3 assembly entry point, but never create helper blocks.
            anchor.GetComponent<R3BlockMountPoints>().SetFootprint(
                R3VehicleModel.OccupiedCells(state).Select(cell=>cell-state.Cell));
        }
    }
}
