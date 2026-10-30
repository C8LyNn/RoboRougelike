using System;

namespace B01.LevelChallenges.SweepOrb
{
    public enum SweepState { Telegraph, Moving, SafeReturn }
    public readonly struct SweepConfig
    {
        public string Id { get; }
        public double X0 { get; }
        public double Z0 { get; }
        public double X1 { get; }
        public double Z1 { get; }
        public double Radius { get; }
        public double TelegraphSeconds { get; }
        public double MoveSeconds { get; }
        public double SafeSeconds { get; }
        public double OffsetSeconds { get; }
        public double Period => TelegraphSeconds+MoveSeconds+SafeSeconds;
        public SweepConfig(string id,double x0,double z0,double x1,double z1,double radius,double telegraph,double move,double safe,double offset)
        {Id=id;X0=x0;Z0=z0;X1=x1;Z1=z1;Radius=radius;TelegraphSeconds=telegraph;MoveSeconds=move;SafeSeconds=safe;OffsetSeconds=offset;}
        public static bool Finite(double d)=>!double.IsNaN(d)&&!double.IsInfinity(d);
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(Id)||Id.Length>64||!Finite(X0)||!Finite(Z0)||!Finite(X1)||!Finite(Z1)||
                !Finite(Radius)||Radius<=0||Radius>4||!Finite(TelegraphSeconds)||TelegraphSeconds<.5||
                !Finite(MoveSeconds)||MoveSeconds<.5||!Finite(SafeSeconds)||SafeSeconds<.5||
                !Finite(OffsetSeconds)||OffsetSeconds<0||!Finite(Period)||OffsetSeconds>=Period||
                (X1-X0)*(X1-X0)+(Z1-Z0)*(Z1-Z0)<.01)
                throw new ArgumentException("Invalid SweepConfig");
        }
    }
    public readonly struct SweepSample
    {
        public double Dt {get;}
        public double OldX {get;}
        public double OldZ {get;}
        public double NewX {get;}
        public double NewZ {get;}
        public bool Alive {get;}
        public bool Paused {get;}
        public SweepSample(double dt,double oldX,double oldZ,double newX,double newZ,bool alive=true,bool paused=false)
        {Dt=dt;OldX=oldX;OldZ=oldZ;NewX=newX;NewZ=newZ;Alive=alive;Paused=paused;}
    }
    public readonly struct SweepResult
    {
        public SweepState State {get;}
        public double Elapsed {get;}
        public double OrbX {get;}
        public double OrbZ {get;}
        public bool Contact {get;}
        public bool DamageRequest {get;}
        public SweepResult(SweepState state,double elapsed,double x,double z,bool contact,bool damage)
        {State=state;Elapsed=elapsed;OrbX=x;OrbZ=z;Contact=contact;DamageRequest=damage;}
    }
    /// <summary>Moving hazard with continuous relative XZ segment closest approach.
    /// Host alone owns actual Unity Rigidbody, visual cues and HP/damage application.</summary>
    public sealed class SweepController
    {
        const double Tolerance=1e-12;
        const double MaxTime=10000000;
        long lastDamageCycle;
        public SweepConfig Config {get;}
        public double Elapsed {get;private set;}
        public SweepController(SweepConfig config){config.Validate();Config=config;Reset();}
        public void Reset(){Elapsed=0;lastDamageCycle=long.MinValue;}
        static double Clamp(double x)=>Math.Max(0,Math.Min(1,x));
        static double Lerp(double a,double b,double u)=>a+(b-a)*u;
        double TimeInCycle(double t)
        {
            double p=(t+Config.OffsetSeconds)%Config.Period;
            if(p<=Tolerance||Config.Period-p<=Tolerance)return 0;
            if(Math.Abs(p-Config.TelegraphSeconds)<=Tolerance)return Config.TelegraphSeconds;
            if(Math.Abs(p-Config.TelegraphSeconds-Config.MoveSeconds)<=Tolerance)
                return Config.TelegraphSeconds+Config.MoveSeconds;
            return p;
        }
        public SweepState StateAt(double time)
        {
            if(!SweepConfig.Finite(time)||time<0||time>MaxTime)throw new ArgumentException("Invalid time");
            double p=TimeInCycle(time);
            return p<Config.TelegraphSeconds?SweepState.Telegraph:
                p<Config.TelegraphSeconds+Config.MoveSeconds?SweepState.Moving:SweepState.SafeReturn;
        }
        public void OrbPosition(double time,out double x,out double z)
        {
            double f=StateAt(time)==SweepState.Telegraph?0:
                StateAt(time)==SweepState.SafeReturn?1:
                (TimeInCycle(time)-Config.TelegraphSeconds)/Config.MoveSeconds;
            x=Lerp(Config.X0,Config.X1,f);
            z=Lerp(Config.Z0,Config.Z1,f);
        }
        bool SweptHit(SweepSample s,double from,double a,double b,double movementStart)
        {
            double u0=s.Dt==0?1:Clamp((a-from)/s.Dt);
            double u1=s.Dt==0?1:Clamp((b-from)/s.Dt);
            double px0=Lerp(s.OldX,s.NewX,u0),pz0=Lerp(s.OldZ,s.NewZ,u0);
            double px1=Lerp(s.OldX,s.NewX,u1),pz1=Lerp(s.OldZ,s.NewZ,u1);
            double f0=Clamp((a-movementStart)/Config.MoveSeconds);
            double f1=Clamp((b-movementStart)/Config.MoveSeconds);
            double dx0=px0-Lerp(Config.X0,Config.X1,f0);
            double dz0=pz0-Lerp(Config.Z0,Config.Z1,f0);
            double vx=px1-Lerp(Config.X0,Config.X1,f1)-dx0;
            double vz=pz1-Lerp(Config.Z0,Config.Z1,f1)-dz0;
            double v2=vx*vx+vz*vz;
            double t=v2==0?0:Clamp(-(dx0*vx+dz0*vz)/v2);
            double dx=dx0+t*vx,dz=dz0+t*vz,r=Config.Radius+1e-9;
            return dx*dx+dz*dz<=r*r;
        }
        public SweepResult Advance(SweepSample s)
        {
            if(!SweepConfig.Finite(s.Dt)||s.Dt<0||s.Dt>.25||
                !SweepConfig.Finite(s.OldX)||!SweepConfig.Finite(s.OldZ)||
                !SweepConfig.Finite(s.NewX)||!SweepConfig.Finite(s.NewZ)||
                !SweepConfig.Finite(Elapsed+s.Dt)||Elapsed+s.Dt>MaxTime)
                throw new ArgumentException("Invalid step; split ticks above .25s");
            double a=Elapsed,b=Elapsed+(s.Paused?0:s.Dt);
            bool contact=false,grant=false;
            if(!s.Paused&&s.Alive)
            {
                long first=(long)Math.Floor((a+Config.OffsetSeconds)/Config.Period);
                long last=(long)Math.Floor((b+Config.OffsetSeconds)/Config.Period);
                for(long cycle=first;cycle<=last;cycle++)
                {
                    double begin=cycle*Config.Period-Config.OffsetSeconds+Config.TelegraphSeconds;
                    double finish=begin+Config.MoveSeconds;
                    double lo=Math.Max(a,begin),hi=Math.Min(b,finish);
                    if(hi<lo-Tolerance)continue;
                    if(hi<lo)hi=lo;
                    if(hi==lo && (lo>=finish||StateAt(lo)!=SweepState.Moving))continue;
                    if(SweptHit(s,a,lo,hi,begin))
                    {
                        contact=true;
                        if(cycle!=lastDamageCycle){lastDamageCycle=cycle;grant=true;}
                    }
                }
            }
            Elapsed=b;
            OrbPosition(b,out double x,out double z);
            return new SweepResult(StateAt(b),b,x,z,contact,grant);
        }
    }
}
