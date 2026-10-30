// B01-23 / P17-W08-MEASURED-SUPPORT-001 R1
// Captures ACTUAL Unity Rigidbody mass + worldCenterOfMass and WheelCollider ground-hit points.
// No synthetic ground locations; no Garage, R3 or Codex gameplay mutation.
using System;
using System.Collections.Generic;
using B01.PARTSIZE001.P17Balance;
using UnityEngine;

namespace B01.MeasuredSupport001
{
    public sealed class MeasuredSupportCapture : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] measuredBodies = Array.Empty<Rigidbody>();
        [SerializeField] private WheelCollider[] supportWheels = Array.Empty<WheelCollider>();

        public bool TryCapture(out StabilityResult result, out string failure)
        {
            return TryMeasure(measuredBodies, supportWheels, out result, out failure);
        }

        [ContextMenu("P17 - Capture Actual Wheel Ground Support")]
        private void CaptureFromInspector()
        {
            StabilityResult result;
            string failure;
            if (TryCapture(out result, out failure))
                Debug.Log("P17 measured static margin (m): " +
                          result.SignedEdgeMarginMeters.ToString("F4") +
                          " ideal angle (deg): " +
                          result.IdealTipAngleDegrees.ToString("F2"), this);
            else
                Debug.LogWarning("P17 support NOT_MEASURED: " + failure, this);
        }

        // All samples are WORLD space metres, at the same instant.
        // One Rigidbody may represent the whole compound rig if its mass/CoM
        // are the actual aggregate. Never infer component mass from grid cells.
        public static bool TryMeasure(IReadOnlyList<Rigidbody> bodies,
                                      IReadOnlyList<WheelCollider> wheels,
                                      out StabilityResult result,
                                      out string failure)
        {
            result = null;
            failure = null;
            if (bodies == null || bodies.Count == 0 || bodies.Count > 180)
                return Fail("MISSING_OR_EXCESS_RIGIDBODIES", out failure);
            if (wheels == null || wheels.Count == 0 || wheels.Count > 64)
                return Fail("MISSING_OR_EXCESS_WHEEL_COLLIDERS", out failure);

            var parts = new List<PartMass>(bodies.Count);
            var seenBodies = new HashSet<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                if (body == null || !seenBodies.Add(body))
                    return Fail("MISSING_OR_DUPLICATE_RIGIDBODY", out failure);
                if (!body.gameObject.activeInHierarchy || body.isKinematic)
                    return Fail("BODY_INACTIVE_OR_KINEMATIC", out failure);
                if (float.IsNaN(body.mass) || float.IsInfinity(body.mass) || body.mass <= 0f)
                    return Fail("INVALID_MEASURED_MASS", out failure);

                Vector3 position = body.worldCenterOfMass;
                // Solver validates NaN/Infinity; there is no reference-grid fallback.
                parts.Add(new PartMass("body:" + body.GetInstanceID(),
                                      body.mass,
                                      new PointM(position.x, position.y, position.z)));
            }

            var contacts = new List<GroundContact>(wheels.Count);
            var seenWheels = new HashSet<WheelCollider>();
            foreach (WheelCollider wheel in wheels)
            {
                if (wheel == null || !seenWheels.Add(wheel))
                    return Fail("MISSING_OR_DUPLICATE_WHEEL_COLLIDER", out failure);
                if (!wheel.enabled || !wheel.gameObject.activeInHierarchy)
                    continue;
                WheelHit hit;
                if (!wheel.GetGroundHit(out hit))
                    continue;
                // Actual WheelCollider physics hit; never use wheel.transform.position
                // for an airborne/non-grounded support.
                contacts.Add(new GroundContact("wheel:" + wheel.GetInstanceID(),
                    new PointM(hit.point.x, hit.point.y, hit.point.z)));
            }

            if (contacts.Count < 3)
                return Fail("INSUFFICIENT_ACTUALLY_GROUNDED_WHEELS", out failure);

            try
            {
                result = MassBalanceSolver.Evaluate(parts, contacts);
                return true;
            }
            catch (BalanceInputException error)
            {
                return Fail(error.Code, out failure);
            }
        }

        private static bool Fail(string code, out string failure)
        {
            failure = code;
            return false;
        }
    }
}
