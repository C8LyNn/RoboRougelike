using System;

namespace B01.LevelChallenges
{
    // Pure deterministic level-puzzle state machine. Unity 6000.3 C#9 / netstandard2.1.
    // No UnityEngine, enemy AI, damage, rewards, Finish override or global state.
    public enum RelayPhase { Idle, WaitingForPartner, Unlocked }

    public readonly struct RelaySite
    {
        public string Id { get; }
        public double X { get; }
        public double Z { get; }
        public double InteractionRadius { get; }
        public RelaySite(string id,double x,double z,double radius)
        {
            Id=id;X=x;Z=z;InteractionRadius=radius;
        }
    }

    public readonly struct SyncRelayConfig
    {
        public string Id { get; }
        public RelaySite A { get; }
        public RelaySite B { get; }
        public double PairWindowSeconds { get; }
        public SyncRelayConfig(string id,RelaySite a,RelaySite b,double pairWindowSeconds)
        {
            Id=id;A=a;B=b;PairWindowSeconds=pairWindowSeconds;
        }
        internal static bool Finite(double value)=>
            !double.IsNaN(value)&&!double.IsInfinity(value);
        private static void ValidateSite(RelaySite site)
        {
            if(string.IsNullOrWhiteSpace(site.Id)||site.Id.Length>64||
                !Finite(site.X)||!Finite(site.Z)||
                !Finite(site.InteractionRadius)||
                site.InteractionRadius<=0||site.InteractionRadius>4)
                throw new ArgumentException("Invalid relay site");
        }
        public void Validate()
        {
            ValidateSite(A);ValidateSite(B);
            if(string.IsNullOrWhiteSpace(Id)||Id.Length>64||
                string.Equals(A.Id,B.Id,StringComparison.Ordinal)||
                !Finite(PairWindowSeconds)||
                PairWindowSeconds<2||PairWindowSeconds>120)
                throw new ArgumentException("Invalid sync relay configuration");
            double dx=A.X-B.X,dz=A.Z-B.Z;
            if(!Finite(dx*dx+dz*dz)||
                dx*dx+dz*dz<=(A.InteractionRadius+B.InteractionRadius)*
                    (A.InteractionRadius+B.InteractionRadius))
                throw new ArgumentException("Relay sites overlap");
        }
    }

    public readonly struct SyncRelaySnapshot
    {
        public RelayPhase Phase { get; }
        public double RemainingSeconds { get; }
        public string FirstRelayId { get; }
        public bool Accepted { get; }
        public bool BecameUnlocked { get; }
        public bool TimedOut { get; }
        public bool Unlocked => Phase==RelayPhase.Unlocked;

        public SyncRelaySnapshot(RelayPhase phase,double remaining,string firstId,
            bool accepted,bool becameUnlocked,bool timedOut)
        {
            Phase=phase;RemainingSeconds=remaining;FirstRelayId=firstId;
            Accepted=accepted;BecameUnlocked=becameUnlocked;TimedOut=timedOut;
        }
    }

    /// <summary>
    /// Either switch can be activated first. The partner must be activated
    /// within a finite window. Timeout resets and allows unlimited retries;
    /// completed unlock latches once until explicit Reset or robot death.
    /// The host is authoritative for actual interaction and game Finish policy.
    /// </summary>
    public sealed class SyncRelayController
    {
        public SyncRelayConfig Config { get; }
        public RelayPhase Phase { get; private set; }
        public double RemainingSeconds { get; private set; }
        public int FirstIndex { get; private set; }

        public SyncRelayController(SyncRelayConfig config)
        {
            config.Validate();Config=config;Reset();
        }
        public void Reset()
        {
            Phase=RelayPhase.Idle;
            RemainingSeconds=0;
            FirstIndex=-1;
        }

        private SyncRelaySnapshot Result(bool accepted=false,bool unlocked=false,bool timedOut=false)
        {
            string first=FirstIndex==0?Config.A.Id:FirstIndex==1?Config.B.Id:string.Empty;
            return new SyncRelaySnapshot(Phase,RemainingSeconds,first,accepted,unlocked,timedOut);
        }
        private static bool Contains(RelaySite site,double x,double z)
        {
            double dx=x-site.X,dz=z-site.Z;
            double radius=site.InteractionRadius+1e-9;
            return dx*dx+dz*dz<=radius*radius;
        }

        public SyncRelaySnapshot Activate(string siteId,double playerX,double playerZ,
            bool playerAlive,bool paused)
        {
            // Validate all supplied telemetry before state mutation, including dead/paused.
            if(string.IsNullOrWhiteSpace(siteId)||
                !SyncRelayConfig.Finite(playerX)||!SyncRelayConfig.Finite(playerZ))
                throw new ArgumentException("Invalid relay interaction telemetry");
            int chosen=string.Equals(siteId,Config.A.Id,StringComparison.Ordinal)?0:
                string.Equals(siteId,Config.B.Id,StringComparison.Ordinal)?1:-1;
            if(chosen<0)throw new ArgumentException("Unknown relay site");
            if(!playerAlive)
            {
                Reset();return Result();
            }
            if(paused||Phase==RelayPhase.Unlocked)return Result();
            RelaySite site=chosen==0?Config.A:Config.B;
            if(!Contains(site,playerX,playerZ))return Result();
            if(Phase==RelayPhase.Idle)
            {
                Phase=RelayPhase.WaitingForPartner;
                FirstIndex=chosen;
                RemainingSeconds=Config.PairWindowSeconds;
                return Result(accepted:true);
            }
            if(chosen==FirstIndex)return Result();
            if(RemainingSeconds<=0)
            {
                Reset();return Result(timedOut:true);
            }
            Phase=RelayPhase.Unlocked;
            RemainingSeconds=0;
            return Result(accepted:true,unlocked:true);
        }

        public SyncRelaySnapshot Advance(double deltaSeconds,bool playerAlive,bool paused)
        {
            if(!SyncRelayConfig.Finite(deltaSeconds)||deltaSeconds<0)
                throw new ArgumentException("Invalid relay clock delta");
            if(!playerAlive){Reset();return Result();}
            if(paused||Phase!=RelayPhase.WaitingForPartner)return Result();
            if(deltaSeconds>=RemainingSeconds)
            {
                Reset();return Result(timedOut:true);
            }
            RemainingSeconds-=deltaSeconds;
            return Result();
        }
    }
}
