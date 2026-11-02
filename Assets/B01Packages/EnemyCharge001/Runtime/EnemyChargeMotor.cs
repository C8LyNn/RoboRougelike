using UnityEngine;

namespace B01.EnemyCharge001
{
    // Game-owned health adapters may implement this; ChargeDemoHealth is a demo-only sink.
    public interface IChargeDamageSink
    {
        void ReceiveChargeDamage(float amount, Vector3 point, GameObject instigator);
    }

    // Optional affiliation adapter for vehicles which do not carry EnemyChargeMotor.
    // If the receiver does not provide affiliation, it must still be the assigned target.
    public interface IChargeTeamIdentity
    {
        int ChargeTeamId { get; }
    }

    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class EnemyChargeMotor : MonoBehaviour
    {
        public enum Phase { Seeking, Telegraph, Charging, Recovery }

        [SerializeField] Transform target;
        [SerializeField] LayerMask obstructionLayers;
        [SerializeField] int teamId = 1;
        [SerializeField, Min(1f)] float range = 12f;
        [SerializeField, Min(.1f)] float minimumDistance = 1.3f;
        [SerializeField, Min(.02f)] float tellTime = .35f, dashTime = .8f, cooldown = 1.2f;
        [SerializeField, Min(.1f)] float acceleration = 48f, maxSpeed = 11f;
        [SerializeField, Min(0f)] float impactDamage = 16f;

        Rigidbody rb;
        Phase phase;
        float timer;
        Vector3 dashDirection;
        bool enabledByAI = true;
        bool dealtDamage;

        public Phase CurrentPhase => phase;
        public int TeamId => teamId;
        public bool HasDealtDamage => dealtDamage;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            phase = Phase.Seeking;
        }

        public void AssignTarget(Transform value) { target = value; }

        public void SetArmed(bool value)
        {
            enabledByAI = value;
            if (!value && phase != Phase.Seeking)
                Recover();
        }

        public void Configure(float tell, float dash, float recover,
                              float accel, float speed, float damage, float detectRange)
        {
            tellTime = Mathf.Max(.02f, tell);
            dashTime = Mathf.Max(.02f, dash);
            cooldown = Mathf.Max(.02f, recover);
            acceleration = Mathf.Max(.1f, accel);
            maxSpeed = Mathf.Max(.1f, speed);
            impactDamage = Mathf.Max(0f, damage);
            range = Mathf.Max(1f, detectRange);
        }

        Vector3 HorizontalToTarget()
        {
            if (!target) return Vector3.zero;
            Vector3 d = target.position - rb.position;
            d.y = 0f;
            return d;
        }

        bool IsFriendly(Transform candidate)
        {
            if (!candidate) return false;
            var motor = candidate.GetComponentInParent<EnemyChargeMotor>();
            if (motor && motor != this && motor.TeamId == teamId) return true;
            var behaviours = candidate.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IChargeTeamIdentity member && member.ChargeTeamId == teamId)
                    return true;
            }
            return false;
        }

        bool CanAcquire()
        {
            if (!enabledByAI || !target || !target.gameObject.activeInHierarchy ||
                !rb || rb.isKinematic) return false;
            if (target == transform || target.IsChildOf(transform) || IsFriendly(target))
                return false;

            float dist = HorizontalToTarget().magnitude;
            if (dist < minimumDistance || dist > range) return false;

            if (obstructionLayers.value != 0)
            {
                Vector3 origin = rb.worldCenterOfMass + Vector3.up * .15f;
                Vector3 d = target.position + Vector3.up * .15f - origin;
                if (Physics.Raycast(origin, d.normalized, out RaycastHit hit, d.magnitude,
                    obstructionLayers, QueryTriggerInteraction.Ignore) &&
                    hit.rigidbody != rb && hit.transform != target &&
                    !hit.transform.IsChildOf(target))
                    return false;
            }
            return true;
        }

        void FixedUpdate()
        {
            if (!rb) return;
            float dt = Time.fixedDeltaTime;
            switch (phase)
            {
                case Phase.Seeking:
                    if (CanAcquire()) { phase = Phase.Telegraph; timer = tellTime; }
                    break;
                case Phase.Telegraph:
                    if (!CanAcquire()) { Recover(); break; }
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        Vector3 d = HorizontalToTarget();
                        if (d.sqrMagnitude < .001f) { Recover(); break; }
                        dashDirection = d.normalized;
                        dealtDamage = false;
                        timer = dashTime;
                        phase = Phase.Charging;
                    }
                    break;
                case Phase.Charging:
                    if (!enabledByAI || !target || IsFriendly(target)) { Recover(); break; }
                    rb.AddForce(dashDirection * acceleration, ForceMode.Acceleration);
                    Vector3 v = rb.linearVelocity;
                    Vector3 h = new Vector3(v.x, 0f, v.z);
                    if (h.sqrMagnitude > maxSpeed * maxSpeed)
                    {
                        h = h.normalized * maxSpeed;
                        rb.linearVelocity = new Vector3(h.x, v.y, h.z);
                    }
                    timer -= dt;
                    if (timer <= 0f) Recover();
                    break;
                case Phase.Recovery:
                    timer -= dt;
                    if (timer <= 0f) phase = Phase.Seeking;
                    break;
            }
        }

        void Recover()
        {
            phase = Phase.Recovery;
            timer = cooldown;
            if (rb)
            {
                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(0f, v.y, 0f);
            }
        }

        void OnCollisionEnter(Collision collision) { HandleChargeContact(collision); }

        // Handles an obstacle already touching the chassis when telegraph ends,
        // or a kinematic/moving obstruction introduced during the charge.
        void OnCollisionStay(Collision collision) { HandleChargeContact(collision); }

        void HandleChargeContact(Collision collision)
        {
            if (phase != Phase.Charging || !enabledByAI || !target ||
                collision.collider == null || collision.rigidbody == rb)
                return;

            Transform struck = collision.collider.transform;
            if (IsFriendly(struck))
            {
                Recover();
                return;
            }

            // A wall, a neutral prop, or a different enemy can stop a charge,
            // but cannot become an accidental damage recipient.
            bool hitAssignedTarget = struck == target || struck.IsChildOf(target);
            if (hitAssignedTarget && !dealtDamage)
            {
                var behaviours = collision.collider.GetComponentsInParent<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is IChargeDamageSink sink)
                    {
                        Vector3 point = collision.contactCount > 0
                            ? collision.GetContact(0).point
                            : collision.collider.ClosestPoint(rb.position);
                        dealtDamage = true; // latch before callback (even if callback re-enters).
                        sink.ReceiveChargeDamage(impactDamage, point, gameObject);
                        break;
                    }
                }
            }
            Recover();
        }

        void OnDisable()
        {
            phase = Phase.Seeking;
            timer = 0f;
            dealtDamage = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
