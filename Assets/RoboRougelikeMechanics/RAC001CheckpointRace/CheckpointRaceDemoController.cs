using UnityEngine;

namespace RoboRougelike.Mechanics.RAC001
{
    /// <summary>Standalone sample autopilot; only moves its own demo cube.</summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointRaceDemoController : MonoBehaviour
    {
        private CheckpointRaceChallenge race;
        private Transform sampleCube;
        private Transform[] points;
        private Vector3 start;
        [SerializeField, Min(0.1f)] private float sampleSpeed = 7f;

        public void Configure(CheckpointRaceChallenge challenge, Transform cube, Transform[] course)
        {
            race = challenge;
            sampleCube = cube;
            points = course;
            start = cube != null ? cube.position : Vector3.zero;
        }

        private void Update()
        {
            if (race == null || !race.Running || sampleCube == null || points == null) return;
            int next = race.NextCheckpoint;
            if (next >= 0 && next < points.Length && points[next] != null)
                sampleCube.position = Vector3.MoveTowards(sampleCube.position, points[next].position,
                    sampleSpeed * Time.deltaTime);
        }

        private void OnGUI()
        {
            if (race == null) return;
            GUILayout.BeginArea(new Rect(12f, 12f, 340f, 145f), GUI.skin.box);
            GUILayout.Label("RAC-001 • standalone checkpoint race");
            GUILayout.Label("Phase: " + race.Phase + "   Checkpoint: " + race.NextCheckpoint +
                            "/" + race.CheckpointCount + "   Time: " + race.ElapsedSeconds.ToString("F1"));
            if (GUILayout.Button("Start / Restart"))
            {
                if (sampleCube != null) sampleCube.position = start;
                race.Begin();
            }
            if (GUILayout.Button("Reset"))
            {
                race.ResetRace();
                if (sampleCube != null) sampleCube.position = start;
            }
            GUILayout.EndArea();
        }
    }
}

