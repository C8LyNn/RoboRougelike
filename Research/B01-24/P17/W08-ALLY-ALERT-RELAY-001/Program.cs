// B01-24 W08 original source executable, compiles original Codex R3 role source unmodified.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.B0124.AllyAlert;

internal static class Program
{
    static int pass, fail;
    static void Assert(bool condition,string why){if(!condition)throw new Exception(why);}
    static void Check(string name,Action action)
    {try{action();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.Error.WriteLine("FAIL "+name+": "+e.GetType().Name+" "+e.Message);}}
    static void Reject(Action action)
    {try{action();}catch(ArgumentException){return;}throw new Exception("Expected bad input rejection");}
    static Vector2 At(float x=0,float z=0)=>new Vector2(x,z);
    static RadioMember Member(int id,R3EnemyArchetype role,float x=0,float z=0,bool alive=true,bool visible=false,bool tx=true,bool rx=true)
        =>new RadioMember(id,role,At(x,z),alive,visible,tx,rx);
    static RadioMember[] Both(bool scoutVisible=false,bool scoutTx=true,bool gunnerRx=true,float gunnerX=15,bool scoutAlive=true,bool gunnerAlive=true)=>
        new[]{Member(10,R3EnemyArchetype.Scout,alive:scoutAlive,visible:scoutVisible,tx:scoutTx),
              Member(20,R3EnemyArchetype.Gunner,x:gunnerX,rx:gunnerRx,alive:gunnerAlive)};
    static SightMessage Msg(int id=1,int sender=10,long tick=0,float x=8,float z=4)=>
        new SightMessage(id,sender,tick,At(x,z));
    static AllyAlertRelay Pair(RelayPolicy? policy=null)=>
        new AllyAlertRelay(new[]{10,20},new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Gunner},policy??RelayPolicy.Standard());
    static RadioIntel[] Pulse(AllyAlertRelay relay,long t,RadioMember[]? m=null,params SightMessage[] reports)=>
        relay.Tick(t,m??Both(),reports);
    static string GitBlob(string path)
    {
        byte[] data=File.ReadAllBytes(path);
        byte[] prefix=Encoding.ASCII.GetBytes("blob "+data.Length+"\0");
        var raw=new byte[prefix.Length+data.Length];
        Buffer.BlockCopy(prefix,0,raw,0,prefix.Length);
        Buffer.BlockCopy(data,0,raw,prefix.Length,data.Length);
        return Convert.ToHexString(SHA1.HashData(raw)).ToLowerInvariant();
    }
    static string RunAuthoredScenarios()
    {
        using var doc=JsonDocument.Parse(File.ReadAllText("Research/B01-24/P17/W08-ALLY-ALERT-RELAY-001/radio_scenarios.json"));
        var root=doc.RootElement;
        Assert(root.GetProperty("schema").GetString()=="B01-24-ALLY-ALERT-RELAY-001-v1","wrong source scenario schema");
        Assert(root.GetProperty("codexOriginalR3Blob").GetString()==GitBlob("Assets/R3/R3EnemyArchetype.cs"),"source R3 drift");
        var scenarios=root.GetProperty("scenarios").EnumerateArray().ToArray();
        Assert(scenarios.Length==3,"not 3 scenarios");
        var receipts=new List<string>();
        foreach(var sc in scenarios)
        {
            Vector2 Vec(JsonElement el)=>new Vector2(el[0].GetSingle(),el[1].GetSingle());
            var original=sc.GetProperty("members").EnumerateArray().ToArray();
            var relay=new AllyAlertRelay(original.Select(x=>x.GetProperty("id").GetInt32()).ToArray(),
                 original.Select(x=>Enum.Parse<R3EnemyArchetype>(x.GetProperty("role").GetString()!)).ToArray(),RelayPolicy.Standard());
            int deliveries=0;int frames=0;
            foreach(var f in sc.GetProperty("steps").EnumerateArray())
            {
                long t=f.GetProperty("tick").GetInt64();
                var obs=f.GetProperty("members").EnumerateArray().Select(x=>new RadioMember(
                    x.GetProperty("id").GetInt32(),
                    Enum.Parse<R3EnemyArchetype>(x.GetProperty("role").GetString()!),
                    Vec(x.GetProperty("xz")),x.GetProperty("alive").GetBoolean(),
                    x.GetProperty("visible").GetBoolean(),x.GetProperty("tx").GetBoolean(),x.GetProperty("rx").GetBoolean())).ToArray();
                var reports=f.GetProperty("messages").EnumerateArray().Select(x=>new SightMessage(
                    x.GetProperty("id").GetInt32(),x.GetProperty("sender").GetInt32(),
                    x.GetProperty("tick").GetInt64(),Vec(x.GetProperty("point")))).ToArray();
                var states=relay.Tick(t,obs,reports);
                Assert(states.Length==2,"scenario wrong receiver count");
                foreach(var intel in states)
                {
                    if(intel.Valid)deliveries++;
                    receipts.Add(sc.GetProperty("id").GetString()+"|"+t+"|"+intel.ReceiverId+"|"+
                        intel.SenderId+"|"+intel.MessageId+"|"+
                        intel.PlayerLastSeenXZ.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                        intel.PlayerLastSeenXZ.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+
                        intel.OriginalSightTick+"|"+intel.ExpiryTick);
                }
                frames++;
            }
            Assert(frames>=5,"too few scenario ticks");
            Assert(deliveries>=1,"scenario never delivers genuine radio intel");
            Console.WriteLine("REPLAY "+sc.GetProperty("id").GetString()+" frames="+frames+" valid="+deliveries);
        }
        string hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",receipts)))).ToLowerInvariant();
        Console.WriteLine("REPLAY_SHA256 "+hash+" ("+receipts.Count+" executed authored recipient frames)");
        return hash;
    }
    public static int Main()
    {
        Check("original pinned real Codex R3 Git blob",()=>Assert(
            GitBlob("Assets/R3/R3EnemyArchetype.cs")=="66cd1ee734b129117b3753d1205469a0496cb673","canonical source changed"));
        Check("four actual canonical R3 roles compile and retain ABI",()=>{
            Assert((int)R3EnemyArchetype.Scout==0&&(int)R3EnemyArchetype.Bruiser==1&&
                (int)R3EnemyArchetype.Gunner==2&&(int)R3EnemyArchetype.Flanker==3,"original R3 changed");
            Assert(R3EnemyProfiles.InEncounter(2,1)==R3EnemyArchetype.Gunner,"original helper not compiled");
        });
        Check("no sight event gives no phantom player location",()=>{
            var p=Pair();var x=Pulse(p,0);
            Assert(!x[0].Valid&&!x[1].Valid&&p.PendingCount==0,"phantom intel");
        });
        Check("visual source sends but latency prevents same tick leak",()=>{
            var p=Pair();var x=Pulse(p,0,Both(scoutVisible:true),Msg());
            Assert(!x[1].Valid&&p.PendingCount==1,"instant omniscience");
        });
        Check("one tick cannot bypass two tick radio latency",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());
            Assert(!Pulse(p,1)[1].Valid,"early delivery");
        });
        Check("exact latency boundary delivers original target and sender",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());
            var x=Pulse(p,2)[1];
            Assert(x.Valid&&x.SenderId==10&&x.MessageId==1&&x.PlayerLastSeenXZ==At(8,4)&&
                x.OriginalSightTick==0&&x.ExpiryTick==12,"wrong intel receipt");
        });
        Check("intel stays until original sight TTL, not delivery TTL",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());Pulse(p,2);
            Assert(Pulse(p,11)[1].Valid,"expired early");
            Assert(!Pulse(p,12)[1].Valid,"stale radio target retained");
        });
        Check("dead transmitter never sends confirmed intel",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true,scoutAlive:false),Msg());
            Assert(p.PendingCount==0&&!Pulse(p,2)[1].Valid,"dead source emitted radio");
        });
        Check("sender must actually see player in host inputs",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:false),Msg());
            Assert(p.PendingCount==0&&!Pulse(p,2)[1].Valid,"fabricated remote vision");
        });
        Check("transmitter jam suppresses report with no deferred send",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true,scoutTx:false),Msg());
            Assert(p.PendingCount==0&&!Pulse(p,2)[1].Valid,"jammed sender emitted");
        });
        Check("receiver jammed during send cannot collect delayed intel",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true,gunnerRx:false),Msg());
            Assert(p.PendingCount==0&&!Pulse(p,2)[1].Valid,"jammed recipient queued");
        });
        Check("receiver jammed at delivery drops queued message",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());
            Assert(!Pulse(p,2,Both(gunnerRx:false))[1].Valid,"jammed receiver accepted");
            Assert(!Pulse(p,3)[1].Valid&&p.PendingCount==0,"jammed delivery replayed later");
        });
        Check("receiver jammed after delivery loses prior radio memory",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());Pulse(p,2);
            Assert(!Pulse(p,3,Both(gunnerRx:false))[1].Valid,"radio memory persisted under jam");
        });
        Check("recovered recipient only hears a NEW verified report",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());Pulse(p,2,Both(gunnerRx:false));
            Pulse(p,3,Both(gunnerRx:true));Pulse(p,4,Both(scoutVisible:true),Msg(2,tick:4));
            Assert(Pulse(p,6)[1].Valid,"fresh source never recovered");
        });
        Check("dead receiver does not accept queued clue",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());
            Assert(!Pulse(p,2,Both(gunnerAlive:false))[1].Valid,"dead receiver accepted");
        });
        Check("dead receiver drops already delivered clue",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());Pulse(p,2);
            Assert(!Pulse(p,3,Both(gunnerAlive:false))[1].Valid,"dead receiver remembers");
        });
        Check("each sender never reports to its own receiver slot",()=>{
            var p=Pair();var x=Pulse(p,0,Both(scoutVisible:true),Msg());
            Assert(!x[0].Valid&&p.PendingCount==1,"self-intel queued");
        });
        Check("outside Bruiser range no communication even if Scout can transmit",()=>{
            var p=new AllyAlertRelay(new[]{10,20},new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Bruiser},RelayPolicy.Standard());
            var obs=new[]{Member(10,R3EnemyArchetype.Scout,visible:true),Member(20,R3EnemyArchetype.Bruiser,x:40)};
            p.Tick(0,obs,new[]{Msg()});
            Assert(p.PendingCount==0,"range bypass");
        });
        Check("exact role pair radio radius is eligible",()=>{
            var p=new AllyAlertRelay(new[]{10,20},new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Bruiser},RelayPolicy.Standard());
            var obs=new[]{Member(10,R3EnemyArchetype.Scout,visible:true),Member(20,R3EnemyArchetype.Bruiser,x:30)};
            p.Tick(0,obs,new[]{Msg()});
            Assert(p.PendingCount==1,"boundary refused");
        });
        Check("Flanker and Gunner canonical radio roles can link",()=>{
            var p=new AllyAlertRelay(new[]{1,2},new[]{R3EnemyArchetype.Flanker,R3EnemyArchetype.Gunner},RelayPolicy.Standard());
            var obs=new[]{Member(1,R3EnemyArchetype.Flanker,visible:true),Member(2,R3EnemyArchetype.Gunner,x:40)};
            p.Tick(0,obs,new[]{Msg(4,sender:1)]);
            Assert(p.Tick(2,new[]{Member(1,R3EnemyArchetype.Flanker),Member(2,R3EnemyArchetype.Gunner,x:40)},Array.Empty<SightMessage>())[1].Valid,"flanker relay failed");
        });
        Check("newer real visual report updates previously received location",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg(1,x:3));Pulse(p,2);
            Pulse(p,3,Both(scoutVisible:true),Msg(2,tick:3,x:12));
            var x=Pulse(p,5)[1];Assert(x.Valid&&x.MessageId==2&&x.PlayerLastSeenXZ.X==12,"stale target");
        });
        Check("already received report cannot be replayed as fresh",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg(9));Pulse(p,2);
            Pulse(p,3,Both(scoutVisible:true),Msg(9,tick:3,x:99));
            Assert(Pulse(p,5)[1].MessageId==9&&Pulse(p,6)[1].PlayerLastSeenXZ.X==8,"reused event refreshed");
        });
        Check("new late report cannot rewind newest sight timestamp",()=>{
            var p=Pair();
            Pulse(p,0,Both(scoutVisible:true),Msg(1));Pulse(p,2);
            Pulse(p,3,Both(scoutVisible:true),Msg(2,tick:3,x:12));
            Assert(Pulse(p,5)[1].OriginalSightTick==3,"new sight timestamp incorrect");
        });
        Check("all cue orders produce deterministic target and message tie",()=>{
            var x=Pair();var y=Pair();
            var a=new[]{Msg(3,x:30),Msg(1,x:10),Msg(2,x:20)};
            var b=new[]{a[2],a[0],a[1]};
            x.Tick(0,Both(scoutVisible:true),a);y.Tick(0,Both(scoutVisible:true),b);
            var rx=Pulse(x,2)[1];var ry=Pulse(y,2)[1];
            Assert(rx.MessageId==1&&ry.MessageId==1&&rx.PlayerLastSeenXZ==ry.PlayerLastSeenXZ,"array order changed result");
        });
        Check("full squad max 4 receives only real neighbor signals",()=>{
            var p=new AllyAlertRelay(new[]{1,2,3,4},new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Gunner,R3EnemyArchetype.Bruiser,R3EnemyArchetype.Flanker},RelayPolicy.Standard());
            var obs=new[]{Member(1,R3EnemyArchetype.Scout,visible:true),Member(2,R3EnemyArchetype.Gunner,x:10),Member(3,R3EnemyArchetype.Bruiser,x:35),Member(4,R3EnemyArchetype.Flanker,x:20)};
            p.Tick(0,obs,new[]{Msg(1,sender:1)});
            var result=p.Tick(2,new[]{Member(1,R3EnemyArchetype.Scout),Member(2,R3EnemyArchetype.Gunner,x:10),Member(3,R3EnemyArchetype.Bruiser,x:35),Member(4,R3EnemyArchetype.Flanker,x:20)},Array.Empty<SightMessage>());
            Assert(!result[0].Valid&&result[1].Valid&&!result[2].Valid&&result[3].Valid,"incorrect radio graph");
        });
        Check("one agent squad never relays to itself",()=>{
            var p=new AllyAlertRelay(new[]{1},new[]{R3EnemyArchetype.Scout},RelayPolicy.Standard());
            var single=new[]{Member(1,R3EnemyArchetype.Scout,visible:true)};
            Assert(!p.Tick(0,single,new[]{Msg(1,sender:1)})[0].Valid&&p.PendingCount==0,"solo radio cheat");
        });
        Check("individual squad instances never share delivery state",()=>{
            var a=Pair();var b=Pair();Pulse(a,0,Both(scoutVisible:true),Msg());
            Assert(!Pulse(b,0)[1].Valid&&!Pulse(b,2)[1].Valid&&Pulse(a,2)[1].Valid,"cross squad data leak");
        });
        Check("external mutation of constructor ID/role arrays cannot mutate squad",()=>{
            var ids=new[]{10,20};var roles=new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Gunner};
            var p=new AllyAlertRelay(ids,roles,RelayPolicy.Standard());ids[0]=99;roles[0]=R3EnemyArchetype.Bruiser;
            Pulse(p,0,Both(scoutVisible:true),Msg());Assert(Pulse(p,2)[1].SenderId==10,"unsafe roster mutation");
        });
        Check("external mutation of policy range cannot bypass role limit",()=>{
            float[] radii={65,30,52,55};var pol=new RelayPolicy(2,12,radii);radii[1]=150;
            var p=new AllyAlertRelay(new[]{10,20},new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Bruiser},pol);
            p.Tick(0,new[]{Member(10,R3EnemyArchetype.Scout,visible:true),Member(20,R3EnemyArchetype.Bruiser,x:40)},new[]{Msg()});
            Assert(p.PendingCount==0&&pol.RangeFor(R3EnemyArchetype.Bruiser)==30,"policy changed after construction");
        });
        Check("mutation of returned array cannot rewrite internal radio memory",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());var x=Pulse(p,2);
            x[1]=RadioIntel.Empty(20);
            Assert(Pulse(p,3)[1].Valid,"mutable external result modified state");
        });
        Check("reject duplicate stable squad member IDs",()=>Reject(()=>new AllyAlertRelay(new[]{1,1},
            new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Gunner},RelayPolicy.Standard())));
        Check("reject negative and unknown original R3 role",()=>{
            Reject(()=>new AllyAlertRelay(new[]{-1},new[]{R3EnemyArchetype.Scout},RelayPolicy.Standard()));
            Reject(()=>new AllyAlertRelay(new[]{1},new[]{(R3EnemyArchetype)99},RelayPolicy.Standard()));
        });
        Check("reject zero and fifth member squad",()=>{
            Reject(()=>new AllyAlertRelay(Array.Empty<int>(),Array.Empty<R3EnemyArchetype>(),RelayPolicy.Standard()));
            Reject(()=>new AllyAlertRelay(new[]{1,2,3,4,5},Enumerable.Repeat(R3EnemyArchetype.Scout,5).ToArray(),RelayPolicy.Standard()));
        });
        Check("reject mismatched squad arrays",()=>Reject(()=>new AllyAlertRelay(new[]{1,2},new[]{R3EnemyArchetype.Scout},RelayPolicy.Standard())));
        Check("reject invalid policy latency TTL and ranged floats",()=>{
            Reject(()=>new RelayPolicy(0,12,new[]{20f,20f,20f,20f}));
            Reject(()=>new RelayPolicy(5,5,new[]{20f,20f,20f,20f}));
            Reject(()=>new RelayPolicy(2,12,new[]{20f,float.NaN,20f,20f}));
            Reject(()=>new RelayPolicy(2,12,new[]{20f,float.PositiveInfinity,20f,20f}));
            Reject(()=>new RelayPolicy(2,12,new[]{20f,0f,20f,20f}));
        });
        Check("duplicate monotonic tick rejected without losing pending delivery",()=>{
            var p=Pair();Pulse(p,0,Both(scoutVisible:true),Msg());
            Reject(()=>Pulse(p,0));Assert(Pulse(p,2)[1].Valid,"failed tick caused mutation");
        });
        Check("negative and long max timestamp rejected",()=>{
            var p=Pair();Reject(()=>Pulse(p,-1));Reject(()=>Pulse(p,long.MaxValue));
            Assert(!Pulse(p,0)[1].Valid,"timestamp rejection affected subsequent run");
        });
        Check("duplicate member observation rejected without internal mutation",()=>{
            var p=Pair();Reject(()=>p.Tick(0,new[]{Both()[0],Both()[0]},Array.Empty<SightMessage>()));
            Pulse(p,0,Both(scoutVisible:true),Msg());Assert(Pulse(p,2)[1].Valid,"bad roster affected state");
        });
        Check("unknown and mismatched role observation rejected",()=>{
            var p=Pair();Reject(()=>p.Tick(0,new[]{Member(10,R3EnemyArchetype.Bruiser),Both()[1]},Array.Empty<SightMessage>()));
            Reject(()=>p.Tick(0,new[]{Member(10,R3EnemyArchetype.Scout),Member(99,R3EnemyArchetype.Gunner)},Array.Empty<SightMessage>()));
        });
        Check("invalid coordinates and noisy NaN event rejected without mutation",()=>{
            var p=Pair();
            Reject(()=>p.Tick(0,new[]{Member(10,R3EnemyArchetype.Scout,x:float.NaN),Both()[1]},new[]{Msg()}));
            Reject(()=>p.Tick(0,Both(scoutVisible:true),new[]{Msg(1,x:float.PositiveInfinity)}));
            Pulse(p,0,Both(scoutVisible:true),Msg());Assert(Pulse(p,2)[1].Valid,"failure corrupted valid event");
        });
        Check("message sender must be original stable squad member",()=>{
            var p=Pair();Reject(()=>p.Tick(0,Both(scoutVisible:true),new[]{Msg(1,sender:99)}));
        });
        Check("message must be from same tick, not predated or future",()=>{
            var p=Pair();Reject(()=>p.Tick(1,Both(scoutVisible:true),new[]{Msg(1,tick:0)}));
            Reject(()=>p.Tick(1,Both(scoutVisible:true),new[]{Msg(1,tick:2)}));
        });
        Check("duplicate event IDs rejected before state mutation",()=>{
            var p=Pair();Reject(()=>p.Tick(0,Both(scoutVisible:true),new[]{Msg(1),Msg(1,x:99)}));
            Pulse(p,0,Both(scoutVisible:true),Msg());Assert(Pulse(p,2)[1].Valid,"duplicate altered mark");
        });
        Check("null and overlarge event batches rejected",()=>{
            var p=Pair();Reject(()=>p.Tick(0,Both(),null!));
            Reject(()=>p.Tick(0,Both(),Enumerable.Range(0,65).Select(i=>Msg(i)).ToArray()));
        });
        Check("null or mismatched observation arrays rejected",()=>{
            var p=Pair();Reject(()=>p.Tick(0,null!,Array.Empty<SightMessage>()));
            Reject(()=>p.Tick(0,new[]{Both()[0]},Array.Empty<SightMessage>()));
        });
        Check("canonical radio role range differences preserved",()=>{
            var p=RelayPolicy.Standard();
            Assert(p.RangeFor(R3EnemyArchetype.Scout)>p.RangeFor(R3EnemyArchetype.Bruiser)&&
                p.RangeFor(R3EnemyArchetype.Gunner)>p.RangeFor(R3EnemyArchetype.Bruiser),"role ranges equal");
        });
        Check("three genuine authored replay scenarios execute and produce SHA",()=>{
            var h=RunAuthoredScenarios();Assert(h.Length==64,"trace hash missing");
        });
        Check("original authored replay produces repeatable byte-level trace SHA",()=>{
            var a=RunAuthoredScenarios();var b=RunAuthoredScenarios();Assert(a==b,"replay diverged");
        });
        Console.WriteLine("B01-24 W08 ORIGINAL ALLY-ALERT-RELAY + REAL Codex R3: "+pass+" PASS / "+fail+" FAIL; Unity/PhysX/Player NOT_RUN");
        return fail==0?0:1;
    }
}
