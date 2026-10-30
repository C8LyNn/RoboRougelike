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
    // Genuine recorder completion with configurable real gate count. This
    // deliberately avoids forging a completed run via field edits.
    private static GhostRun RunLayout(int gates,float step=1f,
                                      string course="CHL003_R1",string rig="Light")
    {
        var r=new GhostRecorder(course,rig,gates,P());
        for(int i=0;i<gates;i++){
            r.Tick(step,false,P(i+1));
            r.Checkpoint(i,P(i+1));
        }
        r.Tick(step,false,P(gates+1));
        return r.Finish(P(gates+1));
    }
    private static GhostPracticeTransport TrainingTransport()
    {
        var coach=new GhostSplitCoach(Run());
        for(int i=0;i<4;i++)coach.AcceptValidatedGate(i,i+1f);
        coach.EndTrial(true,10f);
        return new GhostPracticeTransport(coach.RecommendedLoop);
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
        Test("55 bound playback follows direct validated pose",()=>{
            var track=Run("Light",1);
            var playback=new GhostPlayback(track);
            for(int i=0;i<=20;i++){
                var t=i*.25f;
                var a=playback.Seek(t);var b=GhostMath.At(track,t);
                Equal(a.X,b.X);Equal(a.Yaw,b.Yaw);
            }
        });
        Test("56 reference track is cloned on bind, source mutation cannot poison",()=>{
            var t=Run();
            var playback=new GhostPlayback(t);
            var original=playback.Seek(0).X;
            t.Frames[0].Pose.X=999;
            t.Frames[1].Time=-10;
            Equal(playback.Seek(0).X,original);
            Equal(playback.Seek(2.5f).X,2.5f);
        });
        Test("57 seek pose result cannot mutate internally held frames",()=>{
            var playback=new GhostPlayback(Run());
            var a=playback.Seek(0);
            a.X=1000;a.Yaw=129;
            Equal(playback.Seek(0).X,0f);
            Equal(playback.Seek(0).Yaw,0f);
        });
        Test("58 bound playback rejects failure as reference",()=>{
            var r=new GhostRecorder("X","L",1,P());
            r.Tick(1,false,P(1));
            var bad=r.Fail(P(1));
            Check(Reject(()=>new GhostPlayback(bad)),"failed race accepted");
        });
        Test("59 playback refuses nonfinite seek even on validated track",()=>{
            var p=new GhostPlayback(Run());
            Check(Reject(()=>p.Seek(float.NaN)),"NaN accepted");
            Check(Reject(()=>p.Seek(float.PositiveInfinity)),"Infinity accepted");
        });
        Test("60 playback exposes immutable identity and duration",()=>{
            var p=new GhostPlayback(Run("Heavy",2,"CHLCOURSE"));
            Equal(p.CourseId,"CHLCOURSE");Equal(p.RigId,"Heavy");
            Equal(p.Duration,10f);
        });
        Test("61 stable playback handles 10000 repeated seeks deterministically",()=>{
            var p=new GhostPlayback(Run());
            double total=0;
            for(int i=0;i<10000;i++){
                var v=p.Seek((i%1001)*.005f);
                Check(float.IsFinite(v.X) && float.IsFinite(v.Yaw),"nonfinite replay");
                total+=v.X;
            }
            Check(total>0,"empty replay");
        });
        Test("62 bound playback clamps both endpoints after bind",()=>{
            var p=new GhostPlayback(Run());
            Equal(p.Seek(-12).X,0f);
            Equal(p.Seek(999).X,5f);
        });
        Test("63 pace coach supports complete 4-gate genuine-order trial",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5);
            Check(s.Completed);Equal(s.BiggestLossGate,0);Equal(s.TotalDeltaSeconds,0f);
        });
        Test("64 pace signal behind when a gate is 0.5 sec late",()=>{
            var c=new GhostSplitCoach(Run());
            var r=c.AcceptValidatedGate(0,1.5f);
            Equal(r.Pace,GhostPace.Behind);Equal(r.SegmentDeltaSeconds,.5f);
        });
        Test("65 pace signal ahead when a gate is 0.5 sec early",()=>{
            var c=new GhostSplitCoach(Run());
            Equal(c.AcceptValidatedGate(0,.5f).Pace,GhostPace.Ahead);
        });
        Test("66 pace signal even for exact reference gate",()=>{
            var c=new GhostSplitCoach(Run());
            Equal(c.AcceptValidatedGate(0,1f).Pace,GhostPace.Even);
        });
        Test("67 cumulative split measured against reference absolute time",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            var r=c.AcceptValidatedGate(1,2.25f);
            Check(Math.Abs(r.CumulativeDeltaSeconds-.25f)<1e-5f);
        });
        Test("68 segment split measured independently of cumulative delta",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            var r=c.AcceptValidatedGate(1,2.25f);
            Check(Math.Abs(r.SegmentDeltaSeconds+.25f)<1e-5f);
        });
        Test("69 exact late second segment yields right practice gate",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            c.AcceptValidatedGate(1,3.5f); // second segment = 2.5, +1.5 sec
            c.AcceptValidatedGate(2,4.5f);
            c.AcceptValidatedGate(3,5.5f);
            var s=c.EndTrial(true,6.5f);
            Equal(s.BiggestLossGate,2);
            Check(Math.Abs(s.BiggestLossSeconds-1.5f)<1e-5f);
        });
        Test("70 no unsupported claims about physical cause",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5);
            Check(!s.Guidance.Contains("collision")&&!s.Guidance.Contains("unstable"),"unsupported physical diagnosis");
        });
        Test("71 forged early completed trial refused atomically",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.EndTrial(true,2)),"early success accepted");
            Equal(c.GatesReached,1);
            Check(!c.IsFinished);
        });
        Test("72 rejected duplicate gate does not advance",()=>{
            var c=new GhostSplitCoach(Run());c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.AcceptValidatedGate(0,2)),"duplicate gate");
            Equal(c.GatesReached,1);
        });
        Test("73 skipped gate rejected without mutation",()=>{
            var c=new GhostSplitCoach(Run());
            Check(Reject(()=>c.AcceptValidatedGate(2,3)),"skip accepted");
            Equal(c.GatesReached,0);
        });
        Test("74 zero-duration real segment rejected and recoverable",()=>{
            var c=new GhostSplitCoach(Run());c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.AcceptValidatedGate(1,1)),"zero elapsed accepted");
            Equal(c.GatesReached,1);
            Equal(c.AcceptValidatedGate(1,2).GateNumber,2);
        });
        Test("75 NaN and infinity gate time fail without state mutation",()=>{
            var c=new GhostSplitCoach(Run());
            Check(Reject(()=>c.AcceptValidatedGate(0,float.NaN)),"NaN");
            Check(Reject(()=>c.AcceptValidatedGate(0,float.PositiveInfinity)),"Infinity");
            Equal(c.GatesReached,0);
        });
        Test("76 incorrect final elapsed rejected without ending",()=>{
            var c=new GhostSplitCoach(Run());c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.EndTrial(false,float.NaN)),"NaN finish");
            Check(!c.IsFinished);
        });
        Test("77 loss before checkpoint permitted and no false victory",()=>{
            var c=new GhostSplitCoach(Run());var s=c.EndTrial(false,.5f);
            Check(!s.Completed&&c.IsFinished);
            Equal(s.BiggestLossGate,0);
        });
        Test("78 stop or post-finalization events refused",()=>{
            var c=new GhostSplitCoach(Run());c.EndTrial(false,.5f);
            Check(Reject(()=>c.AcceptValidatedGate(0,1)),"post end gate");
            Check(Reject(()=>c.EndTrial(false,1)),"duplicate end");
        });
        Test("79 retry restores exact reference and clears trial state",()=>{
            var c=new GhostSplitCoach(Run());c.AcceptValidatedGate(0,1.5f);
            var r=c.Retry();
            Equal(r.GatesReached,0);Equal(r.RequiredGates,4);
            Equal(r.AcceptValidatedGate(0,1).Pace,GhostPace.Even);
        });
        Test("80 benchmark is deep-cloned; source edits cannot alter splits",()=>{
            var run=Run();var c=new GhostSplitCoach(run);
            run.CheckpointSeconds[0]=500;run.Frames[0].Pose.X=50;
            Equal(c.AcceptValidatedGate(0,1).Pace,GhostPace.Even);
        });
        Test("81 returned split is copy, cannot mutate internal last split",()=>{
            var c=new GhostSplitCoach(Run());var x=c.AcceptValidatedGate(0,1);
            x.SegmentDeltaSeconds=999;x.GateNumber=99;
            Equal(c.LastSplit.GateNumber,1);
            Equal(c.LastSplit.SegmentDeltaSeconds,0f);
        });
        Test("82 failed benchmark cannot become a practice coach",()=>{
            var rec=new GhostRecorder("X","L",4,P());
            rec.Tick(1,false,P(1));
            var lost=rec.Fail(P(1));
            Check(Reject(()=>new GhostSplitCoach(lost)),"lost reference");
        });
        Test("83 real finish leg correctly outranks all checkpoint segments",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,10);
            Check(s.Completed&&s.BiggestLossIsFinishSegment,"finish leg not detected");
            Equal(s.BiggestLossGate,0);
            Equal(s.BiggestLossSeconds,5f);
            Equal(s.FinishSegmentSeconds,6f);
            Equal(s.ReferenceFinishSegmentSeconds,1f);
            Equal(s.FinishSegmentDeltaSeconds,5f);
        });
        Test("84 equal finish time gives zero finish segment delta",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5);
            Check(!s.BiggestLossIsFinishSegment&&s.BiggestLossGate==0);
            Equal(s.FinishSegmentDeltaSeconds,0f);
        });
        Test("85 faster last leg cannot be declared a lost segment",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,4.5f);
            Check(!s.BiggestLossIsFinishSegment&&s.BiggestLossGate==0);
            Equal(s.FinishSegmentDeltaSeconds,-.5f);
        });
        Test("86 late checkpoint segment beats smaller finish-leg loss",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            c.AcceptValidatedGate(1,3.5f);
            c.AcceptValidatedGate(2,4.5f);
            c.AcceptValidatedGate(3,5.5f);
            var s=c.EndTrial(true,7);
            Equal(s.BiggestLossGate,2);
            Check(!s.BiggestLossIsFinishSegment);
            Equal(s.BiggestLossSeconds,1.5f);
            Equal(s.FinishSegmentDeltaSeconds,.5f);
        });
        Test("87 false completed run without all gates cannot claim finish split",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.EndTrial(true,10)),"premature win");
            Check(!c.IsFinished&&c.GatesReached==1);
        });
        Test("88 failed trial does not invent an authoritative finish leg",()=>{
            var c=new GhostSplitCoach(Run());
            var s=c.EndTrial(false,2);
            Check(!s.Completed&&!s.BiggestLossIsFinishSegment);
            Equal(s.FinishSegmentSeconds,0f);
            Equal(s.ReferenceFinishSegmentSeconds,0f);
            Equal(s.FinishSegmentDeltaSeconds,0f);
        });
        Test("89 completed reference with zero finish leg rejected at bind",()=>{
            var r=Run();r.CheckpointSeconds[3]=5f;
            GhostMath.Validate(r); // valid recorder format, invalid coaching leg
            Check(Reject(()=>new GhostSplitCoach(r)),"zero finish reference");
        });
        Test("90 completed reference with duplicate gate timestamps refused",()=>{
            var r=Run();r.CheckpointSeconds[1]=1f;
            GhostMath.Validate(r); // source snapshot tolerates simultaneous markers
            Check(Reject(()=>new GhostSplitCoach(r)),"zero gate interval");
        });
        Test("91 completed reference with checkpoint at time zero refused",()=>{
            var r=Run();r.CheckpointSeconds[0]=0f;
            GhostMath.Validate(r);
            Check(Reject(()=>new GhostSplitCoach(r)),"zero first interval");
        });
        Test("92 finish advice points to actual last section, not gate 4",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,10);
            Check(s.Guidance.Contains("finish segment after checkpoint 4"),"wrong advice");
            Check(!s.Guidance.Contains("ending at checkpoint 4"),"wrong gate label");
        });
        Test("93 source reference changes after coach bind cannot spoof finish",()=>{
            var r=Run();var c=new GhostSplitCoach(r);
            r.CheckpointSeconds[3]=4.99f;r.ElapsedSeconds=9;
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,10);
            Equal(s.ReferenceFinishSegmentSeconds,1f);
            Equal(s.FinishSegmentDeltaSeconds,5f);
        });
        Test("94 small finish delta below threshold gives neutral advice",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5.1f);
            Check(!s.BiggestLossIsFinishSegment || s.BiggestLossSeconds<.15f);
            Check(s.Guidance.Contains("No measured segment"),"small delta overreported");
        });
        Test("95 failed early completion leaves coach available for valid finish",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            Check(Reject(()=>c.EndTrial(true,9)),"false finish");
            c.AcceptValidatedGate(1,2);
            c.AcceptValidatedGate(2,3);
            c.AcceptValidatedGate(3,4);
            var s=c.EndTrial(true,10);
            Check(s.Completed && s.BiggestLossIsFinishSegment);
        });
        Test("96 tied finish and gate loss preserves first checkpoint focus",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.AcceptValidatedGate(1,2.5f);
            c.AcceptValidatedGate(2,3.5f);
            c.AcceptValidatedGate(3,4.5f);
            var s=c.EndTrial(true,6f);
            Equal(s.BiggestLossGate,1);
            Check(!s.BiggestLossIsFinishSegment);
            Equal(s.BiggestLossSeconds,.5f);
        });
        Test("97 aborted before any gate never claims full-course speedup",()=>{
            var c=new GhostSplitCoach(Run());
            var s=c.EndTrial(false,.5f);
            Check(!s.Completed&&!s.HasFullCourseComparison);
            Equal(s.TotalDeltaSeconds,0f);
            Equal(s.TrialSeconds,.5f);
            Equal(s.ReferenceSeconds,5f);
        });
        Test("98 aborted after valid gates preserves partial losses but no finish delta",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.AcceptValidatedGate(1,2.5f);
            var s=c.EndTrial(false,3f);
            Check(!s.HasFullCourseComparison);
            Equal(s.TotalDeltaSeconds,0f);
            Equal(s.BiggestLossGate,1);
            Equal(s.BiggestLossSeconds,.5f);
            Equal(s.FinishSegmentSeconds,0f);
        });
        Test("99 completed exact reference enables full-course comparison",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5f);
            Check(s.HasFullCourseComparison);
            Equal(s.TotalDeltaSeconds,0f);
        });
        Test("100 completed late finish exposes actual positive full-course delta",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,10f);
            Check(s.HasFullCourseComparison&&s.Completed);
            Equal(s.TotalDeltaSeconds,5f);
        });
        Test("101 subthreshold real gate loss gives neutral HUD focus",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.1f);
            c.AcceptValidatedGate(1,2.1f);
            c.AcceptValidatedGate(2,3.1f);
            c.AcceptValidatedGate(3,4.1f);
            var s=c.EndTrial(true,5.1f);
            Equal(s.BiggestLossGate,1); // R5 HUD incorrectly showed Focus: gate 1
            Check(s.BiggestLossSeconds<GhostSplitCoach.PaceToleranceSeconds);
            Equal(GhostPracticeFocus.Text(s,4),"No significant loss (finish included)");
        });
        Test("102 significant real checkpoint loss names correct gate",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.AcceptValidatedGate(1,2.5f);
            c.AcceptValidatedGate(2,3.5f);
            c.AcceptValidatedGate(3,4.5f);
            var s=c.EndTrial(true,5.5f);
            Equal(GhostPracticeFocus.Text(s,4),"Focus: gate 1");
        });
        Test("103 significant real finish loss names final segment not fifth gate",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,10);
            Equal(GhostPracticeFocus.Text(s,4),"Focus: finish after gate 4");
        });
        Test("104 aborted partial run never exposes actionable full finish focus",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            var s=c.EndTrial(false,2);
            Equal(GhostPracticeFocus.Text(s,4),"Retry: no finish segment measured");
        });
        Test("105 complete reference tie receives neutral focus",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var s=c.EndTrial(true,5);
            Equal(GhostPracticeFocus.Text(s,4),"No significant loss (finish included)");
        });
        Test("106 tolerance boundary itself is neutral, not a gate focus",()=>{
            var s=new GhostPracticeSummary {Completed=true,
                HasFullCourseComparison=true, BiggestLossGate=2,
                BiggestLossSeconds=GhostSplitCoach.PaceToleranceSeconds};
            Equal(GhostPracticeFocus.Text(s,4),"No significant loss (finish included)");
        });
        Test("107 null focus source rejected before UI access",()=>{
            Check(Reject(()=>GhostPracticeFocus.Text(null,4)),"null summary");
        });
        Test("108 invalid gate count rejected before generating false advice",()=>{
            var s=new GhostPracticeSummary {Completed=true,
                HasFullCourseComparison=true, BiggestLossSeconds=1,
                BiggestLossGate=1};
            Check(Reject(()=>GhostPracticeFocus.Text(s,0)),"invalid gate count");
        });
        Test("109 significant loss without a real gate fails closed",()=>{
            var s=new GhostPracticeSummary {Completed=true,
                HasFullCourseComparison=true, BiggestLossSeconds=1,
                BiggestLossGate=0, BiggestLossIsFinishSegment=false};
            Check(Reject(()=>GhostPracticeFocus.Text(s,4)),"forged focus");
        });
        Test("110 significant gate 1 generates replay segment zero to first reference gate",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.AcceptValidatedGate(1,2.5f);
            c.AcceptValidatedGate(2,3.5f);
            c.AcceptValidatedGate(3,4.5f);
            c.EndTrial(true,5.5f);
            var loop=c.RecommendedLoop;
            Check(loop!=null&&!loop.IsFinishSegment);
            Equal(loop.GateNumber,1);Equal(loop.StartSeconds,0f);
            Equal(loop.EndSeconds,1f);Equal(loop.SegmentDurationSeconds,1f);
            Equal(loop.Label,"gate 1");
        });
        Test("111 significant gate 2 replays only reference gate1-to-gate2",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1);
            c.AcceptValidatedGate(1,3.5f);
            c.AcceptValidatedGate(2,4.5f);
            c.AcceptValidatedGate(3,5.5f);
            c.EndTrial(true,6.5f);
            var loop=c.RecommendedLoop;
            Check(loop!=null);Equal(loop.GateNumber,2);
            Equal(loop.StartSeconds,1f);Equal(loop.EndSeconds,2f);
            Check(Math.Abs(loop.PoseAt(.5d).X-1.5f)<1e-5f);
        });
        Test("112 worst finished leg picks last checkpoint to reference finish",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(loop!=null&&loop.IsFinishSegment);
            Equal(loop.GateNumber,4);Equal(loop.StartSeconds,4f);
            Equal(loop.EndSeconds,5f);
            Equal(loop.Label,"finish after gate 4");
        });
        Test("113 replay loops exactly at segment duration with no clock mutation",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Equal(loop.PoseAt(0).X,4f);
            Equal(loop.PoseAt(1).X,4f);
            Equal(loop.PoseAt(2).X,4f);
            Check(Math.Abs(loop.PoseAt(2.5d).X-4.5f)<1e-5f);
        });
        Test("114 no actionable loop for completed benchmark pace tie",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,5f);
            Check(c.RecommendedLoop==null);
        });
        Test("115 failed attempt after real checkpoint never generates a loop",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.EndTrial(false,2f);
            Check(c.RecommendedLoop==null);
        });
        Test("116 subthreshold pace difference does not produce false drill",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.1f);
            c.AcceptValidatedGate(1,2.1f);
            c.AcceptValidatedGate(2,3.1f);
            c.AcceptValidatedGate(3,4.1f);
            c.EndTrial(true,5.1f);
            Check(c.RecommendedLoop==null);
        });
        Test("117 retry clears actionable loop until new genuine finish",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(c.RecommendedLoop!=null);
            Check(c.Retry().RecommendedLoop==null);
        });
        Test("118 invalid visual clocks rejected without altering replay",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(Reject(()=>loop.PoseAt(-1)),"negative clock accepted");
            Check(Reject(()=>loop.PoseAt(double.NaN)),"NaN clock accepted");
            Check(Reject(()=>loop.PoseAt(double.PositiveInfinity)),"infinite clock accepted");
            Check(Reject(()=>loop.PoseAt(86401)),"unbounded clock accepted");
            Equal(loop.PoseAt(0).X,4f);
        });
        Test("119 post-bind caller benchmark corruption cannot change loop",()=>{
            var run=Run();var c=new GhostSplitCoach(run);
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            run.Frames[4].Pose.X=900f;
            run.CheckpointSeconds[3]=4.99f;
            Equal(c.RecommendedLoop.StartSeconds,4f);
            Equal(c.RecommendedLoop.PoseAt(0).X,4f);
        });
        Test("120 defensive sampled pose edits cannot mutate the replay source",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            var pose=loop.PoseAt(.5d);pose.X=9999;
            Check(Math.Abs(loop.PoseAt(.5d).X-4.5f)<1e-5f);
        });
        Test("121 tied gate-versus-finish losses preserve first actionable gate",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);
            c.AcceptValidatedGate(1,2.5f);
            c.AcceptValidatedGate(2,3.5f);
            c.AcceptValidatedGate(3,4.5f);
            c.EndTrial(true,6f);
            var loop=c.RecommendedLoop;
            Check(loop!=null&&!loop.IsFinishSegment);
            Equal(loop.GateNumber,1);
        });
        Test("122 before authoritative end no training loop may be consumed",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            Check(c.RecommendedLoop==null);
        });
        Test("123 failed all-gate trial cannot be upgraded by flipping Completed",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            for(int i=0;i<4;i++){
                r.Tick(1f,false,P(i+1));
                r.Checkpoint(i,P(i+1));
            }
            r.Tick(1f,false,P(5));
            var failed=r.Fail(P(5));
            Check(!failed.Completed&&failed.EndReason=="Failed");
            Equal(failed.CheckpointSeconds.Length,4);
            failed.Completed=true;
            Check(Reject(()=>GhostMath.Validate(failed)),
                "failed completed-checkpoint trial forged into valid completion");
        });
        Test("124 mismatch Completed true and reason Failed rejected",()=>{
            var g=Run();g.EndReason="Failed";
            Check(Reject(()=>GhostMath.Validate(g)),"bad terminal state accepted");
        });
        Test("125 mismatch Completed false and reason Completed rejected",()=>{
            var g=Run();g.Completed=false;
            Check(Reject(()=>GhostMath.Validate(g)),"false failure flag accepted");
        });
        Test("126 blank terminal reason rejected",()=>{
            var g=Run();g.EndReason="";
            Check(Reject(()=>GhostMath.Validate(g)),"blank terminal reason accepted");
        });
        Test("127 null terminal reason rejected",()=>{
            var g=Run();g.EndReason=null;
            Check(Reject(()=>GhostMath.Validate(g)),"null reason accepted");
        });
        Test("128 case or whitespace spoofed reason rejected",()=>{
            var g=Run();g.EndReason="completed";
            Check(Reject(()=>GhostMath.Validate(g)),"case altered reason accepted");
            g.EndReason="Completed ";
            Check(Reject(()=>GhostMath.Validate(g)),"suffix altered reason accepted");
        });
        Test("129 genuine completed recording remains fully valid",()=>{
            var g=Run();GhostMath.Validate(g);
            Equal(GhostMath.Clone(g).EndReason,"Completed");
            var pb=new GhostPlayback(g);
            Equal(pb.Duration,g.ElapsedSeconds);
        });
        Test("130 genuine failed recording remains valid but is not best",()=>{
            var r=new GhostRecorder("CHL","Light",4,P());
            r.Tick(.5f,false,P(1));
            var failed=r.Fail(P(1));
            GhostMath.Validate(failed);
            Check(!new GhostBestBook().Submit(failed),"failed run became personal best");
        });
        Test("131 failed terminal reason cannot replace existing personal best",()=>{
            var book=new GhostBestBook();book.Submit(Run());
            var spoofed=Run();spoofed.EndReason="Failed";
            Check(Reject(()=>book.Submit(spoofed)),"mutated completion replaced PB");
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,5f);
        });
        Test("132 spoofed reference blocked in visual replay and coach",()=>{
            var bad=Run();bad.EndReason="Failed";
            Check(Reject(()=>new GhostPlayback(bad)),"playback accepted bad end");
            Check(Reject(()=>new GhostSplitCoach(bad)),"coach accepted bad end");
        });
        Test("133 inconsistent trial blocked in cross-rig comparison",()=>{
            var trial=Run("Heavy");trial.EndReason="Failed";
            Check(Reject(()=>GhostMath.Compare(Run(),trial)),
                "compare accepted failed terminal reason");
        });
        Test("134 real ended failed all-gate run never becomes benchmark",()=>{
            var r=new GhostRecorder("C","Heavy",4,P());
            for(int i=0;i<4;i++){
                r.Tick(1f,false,P(i+1));r.Checkpoint(i,P(i+1));
            }
            r.Tick(1,false,P(5));
            var failure=r.Fail(P(5));
            GhostMath.Validate(failure);
            Check(Reject(()=>new GhostSplitCoach(failure)),
                "all-gate failed trial accepted as benchmark");
        });
        Test("135 faster three-gate layout cannot overwrite genuine four-gate best",()=>{
            var book=new GhostBestBook();
            Check(book.Submit(RunLayout(4,2f)));
            Check(Reject(()=>book.Submit(RunLayout(3,1f))),
                "incompatible faster three-gate time replaced four-gate record");
            var saved=book.For("CHL003_R1","Light");
            Equal(saved.RequiredCheckpoints,4);Equal(saved.ElapsedSeconds,10f);
        });
        Test("136 faster four-gate layout cannot replace saved three-gate best",()=>{
            var book=new GhostBestBook();
            Check(book.Submit(RunLayout(3,2f)));
            Check(Reject(()=>book.Submit(RunLayout(4,1f))),
                "four-gate time replaced incompatible three-gate record");
            var saved=book.For("CHL003_R1","Light");
            Equal(saved.RequiredCheckpoints,3);Equal(saved.ElapsedSeconds,8f);
        });
        Test("137 equal-duration different layouts fail closed, not treated as tie",()=>{
            var book=new GhostBestBook();
            Check(book.Submit(RunLayout(3,2f))); // 8 s
            Check(Reject(()=>book.Submit(RunLayout(4,1.6f))),"mismatched tie");
            Equal(book.For("CHL003_R1","Light").RequiredCheckpoints,3);
        });
        Test("138 rejected layout does not change stored best frames",()=>{
            var book=new GhostBestBook();book.Submit(RunLayout(4,1f));
            var before=book.For("CHL003_R1","Light");
            Check(Reject(()=>book.Submit(RunLayout(3,.5f))));
            var after=book.For("CHL003_R1","Light");
            Equal(after.Frames.Length,before.Frames.Length);
            Equal(after.Frames[after.Frames.Length-1].Pose.X,5f);
        });
        Test("139 distinct course versions may legitimately use different gate counts",()=>{
            var book=new GhostBestBook();
            Check(book.Submit(RunLayout(4,1f,"CHL_R1")));
            Check(book.Submit(RunLayout(3,1f,"CHL_R2")));
            Equal(book.For("CHL_R1","Light").RequiredCheckpoints,4);
            Equal(book.For("CHL_R2","Light").RequiredCheckpoints,3);
        });
        Test("140 different rigs on same course label remain independent",()=>{
            var book=new GhostBestBook();
            Check(book.Submit(RunLayout(4,1f,"CHL","Light")));
            Check(book.Submit(RunLayout(3,1f,"CHL","Heavy")));
            Equal(book.For("CHL","Light").RequiredCheckpoints,4);
            Equal(book.For("CHL","Heavy").RequiredCheckpoints,3);
        });
        Test("141 same gate count faster time still improves personal best",()=>{
            var book=new GhostBestBook();book.Submit(RunLayout(3,2f));
            Check(book.Submit(RunLayout(3,1f)),"valid improvement refused");
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,4f);
        });
        Test("142 same gate count slower time still leaves best unchanged",()=>{
            var book=new GhostBestBook();book.Submit(RunLayout(3,1f));
            Check(!book.Submit(RunLayout(3,2f)),"slower run replaced record");
            Equal(book.For("CHL003_R1","Light").ElapsedSeconds,4f);
        });
        Test("143 legitimate failed other-layout trial remains no PB update",()=>{
            var book=new GhostBestBook();book.Submit(RunLayout(4));
            var recorder=new GhostRecorder("CHL003_R1","Light",3,P());
            recorder.Tick(1f,false,P(1));
            Check(!book.Submit(recorder.Fail(P(1))),"failed layout changed PB");
            Equal(book.For("CHL003_R1","Light").RequiredCheckpoints,4);
        });
        Test("144 original eight-gate limit retains separate layout protection",()=>{
            var book=new GhostBestBook();book.Submit(RunLayout(8,1f));
            Check(Reject(()=>book.Submit(RunLayout(4,.5f))));
            Equal(book.For("CHL003_R1","Light").RequiredCheckpoints,8);
        });
        Test("145 new best book can accept new course layout after explicit reset",()=>{
            var prior=new GhostBestBook();prior.Submit(RunLayout(4));
            var newBook=new GhostBestBook();
            Check(newBook.Submit(RunLayout(3)),"fresh identity book refused");
            Equal(prior.For("CHL003_R1","Light").RequiredCheckpoints,4);
            Equal(newBook.For("CHL003_R1","Light").RequiredCheckpoints,3);
        });
        Test("146 presentation captures validated source checkpoint",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            v.CaptureGate(c.AcceptValidatedGate(0,1.5f));
            Equal(v.LastGate.GateNumber,1);Equal(v.LastGate.Pace,GhostPace.Behind);
        });
        Test("147 returned host checkpoint mutation cannot forge HUD",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            var g=c.AcceptValidatedGate(0,1.5f);v.CaptureGate(g);
            g.GateNumber=8;g.SegmentSeconds=900;g.Pace=GhostPace.Ahead;
            Equal(v.LastGate.GateNumber,1);Equal(v.LastGate.SegmentSeconds,1.5f);
            Equal(v.LastGate.Pace,GhostPace.Behind);
        });
        Test("148 presentation checkpoint getter returns defensive clone",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            v.CaptureGate(c.AcceptValidatedGate(0,1f));
            var editable=v.LastGate;editable.GateNumber=7;
            Equal(v.LastGate.GateNumber,1);
        });
        Test("149 completed coach finish is retained with real focus",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            v.CaptureEnd(c.EndTrial(true,10f));
            Check(v.FinalSummary.Completed && v.FinalSummary.BiggestLossIsFinishSegment);
            Equal(v.FinalSummary.BiggestLossSeconds,5f);
        });
        Test("150 returned host finish edit cannot forge HUD completion",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            var result=c.EndTrial(true,10f);v.CaptureEnd(result);
            var guidance=v.FinalSummary.Guidance;
            result.Completed=false;result.BiggestLossSeconds=0;
            result.BiggestLossIsFinishSegment=false;result.Guidance="forged";
            Check(v.FinalSummary.Completed && v.FinalSummary.BiggestLossIsFinishSegment);
            Equal(v.FinalSummary.BiggestLossSeconds,5f);
            Equal(v.FinalSummary.Guidance,guidance);
        });
        Test("151 public finish getter cannot alter retained summary",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            v.CaptureEnd(c.EndTrial(true,10f));
            var shown=v.FinalSummary;shown.Completed=false;shown.BiggestLossSeconds=0;
            Check(v.FinalSummary.Completed);Equal(v.FinalSummary.BiggestLossSeconds,5f);
        });
        Test("152 aborted source result remains incomplete on HUD",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            c.AcceptValidatedGate(0,1f);v.CaptureEnd(c.EndTrial(false,2f));
            Check(!v.FinalSummary.Completed&&!v.FinalSummary.HasFullCourseComparison);
        });
        Test("153 retry clears both prior display snapshots",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            v.CaptureGate(c.AcceptValidatedGate(0,1f));
            v.CaptureEnd(c.EndTrial(false,2f));v.Clear();
            Check(v.LastGate==null && v.FinalSummary==null);
        });
        Test("154 null gate rejected without erasing prior checkpoint",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            v.CaptureGate(c.AcceptValidatedGate(0,1f));
            Check(Reject(()=>v.CaptureGate(null)));
            Equal(v.LastGate.GateNumber,1);
        });
        Test("155 impossible 9th gate rejected without mutation",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            v.CaptureGate(c.AcceptValidatedGate(0,1f));
            Check(Reject(()=>v.CaptureGate(new GhostSplitResult{GateNumber=9})));
            Equal(v.LastGate.GateNumber,1);
        });
        Test("156 null finish rejected without losing genuine source finish",()=>{
            var c=new GhostSplitCoach(Run());var v=new GhostPracticePresentation();
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            v.CaptureEnd(c.EndTrial(true,10f));
            Check(Reject(()=>v.CaptureEnd(null)));Check(v.FinalSummary.Completed);
        });
        Test("157 independent presentation buffers cannot alias outputs",()=>{
            var c=new GhostSplitCoach(Run());var a=new GhostPracticePresentation();
            var b=new GhostPracticePresentation();
            var gate=c.AcceptValidatedGate(0,1f);
            a.CaptureGate(gate);b.CaptureGate(gate);
            a.Clear();gate.GateNumber=8;
            Check(a.LastGate==null);Equal(b.LastGate.GateNumber,1);
        });
        Test("158 quarter-speed visual repeat samples one-eighth reference second",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(Math.Abs(c.RecommendedLoop.PoseAt(.5d,.25f).X-4.125f)<1e-5f);
        });
        Test("159 half-speed visual repeat samples one-quarter reference second",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(Math.Abs(c.RecommendedLoop.PoseAt(.5d,.5f).X-4.25f)<1e-5f);
        });
        Test("160 old 1x signature and new explicit 1x agree",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Equal(c.RecommendedLoop.PoseAt(.5d).X,c.RecommendedLoop.PoseAt(.5d,1f).X);
        });
        Test("161 double visual rate advances ghost by double time",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(Math.Abs(c.RecommendedLoop.PoseAt(.25d,2f).X-4.5f)<1e-5f);
        });
        Test("162 double rate wraps exactly at half real second",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Equal(c.RecommendedLoop.PoseAt(.5d,2f).X,4f);
        });
        Test("163 quarter rate wraps at four visual seconds",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Equal(c.RecommendedLoop.PoseAt(4d,.25f).X,4f);
        });
        Test("164 quarter rate preserves end-of-segment source pose before wrap",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(Math.Abs(c.RecommendedLoop.PoseAt(3d,.25f).X-4.75f)<1e-5f);
        });
        Test("165 intermediate 1.25x controls visual source pose",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            Check(Math.Abs(c.RecommendedLoop.PoseAt(.4d,1.25f).X-4.5f)<1e-5f);
        });
        Test("166 slowed middle gate samples only its reference interval",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1f);c.AcceptValidatedGate(1,3.5f);
            c.AcceptValidatedGate(2,4.5f);c.AcceptValidatedGate(3,5.5f);
            c.EndTrial(true,6.5f);
            var loop=c.RecommendedLoop;
            Equal(loop.GateNumber,2);Equal(loop.StartSeconds,1f);
            Check(Math.Abs(loop.PoseAt(1d,.5f).X-1.5f)<1e-5f);
        });
        Test("167 slowed gate one starts from reference t zero",()=>{
            var c=new GhostSplitCoach(Run());
            c.AcceptValidatedGate(0,1.5f);c.AcceptValidatedGate(1,2.5f);
            c.AcceptValidatedGate(2,3.5f);c.AcceptValidatedGate(3,4.5f);
            c.EndTrial(true,5.5f);
            var loop=c.RecommendedLoop;
            Equal(loop.GateNumber,1);Equal(loop.StartSeconds,0f);
            Check(Math.Abs(loop.PoseAt(.75d,.5f).X-.375f)<1e-5f);
        });
        Test("168 zero or negative speed never pauses gameplay or creates fake sample",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(Reject(()=>loop.PoseAt(.5d,0f)));
            Check(Reject(()=>loop.PoseAt(.5d,-1f)));
            Equal(loop.PoseAt(0).X,4f);
        });
        Test("169 speed below quarter or above double rejected before seek",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(Reject(()=>loop.PoseAt(.5d,.24f)));
            Check(Reject(()=>loop.PoseAt(.5d,2.01f)));
            Equal(loop.PoseAt(.5d).X,4.5f);
        });
        Test("170 NaN and both infinite visual speeds rejected",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(Reject(()=>loop.PoseAt(.5d,float.NaN)));
            Check(Reject(()=>loop.PoseAt(.5d,float.PositiveInfinity)));
            Check(Reject(()=>loop.PoseAt(.5d,float.NegativeInfinity)));
        });
        Test("171 sped visual seek still validates clock and clones pose",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1);
            c.EndTrial(true,10f);
            var loop=c.RecommendedLoop;
            Check(Reject(()=>loop.PoseAt(double.NaN,2f)));
            Check(Reject(()=>loop.PoseAt(-1d,.5f)));
            Check(Reject(()=>loop.PoseAt(86401d,.25f)));
            var mutable=loop.PoseAt(.25d,2f);
            mutable.X=1000;
            Check(Math.Abs(loop.PoseAt(.25d,2f).X-4.5f)<1e-5f);
        });
        Test("172 transport rejects absent real coach focus loop",()=>{
            Check(Reject(()=>new GhostPracticeTransport(null)));
        });
        Test("173 new transport starts paused at first focus pose",()=>{
            var c=new GhostSplitCoach(Run());
            for(int i=0;i<4;i++)c.AcceptValidatedGate(i,i+1f);
            c.EndTrial(true,10f);
            var t=new GhostPracticeTransport(c.RecommendedLoop);
            Check(!t.IsPlaying);Equal(t.PlaybackSpeed,1f);
            Equal(t.ReferencePositionSeconds,0d);Equal(t.Pose().X,4f);
        });
        Test("174 paused visual transport ignores valid clock advances",()=>{
            var t=TrainingTransport();t.Advance(.5d);
            Equal(t.ReferencePositionSeconds,0d);Equal(t.Pose().X,4f);
        });
        Test("175 play advances only local reference cursor",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.25d);
            Check(t.IsPlaying);Equal(t.ReferencePositionSeconds,.25d);
            Check(Math.Abs(t.Pose().X-4.25f)<1e-5f);
        });
        Test("176 half speed advances only half future visual delta",()=>{
            var t=TrainingTransport();t.SetSpeed(.5f);t.Play();t.Advance(.5d);
            Equal(t.ReferencePositionSeconds,.25d);Equal(t.Pose().X,4.25f);
        });
        Test("177 changing speed does NOT jump already displayed pose",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.25d);
            var before=t.Pose().X;
            t.SetSpeed(.25f);
            Equal(t.Pose().X,before);Equal(t.ReferencePositionSeconds,.25d);
        });
        Test("178 changed speed applies only on subsequent advances",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.25d);
            t.SetSpeed(.5f);t.Advance(.25d);
            Equal(t.ReferencePositionSeconds,.375d);
            Check(Math.Abs(t.Pose().X-4.375f)<1e-5f);
        });
        Test("179 normal speed wraps completed reference focus segment",()=>{
            var t=TrainingTransport();t.Play();t.Advance(1d);
            Equal(t.ReferencePositionSeconds,0d);Equal(t.Pose().X,4f);
        });
        Test("180 pause freezes existing nonzero source cursor",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.2d);t.Pause();
            t.Advance(.5d);Check(!t.IsPlaying);
            Check(Math.Abs(t.Pose().X-4.2f)<1e-5f);
        });
        Test("181 resume continues from same paused ghost frame",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.2d);
            t.Pause();t.Advance(1d);t.Play();t.Advance(.1d);
            Check(Math.Abs(t.Pose().X-4.3f)<1e-5f);
        });
        Test("182 single step forward is a reference-time increment",()=>{
            var t=TrainingTransport();t.Step(.1d);
            Check(!t.IsPlaying);Check(Math.Abs(t.Pose().X-4.1f)<1e-5f);
        });
        Test("183 backwards step wraps source-only reference loop",()=>{
            var t=TrainingTransport();t.Step(-.1d);
            Check(!t.IsPlaying);Check(Math.Abs(t.Pose().X-4.9f)<1e-5f);
        });
        Test("184 frame stepping while playing is rejected atomically",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.3d);
            Check(Reject(()=>t.Step(.2d)));
            Equal(t.ReferencePositionSeconds,.3d);Check(t.IsPlaying);
        });
        Test("185 restart returns to start and pauses playback",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.4d);
            t.Restart();Check(!t.IsPlaying);
            Equal(t.ReferencePositionSeconds,0d);Equal(t.Pose().X,4f);
        });
        Test("186 explicit seek position and end wrap are deterministic",()=>{
            var t=TrainingTransport();t.Seek(.75d);
            Equal(t.ReferencePositionSeconds,.75d);Equal(t.Pose().X,4.75f);
            t.Seek(1d);Equal(t.ReferencePositionSeconds,0d);
        });
        Test("187 invalid position seeks preserve previous visual cursor",()=>{
            var t=TrainingTransport();t.Seek(.5d);
            Check(Reject(()=>t.Seek(double.NaN)));
            Check(Reject(()=>t.Seek(double.PositiveInfinity)));
            Check(Reject(()=>t.Seek(-.01d)));
            Check(Reject(()=>t.Seek(1.01d)));
            Equal(t.ReferencePositionSeconds,.5d);
        });
        Test("188 corrupt visual clock delta never advances cursor",()=>{
            var t=TrainingTransport();t.Play();t.Advance(.25d);
            Check(Reject(()=>t.Advance(-1d)));
            Check(Reject(()=>t.Advance(double.NaN)));
            Check(Reject(()=>t.Advance(double.PositiveInfinity)));
            Check(Reject(()=>t.Advance(10.01d)));
            Equal(t.ReferencePositionSeconds,.25d);
        });
        Test("189 invalid zero and negative visual speed preserve state",()=>{
            var t=TrainingTransport();t.SetSpeed(.5f);
            Check(Reject(()=>t.SetSpeed(0f)));
            Check(Reject(()=>t.SetSpeed(-1f)));
            Equal(t.PlaybackSpeed,.5f);
        });
        Test("190 inclusive 0.25x and 2x speed boundaries work",()=>{
            var t=TrainingTransport();t.SetSpeed(.25f);t.Play();t.Advance(1d);
            Equal(t.ReferencePositionSeconds,.25d);
            t.SetSpeed(2f);t.Advance(.125d);
            Equal(t.ReferencePositionSeconds,.5d);
        });
        Test("191 NaN infinite and out-of-bounds speed cannot mutate state",()=>{
            var t=TrainingTransport();t.SetSpeed(1.5f);
            Check(Reject(()=>t.SetSpeed(float.NaN)));
            Check(Reject(()=>t.SetSpeed(float.PositiveInfinity)));
            Check(Reject(()=>t.SetSpeed(.24f)));
            Check(Reject(()=>t.SetSpeed(2.01f)));
            Equal(t.PlaybackSpeed,1.5f);
        });
        Test("192 returned ghost pose cannot corrupt transport reference",()=>{
            var t=TrainingTransport();t.Seek(.5d);
            var pose=t.Pose();pose.X=999f;
            Equal(t.Pose().X,4.5f);
        });
        Test("193 paused speed changes preserve current cursor and stay paused",()=>{
            var t=TrainingTransport();t.Seek(.75d);
            t.SetSpeed(.25f);t.Advance(1d);
            Check(!t.IsPlaying);Equal(t.ReferencePositionSeconds,.75d);
            Equal(t.Pose().X,4.75f);
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
