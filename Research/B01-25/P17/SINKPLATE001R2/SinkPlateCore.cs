using System;
namespace B01.LevelChallenges.SinkPlate.R2
{
    public enum PlatePhase { Ready, Warning, Dropped, Recovering, AwaitClear }

    public readonly struct PlateConfig
    {
        public double HeavyMassKg { get; }
        public double LightWarningSeconds { get; }
        public double HeavyWarningSeconds { get; }
        public double DropSeconds { get; }
        public double RecoverySeconds { get; }
        public PlateConfig(double heavyKg,double lightWarning,double heavyWarning,double drop,double recovery)
        {HeavyMassKg=heavyKg;LightWarningSeconds=lightWarning;HeavyWarningSeconds=heavyWarning;DropSeconds=drop;RecoverySeconds=recovery;}
        public static bool Finite(double x) { return !double.IsNaN(x)&&!double.IsInfinity(x); }
        public void Validate()
        {
            if(!Finite(HeavyMassKg)||HeavyMassKg<1||HeavyMassKg>100||
               !Finite(LightWarningSeconds)||LightWarningSeconds<.2||LightWarningSeconds>5||
               !Finite(HeavyWarningSeconds)||HeavyWarningSeconds<.2||HeavyWarningSeconds>5||
               HeavyWarningSeconds>=LightWarningSeconds||
               !Finite(DropSeconds)||DropSeconds<.5||DropSeconds>10||
               !Finite(RecoverySeconds)||RecoverySeconds<.2||RecoverySeconds>10)
                throw new ArgumentException("Invalid sink platform timing");
        }
    }

    public readonly struct PlateInput
    {
        public double Dt { get; }
        public bool OnPlate { get; }
        public double MassKg { get; }
        public bool Paused { get; }
        public PlateInput(double dt,bool onPlate,double massKg,bool paused=false)
        {Dt=dt;OnPlate=onPlate;MassKg=massKg;Paused=paused;}
    }
    public readonly struct PlateState
    {
        public PlatePhase Phase { get; }
        public double PhaseElapsed { get; }
        public double WarningDuration { get; }
        public bool SupportsVehicle { get; }
        public bool CollapseStarted { get; }
        public int CompletedCollapses { get; }
        public PlateState(PlatePhase phase,double elapsed,double warning,bool supports,bool started,int completed)
        {Phase=phase;PhaseElapsed=elapsed;WarningDuration=warning;SupportsVehicle=supports;CollapseStarted=started;CompletedCollapses=completed;}
    }
    /// <summary>
    /// Deterministic one-plate state machine. Host supplies actual Rigidbody mass and
    /// actual XZ overlap; host owns Collider.enabled, visuals, player HP and physics.
    /// Alternate safe lane is a property of the authored level geometry, not this core.
    /// </summary>
    public sealed class SinkPlateController
    {
        const double Eps=1e-12;
        public PlateConfig Config { get; }
        public PlatePhase Phase { get; private set; }
        public double PhaseElapsed { get; private set; }
        public double LockedWarning { get; private set; }
        public int CompletedCollapses { get; private set; }
        public SinkPlateController(PlateConfig config){config.Validate();Config=config;Reset();}
        public void Reset()
        {Phase=PlatePhase.Ready;PhaseElapsed=0;LockedWarning=0;CompletedCollapses=0;}
        public PlateState Snapshot(bool collapsedNow=false)
        {
            bool supports=Phase==PlatePhase.Ready||Phase==PlatePhase.Warning||Phase==PlatePhase.AwaitClear;
            return new PlateState(Phase,PhaseElapsed,LockedWarning,supports,collapsedNow,CompletedCollapses);
        }
        public PlateState Advance(PlateInput input)
        {
            // Fail closed atomically before any clock/latch mutation.
            if(!PlateConfig.Finite(input.Dt)||input.Dt<0||input.Dt>.1||
               !PlateConfig.Finite(input.MassKg)||input.MassKg<.1||input.MassKg>1000)
                throw new ArgumentException("Invalid host plate sample (substep <= 0.1s)");
            if(input.Paused)return Snapshot();
            bool collapse=false;
            if(Phase==PlatePhase.Ready)
            {
                if(!input.OnPlate)return Snapshot();
                LockedWarning=input.MassKg>=Config.HeavyMassKg?Config.HeavyWarningSeconds:Config.LightWarningSeconds;
                Phase=PlatePhase.Warning;PhaseElapsed=0;
            }
            double remaining=input.Dt;
            for(int guard=0;guard<5;guard++)
            {
                double limit=Phase==PlatePhase.Warning?LockedWarning:
                             Phase==PlatePhase.Dropped?Config.DropSeconds:
                             Phase==PlatePhase.Recovering?Config.RecoverySeconds:0;
                if(limit==0)break;
                double need=Math.Max(0,limit-PhaseElapsed);
                if(remaining+Eps<need)
                {
                    PhaseElapsed+=remaining;
                    break;
                }
                remaining=Math.Max(0,remaining-need);
                PhaseElapsed=0;
                if(Phase==PlatePhase.Warning)
                {Phase=PlatePhase.Dropped;collapse=true;CompletedCollapses++;}
                else if(Phase==PlatePhase.Dropped)Phase=PlatePhase.Recovering;
                else if(Phase==PlatePhase.Recovering)Phase=PlatePhase.AwaitClear;
                if(Phase==PlatePhase.AwaitClear)break;
            }
            // No automatic retrigger even if player remains on plate after recovery.
            // A fresh entry sample is mandatory to re-arm, protecting stranded vehicles.
            if(Phase==PlatePhase.AwaitClear && !input.OnPlate)
            {Phase=PlatePhase.Ready;PhaseElapsed=0;LockedWarning=0;}
            return Snapshot(collapse);
        }
    }
}
