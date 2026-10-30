using System;

namespace B01.LevelChallenges.Airlock
{
    public enum AirlockPhase { Idle, EntryOpening, AwaitChamber, EntryClosing,
        Equalizing, ExitOpening, AwaitExit, ExitClosing, RecoveryCloseExit, RecoveryOpenEntry }

    public sealed class AirlockConfig
    {
        public double TravelSeconds { get; }
        public double EqualizeSeconds { get; }
        public AirlockConfig(double travelSeconds, double equalizeSeconds)
        {
            if (!Finite(travelSeconds) || travelSeconds < .2 || travelSeconds > 10 ||
                !Finite(equalizeSeconds) || equalizeSeconds < .1 || equalizeSeconds > 30)
                throw new ArgumentException("Invalid airlock time configuration");
            TravelSeconds=travelSeconds; EqualizeSeconds=equalizeSeconds;
        }
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public readonly struct AirlockInput
    {
        public readonly double Dt, MassKg;
        public readonly bool Approach, Chamber, Exterior, EntryObstructed,
            ExitObstructed, Emergency, Paused, Alive;
        public AirlockInput(double dt, double massKg, bool approach, bool chamber,
            bool exterior, bool entryObstructed, bool exitObstructed,
            bool emergency=false, bool paused=false, bool alive=true)
        {
            Dt=dt; MassKg=massKg; Approach=approach; Chamber=chamber;
            Exterior=exterior; EntryObstructed=entryObstructed;
            ExitObstructed=exitObstructed; Emergency=emergency;
            Paused=paused; Alive=alive;
        }
    }

    public readonly struct AirlockState
    {
        public readonly AirlockPhase Phase;
        public readonly double EntryOpen, ExitOpen, PhaseSeconds, Clock;
        public readonly int CompletedCycles, EmergencyRequests;
        public AirlockState(AirlockPhase phase, double entry, double exit,
            double phaseSeconds, double clock, int cycles, int emergencies)
        {
            Phase=phase;EntryOpen=entry;ExitOpen=exit;PhaseSeconds=phaseSeconds;
            Clock=clock;CompletedCycles=cycles;EmergencyRequests=emergencies;
        }
    }

    /// <summary>
    /// Deterministic dual-gate policy. Sensors are measurements supplied by
    /// the Unity host's real collider probes; this class is not PhysX.
    /// No state commit occurs on rejected samples or overflow.
    /// </summary>
    public sealed class AirlockController
    {
        public AirlockConfig Config { get; }
        public AirlockState State { get; private set; }
        public AirlockController(AirlockConfig config)
        {Config=config ?? throw new ArgumentNullException(nameof(config));Reset();}
        public void Reset()
        {State=new AirlockState(AirlockPhase.Idle,0,0,0,0,0,0);}

        public AirlockState Advance(AirlockInput x)
        {
            if(!AirlockConfig.Finite(x.Dt) || x.Dt<0 || x.Dt>.05 ||
               !AirlockConfig.Finite(x.MassKg) || x.MassKg<.1 || x.MassKg>1000)
                throw new ArgumentException("Invalid airlock physics sample");
            AirlockState s=State;
            if(x.Paused||x.Dt==0)return s;
            double nextClock=s.Clock+x.Dt;
            if(!AirlockConfig.Finite(nextClock))throw new ArgumentException("Clock overflow");
            AirlockPhase phase=s.Phase;
            double a=s.EntryOpen,b=s.ExitOpen,progress=s.PhaseSeconds+x.Dt;
            int cycles=s.CompletedCycles,emergencies=s.EmergencyRequests;
            if(x.Emergency && phase!=AirlockPhase.RecoveryCloseExit &&
               phase!=AirlockPhase.RecoveryOpenEntry)
            {
                if(emergencies==int.MaxValue)throw new ArgumentException("Emergency count overflow");
                emergencies++;
                phase=b>0?AirlockPhase.RecoveryCloseExit:AirlockPhase.RecoveryOpenEntry;
                progress=0;
            }
            if(!x.Alive && phase==AirlockPhase.Idle)
            {
                // No dead-body sensor shall ever initiate a new cycle.
                return Commit(phase,a,b,progress,nextClock,cycles,emergencies);
            }
            double step=x.Dt/Config.TravelSeconds;
            switch(phase)
            {
                case AirlockPhase.Idle:
                    if(x.Alive&&x.Approach){phase=AirlockPhase.EntryOpening;progress=0;}
                    break;
                case AirlockPhase.EntryOpening:
                    a=Math.Min(1,a+step);
                    if(a>=1){phase=AirlockPhase.AwaitChamber;progress=0;}
                    break;
                case AirlockPhase.AwaitChamber:
                    if(x.Alive&&x.Chamber&&!x.EntryObstructed)
                    {phase=AirlockPhase.EntryClosing;progress=0;}
                    break;
                case AirlockPhase.EntryClosing:
                    if(x.EntryObstructed)
                    {phase=AirlockPhase.EntryOpening;progress=0;}
                    else
                    {
                        a=Math.Max(0,a-step);
                        if(a<=0){phase=AirlockPhase.Equalizing;progress=0;}
                    }
                    break;
                case AirlockPhase.Equalizing:
                    if(progress>=Config.EqualizeSeconds)
                    {phase=AirlockPhase.ExitOpening;progress=0;}
                    break;
                case AirlockPhase.ExitOpening:
                    b=Math.Min(1,b+step);
                    if(b>=1){phase=AirlockPhase.AwaitExit;progress=0;}
                    break;
                case AirlockPhase.AwaitExit:
                    if(x.Exterior&&!x.ExitObstructed)
                    {phase=AirlockPhase.ExitClosing;progress=0;}
                    break;
                case AirlockPhase.ExitClosing:
                    if(x.ExitObstructed)
                    {phase=AirlockPhase.ExitOpening;progress=0;}
                    else
                    {
                        b=Math.Max(0,b-step);
                        if(b<=0)
                        {
                            if(cycles==int.MaxValue)throw new ArgumentException("Cycle count overflow");
                            cycles++;phase=AirlockPhase.Idle;progress=0;
                        }
                    }
                    break;
                case AirlockPhase.RecoveryCloseExit:
                    if(x.ExitObstructed)
                    {
                        // Preserve an exit opening while a collider is in the threshold.
                        b=Math.Min(1,b+step);
                    }
                    else
                    {
                        b=Math.Max(0,b-step);
                        if(b<=0){phase=AirlockPhase.RecoveryOpenEntry;progress=0;}
                    }
                    break;
                case AirlockPhase.RecoveryOpenEntry:
                    // Exit must be fully closed before re-opening Entry.
                    if(b!=0)throw new ArgumentException("Interlock recovery invariant");
                    a=Math.Min(1,a+step);
                    break;
                default: throw new ArgumentException("Unknown phase");
            }
            if(a>0 && b>0 || a<0 || a>1 || b<0 || b>1 ||
               !AirlockConfig.Finite(a) || !AirlockConfig.Finite(b) ||
               !AirlockConfig.Finite(progress))
                throw new ArgumentException("Interlock overlap or numeric overflow");
            return Commit(phase,a,b,progress,nextClock,cycles,emergencies);
        }
        private AirlockState Commit(AirlockPhase phase,double a,double b,double time,
            double clock,int cycles,int emergencies)
        {
            var result=new AirlockState(phase,a,b,time,clock,cycles,emergencies);
            State=result;return result;
        }
    }
}
