using System;
using B01.UXCaption;

static class Program
{
    static int pass,fail;
    static void Check(string name,Func<bool> assertion)
    {
        try { if(!assertion())throw new Exception("false"); Console.WriteLine("PASS "+name);pass++; }
        catch(Exception e) {Console.WriteLine("FAIL "+name+": "+e.GetType().Name+" "+e.Message);fail++;}
    }
    static bool Throws<T>(Action a) where T:Exception
    { try {a();return false;}catch(T){return true;} }
    static CaptionTrack Track(int cap=6,int slots=4){return new CaptionTrack(7,cap,slots);}
    static SoundCueInput Cue(long seq,long time,SoundCueKind kind=SoundCueKind.Footsteps,
       string key="cue.steps",string source="enemy1",int bearing=0,int run=7)
    {return new SoundCueInput{RunId=run,Sequence=seq,TimeMs=time,Kind=kind,
        LocalizationKey=key,SourceId=source,BearingDegrees=bearing};}
    static void Main()
    {
        var t=Track();
        Check("new track empty",()=>t.ActiveCount==0&&t.Visible().Length==0);
        Check("initial run",()=>t.RunId==7);
        Check("initial clock",()=>t.NowMs==0);
        Check("new cue accepted",()=>t.Record(Cue(1,10))==CaptionOutcome.Accepted);
        Check("active cue exists",()=>t.ActiveCount==1);
        Check("cue direction front",()=>t.Visible()[0].Direction==CaptionDirection.Front);
        Check("cue key intact",()=>t.Visible()[0].LocalizationKey=="cue.steps");
        Check("cue source intact",()=>t.Visible()[0].SourceId=="enemy1");
        Check("cue sequence intact",()=>t.Visible()[0].FirstSequence==1);
        Check("cue time intact",()=>t.Visible()[0].LastTimeMs==10&&t.NowMs==10);
        Check("cue initial occurrence one",()=>t.Visible()[0].Occurrences==1);
        Check("footsteps normal",()=>CaptionTrack.PriorityOf(SoundCueKind.Footsteps)==CaptionPriority.Normal);
        Check("weapon fire normal",()=>CaptionTrack.PriorityOf(SoundCueKind.WeaponFire)==CaptionPriority.Normal);
        Check("impact normal",()=>CaptionTrack.PriorityOf(SoundCueKind.HeavyImpact)==CaptionPriority.Normal);
        Check("part detached urgent",()=>CaptionTrack.PriorityOf(SoundCueKind.PartDetached)==CaptionPriority.Urgent);
        Check("critical warning urgent",()=>CaptionTrack.PriorityOf(SoundCueKind.CriticalWarning)==CaptionPriority.Urgent);
        Check("pickup normal",()=>CaptionTrack.PriorityOf(SoundCueKind.Pickup)==CaptionPriority.Normal);
        Check("machinery normal",()=>CaptionTrack.PriorityOf(SoundCueKind.Machinery)==CaptionPriority.Normal);
        Check("invalid enum priority throws",()=>Throws<ArgumentOutOfRangeException>(()=>CaptionTrack.PriorityOf((SoundCueKind)99)));
        var angles=new[]{
            new {A=0,D=CaptionDirection.Front},
            new {A=22,D=CaptionDirection.Front},
            new {A=23,D=CaptionDirection.FrontRight},
            new {A=45,D=CaptionDirection.FrontRight},
            new {A=67,D=CaptionDirection.FrontRight},
            new {A=68,D=CaptionDirection.Right},
            new {A=90,D=CaptionDirection.Right},
            new {A=112,D=CaptionDirection.Right},
            new {A=113,D=CaptionDirection.BackRight},
            new {A=135,D=CaptionDirection.BackRight},
            new {A=157,D=CaptionDirection.BackRight},
            new {A=158,D=CaptionDirection.Back},
            new {A=180,D=CaptionDirection.Back},
            new {A=-180,D=CaptionDirection.Back},
            new {A=-158,D=CaptionDirection.Back},
            new {A=-157,D=CaptionDirection.BackLeft},
            new {A=-135,D=CaptionDirection.BackLeft},
            new {A=-113,D=CaptionDirection.BackLeft},
            new {A=-112,D=CaptionDirection.Left},
            new {A=-90,D=CaptionDirection.Left},
            new {A=-68,D=CaptionDirection.Left},
            new {A=-67,D=CaptionDirection.FrontLeft},
            new {A=-45,D=CaptionDirection.FrontLeft},
            new {A=-23,D=CaptionDirection.FrontLeft},
            new {A=-22,D=CaptionDirection.Front},
            new {A=-1,D=CaptionDirection.Front},
            new {A=1,D=CaptionDirection.Front}
        };
        foreach(var a in angles)
        {int x=a.A;var direction=a.D;Check("sector "+x,()=>CaptionTrack.DirectionOf(x)==direction);}
        Check("bearing below -180 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>CaptionTrack.DirectionOf(-181)));
        Check("bearing above 180 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>CaptionTrack.DirectionOf(181)));

        Check("combine same source within 450ms",()=>{
            var f=Track();f.Record(Cue(1,10));return f.Record(Cue(2,460))==CaptionOutcome.Combined&&
                f.Visible()[0].Occurrences==2&&f.ActiveCount==1;
        });
        Check("combine retains first sequence and time",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,460));
            return f.Visible()[0].FirstSequence==1&&f.Visible()[0].LastSequence==2&&
                   f.Visible()[0].FirstTimeMs==10&&f.Visible()[0].LastTimeMs==460;
        });
        Check("combine outside 450 is distinct",()=>{
            var f=Track();f.Record(Cue(1,10));return f.Record(Cue(2,461))==CaptionOutcome.Accepted&&f.ActiveCount==2;
        });
        Check("coalesce based on last time",()=>{
            var f=Track();f.Record(Cue(1,1));f.Record(Cue(2,451));
            f.Record(Cue(3,901));return f.Visible()[0].Occurrences==3;
        });
        Check("different source stays distinct",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,20,source:"enemy2"));return f.ActiveCount==2;
        });
        Check("different caption key stays distinct",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,20,key:"cue.run"));return f.ActiveCount==2;
        });
        Check("different kind stays distinct",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,20,kind:SoundCueKind.Machinery));return f.ActiveCount==2;
        });
        Check("different bearing sector stays distinct",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,20,bearing:90));return f.ActiveCount==2;
        });
        Check("same sector minor bearing variation coalesces",()=>{
            var f=Track();f.Record(Cue(1,10,bearing:0));return f.Record(Cue(2,20,bearing:20))==CaptionOutcome.Combined;
        });
        Check("coalescence does not mutate prior exposed snapshot",()=>{
            var f=Track();f.Record(Cue(1,10));var old=f.Visible()[0];f.Record(Cue(2,20));
            return old.Occurrences==1&&f.Visible()[0].Occurrences==2&&!object.ReferenceEquals(old,f.Visible()[0]);
        });
        Check("combined cue survives until last time TTL",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,460));f.AdvanceTo(2859);
            return f.Visible().Length==1;
        });
        Check("combined cue expires at exact TTL",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,460));f.AdvanceTo(2860);
            return f.Visible().Length==0&&f.ActiveCount==0;
        });
        Check("normal exact TTL-1 visible",()=>{
            var f=Track();f.Record(Cue(1,10));f.AdvanceTo(2409);return f.Visible().Length==1;
        });
        Check("normal exact TTL expired",()=>{
            var f=Track();f.Record(Cue(1,10));f.AdvanceTo(2410);return f.Visible().Length==0;
        });
        Check("urgent exact TTL-1 visible",()=>{
            var f=Track();f.Record(Cue(1,10,SoundCueKind.CriticalWarning));f.AdvanceTo(6009);
            return f.Visible().Length==1;
        });
        Check("urgent exact TTL expired",()=>{
            var f=Track();f.Record(Cue(1,10,SoundCueKind.CriticalWarning));f.AdvanceTo(6010);
            return f.Visible().Length==0&&f.ActiveCount==0;
        });
        Check("priority urgent ahead of newer normal",()=>{
            var f=Track();f.Record(Cue(1,10,SoundCueKind.PartDetached));f.Record(Cue(2,20,SoundCueKind.Machinery));
            return f.Visible()[0].Priority==CaptionPriority.Urgent&&f.Visible()[0].FirstSequence==1;
        });
        Check("urgent-only projection excludes normal",()=>{
            var f=Track();f.Record(Cue(1,10));f.Record(Cue(2,20,SoundCueKind.PartDetached));
            return f.Visible(true).Length==1&&f.Visible(true)[0].Priority==CaptionPriority.Urgent;
        });
        Check("visible slots respected",()=>{
            var f=Track(6,2);for(int i=1;i<=6;i++)f.Record(Cue(i,i,source:"enemy"+i));
            return f.Visible().Length==2&&f.ActiveCount==6;
        });
        Check("visible sorted recent among normals",()=>{
            var f=Track(6,3);for(int i=1;i<=4;i++)f.Record(Cue(i,i,source:"enemy"+i));
            return f.Visible()[0].FirstSequence==4&&f.Visible()[1].FirstSequence==3;
        });
        Check("urgent not evicted by normal flood",()=>{
            var f=Track(3,2);f.Record(Cue(1,1,SoundCueKind.PartDetached));
            for(int i=2;i<=40;i++)f.Record(Cue(i,i,SoundCueKind.Machinery,source:"machine"+i));
            return f.ActiveCount==3&&f.Visible(true).Length==1&&f.Visible(true)[0].FirstSequence==1;
        });
        Check("all urgent slots block incoming normal",()=>{
            var f=Track(2,2);f.Record(Cue(1,10,SoundCueKind.PartDetached));
            f.Record(Cue(2,20,SoundCueKind.CriticalWarning));
            return f.Record(Cue(3,30,SoundCueKind.Pickup))==CaptionOutcome.DroppedNormalForUrgent&&
                   f.ActiveCount==2&&f.Visible(true).Length==2;
        });
        Check("dropped sequence cannot be replayed",()=>{
            var f=Track(1,1);f.Record(Cue(1,10,SoundCueKind.PartDetached));
            f.Record(Cue(2,20,SoundCueKind.Pickup));
            return Throws<ArgumentException>(()=>f.Record(Cue(2,30,SoundCueKind.Pickup)));
        });
        Check("urgent replaces oldest normal on full",()=>{
            var f=Track(2,2);f.Record(Cue(1,10));f.Record(Cue(2,20,source:"enemy2"));
            f.Record(Cue(3,30,SoundCueKind.PartDetached));
            return f.ActiveCount==2&&f.Visible(true).Length==1&&f.Visible()[1].FirstSequence==2;
        });
        Check("when all urgent new urgent replaces oldest urgent",()=>{
            var f=Track(2,2);f.Record(Cue(1,10,SoundCueKind.PartDetached));
            f.Record(Cue(2,20,SoundCueKind.CriticalWarning));
            f.Record(Cue(3,30,SoundCueKind.PartDetached,source:"other"));
            return f.ActiveCount==2&&f.Visible()[0].FirstSequence==3&&
                   f.Visible()[1].FirstSequence==2;
        });
        Check("normal capacity evicts oldest normal",()=>{
            var f=Track(2,2);f.Record(Cue(1,10));f.Record(Cue(2,20,source:"two"));
            f.Record(Cue(3,30,source:"three"));
            return f.Visible()[0].FirstSequence==3&&f.Visible()[1].FirstSequence==2;
        });
        Check("expired urgent no longer blocks normal",()=>{
            var f=Track(1,1);f.Record(Cue(1,10,SoundCueKind.PartDetached));f.AdvanceTo(6010);
            return f.Record(Cue(2,6011))==CaptionOutcome.Accepted&&f.Visible().Length==1;
        });
        Check("visible return arrays independent",()=>{
            var f=Track();f.Record(Cue(1,10));var a=f.Visible();a[0]=null;
            return f.Visible().Length==1&&f.Visible()[0]!=null;
        });
        Check("previous run input ignored",()=>{
            var f=Track();return f.Record(Cue(1,10,run:6))==CaptionOutcome.IgnoredPreviousRun&&f.NowMs==0;
        });
        Check("previous run input ignored after reset",()=>{
            var f=Track();f.StartNewRun(8);return f.Record(Cue(99,100,run:7))==CaptionOutcome.IgnoredPreviousRun;
        });
        Check("future run input rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,run:8))));
        Check("new run flushes live captions",()=>{
            var f=Track();f.Record(Cue(1,10,SoundCueKind.CriticalWarning));f.StartNewRun(8);
            return f.ActiveCount==0&&f.Visible().Length==0&&f.NowMs==0&&f.RunId==8;
        });
        Check("new run sequence resets",()=>{
            var f=Track();f.Record(Cue(100,10));f.StartNewRun(8);
            return f.Record(Cue(1,1,run:8))==CaptionOutcome.Accepted;
        });
        Check("new run ID must increase",()=>Throws<ArgumentOutOfRangeException>(()=>Track().StartNewRun(7)));
        Check("negative run ID rejected",()=>Throws<ArgumentOutOfRangeException>(()=>new CaptionTrack(-1)));
        Check("zero capacity rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Track(0,1)));
        Check("capacity >32 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Track(33,1)));
        Check("zero slots rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Track(3,0)));
        Check("more slots than capacity rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Track(2,3)));
        Check("slots above 6 rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Track(7,7)));
        Check("null input rejected",()=>Throws<ArgumentNullException>(()=>Track().Record(null)));
        Check("zero initial sequence rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(0,10))));
        Check("same sequence rejected",()=>{
            var f=Track();f.Record(Cue(1,10));
            return Throws<ArgumentException>(()=>f.Record(Cue(1,20)));
        });
        Check("descending sequence rejected",()=>{
            var f=Track();f.Record(Cue(5,10));
            return Throws<ArgumentException>(()=>f.Record(Cue(4,20)));
        });
        Check("negative event time rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,-1))));
        Check("event time moving backwards rejected",()=>{
            var f=Track();f.Record(Cue(1,10));return Throws<ArgumentException>(()=>f.Record(Cue(2,9)));
        });
        Check("advance backwards rejected",()=>{
            var f=Track();f.AdvanceTo(10);return Throws<ArgumentOutOfRangeException>(()=>f.AdvanceTo(9));
        });
        Check("unsupported kind rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,(SoundCueKind)99))));
        Check("invalid bearing -181 rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,bearing:-181))));
        Check("invalid bearing 181 rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,bearing:181))));
        Check("empty source rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,source:""))));
        Check("source too long rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,source:new string('a',41)))));
        Check("source newline rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,source:"ab\ncd"))));
        Check("source unicode rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,source:"zażółć"))));
        Check("empty localization key rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:""))));
        Check("unprefixed key rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:"steps"))));
        Check("key too long rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:"cue."+new string('a',61)))));
        Check("localization key newline rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:"cue.steps\nhidden"))));
        Check("localization key uppercase rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:"cue.ABC"))));
        Check("localization key with space rejected",()=>Throws<ArgumentException>(()=>Track().Record(Cue(1,10,key:"cue.weapon fire"))));
        Check("valid localization key with underscore accepted",()=>Track().Record(Cue(1,10,key:"cue.gun_fire"))==CaptionOutcome.Accepted);
        Check("valid source colon accepted",()=>Track().Record(Cue(1,10,source:"bot:front"))==CaptionOutcome.Accepted);
        Check("same timestamp distinct sequence accepted",()=>{
            var f=Track();f.Record(Cue(1,10));return f.Record(Cue(2,10,source:"enemy2"))==CaptionOutcome.Accepted;
        });
        Check("max timestamp no overflow",()=>{
            var f=Track();f.Record(Cue(1,long.MaxValue,SoundCueKind.CriticalWarning));
            return f.NowMs==long.MaxValue&&f.Visible().Length==1;
        });
        Check("near-max timestamp difference safe",()=>{
            var f=Track();f.Record(Cue(1,long.MaxValue-2,SoundCueKind.CriticalWarning));
            f.AdvanceTo(long.MaxValue);return f.Visible().Length==1;
        });
        Check("expired captions physically pruned",()=>{
            var f=Track();f.Record(Cue(1,10));f.AdvanceTo(2410);
            return f.ActiveCount==0&&f.Visible().Length==0;
        });
        Check("urgent-only projection keeps queue intact",()=>{
            var f=Track();f.Record(Cue(1,10));f.Visible(true);
            return f.ActiveCount==1&&f.Visible(false).Length==1;
        });
        Console.WriteLine("RESULT "+pass+" PASS, "+fail+" FAIL");
        if(fail!=0)Environment.Exit(1);
    }
}
