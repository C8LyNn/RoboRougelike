using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    /// <summary>Produces a closed static road prism with a smooth Y profile.</summary>
    public static class MapR5RoadMesh
    {
        public static Mesh Build(Vector3 a, Vector3 b, MapR4EdgeData edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            if (edge.width <= 0f || edge.roadThickness <= 0f)
                throw new ArgumentException("Road width and thickness must be positive");
            if (edge.roadSegments < 2 || edge.roadSegments > 256)
                throw new ArgumentException("R5 roadSegments must be between 2 and 256");
            int count = edge.roadSegments + 1;
            var vertices = new Vector3[4 * count]; // top left/right; bottom left/right
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / edge.roadSegments;
                var center = MapR5RoadMath.Evaluate(a, b, edge, t);
                var forward = MapR5RoadMath.TangentXZ(a, b, edge, t);
                if (forward.sqrMagnitude < 0.5f)
                    throw new ArgumentException("Road curve has a degenerate horizontal tangent: " + edge.id);
                var right = Vector3.Cross(Vector3.up, forward) * edge.width * 0.5f;
                vertices[2 * i] = center - right;
                vertices[2 * i + 1] = center + right;
                vertices[2 * count + 2 * i] = center - right - Vector3.up * edge.roadThickness;
                vertices[2 * count + 2 * i + 1] = center + right - Vector3.up * edge.roadThickness;
            }
            var triangles = new int[edge.roadSegments * 24 + 12];
            int k = 0;
            for (int i = 0; i < edge.roadSegments; i++)
            {
                int l = 2 * i, r = l + 1, nl = l + 2, nr = l + 3;
                int bl = 2 * count + l, br = bl + 1, bnl = bl + 2, bnr = bl + 3;
                // Upper face: +Y normal when the centerline moves forward.
                Add(triangles, ref k, l, nl, r); Add(triangles, ref k, r, nl, nr);
                // Lower face.
                Add(triangles, ref k, bl, br, bnl); Add(triangles, ref k, br, bnr, bnl);
                // Side faces outward.
                Add(triangles, ref k, l, bl, nl); Add(triangles, ref k, nl, bl, bnl);
                Add(triangles, ref k, r, nr, br); Add(triangles, ref k, nr, bnr, br);
            }
            // Close end caps to make a non-convex static collider manifold.
            int bottom = 2 * count;
            Add(triangles, ref k, 0, 1, bottom); Add(triangles, ref k, 1, bottom + 1, bottom);
            int last = 2 * (count - 1);
            Add(triangles, ref k, last, bottom + last, last + 1);
            Add(triangles, ref k, last + 1, bottom + last, bottom + last + 1);
            var mesh = new Mesh { name = "R5_Road_" + edge.id };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Add(int[] a, ref int k, int x, int y, int z)
        {
            a[k++] = x; a[k++] = y; a[k++] = z;
        }
    }
}
