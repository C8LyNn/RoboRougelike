using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Pure aggregation; does not move the vehicle and never claims driving success on its own.
    public sealed class MapR6DriveEvidence
    {
        public float ElapsedSeconds { get; private set; }
        public float DrivenDistanceMeters { get; private set; }
        public float RequestedMotionSeconds { get; private set; }
        public float MaxTiltDegrees { get; private set; }
        public int StuckEvents { get; private set; }
        public bool HasMovementSamples { get; private set; }
        public float StuckAfterSeconds { get; set; } = 3.5f;
        public float MinimumProgressMetersPerSecond { get; set; } = 0.15f;
        public float MinDriverDemand01 { get; set; } = 0.3f;
        private Vector3 _previous;
        private bool _hasPrevious;
        private bool _stuckReported;
        private float _noProgressSeconds;

        public void Sample(Vector3 worldPosition, Vector3 bodyUp, float deltaSeconds, float driverDemand01)
        {
            if (deltaSeconds <= 0f || float.IsNaN(deltaSeconds)) return;
            driverDemand01 = Mathf.Clamp01(driverDemand01);
            float displacement = _hasPrevious ? Vector3.Distance(worldPosition, _previous) : 0f;
            _previous = worldPosition;
            HasMovementSamples |= _hasPrevious;
            _hasPrevious = true;
            ElapsedSeconds += deltaSeconds;
            DrivenDistanceMeters += displacement;
            if (bodyUp.sqrMagnitude > 0.001f)
                MaxTiltDegrees = Mathf.Max(MaxTiltDegrees, Vector3.Angle(bodyUp, Vector3.up));
            if (driverDemand01 < MinDriverDemand01 || !_hasPrevious || !HasMovementSamples)
            {
                _noProgressSeconds = 0f;
                _stuckReported = false;
                return;
            }
            RequestedMotionSeconds += deltaSeconds;
            if (displacement / deltaSeconds < MinimumProgressMetersPerSecond)
            {
                _noProgressSeconds += deltaSeconds;
                if (_noProgressSeconds >= StuckAfterSeconds && !_stuckReported)
                {
                    StuckEvents++;
                    _stuckReported = true;
                }
            }
            else
            {
                _noProgressSeconds = 0f;
                _stuckReported = false;
            }
        }
    }

    [Serializable]
    public struct MapR6DriveEvidenceReport
    {
        public string scene;
        public string mapState;
        public string vehicle;
        public float elapsedSeconds;
        public float travelledMeters;
        public float commandedMotionSeconds;
        public float maxTiltDegrees;
        public int stuckEvents;
        public bool hadMotionSamples;
        public string verdict;
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class MapR6DriveEvidenceRecorder : MonoBehaviour
    {
        [SerializeField] private string vehicleLabel = "prototype_placeholder";
        [SerializeField] private string mapStateLabel = "intact";
        [SerializeField, Range(0f, 1f)] private float driverDemand01;
        [SerializeField] private float minimumProgressMetersPerSecond = 0.15f;
        [SerializeField] private float stuckAfterSeconds = 3.5f;
        private Rigidbody _body;
        private MapR6DriveEvidence _data;

        public MapR6DriveEvidence Evidence => _data;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            ResetEvidence();
        }

        public void SetDriverDemand01(float value) { driverDemand01 = Mathf.Clamp01(value); }
        public void SetMapState(string value) { mapStateLabel = value ?? "unknown"; }

        [ContextMenu("MAP R6 / Reset drive evidence")]
        public void ResetEvidence()
        {
            _data = new MapR6DriveEvidence
            {
                MinimumProgressMetersPerSecond = Mathf.Max(0.001f, minimumProgressMetersPerSecond),
                StuckAfterSeconds = Mathf.Max(0.1f, stuckAfterSeconds)
            };
        }

        private void FixedUpdate()
        {
            if (_body == null || _data == null) return;
            _data.Sample(_body.position, _body.rotation * Vector3.up, Time.fixedDeltaTime, driverDemand01);
        }

        public MapR6DriveEvidenceReport Snapshot()
        {
            var d = _data;
            return new MapR6DriveEvidenceReport
            {
                scene = gameObject.scene.name,
                mapState = mapStateLabel,
                vehicle = vehicleLabel,
                elapsedSeconds = d == null ? 0f : d.ElapsedSeconds,
                travelledMeters = d == null ? 0f : d.DrivenDistanceMeters,
                commandedMotionSeconds = d == null ? 0f : d.RequestedMotionSeconds,
                maxTiltDegrees = d == null ? 0f : d.MaxTiltDegrees,
                stuckEvents = d == null ? 0 : d.StuckEvents,
                hadMotionSamples = d != null && d.HasMovementSamples,
                verdict = "RECORDED_ONLY; route completion and real vehicle dynamics must be verified separately"
            };
        }

        [ContextMenu("MAP R6 / Export evidence JSON manually")]
        public void ExportJson()
        {
            string directory = System.IO.Path.Combine(Application.persistentDataPath, "MapR6Evidence");
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "drive_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".json");
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(Snapshot(), true));
            Debug.Log("MAP R6 evidence exported locally: " + path, this);
        }
    }
}
