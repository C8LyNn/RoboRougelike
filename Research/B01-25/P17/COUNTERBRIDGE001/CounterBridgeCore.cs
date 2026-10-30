using System;

namespace B01.LevelChallenges.CounterBridge
{
    public readonly struct CounterBridgeConfig
    {
        public readonly double HalfWidth,HalfLength,Inertia,Spring,Damping,MaxAngleDegrees,Gravity;
        public CounterBridgeConfig(double halfWidth,double halfLength,double inertia,
            double spring,double damping,double maxAngleDegrees,double gravity=9.81)
        {
            if(!Valid(halfWidth)||halfWidth<=.5||halfWidth>20||
               !Valid(halfLength)||halfLength<=.5||halfLength>20||
               !Valid(inertia)||inertia<=1||inertia>100000||
               !Valid(spring)||spring<0||spring>100000||
               !Valid(damping)||damping<0||damping>100000||
               !Valid(maxAngleDegrees)||maxAngleDegrees<1||maxAngleDegrees>35||
               !Valid(gravity)||gravity<=0||gravity>30)
                throw new ArgumentException("Invalid counterbridge physical configuration");
            HalfWidth=halfWidth;HalfLength=halfLength;Inertia=inertia;
            Spring=spring;Damping=damping;MaxAngleDegrees=maxAngleDegrees;Gravity=gravity;
        }
        public static bool Valid(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }

    public readonly struct BridgeSample
    {
        public readonly double Dt,X,Z,MassKg;
        public readonly bool PhysicallySupported,Paused,Alive;
        public BridgeSample(double dt,double x,double z,double massKg,
            bool physicallySupported,bool paused=false,bool alive=true)
        {
            Dt=dt;X=x;Z=z;MassKg=massKg;PhysicallySupported=physicallySupported;
            Paused=paused;Alive=alive;
        }
    }

    public readonly struct BridgeState
    {
        public readonly double AngleDeg,AngularVelocityDegS,PressureTorqueNm,Elapsed;
        public readonly bool VehicleSupported,AtMechanicalStop;
        public BridgeState(double angle,double velocity,double torque,double elapsed,bool supported,bool stop)
        {AngleDeg=angle;AngularVelocityDegS=velocity;PressureTorqueNm=torque;
         Elapsed=elapsed;VehicleSupported=supported;AtMechanicalStop=stop;}
    }

    /// <summary>
    /// Original, deterministic semi-implicit rotational spring model. The real
    /// Unity host decides contact via Collider.Raycast on the ACTUAL bridge;
    /// this portable source is NOT itself an execution of PhysX.
    /// </summary>
    public sealed class CounterBridgeController
    {
        private const double RadiansToDegrees=180/Math.PI;
        private const double DegreesToRadians=Math.PI/180;
        public CounterBridgeConfig Config {get;}
        public double AngleRadians {get;private set;}
        public double AngularVelocity {get;private set;}
        public double Clock {get;private set;}
        public BridgeState Last {get;private set;}
        public CounterBridgeController(CounterBridgeConfig config)
        {Config=config;Reset();}
        public void Reset()
        {
            AngleRadians=0;AngularVelocity=0;Clock=0;
            Last=new BridgeState(0,0,0,0,false,false);
        }
        public BridgeState Advance(BridgeSample sample)
        {
            // Fail closed BEFORE changing any value; even paused telemetry validated.
            if(!CounterBridgeConfig.Valid(sample.Dt)||sample.Dt<0||sample.Dt>.05||
               !CounterBridgeConfig.Valid(sample.X)||Math.Abs(sample.X)>1000000||
               !CounterBridgeConfig.Valid(sample.Z)||Math.Abs(sample.Z)>1000000||
               !CounterBridgeConfig.Valid(sample.MassKg)||sample.MassKg<.1||sample.MassKg>1000)
                throw new ArgumentException("Invalid physical support telemetry");
            if(sample.Paused||sample.Dt==0)return Last;
            bool support=sample.Alive&&sample.PhysicallySupported&&
                Math.Abs(sample.X)<=Config.HalfWidth&&Math.Abs(sample.Z)<=Config.HalfLength;
            double torque=support?sample.MassKg*Config.Gravity*sample.Z:0;
            // Preflight derived arithmetic; never allow nonfinite state commit.
            double acceleration=(torque-Config.Spring*AngleRadians-Config.Damping*AngularVelocity)/Config.Inertia;
            double nextVelocity=AngularVelocity+acceleration*sample.Dt;
            double nextAngle=AngleRadians+nextVelocity*sample.Dt;
            if(!CounterBridgeConfig.Valid(torque)||!CounterBridgeConfig.Valid(acceleration)||
               !CounterBridgeConfig.Valid(nextVelocity)||!CounterBridgeConfig.Valid(nextAngle))
                throw new ArgumentException("Counterbridge state overflow");
            double max=Config.MaxAngleDegrees*DegreesToRadians;
            bool stopped=false;
            if(nextAngle>max)
            {nextAngle=max;nextVelocity=0;stopped=true;}
            else if(nextAngle< -max)
            {nextAngle= -max;nextVelocity=0;stopped=true;}
            double nextClock=Clock+sample.Dt;
            if(!CounterBridgeConfig.Valid(nextClock))
                throw new ArgumentException("Counterbridge clock overflow");
            AngleRadians=nextAngle;AngularVelocity=nextVelocity;Clock=nextClock;
            Last=new BridgeState(nextAngle*RadiansToDegrees,nextVelocity*RadiansToDegrees,
                torque,Clock,support,stopped);
            return Last;
        }
    }
}
