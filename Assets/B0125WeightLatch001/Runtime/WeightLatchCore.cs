using System;

namespace B01.LevelChallenges.WeightLatch
{
    public enum GateMotion { Closed, Raising, Open, Lowering }

    public readonly struct GateConfig
    {
        public readonly double MinimumMassKg, PadHalfX, PadCenterZ, PadHalfZ, MoveSeconds, HoldSeconds;
        public GateConfig(double minimumMassKg=8,double padHalfX=1.5,double padCenterZ=-3,
            double padHalfZ=1,double moveSeconds=1.2,double holdSeconds=3)
        {
            if(!Finite(minimumMassKg)||minimumMassKg<.1||minimumMassKg>1000||
               !Finite(padHalfX)||padHalfX<=0||padHalfX>20||
               !Finite(padCenterZ)||Math.Abs(padCenterZ)>100||
               !Finite(padHalfZ)||padHalfZ<=0||padHalfZ>20||
               !Finite(moveSeconds)||moveSeconds<.2||moveSeconds>20||
               !Finite(holdSeconds)||holdSeconds<=0||holdSeconds>60)
                throw new ArgumentException("Invalid physically gated latch parameters");
            MinimumMassKg=minimumMassKg;PadHalfX=padHalfX;PadCenterZ=padCenterZ;
            PadHalfZ=padHalfZ;MoveSeconds=moveSeconds;HoldSeconds=holdSeconds;
        }
        public static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    }

    public readonly struct GateInput
    {
        public readonly double Dt,X,Z,MassKg;
        public readonly bool RealPadContact,Paused,Alive;
        public GateInput(double dt,double x,double z,double massKg,
            bool realPadContact,bool paused=false,bool alive=true)
        {
            Dt=dt;X=x;Z=z;MassKg=massKg;RealPadContact=realPadContact;
            Paused=paused;Alive=alive;
        }
    }

    public readonly struct GateState
    {
        public readonly GateMotion Motion;
        public readonly double OpenFraction,HoldRemainingSeconds,ElapsedSeconds;
        public readonly int ActivationCount;
        public readonly bool QualifiedHeavyContact;
        public GateState(GateMotion motion,double fraction,double hold,double elapsed,int activations,bool qualified)
        {
            Motion=motion;OpenFraction=fraction;HoldRemainingSeconds=hold;
            ElapsedSeconds=elapsed;ActivationCount=activations;QualifiedHeavyContact=qualified;
        }
    }

    /// <summary>
    /// Standalone deterministic pressure-latch gameplay policy. Unity host owns
    /// real Collider.Raycast proof, physical Rigidbody gate travel and vehicle
    /// controller; source-only CSharp9 tests do not claim to run PhysX.
    /// </summary>
    public sealed class WeightLatchController
    {
        public GateConfig Config {get;}
        public GateState State {get;private set;}
        public WeightLatchController(GateConfig config)
        {
            // In CSharp9, new GateConfig() on a struct yields default zero fields,
            // despite optional arguments of the user-defined constructor.
            // Reject that footgun before any state can advance.
            if(!GateConfig.Finite(config.MinimumMassKg)||config.MinimumMassKg<.1||
               !GateConfig.Finite(config.PadHalfX)||config.PadHalfX<=0||
               !GateConfig.Finite(config.PadHalfZ)||config.PadHalfZ<=0||
               !GateConfig.Finite(config.MoveSeconds)||config.MoveSeconds<.2||
               !GateConfig.Finite(config.HoldSeconds)||config.HoldSeconds<=0)
                throw new ArgumentException("Missing or zero-initialized gate config");
            Config=config;Reset();
        }
        public void Reset(){State=new GateState(GateMotion.Closed,0,0,0,0,false);}
        public GateState Advance(GateInput input)
        {
            // All reject conditions occur BEFORE any state mutation.
            if(!GateConfig.Finite(input.Dt)||input.Dt<0||input.Dt>.05||
               !GateConfig.Finite(input.X)||Math.Abs(input.X)>1000000||
               !GateConfig.Finite(input.Z)||Math.Abs(input.Z)>1000000||
               !GateConfig.Finite(input.MassKg)||input.MassKg<.1||input.MassKg>1000)
                throw new ArgumentException("Invalid physical contact frame");
            var before=State;
            if(input.Paused||input.Dt==0)return before;
            bool qualifies=input.Alive&&input.RealPadContact&&
                input.MassKg>=Config.MinimumMassKg&&
                Math.Abs(input.X)<=Config.PadHalfX&&
                Math.Abs(input.Z-Config.PadCenterZ)<=Config.PadHalfZ;
            double hold=qualifies?Config.HoldSeconds:Math.Max(0,before.HoldRemainingSeconds-input.Dt);
            bool targetOpen=hold>0;
            double fraction=before.OpenFraction+(targetOpen?input.Dt:-input.Dt)/Config.MoveSeconds;
            fraction=Math.Max(0,Math.Min(1,fraction));
            double nextTime=before.ElapsedSeconds+input.Dt;
            if(!GateConfig.Finite(hold)||!GateConfig.Finite(fraction)||
               !GateConfig.Finite(nextTime))
                throw new ArgumentException("Gate arithmetic overflow");
            GateMotion motion=targetOpen?(fraction>=1?GateMotion.Open:GateMotion.Raising):
                (fraction<=0?GateMotion.Closed:GateMotion.Lowering);
            int activations=before.ActivationCount+(qualifies&&before.HoldRemainingSeconds<=0?1:0);
            if(activations<before.ActivationCount)throw new ArgumentException("Activation overflow");
            State=new GateState(motion,fraction,hold,nextTime,activations,qualifies);
            return State;
        }
    }
}
