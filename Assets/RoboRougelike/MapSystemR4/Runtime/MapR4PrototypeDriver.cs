using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Test-only rover to traverse a blockout. NEVER attach to production vehicle.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MapR4PrototypeDriver : MonoBehaviour
    {
        public float forwardAcceleration = 25f;
        public float reverseAcceleration = 14f;
        public float turnSpeedDeg = 115f;
        public float maxSpeed = 14f;
        private Rigidbody _body;
        private MapR6DriveEvidenceRecorder _evidence;
        private float _throttle;
        private float _steering;

        private void Awake() { _body = GetComponent<Rigidbody>(); _evidence = GetComponent<MapR6DriveEvidenceRecorder>(); }
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                _throttle = 0f;
                _steering = 0f;
            }
            else
            {
                _throttle = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                _steering = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            _throttle = Input.GetAxisRaw("Vertical");
            _steering = Input.GetAxisRaw("Horizontal");
#else
            _throttle = 0f;
            _steering = 0f;
#endif
        }
        private void FixedUpdate()
        {
            if (_body == null) return;
            if (_evidence != null) _evidence.SetDriverDemand01(Mathf.Abs(_throttle));
            var lateral = new Vector3(_body.linearVelocity.x, 0f, _body.linearVelocity.z);
            if (lateral.magnitude < maxSpeed || Vector3.Dot(lateral, transform.forward) * _throttle < 0f)
                _body.AddForce(transform.forward * _throttle * (_throttle > 0 ? forwardAcceleration : reverseAcceleration), ForceMode.Acceleration);
            var angle = _steering * turnSpeedDeg * Time.fixedDeltaTime;
            _body.MoveRotation(_body.rotation * Quaternion.Euler(0, angle, 0));
        }
    }
}
