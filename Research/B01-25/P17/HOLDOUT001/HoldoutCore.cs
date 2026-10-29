using System;

namespace B01.LevelChallenges
{
    // Unity 6000.3 source compatibility: C# 9 / netstandard2.1.
    // Pure, deterministic game rules. No UnityEngine references, serializable
    // Unity records, global state, save, AI, reward or MapFinish override.
    internal static class HoldoutNumbers
    {
        internal static bool Finite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public readonly struct HoldoutConfig
    {
        public string Id { get; }
        public double CenterX { get; }
        public double CenterZ { get; }
        public double RadiusMeters { get; }
        public double RequiredSeconds { get; }
        public double OutsideDecayPerSecond { get; }

        public HoldoutConfig(string id, double centerX, double centerZ,
            double radiusMeters, double requiredSeconds, double outsideDecayPerSecond)
        {
            Id=id;CenterX=centerX;CenterZ=centerZ;
            RadiusMeters=radiusMeters;RequiredSeconds=requiredSeconds;
            OutsideDecayPerSecond=outsideDecayPerSecond;
        }
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Id) || Id.Length>64)
                throw new ArgumentException("Invalid challenge ID");
            if(!HoldoutNumbers.Finite(CenterX)||!HoldoutNumbers.Finite(CenterZ)||
                !HoldoutNumbers.Finite(RadiusMeters)||RadiusMeters<=0||
                !HoldoutNumbers.Finite(RequiredSeconds)||RequiredSeconds<=0||
                !HoldoutNumbers.Finite(OutsideDecayPerSecond)||OutsideDecayPerSecond<0)
                throw new ArgumentException("Invalid objective configuration");
        }
    }

    public enum HoldoutPhase { Waiting, Charging, Contested, Recovering, Ready }

    public readonly struct HoldoutSample
    {
        public double DeltaSeconds { get; }
        public double PlayerX { get; }
        public double PlayerZ { get; }
        public int HostilesInside { get; }
        public bool PlayerAlive { get; }
        public HoldoutSample(double deltaSeconds, double playerX, double playerZ,
            int hostilesInside, bool playerAlive)
        {
            DeltaSeconds=deltaSeconds;PlayerX=playerX;PlayerZ=playerZ;
            HostilesInside=hostilesInside;PlayerAlive=playerAlive;
        }
    }

    public readonly struct HoldoutSnapshot : IEquatable<HoldoutSnapshot>
    {
        public HoldoutPhase Phase { get; }
        public double ProgressSeconds { get; }
        public double RequiredSeconds { get; }
        public bool PlayerInside { get; }
        public bool Contested { get; }
        public bool BecameReady { get; }

        public HoldoutSnapshot(HoldoutPhase phase, double progressSeconds,
            double requiredSeconds, bool playerInside, bool contested, bool becameReady)
        {
            Phase=phase;ProgressSeconds=progressSeconds;RequiredSeconds=requiredSeconds;
            PlayerInside=playerInside;Contested=contested;BecameReady=becameReady;
        }

        public double Fraction => Math.Max(0,Math.Min(1,ProgressSeconds/RequiredSeconds));
        public bool Ready => Phase==HoldoutPhase.Ready;

        // C# 9 struct value equality preserves the previous record-struct
        // deterministic timeline comparison without Unity-incompatible records.
        public bool Equals(HoldoutSnapshot other) =>
            Phase==other.Phase&&ProgressSeconds==other.ProgressSeconds&&
            RequiredSeconds==other.RequiredSeconds&&PlayerInside==other.PlayerInside&&
            Contested==other.Contested&&BecameReady==other.BecameReady;
        public override bool Equals(object obj) => obj is HoldoutSnapshot other&&Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash=(int)Phase;
                hash=hash*31+ProgressSeconds.GetHashCode();
                hash=hash*31+RequiredSeconds.GetHashCode();
                hash=hash*31+PlayerInside.GetHashCode();
                hash=hash*31+Contested.GetHashCode();
                return hash*31+BecameReady.GetHashCode();
            }
        }
        public static bool operator ==(HoldoutSnapshot left,HoldoutSnapshot right) => left.Equals(right);
        public static bool operator !=(HoldoutSnapshot left,HoldoutSnapshot right) => !left.Equals(right);
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
            // Reject malformed telemetry BEFORE ANY state mutation.
            if(!HoldoutNumbers.Finite(sample.DeltaSeconds)||sample.DeltaSeconds<0||
               !HoldoutNumbers.Finite(sample.PlayerX)||!HoldoutNumbers.Finite(sample.PlayerZ)||
               sample.HostilesInside<0)
                throw new ArgumentException("Invalid holdout telemetry");
            bool inside=Inside(sample.PlayerX,sample.PlayerZ);
            bool contested=sample.HostilesInside>0;
            // Death invalidates the attempt even after the one-shot ready event.
            if(!sample.PlayerAlive)
            {
                Reset();
                return new(HoldoutPhase.Waiting,0,Config.RequiredSeconds,inside,contested,false);
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
                ProgressSeconds=Config.RequiredSeconds;IsReady=true;
                return new(HoldoutPhase.Ready,ProgressSeconds,Config.RequiredSeconds,true,false,true);
            }
            return new(HoldoutPhase.Charging,ProgressSeconds,
                Config.RequiredSeconds,true,false,false);
        }
        public bool Inside(double x,double z)
        {
            if(!HoldoutNumbers.Finite(x)||!HoldoutNumbers.Finite(z))return false;
            double dx=x-Config.CenterX,dz=z-Config.CenterZ;
            return dx*dx+dz*dz<=Config.RadiusMeters*Config.RadiusMeters;
        }
    }
}
