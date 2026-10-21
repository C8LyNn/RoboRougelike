using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    /// <summary>Pure geometry of the R5 roads. World-local coordinates, no PhysX required.</summary>
    public static class MapR5RoadMath
    {
        public static Vector3 Evaluate(Vector3 a, Vector3 b, MapR4EdgeData edge, float t)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            t = Mathf.Clamp01(t);
            Vector3 p;
            if (edge.useCurve)
            {
                float u = 1f - t;
                p = u * u * u * a + 3f * u * u * t * edge.controlA +
                    3f * u * t * t * edge.controlB + t * t * t * b;
            }
            else p = Vector3.LerpUnclamped(a, b, t);
            float eased = t * t * (3f - 2f * t);
            p.y = Mathf.LerpUnclamped(a.y, b.y, eased);
            return p;
        }

        public static Vector3 TangentXZ(Vector3 a, Vector3 b, MapR4EdgeData edge, float t)
        {
            t = Mathf.Clamp01(t);
            Vector3 d;
            if (edge.useCurve)
            {
                float u = 1f - t;
                d = 3f * u * u * (edge.controlA - a) +
                    6f * u * t * (edge.controlB - edge.controlA) +
                    3f * t * t * (b - edge.controlB);
            }
            else d = b - a;
            d.y = 0f;
            if (d.sqrMagnitude < 0.000001f)
            {
                // Degenerate Bezier handle: fall back to finite difference.
                Vector3 front = Evaluate(a, b, edge, Mathf.Min(1f, t + 0.001f));
                Vector3 back = Evaluate(a, b, edge, Mathf.Max(0f, t - 0.001f));
                d = front - back;
                d.y = 0f;
            }
            return d.sqrMagnitude < 0.000001f ? Vector3.zero : d.normalized;
        }

        public static float MeasuredMaxSlopeDeg(Vector3 a, Vector3 b, MapR4EdgeData edge, int samples = 64)
        {
            samples = Mathf.Max(8, samples);
            float result = 0f;
            Vector3 before = Evaluate(a, b, edge, 0f);
            for (int i = 1; i <= samples; i++)
            {
                Vector3 after = Evaluate(a, b, edge, (float)i / samples);
                Vector3 delta = after - before;
                float horizontal = new Vector2(delta.x, delta.z).magnitude;
                if (horizontal < 0.0001f) return 90f;
                result = Mathf.Max(result, Mathf.Atan2(Mathf.Abs(delta.y), horizontal) * Mathf.Rad2Deg);
                before = after;
            }
            return result;
        }

        // Geometric horizontal path curvature only; junction-to-junction vehicle turns
        // must still be validated in a physics/kinematic driving test.
        public static float MeasuredMinRadiusXZ(Vector3 a, Vector3 b, MapR4EdgeData edge, int samples = 64)
        {
            if (!edge.useCurve) return float.PositiveInfinity;
            samples = Mathf.Max(8, samples);
            float minRadius = float.PositiveInfinity;
            Vector3 previous = Evaluate(a, b, edge, 0f);
            Vector3 mid = Evaluate(a, b, edge, 1f / samples);
            for (int i = 2; i <= samples; i++)
            {
                Vector3 next = Evaluate(a, b, edge, (float)i / samples);
                Vector2 p0 = new Vector2(previous.x, previous.z);
                Vector2 p1 = new Vector2(mid.x, mid.z);
                Vector2 p2 = new Vector2(next.x, next.z);
                float ab = Vector2.Distance(p0, p1);
                float bc = Vector2.Distance(p1, p2);
                float ca = Vector2.Distance(p2, p0);
                float doubleArea = Mathf.Abs((p1.x - p0.x) * (p2.y - p0.y) -
                                             (p1.y - p0.y) * (p2.x - p0.x));
                if (doubleArea > 0.000001f && ab * bc * ca > 0.000001f)
                {
                    float radius = ab * bc * ca / (2f * doubleArea);
                    minRadius = Mathf.Min(minRadius, radius);
                }
                previous = mid;
                mid = next;
            }
            return minRadius;
        }

        public static float ApproximateLength(Vector3 a, Vector3 b, MapR4EdgeData edge, int samples = 64)
        {
            float length = 0f;
            Vector3 prev = Evaluate(a, b, edge, 0f);
            for (int i = 1; i <= samples; i++)
            {
                Vector3 point = Evaluate(a, b, edge, (float)i / samples);
                length += Vector3.Distance(prev, point);
                prev = point;
            }
            return length;
        }
    }
}
