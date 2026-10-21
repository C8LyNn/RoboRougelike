using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public static class P12PipelineSafeTracer
    {
        public static void Create(
            Vector3 start,
            Vector3 end,
            Material material)
        {
            if (material == null)
                return;

            Vector3 delta = end - start;
            float length = delta.magnitude;

            if (length <= 0.02f)
                return;

            GameObject tracer =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            tracer.name = "P12_Tracer";

            Collider col =
                tracer.GetComponent<Collider>();

            if (col != null)
                Object.Destroy(col);

            tracer.transform.position =
                start + delta * 0.5f;

            tracer.transform.rotation =
                Quaternion.LookRotation(
                    delta.normalized
                );

            tracer.transform.localScale =
                new Vector3(
                    0.022f,
                    0.022f,
                    length
                );

            tracer.GetComponent<Renderer>()
                .sharedMaterial =
                material;

            Object.Destroy(tracer, 0.065f);
        }
    }
}
