using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RoboRougelike.B0110.TrainingGhost;

internal static class Program
{
    private static int passed, failed;
    private static GhostPose P(float x=0, float yaw=0) =>
        new GhostPose { X=x, Y=0, Z=x*.2f, Yaw=yaw };
    private static GhostRun Run(string rig="Light",float multiplier=1f,string course="CHL003_R1")
    {
        var g=new GhostRecorder(course,rig,4,P());
        for(int i=0;i<4;i++){
            g.Tick(1f*multiplier,false,P(i+1,35*(i+1)));
            g.Checkpoint(i,P(i+1,35*(i+1)));
        }
        g.Tick(multiplier,false,P(5,160));
        return g.Finish(P(5,160));
    }
    private static void Check(bool ok,string desc="check failure")
    {
        if(!ok)throw new InvalidOperationException(desc);
    }
    private static void Equal<T>(T actual,T expected)
    {
        Check(EqualityComparer<T>.Default.Equals(actual,expected),
            "expected "+expected+" actual "+actual);
    }
    private static bool Reject(Action act)
    {
        try{act();return false;}
        catch(ArgumentException){return true;}
    }
    private static void Test(string name,Action act)
    {
        try{act();passed++;Console.WriteLine("PASS "+passed.ToString("D2")+" "+name);}
        catch(Exception e){failed++;Console.Error.WriteLine("FAIL "+name+" : "+e);}
    }
    private static int Tests()
    {
        Test("01 start emits one frame at zero",()=>{
            var g=new GhostRecorder("CHL","Light",4,P());
            Equal(g.ElapsedSeconds,0f); Equal(g.CheckpointsReached,0);
        });
        Test("02 real four stage completion has 4 checkpoint splits",()=>{
            var t=Run();Check(t.Completed);Equal(t.CheckpointSeconds.Length,4);
        });
        Test("03 elapsed = five seconds",()=>Equal(Run().ElapsedSeconds,5f));
        Test("04 first split exactly one second",()=>Equal(Run().CheckpointSeconds[0],1f));
        Test("05 final split exactly four seconds",()=>Equal(Run().CheckpointSeconds[3],4f));
        Test("06 end reason Completed",()=>Equal(Run().EndReason,"Completed"));
        Test("07 no early finish before checkpoints",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            r.Tick(1,false,P(1));
            Check(Reject(()=>r.Finish(P(1))),"false win accepted");
        });
        Test("08 checkpoint skip rejected",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            Check(Reject(()=>r.Checkpoint(2,P(1))),"skip");
        });
        Test("09 duplicate checkpoint rejected",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            r.Checkpoint(0,P(1));
            Check(Reject(()=>r.Checkpoint(0,P(1))),"duplicate");
        });
        Test("10 checkpoint after final rejected",()=>{
            var r=new GhostRecorder("CHL","Light",1,P());
            r.Checkpoint(0,P());
            Check(Reject(()=>r.Checkpoint(1,P())),"extra checkpoint");
        });
        Test("11 zero-time duplicate does not duplicate frames",()=>{
            var r=new GhostRecorder("CHL","Light",1,P());
            r.Checkpoint(0,P(1));
            r.Tick(1,false,P(2));
            var t=r.Finish(P(3));Equal(t.Frames[0].Time,0f);
        });
        Test("12 time frozen when paused",()=>{
            var r=new GhostRecorder("CHL","Light",1,P());
            r.Tick(1,false,P(1));r.Tick(3,true,P(100));
            Equal(r.ElapsedSeconds,1f);
        });
        Test("13 paused pose not sampled",()=>{
            var r=new GhostRecorder("CHL","Light",1,P());
            r.Tick(1,false,P(1));r.Tick(3,true,P(100));
            r.Checkpoint(0,P(1));var t=r.Finish(P(1));
            Check(t.Frames.All(f=>f.Pose.X < 10),"pause pose recorded");
        });
        Test("14 failed run does not become completed",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            r.Tick(.5f,false,P(1));var g=r.Fail(P(1));
            Check(!g.Completed && g.EndReason=="Failed");
        });
        Test("15 failed run never saved as personal best",()=>{
            var book=new GhostBestBook();var r=new GhostRecorder("CHL","Light",4,P());
            r.Tick(1,false,P());Check(!book.Submit(r.Fail(P())),"invalid best");
        });
        Test("16 completed run accepted as best",()=>{
            var book=new GhostBestBook();Check(book.Submit(Run()));
        });
        Test("17 slower run cannot replace best",()=>{
            var book=new GhostBestBook();book.Submit(Run());
            Check(!book.Submit(Run("Light",2)),"slow replaced");
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,5f);
        });
        Test("18 faster run replaces best",()=>{
            var book=new GhostBestBook();book.Submit(Run("Light",2));
            Check(book.Submit(Run("Light",1)),"fast ignored");
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,5f);
        });
        Test("19 same-time run cannot replace",()=>{
            var book=new GhostBestBook();book.Submit(Run());
            Check(!book.Submit(Run()),"equal overwrote");
        });
        Test("20 two rig identity records independent",()=>{
            var book=new GhostBestBook();book.Submit(Run("Light",1));
            book.Submit(Run("Heavy",2));
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,5f);
            Equal(book.For("CHL003_R1","Heavy").ElapsedSeconds,10f);
        });
        Test("21 other course separate record",()=>{
            var book=new GhostBestBook();book.Submit(Run("Light",1,"A"));
            book.Submit(Run("Light",2,"B"));
            Equal(book.For("A","Light").ElapsedSeconds,5f);
            Equal(book.For("B","Light").ElapsedSeconds,10f);
        });
        Test("22 best lookup missing returns null",()=>{
            var book=new GhostBestBook();Check(book.For("C","Heavy")==null);
        });
        Test("23 returned best clone cannot mutate source",()=>{
            var book=new GhostBestBook();book.Submit(Run());
            var a=book.For("CHL003_R1","Light");a.Frames[0].Pose.X=900;
            Equal(book.For("CHL003_R1","Light").Frames[0].Pose.X,0f);
        });
        Test("24 recorded run clone cannot mutate owned source",()=>{
            var g=Run();var c=GhostMath.Clone(g);
            c.CheckpointSeconds[0]=900;
            Equal(g.CheckpointSeconds[0],1f);
        });
        Test("25 playback at zero returns start pose",()=>{
            Equal(GhostMath.At(Run(),0).X,0f);
        });
        Test("26 playback at end returns final pose",()=>{
            Equal(GhostMath.At(Run(),5).X,5f);
        });
        Test("27 playback clamps time after finish",()=>{
            Equal(GhostMath.At(Run(),999).X,5f);
        });
        Test("28 playback clamps negative time",()=>{
            Equal(GhostMath.At(Run(),-1).X,0f);
        });
        Test("29 exact midpoint interpolation for 2 frame track",()=>{
            var r=new GhostRecorder("X","Light",1,P());
            r.Tick(1,false,P(10));r.Checkpoint(0,P(10));
            var t=r.Finish(P(10));
            Equal(GhostMath.At(t,.5f).X,5f);
        });
        Test("30 heading interpolation shortest way 350→10",()=>{
            var r=new GhostRecorder("X","Light",1,P(0,350));
            r.Tick(1,false,P(1,10));r.Checkpoint(0,P(1,10));
            var x=GhostMath.At(r.Finish(P(1,10)),.5f).Yaw;
            Check(x<1f||x>359f,"heading long arc "+x);
        });
        Test("31 pitch-free yaw normalized modulo 360",()=>{
            var g=new GhostRecorder("X","L",1,P(0,-20));
            g.Tick(1,false,P(1,725));g.Checkpoint(0,P(1,725));
            var run=g.Finish(P(1,725));
            Equal(run.Frames[0].Pose.Yaw,340f);
            Equal(run.Frames[run.Frames.Length-1].Pose.Yaw,5f);
        });
        Test("32 nonfinite X refused",()=>{
            Check(Reject(()=>new GhostRecorder("X","L",1,P(float.NaN))),"accepted NaN X");
        });
        Test("33 infinite yaw refused",()=>{
            Check(Reject(()=>new GhostRecorder("X","L",1,P(0,float.PositiveInfinity))),"inf yaw");
        });
        Test("34 zero checkpoint count refused",()=>{
            Check(Reject(()=>new GhostRecorder("X","L",0,P())),"zero cp");
        });
        Test("35 blank rig refused",()=>{
            Check(Reject(()=>new GhostRecorder("X"," ",1,P())),"blank rig");
        });
        Test("36 negative tick refused without elapsed mutation",()=>{
            var r=new GhostRecorder("X","L",1,P());
            Check(Reject(()=>r.Tick(-1,false,P())),"negative");
            Equal(r.ElapsedSeconds,0f);
        });
        Test("37 NaN tick refused",()=>{
            var r=new GhostRecorder("X","L",1,P());
            Check(Reject(()=>r.Tick(float.NaN,false,P())),"NaN");
        });
        Test("38 tick over 10 sec refused",()=>{
            var r=new GhostRecorder("X","L",1,P());
            Check(Reject(()=>r.Tick(11,false,P())),"large tick");
        });
        Test("39 game time cap rejects overflow",()=>{
            var r=new GhostRecorder("X","L",1,P());
            for(int i=0;i<90;i++)r.Tick(10,false,P(i));
            Check(Reject(()=>r.Tick(1,false,P())),"too long");
        });
        Test("40 postfinish tick rejected",()=>{
            var r=new GhostRecorder("X","L",1,P());
            r.Tick(1,false,P());r.Checkpoint(0,P());r.Finish(P());
            Check(Reject(()=>r.Tick(1,false,P())),"postfinish tick");
        });
        Test("41 second finish rejected",()=>{
            var r=new GhostRecorder("X","L",1,P());
            r.Tick(1,false,P());r.Checkpoint(0,P());r.Finish(P());
            Check(Reject(()=>r.Finish(P())),"two completes");
        });
        Test("42 full-run reference comparison of Light and Heavy",()=>{
            var diff=GhostMath.Compare(Run("Light",1),Run("Heavy",2));
            Equal(diff.TotalDeltaSeconds,5f);
            Equal(diff.SplitDeltaSeconds.Length,4);
            Equal(diff.SplitDeltaSeconds[2],3f);
        });
        Test("43 incompatible course comparison rejects",()=>{
            Check(Reject(()=>GhostMath.Compare(Run("L",1,"A"),Run("L",1,"B"))),"incompatible");
        });
        Test("44 different layouts comparison rejects",()=>{
            var a=Run();var b=Run();b.CheckpointSeconds=new float[]{1};
            Check(Reject(()=>GhostMath.Compare(a,b)),"layout drift");
        });
        Test("45 malformed decreasing frames rejected",()=>{
            var t=Run();t.Frames[1].Time=-1;
            Check(Reject(()=>GhostMath.Validate(t)),"bad order");
        });
        Test("46 overlong ID rejected",()=>{
            Check(Reject(()=>new GhostRecorder(new string('x',129),"L",4,P())),"long ID");
        });
        Test("47 spoofed nonfinite playback time rejected",()=>{
            Check(Reject(()=>GhostMath.At(Run(),float.NaN)),"NaN playback");
        });
        Test("48 display-only recorder cannot modify actual pose",()=>{
            var orig=P(12,70);
            var rec=new GhostRecorder("A","L",1,orig);
            orig.X=999;
            rec.Tick(1,false,P());rec.Checkpoint(0,P());
            Equal(rec.Finish(P()).Frames[0].Pose.X,12f);
        });
        Test("49 samples downsample after 60Hz ticks",()=>{
            var r=new GhostRecorder("X","L",1,P());
            for(int i=0;i<600;i++)r.Tick(1f/60f,false,P(i/60f));
            r.Checkpoint(0,P(10));
            var run=r.Finish(P(10));
            Check(run.Frames.Length>=95&&run.Frames.Length<=110,"unexpected samples "+run.Frames.Length);
        });
        Test("50 seeded 100 different synthetic trajectories",()=>{
            for(int i=1;i<=100;i++){
                var g=new GhostRecorder("Course"+(i%3),"Rig"+(i%2),4,P());
                for(int cp=0;cp<4;cp++){
                    g.Tick(.2f+(i%20)*.07f,false,P(cp+i*.002f,(cp*80+i)%360));
                    g.Checkpoint(cp,P(cp+1,(cp*80+i)%360));
                }
                g.Tick(.2f,false,P(5,350));
                var run=g.Finish(P(5,350));
                var at=GhostMath.At(run,run.ElapsedSeconds/2);
                Check(run.Completed&&float.IsFinite(at.X),"seed "+i);
            }
        });
        Test("51 forged early completion with one checkpoint is refused",()=>{
            var t=Run();t.CheckpointSeconds=new float[]{1};
            Check(Reject(()=>GhostMath.Validate(t)),"accepted forged partial win");
        });
        Test("52 invalid configured zero checkpoints is refused",()=>{
            var t=Run();t.RequiredCheckpoints=0;
            Check(Reject(()=>GhostMath.Validate(t)),"zero configured gates accepted");
        });
        Test("53 loss with a partial legitimate checkpoint path validates",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            r.Tick(1,false,P(1));r.Checkpoint(0,P(1));r.Tick(1,false,P(2));
            var t=r.Fail(P(2));
            GhostMath.Validate(t);
            Check(!t.Completed&&t.RequiredCheckpoints==4&&t.CheckpointSeconds.Length==1,"partial failure invalid");
        });
        Test("54 JSON demo roundtrip retains configured gate count and interpolation",()=>{
            var source=Run();
            var json=JsonSerializer.Serialize(source);
            var copy=JsonSerializer.Deserialize<GhostRun>(json);
            GhostMath.Validate(copy);
            Equal(copy.RequiredCheckpoints,4);
            Equal(GhostMath.At(copy,copy.ElapsedSeconds).X,5f);
        });
        Console.WriteLine("B01-10 GHOST REAL_CSHARP "+passed+" PASS / "+failed+
          " FAIL | 100 seeded synthetic motion traces | UNITY=NOT_RUN");
        return failed==0?0:1;
    }
    public static int Main(string[] args)
    {
        if(args.Length==1&&args[0]=="--test")return Tests();
        if(args.Length==1&&args[0]=="--demo"){
            var light=Run("Light",1);
            var heavy=Run("Heavy",1.5f);
            var data=new {
                notice="SYNTHETIC EXAMPLE, not native Unity physics or human performance",
                light=light,heavy=heavy,difference=GhostMath.Compare(light,heavy)
            };
            Console.WriteLine(JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
            return 0;
        }
        Console.Error.WriteLine("USAGE: dotnet run --project Research/B01-10/P17Ghost/Ghost.Runner.csproj -- --test OR --demo");
        return 2;
    }
}
