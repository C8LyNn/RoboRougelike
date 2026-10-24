using UnityEngine;

namespace RoboRougelike.Mechanics.RAC001
{
    /// <summary>
    /// Standalone demonstration ONLY. Uses real Rigidbody forces rather than
    /// per-frame Transform.MoveTowards; does not control actual game vehicles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointRaceDemoController : MonoBehaviour
    {
        private CheckpointRaceChallenge race;
        private Transform sampleCube;
        private Rigidbody sampleBody;
        private Transform[] points;
        private Vector3 start;
        [SerializeField, Min(0.1f)] private float sampleSpeed = 7f;
        [SerializeField, Min(.1f)] private float acceleration = 12f;
        [SerializeField, Min(.1f)] private float velocityResponse = 4f;

        public void Configure(CheckpointRaceChallenge challenge, Transform cube, Transform[] course)
        {
            race = challenge;
            sampleCube = cube;
            sampleBody = cube != null ? cube.GetComponent<Rigidbody>() : null;
            points = course;
            start = cube != null ? cube.position : Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (race == null || !race.Running || sampleBody == null || points == null) return;
            int next = race.NextCheckpoint;
            if (next < 0 || next >= points.Length || points[next] == null) return;

            Vector3 error = points[next].position - sampleBody.position;
            error.y = 0f;
            Vector3 current = sampleBody.linearVelocity;
            current.y = 0f;
            // Retard speed near checkpoint so a real inertial cube does not
            // oscillate past the target at high speed.
            float goalSpeed = Mathf.Min(sampleSpeed,
                Mathf.Sqrt(2f * Mathf.Max(.1f, acceleration) * error.magnitude));
            Vector3 desired = error.sqrMagnitude < .0001f
                ? Vector3.zero : error.normalized * goalSpeed;
            Vector3 force = Vector3.ClampMagnitude(
                (desired - current) * Mathf.Max(.1f, velocityResponse),
                Mathf.Max(.1f, acceleration));
            sampleBody.AddForce(force, ForceMode.Acceleration);
        }

        private void ResetSample()
        {
            if (sampleBody != null)
            {
                sampleBody.position = start; // explicit RESET only, not traversal
                sampleBody.rotation = Quaternion.identity;
                sampleBody.linearVelocity = Vector3.zero;
                sampleBody.angularVelocity = Vector3.zero;
                Physics.SyncTransforms();
            }
            else if (sampleCube != null) sampleCube.position = start;
        }

        private void OnGUI()
        {
            if (race == null) return;
            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 170f), GUI.skin.box);
            GUILayout.Label("RAC-001 / physical Rigidbody driving demo");
            GUILayout.Label("Phase: " + race.Phase + "   Checkpoint: " + race.NextCheckpoint +
                            "/" + race.CheckpointCount + "   Time: " + race.ElapsedSeconds.ToString("F1"));
            GUILayout.Label("Rigidbody speed: " +
                (sampleBody != null ? sampleBody.linearVelocity.magnitude.ToString("F2") : "NO_RIGIDBODY"));
            if (GUILayout.Button("Start / Restart"))
            {
                ResetSample();
                race.Begin();
            }
            if (GUILayout.Button("Reset"))
            {
                race.ResetRace();
                ResetSample();
            }
            GUILayout.EndArea();
        }
    }
}
