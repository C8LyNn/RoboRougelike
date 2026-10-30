using System;
using B01.UXComfort;

static class Program
{
    static int passed,failed;
    static void Test(string name,Func<bool> assertion)
    {
        try{if(!assertion())throw new Exception("assert false");passed++;Console.WriteLine("PASS "+name);}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}
    }
    static bool Throws<T>(Action action) where T:Exception
    {try{action();return false;}catch(T){return true;}}
    static VisualStimulusGate Gate(ComfortMode mode=ComfortMode.Standard,int n=2,int gap=250,int shake=45)
    {return new VisualStimulusGate(7,mode,n,gap,shake);}
    static VisualCue Cue(long seq,long ms,VisualCueKind kind=VisualCueKind.DamageFlash,
        int intensity=100,int run=7)
    {return new VisualCue{RunId=run,Sequence=seq,TimeMs=ms,Kind=kind,Intensity=intensity};}
    static bool Static(VisualDecision d,Indicator i)
    {return d!=null&&d.RenderMode==RenderMode.StaticIndicator&&d.Indicator==i&&
        d.Outcome==CueOutcome.ReplacedByStatic&&d.Intensity==0;}
    static void Main()
    {
        Test("new gate standard",()=>Gate().Mode==ComfortMode.Standard);
        Test("initial run",()=>Gate().RunId==7);
        Test("initial time",()=>Gate().NowMs==0);
        Test("initial flash budget clear",()=>Gate().FlashCountWindow==0);
        Test("first flash scheduled",()=>Gate().Record(Cue(1,0)).RenderMode==RenderMode.TimedEffect);
        Test("first flash capped 60",()=>Gate().Record(Cue(1,0)).Intensity==60);
        Test("first flash records one window stamp",()=>{var g=Gate();g.Record(Cue(1,0));return g.FlashCountWindow==1;});
        Test("first flash outcome scheduled",()=>Gate().Record(Cue(1,0)).Outcome==CueOutcome.Scheduled);
        Test("flash source event run retained",()=>Gate().Record(Cue(1,0)).RunId==7);
        Test("flash source event sequence retained",()=>Gate().Record(Cue(15,15)).Sequence==15);
        Test("flash source event time retained",()=>Gate().Record(Cue(1,250)).TimeMs==250);
        Test("flash source event kind retained",()=>Gate().Record(Cue(1,0)).Kind==VisualCueKind.DamageFlash);
        Test("flash mapped to damage semantic glyph",()=>Gate().Record(Cue(1,0)).Indicator==Indicator.Damage);
        Test("zero flash intensity allowed",()=>Gate().Record(Cue(1,0,intensity:0)).Intensity==0);
        Test("small flash intensity unchanged",()=>Gate().Record(Cue(1,0,intensity:25)).Intensity==25);
        Test("same timestamp second flash becomes static",()=>{
            var g=Gate();g.Record(Cue(1,0));return Static(g.Record(Cue(2,0)),Indicator.Damage);
        });
        Test("249ms gap denied",()=>{var g=Gate();g.Record(Cue(1,0));return Static(g.Record(Cue(2,249)),Indicator.Damage);});
        Test("250ms gap permitted",()=>{var g=Gate();g.Record(Cue(1,0));return g.Record(Cue(2,250)).RenderMode==RenderMode.TimedEffect;});
        Test("third flash per 1s static",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));return Static(g.Record(Cue(3,500)),Indicator.Damage);
        });
        Test("denied flash doesn't consume flash budget",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,100));return g.FlashCountWindow==1;
        });
        Test("denied flash still consumes sequence",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,100));
            return Throws<ArgumentException>(()=>g.Record(Cue(2,200)));
        });
        Test("1s boundary expires old flash",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));
            return g.Record(Cue(3,1000)).RenderMode==RenderMode.TimedEffect&&g.FlashCountWindow==2;
        });
        Test("999ms old flash still occupies budget",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));
            return Static(g.Record(Cue(3,999)),Indicator.Damage);
        });
        Test("advance 1000ms purges first stamp",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));g.AdvanceTo(1000);
            return g.FlashCountWindow==1;
        });
        Test("advance 1250ms purges all stamps",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));g.AdvanceTo(1250);
            return g.FlashCountWindow==0;
        });
        Test("flash channel shared across damage and explosion",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250,VisualCueKind.ExplosionFlash));
            return Static(g.Record(Cue(3,500,VisualCueKind.CriticalFlash)),Indicator.Critical);
        });
        Test("critical flash denied still has critical static glyph",()=>{
            var g=Gate(ComfortMode.NoFlashes);
            return Static(g.Record(Cue(1,0,VisualCueKind.CriticalFlash)),Indicator.Critical);
        });
        Test("critical warning always static glyph",()=>{
            var g=Gate();
            return Static(g.Record(Cue(1,0,VisualCueKind.CriticalWarning)),Indicator.Critical);
        });
        Test("critical warning does not consume flash budget",()=>{
            var g=Gate();g.Record(Cue(1,0,VisualCueKind.CriticalWarning));
            return g.FlashCountWindow==0&&g.Record(Cue(2,0)).RenderMode==RenderMode.TimedEffect;
        });
        Test("critical warning remains visible when flashes disabled",()=>{
            var g=Gate(ComfortMode.NoFlashes);
            return Static(g.Record(Cue(1,0,VisualCueKind.CriticalWarning)),Indicator.Critical);
        });
        Test("reduced mode flash intensity capped 20",()=>{
            var g=Gate(ComfortMode.ReducedMotion);
            var d=g.Record(Cue(1,0));return d.Intensity==20&&d.RenderMode==RenderMode.ReducedEffect;
        });
        Test("reduced mode small intensity retained",()=>{
            var d=Gate(ComfortMode.ReducedMotion).Record(Cue(1,0,intensity:9));
            return d.Intensity==9&&d.RenderMode==RenderMode.ReducedEffect;
        });
        Test("reduced mode one flash per window",()=>{
            var g=Gate(ComfortMode.ReducedMotion);g.Record(Cue(1,0));
            return Static(g.Record(Cue(2,500)),Indicator.Damage);
        });
        Test("reduced mode 999ms still blocked",()=>{
            var g=Gate(ComfortMode.ReducedMotion);g.Record(Cue(1,0));
            return Static(g.Record(Cue(2,999)),Indicator.Damage);
        });
        Test("reduced mode 1000ms reenabled",()=>{
            var g=Gate(ComfortMode.ReducedMotion);g.Record(Cue(1,0));
            return g.Record(Cue(2,1000)).RenderMode==RenderMode.ReducedEffect;
        });
        Test("reduced mode camera shake becomes static",()=>{
            return Static(Gate(ComfortMode.ReducedMotion).Record(Cue(1,0,VisualCueKind.CollisionShake)),Indicator.Collision);
        });
        Test("reduced mode part shake becomes static",()=>{
            return Static(Gate(ComfortMode.ReducedMotion).Record(Cue(1,0,VisualCueKind.PartDetachShake)),Indicator.Detach);
        });
        Test("reduced mode symbolic pickup still static",()=>{
            return Static(Gate(ComfortMode.ReducedMotion).Record(Cue(1,0,VisualCueKind.PickupHighlight)),Indicator.Pickup);
        });
        Test("no flashes means all explosions static",()=>{
            return Static(Gate(ComfortMode.NoFlashes).Record(Cue(1,0,VisualCueKind.ExplosionFlash)),Indicator.Explosion);
        });
        Test("no flashes means all damage flashes static",()=>{
            return Static(Gate(ComfortMode.NoFlashes).Record(Cue(1,0)),Indicator.Damage);
        });
        Test("no flashes camera shake remains eligible",()=>{
            var d=Gate(ComfortMode.NoFlashes).Record(Cue(1,0,VisualCueKind.CollisionShake));
            return d.RenderMode==RenderMode.TimedEffect&&d.Intensity==45;
        });
        Test("zero flash setting yields static even standard",()=>{
            return Static(Gate(n:0).Record(Cue(1,0)),Indicator.Damage);
        });
        Test("one flash cap",()=>{
            var g=Gate(n:1);g.Record(Cue(1,0));
            return Static(g.Record(Cue(2,500)),Indicator.Damage);
        });
        Test("max four flash cap",()=>{
            var g=Gate(n:4,gap:0);for(int i=1;i<=4;i++)g.Record(Cue(i,i*100));
            return g.FlashCountWindow==4&&Static(g.Record(Cue(5,500)),Indicator.Damage);
        });
        Test("zero gap permits two at identical time",()=>{
            var g=Gate(gap:0);g.Record(Cue(1,0));
            return g.Record(Cue(2,0)).RenderMode==RenderMode.TimedEffect;
        });
        Test("custom gap at 800ms",()=>{
            var g=Gate(gap:800);g.Record(Cue(1,0));
            return Static(g.Record(Cue(2,799)),Indicator.Damage)&&
                   g.Record(Cue(3,800)).RenderMode==RenderMode.TimedEffect;
        });
        Test("mode update to no flashes takes immediate effect",()=>{
            var g=Gate();g.Record(Cue(1,0));g.SetMode(ComfortMode.NoFlashes);
            return Static(g.Record(Cue(2,300)),Indicator.Damage);
        });
        Test("mode update retains flash history on switch back",()=>{
            var g=Gate();g.Record(Cue(1,0));g.SetMode(ComfortMode.NoFlashes);
            g.Record(Cue(2,300));g.SetMode(ComfortMode.Standard);
            return g.FlashCountWindow==1&&g.Record(Cue(3,500)).RenderMode==RenderMode.TimedEffect;
        });
        Test("mode switch cannot bypass budget from standard to reduced",()=>{
            var g=Gate();g.Record(Cue(1,0));g.Record(Cue(2,250));
            g.SetMode(ComfortMode.ReducedMotion);
            return Static(g.Record(Cue(3,500)),Indicator.Damage);
        });
        Test("mode update reduced motion blocks live shake",()=>{
            var g=Gate();g.SetMode(ComfortMode.ReducedMotion);
            return Static(g.Record(Cue(1,0,VisualCueKind.PartDetachShake)),Indicator.Detach);
        });
        Test("invalid mode update rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate().SetMode((ComfortMode)99)));
        Test("invalid mode does not mutate configuration",()=>{
            var g=Gate();try{g.SetMode((ComfortMode)99);}catch(ArgumentOutOfRangeException){}
            return g.Mode==ComfortMode.Standard;
        });
        Test("standard shake intensity capped at 45",()=>{
            var d=Gate().Record(Cue(1,0,VisualCueKind.CollisionShake));return d.Intensity==45;
        });
        Test("standard low intensity shake unchanged",()=>{
            var d=Gate().Record(Cue(1,0,VisualCueKind.PartDetachShake,intensity:20));
            return d.Intensity==20&&d.RenderMode==RenderMode.TimedEffect;
        });
        Test("custom shake intensity capped",()=>{
            var d=Gate(shake:15).Record(Cue(1,0,VisualCueKind.CollisionShake));return d.Intensity==15;
        });
        Test("zero shake cap yields static",()=>{
            return Static(Gate(shake:0).Record(Cue(1,0,VisualCueKind.CollisionShake)),Indicator.Collision);
        });
        Test("shake does not spend flash quota",()=>{
            var g=Gate();g.Record(Cue(1,0,VisualCueKind.CollisionShake));
            return g.FlashCountWindow==0&&g.Record(Cue(2,0)).RenderMode==RenderMode.TimedEffect;
        });
        Test("static cue does not spend flash quota",()=>{
            var g=Gate();g.Record(Cue(1,0,VisualCueKind.PickupHighlight));
            return g.FlashCountWindow==0&&g.Record(Cue(2,0)).RenderMode==RenderMode.TimedEffect;
        });
        Test("all seven cues generate deterministic indicator",()=>{
            var expected=new[]{Indicator.Explosion,Indicator.Damage,Indicator.Critical,
                Indicator.Collision,Indicator.Detach,Indicator.Pickup,Indicator.Critical};
            for(int i=0;i<expected.Length;i++)
            {
                var g=Gate();var d=g.Record(Cue(1,0,(VisualCueKind)i));
                if(d.Indicator!=expected[i])return false;
            }return true;
        });
        Test("old run event ignored",()=>{
            var g=Gate();return g.Record(Cue(1,10,run:6))==null&&g.NowMs==0;
        });
        Test("old run ignored after new run",()=>{
            var g=Gate();g.StartNewRun(8);return g.Record(Cue(1,10,run:7))==null;
        });
        Test("old run ignored without sequence use",()=>{
            var g=Gate();g.Record(Cue(999,100,run:6));
            return g.Record(Cue(1,1)).Sequence==1;
        });
        Test("new run clears flash timestamps",()=>{
            var g=Gate();g.Record(Cue(1,0));g.StartNewRun(8);
            return g.FlashCountWindow==0&&g.RunId==8&&g.NowMs==0;
        });
        Test("new run resets sequence",()=>{
            var g=Gate();g.Record(Cue(99,0));g.StartNewRun(8);
            return g.Record(Cue(1,0,run:8)).Sequence==1;
        });
        Test("new run retains selected mode",()=>{
            var g=Gate(ComfortMode.NoFlashes);g.StartNewRun(8);
            return g.Mode==ComfortMode.NoFlashes&&Static(g.Record(Cue(1,0,run:8)),Indicator.Damage);
        });
        Test("run cannot stay same",()=>Throws<ArgumentOutOfRangeException>(()=>Gate().StartNewRun(7)));
        Test("run cannot go backwards",()=>Throws<ArgumentOutOfRangeException>(()=>Gate().StartNewRun(6)));
        Test("negative initial run rejected",()=>Throws<ArgumentOutOfRangeException>(()=>new VisualStimulusGate(-1)));
        Test("future run rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(1,0,run:8))));
        Test("zero sequence rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(0,0))));
        Test("negative sequence rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(-1,0))));
        Test("repeat sequence rejected",()=>{
            var g=Gate();g.Record(Cue(1,0));return Throws<ArgumentException>(()=>g.Record(Cue(1,250)));
        });
        Test("backward sequence rejected",()=>{
            var g=Gate();g.Record(Cue(4,0));return Throws<ArgumentException>(()=>g.Record(Cue(3,250)));
        });
        Test("backward timestamp rejected",()=>{
            var g=Gate();g.Record(Cue(1,250));return Throws<ArgumentException>(()=>g.Record(Cue(2,249)));
        });
        Test("negative timestamp rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(1,-1))));
        Test("negative intensity rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(1,0,intensity:-1))));
        Test("intensity above 100 rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(1,0,intensity:101))));
        Test("invalid effect kind rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Cue(1,0,(VisualCueKind)127))));
        Test("null input rejected",()=>Throws<ArgumentNullException>(()=>Gate().Record(null)));
        Test("invalid effect leaves previous timestamp unchanged",()=>{
            var g=Gate();g.Record(Cue(1,10));
            try{g.Record(Cue(2,20,intensity:101));}catch(ArgumentException){}
            return g.NowMs==10&&g.FlashCountWindow==1;
        });
        Test("invalid event doesn't advance sequence",()=>{
            var g=Gate();try{g.Record(Cue(1,20,intensity:101));}catch(ArgumentException){}
            return g.Record(Cue(1,20)).Sequence==1;
        });
        Test("advance time backwards rejected",()=>{
            var g=Gate();g.AdvanceTo(50);return Throws<ArgumentOutOfRangeException>(()=>g.AdvanceTo(49));
        });
        Test("advance exact current time accepted",()=>{
            var g=Gate();g.AdvanceTo(50);g.AdvanceTo(50);return g.NowMs==50;
        });
        Test("flash at long MaxValue safe",()=>{
            var g=Gate();var d=g.Record(Cue(1,long.MaxValue));
            return d.TimeMs==long.MaxValue&&g.FlashCountWindow==1;
        });
        Test("flash near long MaxValue has safe gap calculation",()=>{
            var g=Gate();g.Record(Cue(1,long.MaxValue-250));
            return g.Record(Cue(2,long.MaxValue)).RenderMode==RenderMode.TimedEffect;
        });
        Test("flash at max duration history expiry",()=>{
            var g=Gate();g.Record(Cue(1,long.MaxValue-1000));g.AdvanceTo(long.MaxValue);
            return g.FlashCountWindow==0;
        });
        Test("max intensity clamp same after reset",()=>{
            var g=Gate();g.StartNewRun(8);
            return g.Record(Cue(1,0,intensity:100,run:8)).Intensity==60;
        });
        Test("large repeated flashes never overflow queue",()=>{
            var g=Gate(n:4,gap:0);
            for(int i=1;i<=5000;i++)g.Record(Cue(i,i/4));
            return g.FlashCountWindow<=4;
        });
        Test("5000 suppressed flashes continue monotonic sequence",()=>{
            var g=Gate(n:0);
            for(int i=1;i<=5000;i++)g.Record(Cue(i,i));
            return g.Record(Cue(5001,5001)).Outcome==CueOutcome.ReplacedByStatic;
        });
        Test("disabled flashes still critical static and identified",()=>{
            var g=Gate(ComfortMode.NoFlashes);
            for(int i=1;i<=100;i++)g.Record(Cue(i,i,VisualCueKind.ExplosionFlash));
            return Static(g.Record(Cue(101,101,VisualCueKind.CriticalWarning)),Indicator.Critical);
        });
        Test("invalid constructor mode rejected",()=>Throws<ArgumentOutOfRangeException>(()=>
            new VisualStimulusGate(7,(ComfortMode)99)));
        Test("invalid constructor negative flash count rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(n:-1)));
        Test("invalid constructor excess flash count rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(n:5)));
        Test("invalid constructor negative gap rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(gap:-1)));
        Test("invalid constructor gap above 1000 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(gap:1001)));
        Test("invalid constructor shake below zero rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(shake:-1)));
        Test("invalid constructor shake above 100 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(shake:101)));
        Test("zero budget symbolic critical retained",()=>{
            var g=Gate(n:0,shake:0);
            return Static(g.Record(Cue(1,0,VisualCueKind.CriticalFlash)),Indicator.Critical);
        });
        Console.WriteLine("RESULT "+passed+" PASS, "+failed+" FAIL");
        if(failed!=0)Environment.Exit(1);
    }
}
