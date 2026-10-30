using B01.Performance.DebrisSleep001;
using UnityEngine;

namespace B01.Performance.DebrisSleep001.Unity
{
    /// <summary>
    /// Requires deliberate opt-in on NON-GAMEPLAY detached cosmetic debris only.
    /// Never place on player vehicle, weapons, wheels, active modules, vital pieces,
    /// movable cargo, objectives or puzzle physics. No automatic scanning.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("B01/Performance/Optional Cosmetic Debris Sleep")]
    public sealed class B0150OptionalDebrisSleepGovernor : MonoBehaviour
    {
        [SerializeField] private bool allowCosmeticSleep = false; // default OFF: explicit consent
        [SerializeField, Range(8, 600)] private int requiredQuietFixedSteps = 60;
        [SerializeField, Range(0.01f, 10f)] private float maxLinearSpeed = 0.08f;
        [SerializeField, Range(0.01f, 20f)] private float maxAngularSpeed = 0.10f;

        private DebrisSleepPolicy policy;
        private Rigidbody body;
        private bool configured;
        private bool samplingActive;
        private bool retired;
        private int requests;

        /// <summary>Diagnostic counts, NOT proven accepted native sleeps or performance gain.</summary>
        public int SleepRequests { get { return requests; } }

        private void Awake()
        {
            // Consent and validated configuration are frozen once per component lifetime.
            if (!allowCosmeticSleep ||
                requiredQuietFixedSteps < DebrisSleepPolicy.MinQuietSteps ||
                requiredQuietFixedSteps > DebrisSleepPolicy.MaxQuietSteps ||
                float.IsNaN(maxLinearSpeed) || float.IsInfinity(maxLinearSpeed) ||
                maxLinearSpeed <= 0f || maxLinearSpeed > DebrisSleepPolicy.MaxLinearThreshold ||
                float.IsNaN(maxAngularSpeed) || float.IsInfinity(maxAngularSpeed) ||
                maxAngularSpeed <= 0f || maxAngularSpeed > DebrisSleepPolicy.MaxAngularThreshold)
            {
                enabled = false;
                return;
            }

            Rigidbody candidate = GetComponent<Rigidbody>();
            // Attached or locally jointed objects are never disposable free debris.
            if (candidate == null || candidate.isKinematic || !candidate.detectCollisions ||
                GetComponent<Joint>() != null)
            {
                enabled = false;
                return;
            }

            body = candidate;
            policy = new DebrisSleepPolicy(requiredQuietFixedSteps, maxLinearSpeed, maxAngularSpeed);
            configured = true;
        }

        private void OnEnable()
        {
            if (!retired && configured && enabled && allowCosmeticSleep && body != null)
            {
                policy.Reset();
                samplingActive = true;
            }
        }

        private void OnDisable()
        {
            samplingActive = false;
            if (policy != null) policy.Reset();
            // Intentionally no WakeUp() and no change of physics authority on disable.
        }

        private void OnDestroy()
        {
            samplingActive = false;
            retired = true;
            if (policy != null) policy.Reset();
            body = null;
        }

        private void FixedUpdate()
        {
            if (!samplingActive || !configured || retired || !enabled || !allowCosmeticSleep ||
                body == null || body.isKinematic || !body.detectCollisions)
            {
                if (policy != null) policy.Reset();
                return;
            }

            // Speed comparisons use squared magnitudes and require no sqrt/allocations.
            bool request = policy.Step(body.linearVelocity.sqrMagnitude,
                                       body.angularVelocity.sqrMagnitude, body.IsSleeping());
            if (!request) return;
            // Local joints must still fail closed at the actual Sleep boundary.
            // Querying Joint on EVERY FixedUpdate is unnecessary native engine work:
            // we only need the answer immediately before a sleep request.
            if (GetComponent<Joint>() != null)
            {
                policy.Reset();
                return;
            }
            ++requests;
            body.Sleep(); // Single request; native solver can reject/revoke sleep on contacts.
            // If native IsSleeping reports success now, a subsequent wake may start
            // a fresh quiet interval without requiring any external Reset call.
            if (body.IsSleeping()) policy.Reset();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (samplingActive && !retired && policy != null && collision != null) policy.Reset();
        }

        private void OnCollisionStay(Collision collision)
        {
            // Sustained contact with another *physical body* can transfer forces
            // and invalidates quiet eligibility, even if speeds temporarily read 0.
            // Static environment Collider (Collision.body == null) instead supports
            // natural resting debris; resetting every such Stay defeats sleep.
            // Collision.body also covers ArticulationBody, unlike rigidbody alone.
            // Scene code moving a static Collider via Transform is outside this safe
            // scope and must not be treated as validated native gameplay behavior.
            if (samplingActive && configured && !retired && enabled && allowCosmeticSleep &&
                policy != null && collision != null && collision.body != null)
                policy.Reset();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (samplingActive && !retired && policy != null && collision != null) policy.Reset();
        }
    }
}
