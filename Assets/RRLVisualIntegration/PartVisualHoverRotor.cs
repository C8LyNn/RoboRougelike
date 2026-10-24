using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV007;

namespace RoboRougelike.VisualIntegration
{
    [DefaultExecutionOrder(710)]
    [DisallowMultipleComponent]
    public sealed class PartVisualHoverRotor : MonoBehaviour
    {
        Transform rotor;
        Transform[] guides;
        Quaternion initialRotor;
        Quaternion[] initialGuides;
        R3MountedPart part;
        HoverLiftPod lift;
        Rigidbody body;
        float rpm;
        float degrees;

        public void Configure(Transform importedModel, R3MountedPart owner)
        {
            part = owner;
            rotor = FindNamed(importedModel, "RotorAssembly");
            guides = new Transform[8];
            initialGuides = new Quaternion[8];
            for (int i = 0; i < 8; i++)
            {
                guides[i] = FindNamed(importedModel, "GuidePivot_" + (i + 1).ToString("D2"));
                if (guides[i] != null) initialGuides[i] = guides[i].localRotation;
            }
            if (rotor == null)
            {
                Debug.LogWarning("[RRL Visual] H01 prefab missing RotorAssembly");
                enabled = false;
                return;
            }
            initialRotor = rotor.localRotation;
            body = GetComponentInParent<Rigidbody>();

            // Imported GLB reference demo clips must not fight runtime telemetry.
            foreach (var animator in importedModel.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            foreach (var animation in importedModel.GetComponentsInChildren<Animation>(true))
                animation.enabled = false;
        }

        static Transform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == name) return item;
            return null;
        }

        public static float NormalizedLift(float forceNewtons, float nominalLoadKg)
        {
            if (nominalLoadKg <= 0f || forceNewtons <= 0f) return 0f;
            return Mathf.Clamp01(forceNewtons / (nominalLoadKg * 9.81f));
        }

        void LateUpdate()
        {
            if (rotor == null) return;
            if (part == null || !part.Operational)
            {
                rpm = 0f;
                return;
            }
            if (body == null) body = GetComponentInParent<Rigidbody>();
            if (lift == null) lift = part.GetComponent<HoverLiftPod>();
            bool active = body != null && lift != null && lift.IsAvailableFor(body);
            float throttle = active ? NormalizedLift(lift.LastLiftForceNewtons, lift.nominalLoadKg) : 0f;
            rpm = Mathf.MoveTowards(rpm, 2600f * throttle, 4500f * Time.deltaTime);
            degrees = Mathf.Repeat(degrees + rpm * 6f * Time.deltaTime, 360f);
            // H01 V4 GLB: rotor around local Y; guide pivots around local X.
            rotor.localRotation = initialRotor * Quaternion.AngleAxis(degrees, Vector3.up);
            for (int i = 0; i < guides.Length; i++)
                if (guides[i] != null)
                    guides[i].localRotation = initialGuides[i] *
                        Quaternion.AngleAxis(throttle * 18f, Vector3.right);
        }
    }
}
