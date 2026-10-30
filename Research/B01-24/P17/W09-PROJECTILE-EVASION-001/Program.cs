// B01-24 W09 — Executable original C#9 regression suite for actual swept 2D projectile evasion.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.B0124.Evasion;
internal static class Program
{
    static int passed,failed;
    static void Check(string name,Action act)
    {
        try { act(); passed++; Console.WriteLine("PASS "+name); }
        catch(Exception ex){failed++;Console.Error.WriteLine("FAIL "+name+": "+ex.Message);}
    }
    static void Ensure(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Reject(Action fn){try{fn();}catch(ArgumentException){return;}throw new Exception("Malformed input accepted");}
    static Vector2 V(float x=0,float y=0)=>new Vector2(x,y);
    static IncomingProjectile P(int id=1,float x=10,float y=0,float vx=-10,float vy=0,float radius=.3f,bool hostile=true)=>
        new IncomingProjectile(id,V(x,y),V(vx,vy),radius,hostile);
    static EvasionFrame F(long tick=0,bool move=true,Vector2? pos=null,Vector2? vel=null,float rad=1,
        float maxSpeed=6,float accel=15,float reaction=.5f,float horizon=2,params IncomingProjectile[] shots)=>
        new EvasionFrame(tick,pos??V(),vel??V(),rad,maxSpeed,accel,reaction,horizon,move,shots);
    static string GitBlob(string path)
    {
        byte[] v=File.ReadAllBytes(path),h=Encoding.ASCII.GetBytes("blob "+File.ReadAllBytes(path).Length+"\0");
        var d=new byte[h.Length+v.Length];Buffer.BlockCopy(h,0,d,0,h.Length);Buffer.BlockCopy(v,0,d,h.Length,v.Length);
        return Convert.ToHexString(SHA1.HashData(d)).ToLowerInvariant();
    }
    static string Scenarios()
    {
        string file="Research/B01-24/P17/W09-PROJECTILE-EVASION-001/evasion_scenarios.json";
        using var data=JsonDocument.Parse(File.ReadAllText(file));
        var root=data.RootElement;
        Ensure(root.GetProperty("schema").GetString()=="B01-24-PROJECTILE-EVASION-001-v1","scene schema drift");
        Ensure(root.GetProperty("original_R3_blob").GetString()==GitBlob("Assets/R3/R3EnemyArchetype.cs"),"original source changed");
        var scenarios=root.GetProperty("scenarios").EnumerateArray().ToArray();
        Ensure(scenarios.Length==3,"need 3 original scenarios");
        Vector2 Vec(JsonElement a)=>new Vector2(a[0].GetSingle(),a[1].GetSingle());
        var trace=new List<string>();
        foreach(var sc in scenarios)
        {
            var role=Enum.Parse<R3EnemyArchetype>(sc.GetProperty("role").GetString()!);
            var solver=new ProjectileEvasionPlanner(role);
            int frames=0,evades=0;
            foreach(var step in sc.GetProperty("frames").EnumerateArray())
            {
                var shots=step.GetProperty("shots").EnumerateArray().Select(s=>new IncomingProjectile(
                    s.GetProperty("id").GetInt32(),Vec(s.GetProperty("pos")),Vec(s.GetProperty("velocity")),
                    s.GetProperty("radius").GetSingle(),s.GetProperty("hostile").GetBoolean())).ToArray();
                long tick=step.GetProperty("tick").GetInt64();
                var f=new EvasionFrame(tick,Vec(step.GetProperty("self")),
                    Vec(step.GetProperty("selfVelocity")),step.GetProperty("selfRadius").GetSingle(),
                    step.GetProperty("maxSpeed").GetSingle(),step.GetProperty("maxAcceleration").GetSingle(),
                    step.GetProperty("reactionSeconds").GetSingle(),step.GetProperty("horizonSeconds").GetSingle(),
                    step.GetProperty("canMove").GetBoolean(),shots);
                var order=solver.Tick(f);
                if(order.State==EvasionState.Dodge)evades++;
                var expected=Enum.Parse<EvasionState>(step.GetProperty("expect").GetString()!);
                Ensure(order.State==expected,"authored "+sc.GetProperty("id").GetString()+" tick="+tick+
                    " got "+order.State+" expected "+expected);
                trace.Add(sc.GetProperty("id").GetString()+"|"+tick+"|"+order.State+"|"+order.NearestProjectileId+"|"+
                    order.PreferredVelocityXZ.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                    order.PreferredVelocityXZ.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                    order.ClosestBefore.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                    order.ClosestAfter.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                    order.DirectionsEvaluated);
                frames++;
            }
            Ensure(frames>=3,"scenario too short");
            Console.WriteLine("REPLAY "+sc.GetProperty("id").GetString()+" frames="+frames+" dodges="+evades);
        }
        var sha=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",trace)))).ToLowerInvariant();
        Console.WriteLine("REPLAY_SHA256 "+sha+" ("+trace.Count+" executed original source decisions)");
        return sha;
    }
    public static int Main()
    {
        Check("original unchanged Codex R3 source pinned to SHA-1",()=>Ensure(
            GitBlob("Assets/R3/R3EnemyArchetype.cs")=="66cd1ee734b129117b3753d1205469a0496cb673","R3 changed"));
        Check("original R3 four roles and profile compiled, not mocked",()=>{
            Ensure(R3EnemyProfiles.InEncounter(2,1)==R3EnemyArchetype.Gunner&&
                (int)R3EnemyArchetype.Flanker==3,"role ABI changed");
        });
        Check("empty projectile frame returns Clear and unchanged motion",()=>{
            var s=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            var o=s.Tick(F());Ensure(o.State==EvasionState.Clear&&o.PreferredVelocityXZ==V()&&o.DirectionsEvaluated==0,"phantom hazard");
        });
        Check("friendly shot never triggers an evasive maneuver",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P(hostile:false)}));
            Ensure(o.State==EvasionState.Clear,"friendly shot risk");
        });
        Check("actual hostile head-on bullet produces a dodge",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P()}));
            Ensure(o.State==EvasionState.Dodge&&o.NearestProjectileId==1&&o.ClosestAfter>o.ClosestBefore+1f,"no useful dodge");
        });
        Check("selected dodge respects maximum linearized speed",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Flanker).Tick(F(shots:new[]{P()}));
            Ensure(o.PreferredVelocityXZ.Length()<=6.0001f,"speed limit breached");
        });
        Check("actual candidate selection evaluates all eight steering directions",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Bruiser).Tick(F(shots:new[]{P()}));
            Ensure(o.DirectionsEvaluated==8,"candidate count wrong");
        });
        Check("distant projectile whose arrival is outside horizon is ignored",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(horizon:1,shots:new[]{P(x:70)}));
            Ensure(o.State==EvasionState.Clear,"false distant dodge");
        });
        Check("stationary projectile far outside movement corridor harmless",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P(x:0,y:80,vx:0)}));
            Ensure(o.State==EvasionState.Clear,"false static dodge");
        });
        Check("projectile moving away has no future collision",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P(vx:10)}));
            Ensure(o.State==EvasionState.Clear,"pursued departing shot");
        });
        Check("off-axis near miss leaves existing movement alone",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P(y:8)}));
            Ensure(o.State==EvasionState.Clear,"over-reacted to miss");
        });
        Check("zero vehicle movement ability reports MobilityDisabled",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(move:false,shots:new[]{P()}));
            Ensure(o.State==EvasionState.MobilityDisabled&&o.PreferredVelocityXZ==V(),"impossible dodge issued");
        });
        Check("zero speed is not an imaginary teleport",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(maxSpeed:0,shots:new[]{P()}));
            Ensure(o.State==EvasionState.MobilityDisabled,"zero speed dodge");
        });
        Check("zero acceleration is not an imaginary dodge",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(accel:0,shots:new[]{P()}));
            Ensure(o.State==EvasionState.MobilityDisabled,"zero acceleration dodge");
        });
        Check("immediate overlap and inability to move is flagged",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Gunner).Tick(F(move:false,shots:new[]{P(x:0)}));
            Ensure(o.State==EvasionState.MobilityDisabled&&o.ClosestBefore<.1f,"overlap hidden");
        });
        Check("two projectile ids are independent of incoming array order",()=>{
            var a=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            var b=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            var ps=new[]{P(id:5),P(id:3,y:1)};
            var x=a.Tick(F(shots:ps));var y=b.Tick(F(shots:new[]{ps[1],ps[0]}));
            Ensure(x.State==y.State&&x.PreferredVelocityXZ==y.PreferredVelocityXZ&&x.NearestProjectileId==y.NearestProjectileId,"order-dependent action");
        });
        Check("projectile velocity as 2D relative motion catches crossing shot",()=>{
            var o=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout).Tick(F(shots:new[]{P(x:0,y:10,vx:0,vy:-10)}));
            Ensure(o.State==EvasionState.Dodge,"axis-dependent false negative");
        });
        Check("four canonical R3 roles all run evasion with their actual policy",()=>{
            foreach(R3EnemyArchetype role in Enum.GetValues(typeof(R3EnemyArchetype))){
                var o=new ProjectileEvasionPlanner(role).Tick(F(shots:new[]{P()}));
                Ensure(o.State==EvasionState.Dodge,"role could not dodge "+role);
            }
        });
        Check("Scout reacts more aggressively than Bruiser under fixed ability",()=>{
            var policy=EvasionPolicy.Standard();
            Ensure(policy.ReactionFor(R3EnemyArchetype.Scout)>policy.ReactionFor(R3EnemyArchetype.Bruiser),"role balance order");
        });
        Check("external mutation of reaction array cannot change policy",()=>{
            float[] a={1.1f,.5f,.9f,1.2f};
            var policy=new EvasionPolicy(.1f,a);a[0]=.2f;
            Ensure(policy.ReactionFor(R3EnemyArchetype.Scout)==1.1f,"outside policy mutation");
        });
        Check("unspecified original role rejected fail-closed",()=>Reject(()=>new ProjectileEvasionPlanner((R3EnemyArchetype)91)));
        Check("nonmonotonic tick rejected without damaging following decision",()=>{
            var planner=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            planner.Tick(F(tick:0));
            Reject(()=>planner.Tick(F(tick:0,shots:new[]{P()})));
            Reject(()=>planner.Tick(F(tick:-1,shots:new[]{P()})));
            Ensure(planner.Tick(F(tick:1,shots:new[]{P()})).State==EvasionState.Dodge,"invalid tick damaged planner");
        });
        Check("overflow tick rejected before changing planner",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(tick:long.MaxValue)));
            Ensure(p.Tick(F(tick:0)).State==EvasionState.Clear,"overflow damaged state");
        });
        Check("NaN self position rejected without changing tick",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(pos:V(float.NaN,0))));
            Ensure(p.Tick(F(shots:new[]{P()})).State==EvasionState.Dodge,"bad position altered state");
        });
        Check("infinite self velocity rejected before changing tick",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(vel:V(0,float.PositiveInfinity))));
            Ensure(p.Tick(F()).State==EvasionState.Clear,"bad velocity altered state");
        });
        Check("infinite and NaN projectile position rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(shots:new[]{P(x:float.PositiveInfinity)})));
            Reject(()=>p.Tick(F(shots:new[]{P(y:float.NaN)})));
        });
        Check("nonfinite bullet velocity rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(shots:new[]{P(vx:float.NaN)})));
            Reject(()=>p.Tick(F(shots:new[]{P(vy:float.NegativeInfinity)})));
        });
        Check("invalid radii or horizon rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(rad:-1)));Reject(()=>p.Tick(F(rad:float.NaN)));
            Reject(()=>p.Tick(F(horizon:0)));Reject(()=>p.Tick(F(horizon:float.PositiveInfinity)));
        });
        Check("bad acceleration, speed and reaction time rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(maxSpeed:-1)));Reject(()=>p.Tick(F(accel:float.NaN)));
            Reject(()=>p.Tick(F(reaction:0)));
        });
        Check("projectile zero radius and negative ID rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(shots:new[]{P(radius:0)})));
            Reject(()=>p.Tick(F(shots:new[]{P(id:-1)})));
        });
        Check("duplicate projectile IDs rejected before changing tick",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(shots:new[]{P(id:2),P(id:2)})));
            Ensure(p.Tick(F(shots:new[]{P()})).State==EvasionState.Dodge,"duplicate modified state");
        });
        Check("null and overlarge projectile batches rejected",()=>{
            var p=new ProjectileEvasionPlanner(R3EnemyArchetype.Scout);
            Reject(()=>p.Tick(F(shots:null!)));
            Reject(()=>p.Tick(F(shots:Enumerable.Range(0,65).Select(i=>P(id:i)).ToArray())));
        });
        Check("bad policy bounds and role scales rejected",()=>{
            Reject(()=>new EvasionPolicy(-1,new[]{1f,1f,1f,1f}));
            Reject(()=>new EvasionPolicy(.1f,new[]{1f,float.NaN,1f,1f}));
            Reject(()=>new EvasionPolicy(.1f,new[]{1f,0f,1f,1f}));
            Reject(()=>new EvasionPolicy(.1f,new[]{1f,1f}));
        });
        Check("independent planners do not leak simulation ticks",()=>{
            var a=new ProjectileEvasionPlanner(R3EnemyArchetype.Gunner);
            var b=new ProjectileEvasionPlanner(R3EnemyArchetype.Gunner);
            a.Tick(F(tick:20));
            Ensure(b.Tick(F(tick:0,shots:new[]{P()})).State==EvasionState.Dodge,"planners shared state");
        });
        Check("real authored three projectile engagements execute",()=>{
            Ensure(Scenarios().Length==64,"scenario digest");
        });
        Check("authored scenario traces byte-identical after independent replay",()=>{
            Ensure(Scenarios()==Scenarios(),"trace unstable");
        });
        Console.WriteLine("B01-24 W09 ORIGINAL PROJECTILE-EVASION + REAL CODEX R3: "+passed+" PASS / "+failed+" FAIL; Unity/PhysX/Player NOT_RUN");
        return failed==0?0:1;
    }
}
