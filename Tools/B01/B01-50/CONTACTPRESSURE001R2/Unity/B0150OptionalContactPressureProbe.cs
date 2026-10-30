using B01.Performance.ContactPressure001;
using UnityEngine;

namespace B01.Performance.ContactPressure001.Unity
{
    /// <summary>
    /// Opt-in observational component for a vehicle part with a Unity Collider/Rigidbody.
    /// Does not change collision responses, forces, materials, timers or other game systems.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("B01/Performance/Optional Contact Pressure Probe")]
    public sealed class B0150OptionalContactPressureProbe : MonoBehaviour
    {
        [SerializeField, Range(4, 512)] private int historyFixedTicks = 120;
        private ContactPressureMeter meter;
        private long tickId;
        // Keep the observation lease explicitly bounded by OnEnable/OnDisable/OnDestroy.
        // Unity can dispatch collision messages to disabled MonoBehaviours; presence of meter
        // is not evidence that the receiver opted in to keep sampling.
        private bool samplingActive;
        private bool permanentlyDestroyed;

        private void Awake()
        {
            if (historyFixedTicks < ContactPressureMeter.MinHistoryTicks ||
                historyFixedTicks > ContactPressureMeter.MaxHistoryTicks)
            {
                Debug.LogError("B01-50 contact probe history outside 4..512 ticks; disabled", this);
                enabled = false;
                return;
            }
            meter = new ContactPressureMeter(historyFixedTicks);
        }

        private void OnEnable()
        {
            if (permanentlyDestroyed || !enabled || meter == null) return;
            // An explicit new observation interval; inactive history cannot leak across runs.
            meter.Reset();
            tickId = 0;
            samplingActive = true;
        }

        private void OnDisable()
        {
            samplingActive = false;
            if (meter != null) meter.Reset();
            tickId = 0;
        }

        private void OnDestroy()
        {
            samplingActive = false;
            permanentlyDestroyed = true;
            if (meter != null) meter.Reset();
            meter = null;
            tickId = 0;
        }

        private void FixedUpdate()
        {
            if (!samplingActive || !enabled || permanentlyDestroyed || meter == null || tickId == long.MaxValue) return;
            meter.BeginTick(++tickId);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (samplingActive && enabled && !permanentlyDestroyed && meter != null && collision != null)
                meter.TryRecord(ContactKind.Enter, collision.contactCount);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (samplingActive && enabled && !permanentlyDestroyed && meter != null && collision != null)
                meter.TryRecord(ContactKind.Stay, collision.contactCount);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (samplingActive && enabled && !permanentlyDestroyed && meter != null && collision != null)
                meter.TryRecord(ContactKind.Exit, collision.contactCount);
        }

        /// <summary>
        /// Receiver may poll this on the main thread. No implicit OnGUI, Debug.Log spam, or save writes.
        /// False before first observed FixedUpdate; returned report is a value copy.
        /// </summary>
        public bool TryCapture(out ContactPressureSnapshot snapshot)
        {
            if (!samplingActive || !enabled || permanentlyDestroyed || meter == null || meter.ObservedTicks == 0)
            {
                snapshot = default(ContactPressureSnapshot);
                return false;
            }
            snapshot = meter.Snapshot();
            return true;
        }
    }
}
