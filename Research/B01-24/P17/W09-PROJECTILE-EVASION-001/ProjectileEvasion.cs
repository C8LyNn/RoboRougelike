// B01-24 PASS17 W09: original predictive projectile evasion decision engine.
// C#9, real original R3 archetypes. Does NOT move any Rigidbody, fire, damage, override Codex game AI.
// Distances XZ in metres, velocity m/s, horizon seconds; linearized after one acceleration-limited step.
using System;
using System.Collections.Generic;
using System.Numerics;
using RoboRougelike.R3;
namespace RoboRougelike.B0124.Evasion
{
    public enum EvasionState { Clear, Dodge, NoBetterPath, MobilityDisabled }
    public readonly struct IncomingProjectile
    {
        public readonly int Id;
        public readonly Vector2 PositionXZ;
        public readonly Vector2 VelocityXZ;
        public readonly float Radius;
        public readonly bool Hostile;
        public IncomingProjectile(int id, Vector2 positionXZ,Vector2 velocityXZ,float radius,bool hostile)
        { Id=id;PositionXZ=positionXZ;VelocityXZ=velocityXZ;Radius=radius;Hostile=hostile; }
    }
    public readonly struct EvasionFrame
    {
        public readonly long Tick;
        public readonly Vector2 SelfPositionXZ,SelfVelocityXZ;
        public readonly float SelfRadius,MaxSpeed,MaxAcceleration,ReactionSeconds,HorizonSeconds;
        public readonly bool CanMove;
        public readonly IncomingProjectile[] Projectiles;
        public EvasionFrame(long tick,Vector2 selfPositionXZ,Vector2 selfVelocityXZ,
            float selfRadius,float maxSpeed,float maxAcceleration,float reactionSeconds,
            float horizonSeconds,bool canMove,IncomingProjectile[] projectiles)
        {
            Tick=tick;SelfPositionXZ=selfPositionXZ;SelfVelocityXZ=selfVelocityXZ;
            SelfRadius=selfRadius;MaxSpeed=maxSpeed;MaxAcceleration=maxAcceleration;
            ReactionSeconds=reactionSeconds;HorizonSeconds=horizonSeconds;CanMove=canMove;
            Projectiles=projectiles;
        }
    }
    public readonly struct EvasionOrder
    {
        public readonly EvasionState State;
        public readonly Vector2 PreferredVelocityXZ;
        public readonly int NearestProjectileId;
        public readonly float ClosestBefore,ClosestAfter;
        public readonly int DirectionsEvaluated;
        public EvasionOrder(EvasionState state,Vector2 desired,int id,float before,float after,int count)
        { State=state;PreferredVelocityXZ=desired;NearestProjectileId=id;ClosestBefore=before;ClosestAfter=after;DirectionsEvaluated=count; }
    }
    public sealed class EvasionPolicy
    {
        private readonly float[] reactionScale;
        public readonly float SafetyMargin;
        public EvasionPolicy(float safetyMargin,float[] roleReactionScales)
        {
            if(!Finite(safetyMargin)||safetyMargin<0||safetyMargin>4||
                roleReactionScales==null||roleReactionScales.Length!=4)
                throw new ArgumentException("Invalid projectile evasive policy");
            for(int i=0;i<4;i++)if(!Finite(roleReactionScales[i])||
                roleReactionScales[i]<=0||roleReactionScales[i]>2)
                throw new ArgumentException("Invalid role reaction scale");
            SafetyMargin=safetyMargin;reactionScale=(float[])roleReactionScales.Clone();
        }
        internal static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        internal static bool Finite(Vector2 v)=>Finite(v.X)&&Finite(v.Y);
        public float ReactionFor(R3EnemyArchetype role)=>reactionScale[(int)role];
        // Original provisional role tuning, not an approved physics or balance model.
        public static EvasionPolicy Standard()=>new EvasionPolicy(.25f,new[]{1.15f,.65f,.85f,1.2f});
    }
    public sealed class ProjectileEvasionPlanner
    {
        private readonly R3EnemyArchetype role;
        private readonly EvasionPolicy policy;
        private long lastTick=-1;
        private static readonly Vector2[] Directions={
            new Vector2(1,0),new Vector2(0,1),new Vector2(-1,0),new Vector2(0,-1),
            Vector2.Normalize(new Vector2(1,1)),Vector2.Normalize(new Vector2(-1,1)),
            Vector2.Normalize(new Vector2(-1,-1)),Vector2.Normalize(new Vector2(1,-1))
        };
        public ProjectileEvasionPlanner(R3EnemyArchetype role,EvasionPolicy? policy=null)
        {
            if(!Enum.IsDefined(typeof(R3EnemyArchetype),role))
                throw new ArgumentException("Unknown canonical R3 role");
            this.role=role;this.policy=policy??EvasionPolicy.Standard();
        }
        private static float Closest(Vector2 start,Vector2 relativeVelocity,float horizon)
        {
            float vv=Vector2.Dot(relativeVelocity,relativeVelocity);
            float t=vv<=1e-12f?0:Math.Clamp(-Vector2.Dot(start,relativeVelocity)/vv,0,horizon);
            return (start+relativeVelocity*t).Length();
        }
        private static Vector2 Limit(Vector2 v,float speed)
        {
            float len=v.Length();
            return len>speed? v*(speed/len):v;
        }
        private static void Validate(EvasionFrame f,long prev)
        {
            if(f.Tick<0||f.Tick<=prev||f.Tick>long.MaxValue-1000||
                !EvasionPolicy.Finite(f.SelfPositionXZ)||!EvasionPolicy.Finite(f.SelfVelocityXZ)||
                !EvasionPolicy.Finite(f.SelfRadius)||f.SelfRadius<=0||f.SelfRadius>30||
                !EvasionPolicy.Finite(f.MaxSpeed)||f.MaxSpeed<0||f.MaxSpeed>200||
                !EvasionPolicy.Finite(f.MaxAcceleration)||f.MaxAcceleration<0||f.MaxAcceleration>500||
                !EvasionPolicy.Finite(f.ReactionSeconds)||f.ReactionSeconds<=0||f.ReactionSeconds>1||
                !EvasionPolicy.Finite(f.HorizonSeconds)||f.HorizonSeconds<=0||f.HorizonSeconds>10||
                f.Projectiles==null||f.Projectiles.Length>64)
                throw new ArgumentException("Invalid evasion simulation frame");
            var ids=new HashSet<int>();
            for(int i=0;i<f.Projectiles.Length;i++)
            {
                var p=f.Projectiles[i];
                if(p.Id<0||!ids.Add(p.Id)||!EvasionPolicy.Finite(p.PositionXZ)||
                    !EvasionPolicy.Finite(p.VelocityXZ)||!EvasionPolicy.Finite(p.Radius)||
                    p.Radius<=0||p.Radius>30)
                    throw new ArgumentException("Invalid or duplicate projectile");
            }
        }
        private readonly struct Danger
        {
            public readonly float WorstPenetration,ClosestDistance;
            public readonly int Id;
            public Danger(float risk,float distance,int id){WorstPenetration=risk;ClosestDistance=distance;Id=id;}
        }
        private Danger Evaluate(EvasionFrame f,Vector2 futureVelocity)
        {
            float worst=0,closest=float.MaxValue;
            int id=-1;
            // Collision is evaluated against a swept relative segment over [0,horizon].
            // Margin is additive, so 0 means clearance, >0 means predicted overlap.
            for(int i=0;i<f.Projectiles.Length;i++)
            {
                var p=f.Projectiles[i];
                if(!p.Hostile)continue; // friendly projectiles never trigger enemy evasive movement
                var relativeStart=p.PositionXZ-f.SelfPositionXZ;
                var relativeVelocity=p.VelocityXZ-futureVelocity;
                float d=Closest(relativeStart,relativeVelocity,f.HorizonSeconds);
                float clearance=d-(p.Radius+f.SelfRadius+policy.SafetyMargin);
                float penetration=Math.Max(0,-clearance);
                if(penetration>worst+1e-5f||
                    (Math.Abs(penetration-worst)<=1e-5f && d<closest-1e-5f)||
                    (Math.Abs(penetration-worst)<=1e-5f&&Math.Abs(d-closest)<=1e-5f&&p.Id<id))
                { worst=penetration;closest=d;id=p.Id; }
            }
            return new Danger(worst,closest,id);
        }
        public EvasionOrder Tick(EvasionFrame frame)
        {
            Validate(frame,lastTick);
            lastTick=frame.Tick;
            var baseline=Evaluate(frame,frame.SelfVelocityXZ);
            if(baseline.WorstPenetration<=1e-5f)
                return new EvasionOrder(EvasionState.Clear,frame.SelfVelocityXZ,-1,baseline.ClosestDistance,baseline.ClosestDistance,0);
            if(!frame.CanMove||frame.MaxSpeed<=0||frame.MaxAcceleration<=0)
                return new EvasionOrder(EvasionState.MobilityDisabled,frame.SelfVelocityXZ,baseline.Id,baseline.ClosestDistance,baseline.ClosestDistance,0);
            float dv=Math.Min(frame.MaxAcceleration*frame.ReactionSeconds*policy.ReactionFor(role),frame.MaxSpeed*2);
            if(dv<=0)return new EvasionOrder(EvasionState.MobilityDisabled,frame.SelfVelocityXZ,baseline.Id,baseline.ClosestDistance,baseline.ClosestDistance,0);
            Vector2 chosen=frame.SelfVelocityXZ;
            float bestRisk=baseline.WorstPenetration;
            float bestClearance=baseline.ClosestDistance;
            bool improved=false;
            for(int i=0;i<Directions.Length;i++)
            {
                var candidate=Limit(frame.SelfVelocityXZ+Directions[i]*dv,frame.MaxSpeed);
                var forecast=Evaluate(frame,candidate);
                if(forecast.WorstPenetration<bestRisk-1e-5f||
                    (improved&&Math.Abs(forecast.WorstPenetration-bestRisk)<=1e-5f&&forecast.ClosestDistance>bestClearance+1e-5f))
                {
                    chosen=candidate;bestRisk=forecast.WorstPenetration;
                    bestClearance=forecast.ClosestDistance;improved=true;
                }
            }
            return new EvasionOrder(improved?EvasionState.Dodge:EvasionState.NoBetterPath,
                chosen,baseline.Id,baseline.ClosestDistance,bestClearance,Directions.Length);
        }
    }
}
