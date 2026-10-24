using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering
{
    /// <summary>One steering mount. Does not apply forces. Bind to R6's block damage/detach notifications.</summary>
    [DisallowMultipleComponent]
    public sealed class SteeringWheelModule : MonoBehaviour
    {
        [SerializeField] private SteeringWheelPosition position;
        [Tooltip("Optional real WheelCollider. Its steerAngle is driven when assigned.")]
        [SerializeField] private WheelCollider wheelCollider;
        [Tooltip("Optional yaw pivot; should be a dedicated child and NOT the whole vehicle root.")]
        [SerializeField] private Transform visualSteerPivot;
        [SerializeField] private bool operational = true;
        [SerializeField] private bool structurallyAttached = true;
        private Quaternion originalPivotLocalRotation;
        private bool hasPivotBaseline;
        private float currentAngle;

        public SteeringWheelPosition Position => position;
        public bool IsSteerable => isActiveAndEnabled && operational && structurallyAttached;
        public float CurrentAngleDegrees => currentAngle;

        private void Awake() { CachePivot(); }
        private void CachePivot()
        {
            if (visualSteerPivot != null)
            {
                originalPivotLocalRotation = visualSteerPivot.localRotation;
                hasPivotBaseline = true;
            }
            else hasPivotBaseline = false;
        }
        public void Configure(SteeringWheelPosition newPosition, Transform pivot = null, WheelCollider collider = null)
        {
            Restore();
            position = newPosition;
            visualSteerPivot = pivot;
            wheelCollider = collider;
            CachePivot();
        }
        public void SetOperational(bool value) { operational = value; if (!value) Restore(); }
        public void SetStructurallyAttached(bool value) { structurallyAttached = value; if (!value) Restore(); }
        internal void Apply(float angleDegrees)
        {
            if (!IsSteerable) { Restore(); return; }
            currentAngle = SteeringMath.SafeClamp(angleDegrees, -80f, 80f, 0f);
            if (wheelCollider != null) wheelCollider.steerAngle = currentAngle;
            if (visualSteerPivot != null)
            {
                if (!hasPivotBaseline) CachePivot();
                visualSteerPivot.localRotation = originalPivotLocalRotation * Quaternion.Euler(0f, currentAngle, 0f);
            }
        }
        private void Restore()
        {
            currentAngle = 0f;
            if (wheelCollider != null) wheelCollider.steerAngle = 0f;
            if (visualSteerPivot != null && hasPivotBaseline) visualSteerPivot.localRotation = originalPivotLocalRotation;
        }
        private void OnDisable() { Restore(); }
        private void OnDestroy() { Restore(); }
    }
}
