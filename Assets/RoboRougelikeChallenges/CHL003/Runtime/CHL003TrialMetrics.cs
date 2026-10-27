using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace B01.CHL003
{
    /// <summary>
    /// Recorded OBSERVATIONS, never synthetic completions. Receives actual
    /// actor Rigidbody positions, speed and Collision contacts from Unity.
    /// No disk IO, Unity APIs, R3, economy or player reward integration.
    /// </summary>
    public sealed class CHL003TrialMetrics
    {
        private readonly List<string> events = new List<string>(64);
        private bool sampled;
        private float priorX, priorY, priorZ;
        public string Profile { get; private set; } = "";
        public int Attempt { get; private set; }
        public float MassKg { get; private set; }
        public float WidthMeters { get; private set; }
        public float LengthMeters { get; private set; }
        public int PhysicsSamples { get; private set; }
        public int CollisionEntries { get; private set; }
        public int ContactPoints { get; private set; }
        public int BarrierContacts { get; private set; }
        public int RampContacts { get; private set; }
        public int BridgeContacts { get; private set; }
        public int GroundContacts { get; private set; }
        public int Falls { get; private set; }
        public int Checkpoints { get; private set; }
        public int Completions { get; private set; }
        public float PeakSpeedMetersPerSecond { get; private set; }
        public float DistanceMeters { get; private set; }
        public float MinHeightMeters { get; private set; }
        public float MaxHeightMeters { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public string TerminalState { get; private set; } = "Ready";
        public IReadOnlyList<string> Events => events;

        private static void FiniteNonnegative(float v, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v < 0f)
                throw new ArgumentOutOfRangeException(name);
        }
        private static void Finite(float v, string name)
        {
            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new ArgumentOutOfRangeException(name);
        }
        private static string N(float v) => v.ToString("R", CultureInfo.InvariantCulture);
        private void Event(string kind, string detail)
        {
            if (events.Count < 8192)
                events.Add(Attempt + "," + Profile + "," + kind + "," +
                    detail + "," + N(ElapsedSeconds));
        }

        public void Reset(string profile, int attempt, float mass, float width, float length)
        {
            if (profile != "LIGHT" && profile != "HEAVY")
                throw new ArgumentException("CHL003_PROFILE_INVALID");
            if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt));
            FiniteNonnegative(mass, nameof(mass));
            FiniteNonnegative(width, nameof(width));
            FiniteNonnegative(length, nameof(length));
            if (mass <= 0 || width <= 0 || length <= 0)
                throw new ArgumentException("CHL003_DIMENSIONS_ZERO");
            Profile = profile; Attempt = attempt; MassKg = mass;
            WidthMeters = width; LengthMeters = length;
            PhysicsSamples = CollisionEntries = ContactPoints = 0;
            BarrierContacts = RampContacts = BridgeContacts = GroundContacts = 0;
            Falls = Checkpoints = Completions = 0;
            PeakSpeedMetersPerSecond = DistanceMeters = ElapsedSeconds = 0f;
            MinHeightMeters = float.PositiveInfinity;
            MaxHeightMeters = float.NegativeInfinity;
            TerminalState = "Ready"; sampled = false;
            events.Clear();
            Event("RESET", N(mass) + "/" + N(width) + "/" + N(length));
        }

        public void Begin()
        {
            if (TerminalState == "Ready")
            { TerminalState = "Running"; Event("BEGIN", ""); }
        }
        public void RecordPhysics(float x, float y, float z, float speed, float elapsed)
        {
            Finite(x, nameof(x)); Finite(y, nameof(y)); Finite(z, nameof(z));
            FiniteNonnegative(speed, nameof(speed));
            FiniteNonnegative(elapsed, nameof(elapsed));
            if (TerminalState != "Running") return;
            if (sampled)
            {
                double dx=x-priorX,dy=y-priorY,dz=z-priorZ;
                double distance=Math.Sqrt(dx*dx+dy*dy+dz*dz);
                // Exclude impossible discontinuous motion from physical distance:
                // caller may only sample a continuous attempt, never Retry teleport.
                if (distance > 1000d)
                    throw new ArgumentException("CHL003_SAMPLE_POSITION_TELEPORT");
                DistanceMeters += (float)distance;
            }
            sampled=true;priorX=x;priorY=y;priorZ=z;
            PhysicsSamples++;
            if (speed > PeakSpeedMetersPerSecond) PeakSpeedMetersPerSecond=speed;
            if (y < MinHeightMeters) MinHeightMeters=y;
            if (y > MaxHeightMeters) MaxHeightMeters=y;
            ElapsedSeconds=elapsed;
        }
        public void RecordCollision(string feature, int actualContactPoints, bool entering)
        {
            if (feature==null || feature.Length>100 || feature.Contains(",") ||
                feature.Contains("\n") || feature.Contains("\r"))
                throw new ArgumentException("CHL003_BAD_COLLIDER_NAME");
            if (actualContactPoints<0 || actualContactPoints>512)
                throw new ArgumentOutOfRangeException(nameof(actualContactPoints));
            if (TerminalState!="Running") return;
            if (entering) CollisionEntries++;
            ContactPoints+=actualContactPoints;
            if (feature.Contains("BARRIER")) BarrierContacts+=actualContactPoints;
            else if (feature.Contains("RAMP")) RampContacts+=actualContactPoints;
            else if (feature.Contains("BRIDGE")) BridgeContacts+=actualContactPoints;
            else GroundContacts+=actualContactPoints;
            if (entering) Event("COLLISION", feature + "/" + actualContactPoints);
        }
        public void RecordCheckpoint(int reached, float elapsed)
        {
            FiniteNonnegative(elapsed,nameof(elapsed));
            if (TerminalState!="Running" || reached!=Checkpoints+1)
                throw new InvalidOperationException("CHL003_TELEMETRY_CHECKPOINT_ORDER");
            Checkpoints=reached;ElapsedSeconds=elapsed;Event("CHECKPOINT",reached.ToString());
        }
        public void RecordCompleted(float elapsed)
        {
            FiniteNonnegative(elapsed,nameof(elapsed));
            if (TerminalState!="Running") return;
            Completions++; ElapsedSeconds=elapsed; TerminalState="Completed";
            Event("COMPLETE","");
        }
        public void RecordFailed(string reason,float elapsed)
        {
            FiniteNonnegative(elapsed,nameof(elapsed));
            if (TerminalState!="Running") return;
            if(reason=="FellOffCourse") Falls++;
            ElapsedSeconds=elapsed;TerminalState=reason;
            Event("FAILED",reason);
        }
        public string ToCsv()
        {
            var s=new StringBuilder("attempt,profile,event,detail,elapsedSec\n");
            foreach(var e in events) s.Append(e).Append('\n');
            return s.ToString();
        }
        public string SummaryCsv()
        {
            return "profile,attempt,massKg,widthM,lengthM,elapsedSec,state," +
              "physicsSamples,collisionEntries,contactPoints,barrierPoints," +
              "rampPoints,bridgePoints,groundPoints,falls,checkpoints,completions," +
              "peakSpeedMps,travelMeters,minHeightM,maxHeightM\n" +
              Profile+","+Attempt+","+N(MassKg)+","+N(WidthMeters)+","+
              N(LengthMeters)+","+N(ElapsedSeconds)+","+TerminalState+","+
              PhysicsSamples+","+CollisionEntries+","+ContactPoints+","+
              BarrierContacts+","+RampContacts+","+BridgeContacts+","+
              GroundContacts+","+Falls+","+Checkpoints+","+Completions+","+
              N(PeakSpeedMetersPerSecond)+","+N(DistanceMeters)+","+
              N(PhysicsSamples>0?MinHeightMeters:0f)+","+
              N(PhysicsSamples>0?MaxHeightMeters:0f)+"\n";
        }
    }
}
