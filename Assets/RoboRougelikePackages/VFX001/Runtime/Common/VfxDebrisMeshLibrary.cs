using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Shared tetrahedron shards. Cosmetic mesh particles, NOT physics colliders or detached game parts.</summary>
    public static class VfxDebrisMeshLibrary
    {
        private static Mesh _metal, _rubber;
        public static Mesh ForMaterial(ImpactMaterialType type)
        {
            if (type == ImpactMaterialType.Rubber)
            {
                if (_rubber == null) _rubber = Build(.65f, .75f, "VFX001 Rubber Chunk");
                return _rubber;
            }
            if (_metal == null) _metal = Build(1.45f, .35f, "VFX001 Metal Shard");
            return _metal;
        }
        private static Mesh Build(float length, float width, string name)
        {
            var m = new Mesh { name=name, hideFlags=HideFlags.DontSave };
            m.vertices = new[] {
                new Vector3(0f,0f,length*.5f), new Vector3(-width*.5f,-width*.35f,-length*.5f),
                new Vector3(width*.5f,-width*.35f,-length*.5f), new Vector3(0f,width*.75f,-length*.32f)
            };
            m.uv = new[] { Vector2.up, Vector2.zero, Vector2.right, new Vector2(.5f,.5f) };
            m.triangles = new[] {0,1,2, 0,2,3, 0,3,1, 1,3,2};
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
