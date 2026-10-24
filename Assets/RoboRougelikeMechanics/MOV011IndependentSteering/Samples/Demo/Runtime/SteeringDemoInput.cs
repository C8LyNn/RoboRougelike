using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering.Demo
{
    /// <summary>Optional legacy-input prototype control; remove from production. Requires legacy Input Manager enabled.</summary>
    [DisallowMultipleComponent]
    public sealed class SteeringDemoInput : MonoBehaviour
    {
        [SerializeField] private IndependentSteeringController steering;
        [SerializeField] private SteeringDemoDrive drive;
        private void Awake()
        {
            if (steering == null) steering=GetComponent<IndependentSteeringController>();
            if (drive == null) drive=GetComponent<SteeringDemoDrive>();
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            if (steering==null || drive==null) return;
            float steer=0f, throttle=0f;
            if (Input.GetKey(KeyCode.A)) steer-=1f;
            if (Input.GetKey(KeyCode.D)) steer+=1f;
            if (Input.GetKey(KeyCode.W)) throttle+=1f;
            if (Input.GetKey(KeyCode.S)) throttle-=1f;
            steering.SetSteeringInput(steer);
            drive.SetThrottle(throttle);
            if (Input.GetKeyDown(KeyCode.Alpha1)) steering.SetMode(SteeringMode.FrontOnly);
            if (Input.GetKeyDown(KeyCode.Alpha2)) steering.SetMode(SteeringMode.RearOnly);
            if (Input.GetKeyDown(KeyCode.Alpha3)) steering.SetMode(SteeringMode.CounterPhase);
            if (Input.GetKeyDown(KeyCode.Alpha4)) steering.SetMode(SteeringMode.Crab);
        }
#endif
    }
}
