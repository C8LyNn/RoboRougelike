using System;

namespace B01.LevelChallenges
{
    // Pure game-rule module. No Unity references, global state, persistence,
    // enemy AI, reward handling or game Finish-trigger override.
    public readonly record struct HoldoutConfig(
        string Id, double CenterX, double CenterZ, double RadiusMeters,
        double RequiredSeconds, double OutsideDecayPerSecond)
    {
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Id) || Id.Length>64)
                throw new ArgumentException("Invalid challenge ID");
            if(!double.IsFinite(CenterX)||!double.IsFinite(CenterZ)||
               !double.IsFinite(RadiusMeters)||RadiusMeters<=0||
               !double.IsFinite(RequiredSeconds)||RequiredSeconds<=0||
               !double.IsFinite(OutsideDecayPerSecond)||OutsideDecayPerSecond<0)
                throw new ArgumentException("Invalid objective configuration");
        }
    }

    public enum HoldoutPhase { Waiting, Charging, Contested, Recovering, Ready }
    public readonly record struct HoldoutSample(
        double DeltaSeconds, double PlayerX, double PlayerZ,
        int HostilesInside, bool PlayerAlive);
    public readonly record struct HoldoutSnapshot(
        HoldoutPhase Phase, double ProgressSeconds, double RequiredSeconds,
        bool PlayerInside, bool Contested, bool BecameReady)
    {
        public double Fraction => Math.Clamp(ProgressSeconds / RequiredSeconds,0,1);
        public bool Ready => Phase==HoldoutPhase.Ready;
    }

    public sealed class HoldoutController
    {
        public HoldoutConfig Config {get;}
        public double ProgressSeconds {get;private set;}
        public bool IsReady {get;private set;}
        public HoldoutController(HoldoutConfig config){config.Validate();Config=config;}
        public void Reset(){ProgressSeconds=0;IsReady=false;}
        public HoldoutSnapshot Tick(HoldoutSample sample)
        {
            // Reject all malformed telemetry BEFORE any state mutation.
            if(!double.IsFinite(sample.DeltaSeconds)||sample.DeltaSeconds<0||
               !double.IsFinite(sample.PlayerX)||!double.IsFinite(sample.PlayerZ)||
               sample.HostilesInside<0)
                throw new ArgumentException("Invalid holdout telemetry");
            bool inside=Inside(sample.PlayerX,sample.PlayerZ);
            bool contested=sample.HostilesInside>0;
            // Death invalidates the attempt even after the one-shot ready event.
            // The host may separately apply a run-fail policy; this local state
            // cannot grant an extraction after a destroyed robot.
            if(!sample.PlayerAlive)
            {
                Reset();
                return new(HoldoutPhase.Waiting,0,Config.RequiredSeconds,
                    inside,contested,false);
            }
            if(IsReady)
                return new(HoldoutPhase.Ready,Config.RequiredSeconds,
                    Config.RequiredSeconds,inside,contested,false);
            if(!inside)
            {
                ProgressSeconds=Math.Max(0,ProgressSeconds-
                    sample.DeltaSeconds*Config.OutsideDecayPerSecond);
                return new(ProgressSeconds>0?HoldoutPhase.Recovering:HoldoutPhase.Waiting,
                    ProgressSeconds,Config.RequiredSeconds,false,contested,false);
            }
            if(contested)
                return new(HoldoutPhase.Contested,ProgressSeconds,
                    Config.RequiredSeconds,true,true,false);
            if(sample.DeltaSeconds==0)
                return new(ProgressSeconds>0?HoldoutPhase.Charging:HoldoutPhase.Waiting,
                    ProgressSeconds,Config.RequiredSeconds,true,false,false);
            ProgressSeconds=Math.Min(Config.RequiredSeconds,ProgressSeconds+sample.DeltaSeconds);
            if(ProgressSeconds>=Config.RequiredSeconds)
            {
                ProgressSeconds=Config.RequiredSeconds;
                IsReady=true;
                return new(HoldoutPhase.Ready,ProgressSeconds,Config.RequiredSeconds,true,false,true);
            }
            return new(HoldoutPhase.Charging,ProgressSeconds,
                Config.RequiredSeconds,true,false,false);
        }
        public bool Inside(double x,double z)
        {
            if(!double.IsFinite(x)||!double.IsFinite(z))return false;
            double dx=x-Config.CenterX,dz=z-Config.CenterZ;
            return dx*dx+dz*dz<=Config.RadiusMeters*Config.RadiusMeters;
        }
    }
}
