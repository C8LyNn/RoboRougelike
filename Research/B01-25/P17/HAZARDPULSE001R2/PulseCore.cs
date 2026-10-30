using System;

namespace B01.LevelChallenges
{
    // Unity 6000.3-compatible C#9/netstandard2.1 rule module.
    // Host provides actual player position/alive and applies any damage itself.
    public enum PulsePhase { Warning, Active, Recovery }

    public readonly struct PulseZoneConfig
    {
        public string Id { get; }
        public double CenterX { get; }
        public double CenterZ { get; }
        public double RadiusMeters { get; }
        public double WarningSeconds { get; }
        public double ActiveSeconds { get; }
        public double RecoverySeconds { get; }
        public double OffsetSeconds { get; }
        public double PeriodSeconds => WarningSeconds + ActiveSeconds + RecoverySeconds;

        public PulseZoneConfig(string id, double x, double z, double radius,
            double warning, double active, double recovery, double offset)
        {
            Id=id;CenterX=x;CenterZ=z;RadiusMeters=radius;
            WarningSeconds=warning;ActiveSeconds=active;
            RecoverySeconds=recovery;OffsetSeconds=offset;
        }

        internal static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Id) || Id.Length>64 ||
                !Finite(CenterX) || !Finite(CenterZ) ||
                !Finite(RadiusMeters) || RadiusMeters<=0 ||
                !Finite(WarningSeconds) || WarningSeconds<0.75 ||
                !Finite(ActiveSeconds) || ActiveSeconds<0.25 ||
                !Finite(RecoverySeconds) || RecoverySeconds<=0 ||
                !Finite(OffsetSeconds) || OffsetSeconds<0 ||
                !Finite(PeriodSeconds) || OffsetSeconds>=PeriodSeconds)
                throw new ArgumentException("Invalid PulseZoneConfig");
        }
    }

    public readonly struct PulseSample
    {
        public double DeltaSeconds { get; }
        public double PlayerX { get; }
        public double PlayerZ { get; }
        public bool PlayerAlive { get; }
        public bool Paused { get; }
        public PulseSample(double dt, double x, double z, bool alive, bool paused)
        {
            DeltaSeconds=dt;PlayerX=x;PlayerZ=z;
            PlayerAlive=alive;Paused=paused;
        }
    }

    public readonly struct PulseSnapshot
    {
        public PulsePhase Phase { get; }
        public double ElapsedSeconds { get; }
        public double PhaseRemainingSeconds { get; }
        public bool PlayerInside { get; }
        public bool Exposed { get; }
        public bool DamagePulse { get; }

        public PulseSnapshot(PulsePhase phase, double elapsed,
            double remaining, bool playerInside, bool exposed, bool damagePulse)
        {
            Phase=phase;ElapsedSeconds=elapsed;PhaseRemainingSeconds=remaining;
            PlayerInside=playerInside;Exposed=exposed;DamagePulse=damagePulse;
        }
    }

    /// <summary>
    /// Deterministic telegraphed hazard with only one authorized damage
    /// request per activation cycle. Not a Unity collision/damage engine.
    /// Caller must substep deltaSeconds <= 0.25; otherwise reject before mutation.
    /// </summary>
    public sealed class PulseZoneController
    {
        public PulseZoneConfig Config { get; }
        public double ElapsedSeconds { get; private set; }
        private double lastSignaledCycle;
        public PulseZoneController(PulseZoneConfig config) { config.Validate(); Config=config; Reset(); }

        public void Reset()
        {
            ElapsedSeconds=0;
            lastSignaledCycle=-1;
        }

        // QA #444: 10x0.1s may sum to 0.9999999999999999, below 1s.
        // Canonicalize only sub-picosecond numerical drift near phase edges.
        private const double PhaseEdgeToleranceSeconds = 1e-12;

        private double PositionAt(double time)
        {
            double p = (time + Config.OffsetSeconds) % Config.PeriodSeconds;
            if (p <= PhaseEdgeToleranceSeconds ||
                Config.PeriodSeconds - p <= PhaseEdgeToleranceSeconds)
                return 0;
            if (Math.Abs(p - Config.WarningSeconds) <= PhaseEdgeToleranceSeconds)
                return Config.WarningSeconds;
            double activeEnd = Config.WarningSeconds + Config.ActiveSeconds;
            if (Math.Abs(p - activeEnd) <= PhaseEdgeToleranceSeconds)
                return activeEnd;
            return p;
        }

        // Use the same wrap convention for the damage latch and phase.
        private double CycleAt(double time)
        {
            double absolute = time + Config.OffsetSeconds;
            double cycle = Math.Floor(absolute / Config.PeriodSeconds);
            double remainder = absolute % Config.PeriodSeconds;
            if (Config.PeriodSeconds - remainder <= PhaseEdgeToleranceSeconds)
                cycle++;
            return cycle;
        }

        public PulsePhase PhaseAt(double time)
        {
            if(!PulseZoneConfig.Finite(time) || time<0)
                throw new ArgumentException("Invalid absolute time");
            var p=PositionAt(time);
            if(p<Config.WarningSeconds)return PulsePhase.Warning;
            if(p<Config.WarningSeconds+Config.ActiveSeconds)return PulsePhase.Active;
            return PulsePhase.Recovery;
        }

        private double RemainingAt(double time, PulsePhase phase)
        {
            var p=PositionAt(time);
            var end=phase==PulsePhase.Warning?Config.WarningSeconds:
                phase==PulsePhase.Active?Config.WarningSeconds+Config.ActiveSeconds:
                Config.PeriodSeconds;
            return Math.Max(0,end-p);
        }

        public bool Contains(double x,double z)
        {
            if(!PulseZoneConfig.Finite(x) || !PulseZoneConfig.Finite(z))return false;
            var dx=x-Config.CenterX;
            var dz=z-Config.CenterZ;
            // Sub-micrometer tolerance avoids an IEEE754 false miss exactly at
            // an authored circle boundary (e.g. world 28 + radius 1.35).
            double tolerantRadius=Config.RadiusMeters+1e-9;
            return dx*dx+dz*dz<=tolerantRadius*tolerantRadius;
        }

        public PulseSnapshot Advance(PulseSample sample)
        {
            // Fail-closed; never mutate clock or damage latch on invalid telemetry.
            if(!PulseZoneConfig.Finite(sample.DeltaSeconds) ||
                sample.DeltaSeconds<0 || sample.DeltaSeconds>0.25 ||
                !PulseZoneConfig.Finite(sample.PlayerX) ||
                !PulseZoneConfig.Finite(sample.PlayerZ) ||
                !PulseZoneConfig.Finite(ElapsedSeconds+sample.DeltaSeconds))
                throw new ArgumentException("Invalid pulse telemetry (substep max .25s)");

            double next=ElapsedSeconds+(sample.Paused?0:sample.DeltaSeconds);
            var phase=PhaseAt(next);
            bool inside=Contains(sample.PlayerX,sample.PlayerZ);
            bool exposed=phase==PulsePhase.Active && inside && sample.PlayerAlive;
            bool pulse=false;
            if(!sample.Paused && exposed)
            {
                // One damage authorization per activation, including leave/reenter.
                // This prevents frame-rate-dependent repeat damage.
                double cycle=CycleAt(next);
                if(cycle!=lastSignaledCycle)
                {
                    lastSignaledCycle=cycle;
                    pulse=true;
                }
            }
            ElapsedSeconds=next;
            return new PulseSnapshot(phase,next,RemainingAt(next,phase),
                inside,exposed,pulse);
        }
    }
}
