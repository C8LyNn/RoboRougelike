using System;

namespace B01.LevelChallenges.MagnetLane.R3
{
    public enum MagnetPhase { Warning, Active, Safe }

    public readonly struct MagnetConfig
    {
        public readonly double WarningSeconds,ActiveSeconds,SafeSeconds,ForceNewtons,HalfX,HalfZ;
        public MagnetConfig(double warning,double active,double safe,double force,double halfX,double halfZ)
        {
            WarningSeconds=warning; ActiveSeconds=active; SafeSeconds=safe;
            ForceNewtons=force; HalfX=halfX; HalfZ=halfZ;
            if(!Finite(warning)||warning<.1||warning>10||!Finite(active)||active<.1||active>10||
               !Finite(safe)||safe<.1||safe>10||!Finite(force)||force<=0||force>2000||
               !Finite(halfX)||halfX<=.5||halfX>100||!Finite(halfZ)||halfZ<=.5||halfZ>100)
                throw new ArgumentException("Invalid magnetic lane config");
        }
        public static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        public double Cycle=>WarningSeconds+ActiveSeconds+SafeSeconds;
    }

    /// <summary>Actual previous and current Rigidbody positions are mandatory:
    /// no endpoint-only legacy overload is provided.</summary>
    public readonly struct MagnetSample
    {
        public readonly double Dt,FromX,FromZ,X,Z,MassKg;
        public readonly bool Paused,Alive;
        public MagnetSample(double dt,double fromX,double fromZ,double toX,double toZ,double massKg,bool paused=false,bool alive=true)
        {Dt=dt;FromX=fromX;FromZ=fromZ;X=toX;Z=toZ;MassKg=massKg;Paused=paused;Alive=alive;}
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
    /// Exact integral over every in-field portion of a LINEAR host-reported movement
    /// segment during every active time slice.  Source math, not actual native PhysX.
    /// Force does NOT mutate physics until an authorized Unity Rigidbody host calls
    /// AddForce(...,ForceMode.Force). No HP, game Save/Load or canonical edits.
    /// </summary>
    public sealed class MagnetLaneController
    {
        const double Eps=1e-12;
        // Guard realistic host/world samples before any phase-clock mutation.
        // Finite endpoints alone do NOT guarantee finite (end-start) subtraction.
        public const double MaxAbsPositionMeters=1000000;
        public const double MaxPathPerSampleMeters=1000;

        private static void PreflightPoint(double x,double z)
        {
            if(!MagnetConfig.Finite(x)||!MagnetConfig.Finite(z)||
               Math.Abs(x)>MaxAbsPositionMeters||Math.Abs(z)>MaxAbsPositionMeters)
                throw new ArgumentException("Nonfinite or out-of-range world position");
        }
        private static void PreflightPath(double fromX,double fromZ,double x,double z)
        {
            PreflightPoint(fromX,fromZ); PreflightPoint(x,z);
            double dx=x-fromX, dz=z-fromZ;
            if(!MagnetConfig.Finite(dx)||!MagnetConfig.Finite(dz)||
               Math.Abs(dx)>MaxPathPerSampleMeters||
               Math.Abs(dz)>MaxPathPerSampleMeters)
                throw new ArgumentException("Physics path overflow or teleport: require host reset");
        }
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

        // Exact closed-form integral of max(0,1-|z|/halfZ) for a straight
        // segment clipped to the axis-aligned magnetic rectangle.
        public static double StrengthIntegral(double x0,double z0,double x1,double z1,double halfX,double halfZ,
                                              out double occupancy)
        {
            PreflightPath(x0,z0,x1,z1);
            if(!MagnetConfig.Finite(halfX)||!MagnetConfig.Finite(halfZ)||
               halfX<=0||halfZ<=0||halfX>100||halfZ>100)
                throw new ArgumentException("Invalid integral bounds");
            double enter=0,exit=1;
            if(!Clip(x0,x1-x0,-halfX,halfX,ref enter,ref exit)||
               !Clip(z0,z1-z0,-halfZ,halfZ,ref enter,ref exit))
            {occupancy=0;return 0;}
            occupancy=Math.Max(0,exit-enter);
            if(occupancy==0)return 0;
            double dz=z1-z0;
            // Partition at z=0; |z| is piecewise affine. Trapezoid is exact.
            double area=0,cut=dz==0?double.NaN:-z0/dz;
            if(cut>enter&&cut<exit)
            {
                area+=Area(z0,dz,enter,cut,halfZ);
                area+=Area(z0,dz,cut,exit,halfZ);
            }
            else area+=Area(z0,dz,enter,exit,halfZ);
            return Math.Max(0,Math.Min(occupancy,area));
        }
        private static double Area(double z0,double dz,double t0,double t1,double halfZ)
        {
            double a=Math.Max(0,1-Math.Abs(z0+dz*t0)/halfZ);
            double b=Math.Max(0,1-Math.Abs(z0+dz*t1)/halfZ);
            return (t1-t0)*(a+b)*.5;
        }
        private static bool Clip(double position,double delta,double low,double high,ref double enter,ref double exit)
        {
            if(delta==0)return position>=low&&position<=high;
            double a=(low-position)/delta,b=(high-position)/delta;
            if(a>b){double tmp=a;a=b;b=tmp;}
            enter=Math.Max(enter,a);exit=Math.Min(exit,b);
            return enter<=exit;
        }

        public MagnetOutput Advance(MagnetSample sample)
        {
            // Invalid host telemetry fails BEFORE changing any clock or polarity.
            if(!MagnetConfig.Finite(sample.Dt)||sample.Dt<0||sample.Dt>.1||
               !MagnetConfig.Finite(sample.FromX)||!MagnetConfig.Finite(sample.FromZ)||
               !MagnetConfig.Finite(sample.X)||!MagnetConfig.Finite(sample.Z)||
               !MagnetConfig.Finite(sample.MassKg)||sample.MassKg<.1||sample.MassKg>1000)
                throw new ArgumentException("Invalid real sampled movement (0-100ms)");
            // All arithmetic preflight must pass BEFORE we advance Clock/Polarity.
            // R2 could overflow finite X-FromX after partially advancing Warning.
            PreflightPath(sample.FromX,sample.FromZ,sample.X,sample.Z);
            bool inside=Math.Abs(sample.X)<=Config.HalfX&&Math.Abs(sample.Z)<=Config.HalfZ;
            if(sample.Paused||sample.Dt==0)
                return new MagnetOutput(PhaseAt(Clock),Clock,0,0,0,Polarity,CompletedCycles,inside);
            double remaining=sample.Dt,weightedForce=0,activeDuration=0;
            for(int guard=0;guard<8&&remaining>Eps;guard++)
            {
                MagnetPhase phase=PhaseAt(Clock);
                double boundary=phase==MagnetPhase.Warning?Config.WarningSeconds:
                    phase==MagnetPhase.Active?Config.WarningSeconds+Config.ActiveSeconds:Config.Cycle;
                double take=Math.Min(remaining,Math.Max(0,boundary-Clock));
                if(take>Eps)
                {
                    if(phase==MagnetPhase.Active&&sample.Alive)
                    {
                        double t0=(sample.Dt-remaining)/sample.Dt;
                        double t1=(sample.Dt-remaining+take)/sample.Dt;
                        double x0=sample.FromX+(sample.X-sample.FromX)*t0;
                        double z0=sample.FromZ+(sample.Z-sample.FromZ)*t0;
                        double x1=sample.FromX+(sample.X-sample.FromX)*t1;
                        double z1=sample.FromZ+(sample.Z-sample.FromZ)*t1;
                        double coverage;
                        double spatial=StrengthIntegral(x0,z0,x1,z1,Config.HalfX,Config.HalfZ,out coverage);
                        weightedForce+=take*Polarity*Config.ForceNewtons*spatial;
                        activeDuration+=take*coverage;
                    }
                    Clock+=take;remaining-=take;
                }
                if(Clock+Eps>=boundary)
                {
                    Clock=boundary;
                    if(boundary==Config.Cycle)
                    {Clock=0;Polarity=-Polarity;CompletedCycles++;}
                }
            }
            if(remaining>Eps)throw new InvalidOperationException("Unresolved phase step");
            double mean=weightedForce/sample.Dt;
            return new MagnetOutput(PhaseAt(Clock),Clock,mean,mean/sample.MassKg,
                activeDuration/sample.Dt,Polarity,CompletedCycles,inside);
        }
    }
}
