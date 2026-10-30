// B01-24 W07 actual .NET8 executable tests compiling unchanged Codex R3 with original enemy investigation source.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using RoboRougelike.R3;
using RoboRougelike.B0124.Investigation;
internal static class Program
{
    private static int passed,failed;
    static readonly Vector2 Zero=Vector2.Zero;
    static void Must(bool c,string m){if(!c)throw new Exception(m);}
    static void Check(string name,Action a){try{a();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.Error.WriteLine("FAIL "+name+" :: "+e.Message);}}
    static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Bad input accepted");}
    static PerceptionFrame Frame(long tick,Vector2? pos=null,bool see=false,Vector2? target=null,bool drive=true,params NoiseCue[] cues)=>
        new PerceptionFrame(tick,pos??Zero,see,target??Zero,drive,cues);
    static NoiseCue Cue(int id=1,long tick=0,Vector2? at=null,float volume=1,bool blocked=false)=>
        new NoiseCue(id,tick,at??new Vector2(10,0),volume,blocked);
    static string Blob(string path){
        byte[] payload=File.ReadAllBytes(path),header=Encoding.UTF8.GetBytes("blob "+payload.Length+"\0");
        var full=new byte[header.Length+payload.Length];Array.Copy(header,0,full,0,header.Length);Array.Copy(payload,0,full,header.Length,payload.Length);
        return Convert.ToHexString(SHA1.HashData(full)).ToLowerInvariant();
    }
    static string Scenarios()
    {
        var path="Research/B01-24/P17/W07-NOISE-INVESTIGATION-001/investigation_scenarios.json";
        using var doc=JsonDocument.Parse(File.ReadAllText(path));
        var root=doc.RootElement;
        Must(root.GetProperty("schema").GetString()=="B01-24-NOISE-INVESTIGATION-001-v1","schema mismatch");
        Must(root.GetProperty("original_codex_R3_sha").GetString()==Blob("Assets/R3/R3EnemyArchetype.cs"),"source drift");
        var cases=root.GetProperty("scenarios").EnumerateArray().ToArray();
        Must(cases.Length==3,"not 3 authored cases");
        var receipt=new List<string>();
        foreach(var sc in cases)
        {
            string id=sc.GetProperty("id").GetString()??"unknown";
            var role=Enum.Parse<R3EnemyArchetype>(sc.GetProperty("role").GetString()??"unknown");
            var agent=new EnemyInvestigation(role);
            int count=0,search=0;
            foreach(var step in sc.GetProperty("frames").EnumerateArray())
            {
                Vector2 Vec(JsonElement a)=>new Vector2(a[0].GetSingle(),a[1].GetSingle());
                var events=step.GetProperty("noises").EnumerateArray().Select(n=>new NoiseCue(n.GetProperty("id").GetInt32(),
                    n.GetProperty("tick").GetInt64(),Vec(n.GetProperty("pos")),
                    n.GetProperty("strength").GetSingle(),n.GetProperty("occluded").GetBoolean())).ToArray();
                long tick=step.GetProperty("tick").GetInt64();
                var frame=new PerceptionFrame(tick,Vec(step.GetProperty("self")),
                    step.GetProperty("visible").GetBoolean(),Vec(step.GetProperty("player")),
                    step.GetProperty("canMove").GetBoolean(),events);
                var d=agent.Tick(frame);
                Must(d.Mode!=PerceptionMode.Idle||d.Source==ClueSource.None,"idle with stale source");
                Must(EnemyInvestigation.Finite(d.Target),"bad target");
                if(d.Mode==PerceptionMode.SearchClue)search++;
                count++;
                receipt.Add(id+"|"+tick+"|"+d.Mode+"|"+d.Source+"|"+d.Target.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                    +"|"+d.Target.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+d.NoiseId+"|"+d.ClueExpiryTick);
            }
            Must(count>=7,"scenario too short");
            Must(search>=1,"search state never exercised "+id);
            Console.WriteLine("REPLAY "+id+" frames="+count+" search="+search);
        }
        var sha=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",receipt)))).ToLowerInvariant();
        Console.WriteLine("REPLAY_SHA256 "+sha+" ("+receipt.Count+" real original source frame decisions)");
        return sha;
    }
    public static int Main()
    {
        Check("real unmodified Codex R3EnemyArchetype blob pinned",()=>Must(Blob("Assets/R3/R3EnemyArchetype.cs")==
            "66cd1ee734b129117b3753d1205469a0496cb673","R3 source changed"));
        Check("original four Codex R3 roles compiled",()=>{
            Must((int)R3EnemyArchetype.Scout==0&&(int)R3EnemyArchetype.Bruiser==1&&
                (int)R3EnemyArchetype.Gunner==2&&(int)R3EnemyArchetype.Flanker==3,"enum role mismatch");
            Must(R3EnemyProfiles.Name(R3EnemyArchetype.Scout)=="Zwiadowca","real source not linked");
        });
        Check("quiet idle does not manufacture a player target",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0));
            Must(d.Mode==PerceptionMode.Idle&&d.Source==ClueSource.None&&d.NoiseId==-1,"ghost target");
        });
        Check("direct unoccluded shot yields a genuine investigation order",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{Cue()}));
            Must(d.Mode==PerceptionMode.MoveToClue&&d.Source==ClueSource.Noise&&d.NoiseId==1&&d.Target.X==10,"no hearing");
        });
        Check("real original Scout hearing range differs from Bruiser",()=>{
            var cue=Cue(at:new Vector2(10,0),blocked:true);
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{cue}));
            var d=new EnemyInvestigation(R3EnemyArchetype.Bruiser).Tick(Frame(0,cues:new[]{cue}));
            Must(a.Mode==PerceptionMode.MoveToClue&&d.Mode==PerceptionMode.Idle,"role hearing not respected");
        });
        Check("Gunner hearing reacts to near unoccluded shot",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Gunner).Tick(Frame(0,cues:new[]{Cue(at:new Vector2(5,0))}));
            Must(d.Source==ClueSource.Noise,"gunner ignored shot");
        });
        Check("Flanker hearing reacts to distant audible shot",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Flanker).Tick(Frame(0,cues:new[]{Cue(at:new Vector2(30,0))}));
            Must(d.Mode==PerceptionMode.MoveToClue,"flanker should investigate");
        });
        Check("near zero-strength cue does not create fake target",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{Cue(volume:0)}));
            Must(d.Mode==PerceptionMode.Idle,"silent event accepted");
        });
        Check("very distant quiet event falls below real hearing threshold",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Bruiser).Tick(Frame(0,cues:new[]{Cue(at:new Vector2(190,0),volume:.2f)}));
            Must(d.Mode==PerceptionMode.Idle,"too distant noise accepted");
        });
        Check("blocked low-volume event suppressed by occlusion",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{Cue(volume:.4f,blocked:true)}));
            Must(d.Mode==PerceptionMode.Idle,"occluded too quiet sound accepted");
        });
        Check("best signal wins even with inverse event ID order",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{
                Cue(id:2,at:new Vector2(20,0),volume:1),Cue(id:1,at:new Vector2(2,0),volume:1)}));
            Must(d.NoiseId==1&&d.Target.X==2,"did not choose stronger auditory signal");
        });
        Check("exact signal tie breaks by lower event ID consistently",()=>{
            var d=new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(0,cues:new[]{
                Cue(id:9,at:new Vector2(0,5)),Cue(id:2,at:new Vector2(0,-5))}));
            Must(d.NoiseId==2&&d.Target.Y==-5,"noise order affected tie");
        });
        Check("old repeated cue not consumed twice",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));
            var d=a.Tick(Frame(1,cues:new[]{Cue(id:1,tick:0,at:new Vector2(20,0))}));
            Must(d.NoiseId==1&&d.Target.X==10,"old repeated noise displaced previous clue");
        });
        Check("new louder cue redirects existing investigation",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));
            var d=a.Tick(Frame(1,cues:new[]{Cue(id:2,tick:1,at:new Vector2(-5,0))}));
            Must(d.NoiseId==2&&d.Target.X==-5,"new clue not adopted");
        });
        Check("visible player overrides any noise cue",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var d=a.Tick(Frame(0,see:true,target:new Vector2(6,-4),cues:new[]{Cue()}));
            Must(d.Mode==PerceptionMode.PursueVisible&&d.Source==ClueSource.Sight&&d.Target.Y==-4,"vision not dominant");
        });
        Check("when LOS lost remembers exact last player position",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,see:true,target:new Vector2(8,7)));
            var d=a.Tick(Frame(1,pos:new Vector2(0,0)));
            Must(d.Mode==PerceptionMode.MoveToClue&&d.Source==ClueSource.Sight&&d.Target.X==8&&d.Target.Y==7,"lost visual not remembered");
        });
        Check("continued visible player updates last observed position",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,see:true,target:new Vector2(3,3)));
            var d=a.Tick(Frame(1,see:true,target:new Vector2(9,5)));
            Must(d.Mode==PerceptionMode.PursueVisible&&d.Target.X==9,"stale visual clue");
        });
        Check("actual search waypoint begins after reaching clue",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));
            var d=a.Tick(Frame(1,pos:new Vector2(10,0)));
            Must(d.Mode==PerceptionMode.SearchClue&&d.Target.X==14,"search did not start");
        });
        Check("search moves across all 4 cardinal targets and ends",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));a.Tick(Frame(1,pos:new Vector2(10,0)));
            var first=a.Tick(Frame(2,pos:new Vector2(14,0)));
            Must(first.Mode==PerceptionMode.SearchClue&&first.Target.Y==4,"east to north");
            var second=a.Tick(Frame(3,pos:new Vector2(10,4)));
            Must(second.Target.X==6&&second.Target.Y==0,"north to west");
            var third=a.Tick(Frame(4,pos:new Vector2(6,0)));
            Must(third.Target.X==10&&third.Target.Y==-4,"west to south");
            var last=a.Tick(Frame(5,pos:new Vector2(10,-4)));
            Must(last.Mode==PerceptionMode.Idle&&last.Source==ClueSource.None,"search never finished");
        });
        Check("search timeout clears clue",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Bruiser);
            a.Tick(Frame(0,cues:new[]{Cue(at:new Vector2(5,0))}));
            a.Tick(Frame(1,pos:new Vector2(5,0)));
            var d=a.Tick(Frame(9,pos:new Vector2(5,0)));
            Must(d.Mode==PerceptionMode.Idle&&d.Source==ClueSource.None,"search timed out incorrectly");
        });
        Check("memory deadline clears clue without teleport",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Bruiser);
            a.Tick(Frame(0,see:true,target:new Vector2(9,0)));
            var d=a.Tick(Frame(10));
            Must(d.Mode==PerceptionMode.Idle&&d.Source==ClueSource.None,"expiry missing");
        });
        Check("disabled drive holds action without issuing movement",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));
            var d=a.Tick(Frame(1,drive:false));
            Must(d.Mode==PerceptionMode.Hold&&d.Target==Vector2.Zero,"unarmed vehicle moved");
        });
        Check("disabled drive with visible target still holds with target intent",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var d=a.Tick(Frame(0,see:true,target:new Vector2(5,5),drive:false));
            Must(d.Mode==PerceptionMode.Hold&&d.Source==ClueSource.Sight,"drive disable ignored");
        });
        Check("drive recovery resumes approach to remembered sound",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));a.Tick(Frame(1,drive:false));
            var d=a.Tick(Frame(2));
            Must(d.Mode==PerceptionMode.MoveToClue&&d.Target.X==10,"drive recovery broken");
        });
        Check("expired noise never awakens a sleeping squad",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var d=a.Tick(Frame(40,cues:new[]{Cue(id:8,tick:0)}));
            Must(d.Mode==PerceptionMode.Idle,"expired stimulus revived");
        });
        Check("event IDs are strictly consumed once, including unheard earlier stronger later",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue(id:7,volume:0)}));
            var d=a.Tick(Frame(1,cues:new[]{Cue(id:7,tick:0,volume:1)}));
            Must(d.Mode==PerceptionMode.Idle,"replayed event raised");
        });
        Check("same investigation replay invariant to cue permutation",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var b=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var one=new[]{Cue(id:3,at:new Vector2(2,0)),Cue(id:7,at:new Vector2(4,0))};
            var two=new[]{one[1],one[0]};
            var da=a.Tick(Frame(0,cues:one));var db=b.Tick(Frame(0,cues:two));
            Must(da.Target==db.Target&&da.NoiseId==db.NoiseId&&da.Source==db.Source,"cue permutation nondeterminism");
        });
        Check("separate enemy memories never share noise watermark",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            var b=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue(id:99)}));
            var d=b.Tick(Frame(0,cues:new[]{Cue(id:1)}));
            Must(d.Mode==PerceptionMode.MoveToClue&&d.NoiseId==1,"cross-enemy memory leakage");
        });
        Check("out of order and duplicate ticks rejected without mutation",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            a.Tick(Frame(0,cues:new[]{Cue()}));
            Reject(()=>a.Tick(Frame(0)));
            Reject(()=>a.Tick(Frame(-1)));
            var d=a.Tick(Frame(1));
            Must(d.Mode==PerceptionMode.MoveToClue&&d.NoiseId==1,"bad tick mutated memory");
        });
        Check("malformed NaN self rejected before clue consumed",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,pos:new Vector2(float.NaN,0),cues:new[]{Cue()})));
            Must(a.Tick(Frame(0,cues:new[]{Cue()})).Mode==PerceptionMode.MoveToClue,"invalid self changed state");
        });
        Check("infinite and NaN player positions rejected even without visual",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,target:new Vector2(float.PositiveInfinity,0))));
            Reject(()=>a.Tick(Frame(0,target:new Vector2(float.NaN,0))));
            Must(a.Tick(Frame(0)).Mode==PerceptionMode.Idle,"invalid target changed state");
        });
        Check("malformed negative or future event ticks rejected",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(tick:-1)})));
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(tick:1)})));
        });
        Check("duplicate event identities rejected before event watermark",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(),Cue()})));
            Must(a.Tick(Frame(0,cues:new[]{Cue()})).NoiseId==1,"invalid cues changed state");
        });
        Check("negative event id and out-of-range intensity rejected",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(id:-1)})));
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(volume:-.1f)})));
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(volume:1.01f)})));
        });
        Check("NaN and Infinity sound strength rejected",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(volume:float.NaN)})));
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(volume:float.PositiveInfinity)})));
        });
        Check("NaN and infinity sound positions rejected",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(at:new Vector2(float.NaN,0))})));
            Reject(()=>a.Tick(Frame(0,cues:new[]{Cue(at:new Vector2(0,float.NegativeInfinity))})));
        });
        Check("null/large batch noise rejected",()=>{
            var a=new EnemyInvestigation(R3EnemyArchetype.Scout);
            Reject(()=>a.Tick(new PerceptionFrame(0,Zero,false,Zero,true,null!)));
            Reject(()=>a.Tick(Frame(0,cues:Enumerable.Range(0,129).Select(i=>Cue(id:i)).ToArray())));
        });
        Check("long tick overflow never accepted",()=>Reject(()=>new EnemyInvestigation(R3EnemyArchetype.Scout).Tick(Frame(long.MaxValue))));
        Check("invalid canonical enum rejected on construction",()=>Reject(()=>new EnemyInvestigation((R3EnemyArchetype)100)));
        Check("zero hearing and invalid thresholds rejected by policy",()=>{
            Reject(()=>new InvestigationPolicy(0,.2f,.3f,1,8,8,4));
            Reject(()=>new InvestigationPolicy(10,0,.3f,1,8,8,4));
            Reject(()=>new InvestigationPolicy(10,.2f,0,1,8,8,4));
            Reject(()=>new InvestigationPolicy(10,.2f,.3f,1,0,8,4));
            Reject(()=>new InvestigationPolicy(10,.2f,.3f,1,8,0,4));
        });
        Check("cannot create policy with search radius smaller than arrival",()=>Reject(()=>new InvestigationPolicy(10,.2f,.3f,2,8,8,1)));
        Check("real authored 3 scenario journeys execute through original state machine",()=>{
            var digest=Scenarios();
            Must(digest.Length==64,"no scene SHA");
        });
        Check("three replay results are deterministic and byte-for-byte identical",()=>{
            var first=Scenarios();var second=Scenarios();
            Must(first==second,"scenario replay mismatch");
        });
        Console.WriteLine("B01-24 W07 ORIGINAL enemy investigation + original Codex R3: "+passed+" PASS / "+failed+" FAIL; UNITY NATIVE NOT_RUN");
        return failed==0?0:1;
    }
}
