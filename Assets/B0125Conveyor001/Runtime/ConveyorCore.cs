using System;

namespace B01.LevelChallenges.Conveyor
{
    public enum BeltPhase { Forward, ReversalWarning, Reverse, ForwardWarning }

    public sealed class ConveyorConfig
    {
        public readonly double HalfX, HalfZ, RunSeconds, WarningSeconds;
        public readonly double SpeedMetersPerSecond, ResponseSeconds, MaxForceNewtons;
        public double Period => 2*(RunSeconds+WarningSeconds);
        public ConveyorConfig(double halfX,double halfZ,double runSeconds,
            double warningSeconds,double speed,double responseSeconds,double maxForce)
        {
            if(!Finite(halfX)||halfX<=.5||halfX>20||
               !Finite(halfZ)||halfZ<=1||halfZ>50||
               !Finite(runSeconds)||runSeconds<.5||runSeconds>20||
               !Finite(warningSeconds)||warningSeconds<.2||warningSeconds>5||
               !Finite(speed)||speed<=0||speed>50||
               !Finite(responseSeconds)||responseSeconds<.05||responseSeconds>10||
               !Finite(maxForce)||maxForce<=0||maxForce>100000)
                throw new ArgumentException("Invalid surface conveyor parameters");
            HalfX=halfX;HalfZ=halfZ;RunSeconds=runSeconds;
            WarningSeconds=warningSeconds;SpeedMetersPerSecond=speed;
            ResponseSeconds=responseSeconds;MaxForceNewtons=maxForce;
        }
        public static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
    }

    public readonly struct ConveyorSample
    {
        public readonly double Dt,X,Z,MassKg,VelocityZ;
        public readonly bool ColliderSupport,Alive,Paused;
        public ConveyorSample(double dt,double x,double z,double massKg,
            double velocityZ,bool colliderSupport,bool alive=true,bool paused=false)
        {Dt=dt;X=x;Z=z;MassKg=massKg;VelocityZ=velocityZ;
         ColliderSupport=colliderSupport;Alive=alive;Paused=paused;}
    }

    public readonly struct ConveyorFrame
    {
        public readonly BeltPhase Phase;
        public readonly double ForceZ,TargetVelocityZ,ElapsedSeconds;
        public readonly int CompletedPeriods;
        public readonly bool Supported;
        public ConveyorFrame(BeltPhase phase,double force,double target,double elapsed,
            int cycles,bool supported)
        {Phase=phase;ForceZ=force;TargetVelocityZ=target;ElapsedSeconds=elapsed;
         CompletedPeriods=cycles;Supported=supported;}
    }

    /// <summary>
    /// Safe pure C#9 per-frame belt force controller; Unity host must independently
    /// confirm genuine Collider.Raycast support on THIS belt. Not native PhysX.
    /// </summary>
    public sealed class ConveyorController
    {
        public ConveyorConfig Config {get;}
        public ConveyorFrame Last {get;private set;}
        public ConveyorController(ConveyorConfig config)
        {Config=config??throw new ArgumentNullException(nameof(config));Reset();}
        public void Reset(){Last=new ConveyorFrame(BeltPhase.Forward,0,0,0,0,false);}
        public ConveyorFrame Advance(ConveyorSample s)
        {
            // Fail before any clock/phase/force changes, even paused input.
            if(!ConveyorConfig.Finite(s.Dt)||s.Dt<0||s.Dt>.05||
               !ConveyorConfig.Finite(s.X)||Math.Abs(s.X)>1000000||
               !ConveyorConfig.Finite(s.Z)||Math.Abs(s.Z)>1000000||
               !ConveyorConfig.Finite(s.MassKg)||s.MassKg<.1||s.MassKg>1000||
               !ConveyorConfig.Finite(s.VelocityZ)||Math.Abs(s.VelocityZ)>1000)
                throw new ArgumentException("Invalid physical conveyor telemetry");
            if(s.Paused||s.Dt==0)return Last;
            double elapsed=Last.ElapsedSeconds+s.Dt;
            double loops=Math.Floor(elapsed/Config.Period);
            if(!ConveyorConfig.Finite(elapsed)||!ConveyorConfig.Finite(loops)||
               loops<0||loops>int.MaxValue)
                throw new ArgumentException("Conveyor clock overflow");
            double cycle=elapsed%Config.Period;
            // Normalize floating-point fixed-step boundary bias on exactly
            // authored phase edges. This is deterministic across runs/OS.
            if(Math.Abs(cycle-Config.Period)<1e-9){cycle=0;loops++;}
            BeltPhase phase;
            double v;
            if(cycle<Config.RunSeconds-1e-9)
            {phase=BeltPhase.Forward;v=Config.SpeedMetersPerSecond;}
            else if(cycle<Config.RunSeconds+Config.WarningSeconds-1e-9)
            {phase=BeltPhase.ReversalWarning;v=0;}
            else if(cycle<2*Config.RunSeconds+Config.WarningSeconds-1e-9)
            {phase=BeltPhase.Reverse;v=-Config.SpeedMetersPerSecond;}
            else {phase=BeltPhase.ForwardWarning;v=0;}
            bool support=s.Alive&&s.ColliderSupport&&
                Math.Abs(s.X)<=Config.HalfX&&Math.Abs(s.Z)<=Config.HalfZ;
            double demand=support&&v!=0?
                (v-s.VelocityZ)*s.MassKg/Config.ResponseSeconds:0;
            if(!ConveyorConfig.Finite(demand))throw new ArgumentException("Force overflow");
            double force=Math.Max(-Config.MaxForceNewtons,
                Math.Min(Config.MaxForceNewtons,demand));
            if(!ConveyorConfig.Finite(force)||loops>int.MaxValue)
                throw new ArgumentException("Conveyor period overflow");
            Last=new ConveyorFrame(phase,force,v,elapsed,(int)loops,support);
            return Last;
        }
    }
}
