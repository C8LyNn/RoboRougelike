using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.MapSystemR4;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R6
{
    // Opt-in QA instrumentation: never drives, teleports or reconfigures the Rigidbody.
    // The source MAP-R6 package is isolated from R1; this Assembly-CSharp bridge is the
    // only integration point to the real robot. No scene or startup hook installs it.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MapR6ProductionEvidenceBridge : MonoBehaviour
    {
        [SerializeField] private bool keyboardDemandFallback = true;
        private MapR6DriveEvidenceRecorder recorder;
        private float externalDemand;
        private bool hasExternalDemand;

        public MapR6DriveEvidenceRecorder Recorder => recorder;

        private void Awake()
        {
            recorder = GetComponent<MapR6DriveEvidenceRecorder>();
            if (recorder == null)
                recorder = gameObject.AddComponent<MapR6DriveEvidenceRecorder>();
            recorder.SetMapState("live_vehicle_unverified_route");
        }

        // Preferred source: a real movement controller can feed its resolved command.
        // Does not modify production controller inputs or physical motion.
        public void SetExternalDemand01(float demand)
        {
            externalDemand = Mathf.Clamp01(demand);
            hasExternalDemand = true;
        }

        public void ClearExternalDemand() { hasExternalDemand = false; externalDemand = 0f; }

        private void Update()
        {
            if (recorder == null) return;
            var run = R1RunController.Instance;
            bool active = run != null && run.Player != null &&
                run.Player.gameObject == gameObject &&
                !run.Transitioning && !run.Paused &&
                (run.Progress?.State == R1RunState.Combat ||
                 run.Progress?.State == R1RunState.TestDrive);

            float demand = 0f;
            if (active)
            {
                demand = hasExternalDemand ? externalDemand :
                    keyboardDemandFallback ? KeyboardThrottleDemand() : 0f;
                recorder.SetMapState(run.Progress.State == R1RunState.Combat ?
                    run.RoadState!=null?"MAP-R6/"+run.RoadState.StateId:run.CheckpointRace!=null?"RAC001/generated_course":"gauntlet_existing_arena" : "practice_range");
            }
            recorder.SetDriverDemand01(demand);
        }

        private static float KeyboardThrottleDemand()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0f;
            return (keyboard.wKey.isPressed || keyboard.sKey.isPressed ||
                    keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Mathf.Abs(Input.GetAxisRaw("Vertical"));
#else
            return 0f;
#endif
        }

        private void OnDisable()
        {
            if (recorder != null) recorder.SetDriverDemand01(0f);
        }
    }
}
