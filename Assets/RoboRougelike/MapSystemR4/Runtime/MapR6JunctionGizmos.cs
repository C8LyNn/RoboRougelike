using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Visual ONLY. The reference node pads remain the physical surface until QA approves a
    // unified junction mesh. No phantom colliders or double-overlapping junction road meshes.
    public static class MapR6JunctionGizmos
    {
        public static void Draw(MapR6JunctionPlan plan)
        {
            if (!plan.Valid) return;
            Gizmos.color = Color.green;
            Vector3 previous = plan.Evaluate(0f);
            for (int i = 1; i <= 32; i++)
            {
                Vector3 next = plan.Evaluate(i / 32f);
                Gizmos.DrawLine(previous + Vector3.up * 0.15f, next + Vector3.up * 0.15f);
                previous = next;
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(plan.Start + Vector3.up * 0.15f, 0.55f);
            Gizmos.DrawSphere(plan.End + Vector3.up * 0.15f, 0.55f);
        }
    }
}
