// Actual .NET8 executable acceptance, not a Unity Native/PhysX or balance approval.
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Text.Json;
using B0124.CoverRetreat;
internal static class Program {
    private static int pass, fail;
    private static void Assert(bool condition,string reason) {
        if(!condition)throw new InvalidOperationException(reason);
    }
    private static void Check(string name,Action run) {
        try{run();pass++;Console.WriteLine("PASS "+name);}
        catch(Exception ex){fail++;Console.Error.WriteLine("FAIL "+name+" "+ex.GetType().Name+": "+ex.Message);}
    }
    private static void Throws(Action test) {
        try {test();}catch(ArgumentException){return;}
        throw new InvalidOperationException("invalid input unexpectedly accepted");
    }
    private static CoverCatalog Fresh()=>RetreatPlanner.Read(File.ReadAllText("Research/B01-24/P17/W02-COVER-RETREAT-001/cover_sites.json"));
    private static CoverCatalog Clone(CoverCatalog c)=>RetreatPlanner.Read(JsonSerializer.Serialize(c));
    private static bool Eq(Vector2 x,Vector2 y)=>Vector2.Distance(x,y)<.001f;
    public static int Main() {
        var root=Directory.GetCurrentDirectory();
        Check("original R5Arena and original P12 enemy code exact SHA",()=>{
            var c=Fresh();
            RetreatPlanner.VerifyGitBlob(root,c.GameSource.ArenaPath,c.GameSource.ArenaBlob);
            RetreatPlanner.VerifyGitBlob(root,c.GameSource.EnemyPath,c.GameSource.EnemyBlob);
        });
        Check("3 real R5 arena types and 10 original safe-covered sites",()=>{
            var c=Fresh();RetreatPlanner.Validate(c);
            Assert(c.Maps.Length==3&&c.Maps.Sum(m=>m.CoverSites.Length)==10,"site count");
            Assert(c.Maps.Select(x=>x.ArenaKind).Distinct().Count()==3,"arena enum");
        });
        foreach(var arena in Fresh().Maps) {
            Check(arena.Id+" all sites are actually hidden behind original static R5 geometry",()=>{
                var c=Fresh();var m=c.Maps.Single(x=>x.ArenaKind==arena.ArenaKind);
                foreach(var site in m.CoverSites)
                    Assert(RetreatPlanner.BehindCover(m,c.World.Player.V,site.V),site.Id+" unprotected");
            });
            Check(arena.Id+" all sites physically clear at 2m planning envelope",()=>{
                var c=Fresh();var m=c.Maps.Single(x=>x.ArenaKind==arena.ArenaKind);
                foreach(var site in m.CoverSites)
                    Assert(RetreatPlanner.ClearSegment(m,c.World,site.V,site.V,c.World.ClearanceRadius),site.Id+" overlaps static cover");
            });
            Check(arena.Id+" health-triggered real world route to cover",()=>{
                var c=Fresh();var m=c.Maps.Single(x=>x.ArenaKind==arena.ArenaKind);var start=new Vector2(0,20);
                var d=RetreatPlanner.Evaluate(c,arena.ArenaKind,start,.2f,true,true,false);
                Assert(d.Mode=="RETREAT_TO_COVER",arena.Id+": "+d.Mode+" "+d.Reason);
                Assert(d.Route.Length>=2 && Eq(d.Route[0],start),"no original start");
                var site=c.Maps.Single(x=>x.ArenaKind==arena.ArenaKind).CoverSites.Single(x=>x.Id==d.CoverId);
                Assert(Eq(d.Route[^1],site.V),"no exact authored cover end");
                Assert(d.RouteMetres>0 && float.IsFinite(d.RouteMetres),"invalid route length");
                for(int i=1;i<d.Route.Length;i++)
                    Assert(RetreatPlanner.ClearSegment(m,c.World,d.Route[i-1],d.Route[i],c.World.ClearanceRadius),
                        "unsafe segment "+i);
            });
        }
        Check("healthy enemy remains original combat behavior",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.8f,true,true,false);
            Assert(d.Mode=="NORMAL_COMBAT"&&d.Route.Length==0&&!d.RetreatActive,"healthy interrupted");
        });
        Check("exact enter threshold permits optional emergency retreat",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.35f,true,true,false);
            Assert(d.Mode=="RETREAT_TO_COVER"&&d.RetreatActive,"enter threshold");
        });
        Check("below exit threshold preserves retreat without oscillation",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.5f,true,true,true);
            Assert(d.Mode=="RETREAT_TO_COVER","hysteresis");
        });
        Check("exit threshold restores ordinary combat",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.6f,true,true,true);
            Assert(d.Mode=="NORMAL_COMBAT"&&d.Reason=="RECOVERED","exit threshold");
        });
        Check("lost all drive never invents physics thrust",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.1f,false,true,false);
            Assert(d.Mode=="HOLD"&&d.Route.Length==0&&d.Reason=="DRIVE_OFFLINE","dead drive moved");
        });
        Check("no weapon defers unarmed ram to existing Codex source owner",()=>{
            var d=RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.1f,true,false,false);
            Assert(d.Mode=="DEFER_TO_CODEX_RAM"&&d.Route.Length==0,"duplicate existing ram");
        });
        Check("full repeat determinism original selected route and waypoint bytes",()=>{
            var c=Fresh();
            var a=RetreatPlanner.Evaluate(c,2,new Vector2(0,20),.15f,true,true,false);
            var b=RetreatPlanner.Evaluate(c,2,new Vector2(0,20),.15f,true,true,false);
            Assert(a.CoverId==b.CoverId&&a.RouteMetres==b.RouteMetres&&a.Route.SequenceEqual(b.Route),
                "nondeterministic A*");
        });
        Check("input catalog not mutated by planning",()=>{
            var c=Fresh();var first=JsonSerializer.Serialize(c);
            _=RetreatPlanner.Evaluate(c,0,new Vector2(0,20),.10f,true,true,false);
            Assert(JsonSerializer.Serialize(c)==first,"mutated user content");
        });
        Check("wall blocks swept route, no thin-wall tunnelling",()=>{
            var m=Fresh().Maps[0];
            Assert(RetreatPlanner.HitsBox(new Vector2(0,0),new Vector2(0,50),
                m.Obstacles[0],2f),"north wall bypass");
        });
        Check("same-side segment does not falsely hit distant rectangle",()=>{
            var m=Fresh().Maps[0];
            Assert(!RetreatPlanner.HitsBox(new Vector2(0,0),new Vector2(0,1),
                m.Obstacles[0],2f),"false wall hit");
        });
        Check("invalid site inside original press rejected",()=>{
            var c=Fresh();c.Maps[0].CoverSites[0].X=-10;c.Maps[0].CoverSites[0].Z=0;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("invalid site with exposed firing lane rejected",()=>{
            var c=Fresh();c.Maps[0].CoverSites[0].X=0;c.Maps[0].CoverSites[0].Z=20;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("missing source R5 arena kind rejected",()=>{
            var c=Fresh();c.Maps[2].ArenaKind=8;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("duplicate R5 arena kind rejected",()=>{
            var c=Fresh();c.Maps[2].ArenaKind=0;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("nonfinite radius rejected",()=>{
            var c=Fresh();c.World.ClearanceRadius=float.NaN;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("invalid fake radius that underestimates vehicle rejected",()=>{
            var c=Fresh();c.World.ClearanceRadius=.1f;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("malformed coordinate NaN rejected",()=>{
            var c=Fresh();c.Maps[1].CoverSites[0].X=float.NaN;
            Throws(()=>RetreatPlanner.Validate(c));
        });
        Check("negative HP rejected",()=>{
            Throws(()=>RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),-.1f,true,true,false));
        });
        Check("nonsensical hysteresis thresholds rejected",()=>{
            Throws(()=>RetreatPlanner.Evaluate(Fresh(),0,new Vector2(0,20),.1f,true,true,false,.7f,.4f));
        });
        Check("source drift fails closed even for unmodified existing path",()=>{
            var c=Fresh();Throws(()=>RetreatPlanner.VerifyGitBlob(root,c.GameSource.ArenaPath,new string('0',40)));
        });
        Check("no reachable cover fails safe with zero proposed movement",()=>{
            var c=Fresh();var m=c.Maps[0];
            // Valid site data stays intact; dynamic full arena obstruction is injected
            // only AFTER static content validation, no owner source changes.
            RetreatPlanner.Validate(c);
            m.Obstacles=m.Obstacles.Concat(new[]{new StaticBox{Id="temporary gate",X=0,Z=20,W=30,H=30}}).ToArray();
            var d=RetreatPlanner.Route(m,c.World,new Vector2(0,20),new Vector2(14,9));
            Assert(d.Length==0,"planned collision through obstacle at source");
        });
        Console.WriteLine("B01-24-COVER-RETREAT-001 "+pass+" PASS / "+fail+
            " FAIL; genuine .NET8 source execution; Unity/PhysX/Player NOT_RUN.");
        return fail==0?0:1;
    }
}
