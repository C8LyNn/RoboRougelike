using System;

namespace B01.LevelChallenges.MagnetLane
{
    public enum MagnetPhase { Warning, Active, Safe }

    public readonly struct MagnetConfig
    {
        public readonly double WarningSeconds, ActiveSeconds, SafeSeconds, ForceNewtons, HalfX, HalfZ;
        public MagnetConfig(double warning, double active, double safe, double force, double halfX, double halfZ)
        {
            WarningSeconds=warning; ActiveSeconds=active; SafeSeconds=safe;
            ForceNewtons=force; HalfX=halfX; HalfZ=halfZ;
            if(!Finite(warning)||warning<.1||warning>10||!Finite(active)||active<.1||active>10||
               !Finite(safe)||safe<.1||safe>10||!Finite(force)||force<=0||force>2000||
               !Finite(halfX)||halfX<=.5||halfX>100||!Finite(halfZ)||halfZ<=.5||halfZ>100)
                throw new ArgumentException("Invalid magnetic lane");
        }
        public static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        public double Cycle=>WarningSeconds+ActiveSeconds+SafeSeconds;
    }

    public readonly struct MagnetSample
    {
        public readonly double Dt,X,Z,MassKg;
        public readonly bool Paused,Alive;
        public MagnetSample(double dt,double x,double z,double massKg,bool paused=false,bool alive=true)
        {Dt=dt;X=x;Z=z;MassKg=massKg;Paused=paused;Alive=alive;}
    }

    public readonly struct MagnetOutput
    {
        public readonly MagnetPhase Phase;
        public readonly double Elapsed,ForceZ,AccelerationZ,ActiveFraction;
        public readonly int Polarity,CompletedCycles;
        public readonly bool InsideLane;
        public MagnetOutput(MagnetPhase phase,double elapsed,double force,double accel,double fraction,int polarity,int cycles,bool inside)
        {Phase=phase;Elapsed=elapsed;ForceZ=force;AccelerationZ=accel;ActiveFraction=fraction;
         Polarity=polarity;CompletedCycles=cycles;InsideLane=inside;}
    }

    /// <summary>
    /// Deterministic pure C#9 rule. One averaged per-frame magnetic FORCE (N),
    /// not velocity/teleport/HP. Host applies ForceMode.Force on actual Rigidbody.
    /// This lane is OPTIONAL: authored northern corridor remains always traversable.
    /// </summary>
    public sealed class MagnetLaneController
    {
        const double Eps=1e-12;
        public MagnetConfig Config { get; }
        public double Clock { get; private set; }
        public int Polarity { get; private set; }
        public int CompletedCycles { get; private set; }
        public MagnetLaneController(MagnetConfig config){Config=config;Reset();}
        public void Reset(){Clock=0;Polarity=1;CompletedCycles=0;}

        public MagnetPhase PhaseAt(double t)
        {
            if(t<Config.WarningSeconds-Eps)return MagnetPhase.Warning;
            if(t<Config.WarningSeconds+Config.ActiveSeconds-Eps)return MagnetPhase.Active;
            return MagnetPhase.Safe;
        }
        public MagnetOutput Advance(MagnetSample sample)
        {
            // Validate BEFORE mutation: invalid samples fail atomically.
            if(!MagnetConfig.Finite(sample.Dt)||sample.Dt<0||sample.Dt>.1||
               !MagnetConfig.Finite(sample.X)||!MagnetConfig.Finite(sample.Z)||
               !MagnetConfig.Finite(sample.MassKg)||sample.MassKg<.1||sample.MassKg>1000)
                throw new ArgumentException("Invalid physics sample (0-100 ms)");
            bool inside=Math.Abs(sample.X)<=Config.HalfX&&Math.Abs(sample.Z)<=Config.HalfZ;
            if(sample.Paused||sample.Dt==0)
                return new MagnetOutput(PhaseAt(Clock),Clock,0,0,0,Polarity,CompletedCycles,inside);
            double remaining=sample.Dt,weightedForce=0,activeSeconds=0;
            for(int guard=0;guard<8&&remaining>Eps;guard++)
            {
                MagnetPhase phase=PhaseAt(Clock);
                double boundary=phase==MagnetPhase.Warning?Config.WarningSeconds:
                                phase==MagnetPhase.Active?Config.WarningSeconds+Config.ActiveSeconds:Config.Cycle;
                double take=Math.Min(remaining,Math.Max(0,boundary-Clock));
                if(take>Eps)
                {
                    if(phase==MagnetPhase.Active&&inside&&sample.Alive)
                    {
                        double force=Polarity*Config.ForceNewtons*(1-Math.Abs(sample.Z)/Config.HalfZ);
                        weightedForce+=take*force; activeSeconds+=take;
                    }
                    Clock+=take; remaining-=take;
                }
                if(Clock+Eps>=boundary)
                {
                    Clock=boundary;
                    if(boundary==Config.Cycle)
                    {
                        Clock=0; Polarity=-Polarity; CompletedCycles++;
                    }
                }
            }
            if(remaining>Eps)throw new InvalidOperationException("Unresolved phase step");
            double forceAverage=weightedForce/sample.Dt;
            return new MagnetOutput(PhaseAt(Clock),Clock,forceAverage,forceAverage/sample.MassKg,
                activeSeconds/sample.Dt,Polarity,CompletedCycles,inside);
        }
    }
}
