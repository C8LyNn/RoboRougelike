using System;
using B01.UXHaptic;

static class Program
{
    static int pass,fail;
    static void Check(string label,Func<bool> assertion)
    {
        try {if(!assertion())throw new Exception("false");pass++;Console.WriteLine("PASS "+label);}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+label+": "+e.GetType().Name+": "+e.Message);}
    }
    static bool Throws<T>(Action a) where T:Exception
    {try{a();return false;}catch(T){return true;}}
    static HapticGate Gate(bool enabled=true,int critical=80,int major=60,int minor=30)
    {return new HapticGate(7,enabled,critical,major,minor);}
    static HapticEvent Event(long seq,long time,HapticKind kind=HapticKind.WeaponImpact,
        int strength=100,HapticSide side=HapticSide.Center,int run=7)
    {return new HapticEvent{RunId=run,Sequence=seq,TimeMs=time,Kind=kind,Strength=strength,Side=side};}
    static bool Suppressed(HapticDecision d,HapticOutcome outcome,bool critical=false)
    {return d.Outcome==outcome&&d.LeftPercent==0&&d.RightPercent==0&&d.DurationMs==0&&
        !d.PreemptPrevious&&d.RequireVisibleCriticalAlert==critical;}
    static void Main()
    {
        Check("initial run",()=>Gate().RunId==7);
        Check("initial enabled",()=>Gate().Enabled);
        Check("initial disabled",()=>!Gate(false).Enabled);
        Check("initial clock",()=>Gate().NowMs==0);
        Check("initial active until",()=>Gate().ActiveUntilMs==0);
        Check("initial played sentinel",()=>Gate().LastPlayedMs==-1);
        Check("normal cooldown constant",()=>HapticGate.NormalCooldownMs==180);
        Check("critical maps tier",()=>HapticGate.TierOf(HapticKind.CriticalDamage)==HapticTier.Critical);
        Check("part detached maps major",()=>HapticGate.TierOf(HapticKind.PartDetached)==HapticTier.Major);
        Check("collision maps major",()=>HapticGate.TierOf(HapticKind.Collision)==HapticTier.Major);
        Check("weapon maps minor",()=>HapticGate.TierOf(HapticKind.WeaponImpact)==HapticTier.Minor);
        Check("pickup maps minor",()=>HapticGate.TierOf(HapticKind.Pickup)==HapticTier.Minor);
        Check("invalid enum tier rejected",()=>Throws<ArgumentOutOfRangeException>(()=>HapticGate.TierOf((HapticKind)99)));
        Check("normal impact plays",()=>Gate().Record(Event(1,0)).Outcome==HapticOutcome.Play);
        Check("normal impact capped 30",()=>Gate().Record(Event(1,0)).LeftPercent==30);
        Check("normal impact equal channels",()=>{
            var d=Gate().Record(Event(1,0));return d.LeftPercent==d.RightPercent;
        });
        Check("normal impact duration 80",()=>Gate().Record(Event(1,0)).DurationMs==80);
        Check("normal active until",()=>{var g=Gate();g.Record(Event(1,1));return g.ActiveUntilMs==81;});
        Check("normal last played zero valid",()=>{var g=Gate();g.Record(Event(1,0));return g.LastPlayedMs==0;});
        Check("normal source run preserved",()=>Gate().Record(Event(1,0)).RunId==7);
        Check("normal source sequence preserved",()=>Gate().Record(Event(50,0)).Sequence==50);
        Check("normal source time preserved",()=>Gate().Record(Event(1,55)).TimeMs==55);
        Check("normal source kind preserved",()=>Gate().Record(Event(1,0)).Kind==HapticKind.WeaponImpact);
        Check("normal source side preserved",()=>Gate().Record(Event(1,0,side:HapticSide.Right)).Side==HapticSide.Right);
        Check("normal tier preserved",()=>Gate().Record(Event(1,0)).Tier==HapticTier.Minor);
        Check("normal no critical visual flag",()=>!Gate().Record(Event(1,0)).RequireVisibleCriticalAlert);
        Check("major duration 160",()=>Gate().Record(Event(1,0,HapticKind.Collision)).DurationMs==160);
        Check("major capped 60",()=>Gate().Record(Event(1,0,HapticKind.PartDetached)).LeftPercent==60);
        Check("major tier",()=>Gate().Record(Event(1,0,HapticKind.Collision)).Tier==HapticTier.Major);
        Check("critical duration 300",()=>Gate().Record(Event(1,0,HapticKind.CriticalDamage)).DurationMs==300);
        Check("critical cap 80",()=>Gate().Record(Event(1,0,HapticKind.CriticalDamage)).LeftPercent==80);
        Check("critical visual required when playable",()=>Gate().Record(Event(1,0,HapticKind.CriticalDamage)).RequireVisibleCriticalAlert);
        Check("critical center channels equal",()=>{
            var d=Gate().Record(Event(1,0,HapticKind.CriticalDamage));return d.LeftPercent==d.RightPercent;
        });
        Check("left impact stronger left channel",()=>{
            var d=Gate().Record(Event(1,0,side:HapticSide.Left));return d.LeftPercent==30&&d.RightPercent==10;
        });
        Check("right impact stronger right channel",()=>{
            var d=Gate().Record(Event(1,0,side:HapticSide.Right));return d.LeftPercent==10&&d.RightPercent==30;
        });
        Check("left critical preserves critical visual",()=>{
            var d=Gate().Record(Event(1,0,HapticKind.CriticalDamage,side:HapticSide.Left));
            return d.LeftPercent==80&&d.RightPercent==26&&d.RequireVisibleCriticalAlert;
        });
        Check("strength below tier cap preserved",()=>{
            var d=Gate().Record(Event(1,0,strength:5));return d.LeftPercent==5&&d.RightPercent==5;
        });
        Check("custom minor cap",()=>Gate(minor:7).Record(Event(1,0)).LeftPercent==7);
        Check("custom major cap",()=>Gate(major:7).Record(Event(1,0,HapticKind.Collision)).LeftPercent==7);
        Check("custom critical cap",()=>Gate(critical:7).Record(Event(1,0,HapticKind.CriticalDamage)).LeftPercent==7);
        Check("disabled never plays",()=>Suppressed(Gate(false).Record(Event(1,0)),HapticOutcome.SuppressedDisabled));
        Check("disabled critical always visible",()=>Suppressed(Gate(false).Record(Event(1,0,HapticKind.CriticalDamage)),
            HapticOutcome.SuppressedDisabled,true));
        Check("disabled event consumes sequence",()=>{
            var g=Gate(false);g.Record(Event(1,0));return Throws<ArgumentException>(()=>g.Record(Event(1,1)));
        });
        Check("disabled event no cooldown used",()=>{
            var g=Gate(false);g.Record(Event(1,0));return g.LastPlayedMs==-1&&g.ActiveUntilMs==0;
        });
        Check("zero strength suppressed",()=>Suppressed(Gate().Record(Event(1,0,strength:0)),HapticOutcome.SuppressedZero));
        Check("zero critical keeps visual",()=>Suppressed(Gate().Record(Event(1,0,HapticKind.CriticalDamage,strength:0)),
            HapticOutcome.SuppressedZero,true));
        Check("zero strength doesn't delay real impulse",()=>{
            var g=Gate();g.Record(Event(1,0,strength:0));return g.Record(Event(2,0)).Outcome==HapticOutcome.Play;
        });
        Check("zero cap minor suppressed",()=>Suppressed(Gate(minor:0).Record(Event(1,0)),HapticOutcome.SuppressedZero));
        Check("zero cap major suppressed",()=>Suppressed(Gate(major:0).Record(Event(1,0,HapticKind.Collision)),HapticOutcome.SuppressedZero));
        Check("zero cap critical visible",()=>Suppressed(Gate(critical:0).Record(Event(1,0,HapticKind.CriticalDamage)),
            HapticOutcome.SuppressedZero,true));
        Check("busy minor suppresses another minor",()=>{
            var g=Gate();g.Record(Event(1,0));return Suppressed(g.Record(Event(2,30)),HapticOutcome.SuppressedBusy);
        });
        Check("busy major suppresses lower minor",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.PartDetached));
            return Suppressed(g.Record(Event(2,30)),HapticOutcome.SuppressedBusy);
        });
        Check("busy critical suppresses new major",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            return Suppressed(g.Record(Event(2,30,HapticKind.Collision)),HapticOutcome.SuppressedBusy);
        });
        Check("busy critical repeat still visual",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            return Suppressed(g.Record(Event(2,30,HapticKind.CriticalDamage)),HapticOutcome.SuppressedBusy,true);
        });
        Check("major preempts minor while busy",()=>{
            var g=Gate();g.Record(Event(1,0));
            var d=g.Record(Event(2,10,HapticKind.Collision));
            return d.Outcome==HapticOutcome.Play&&d.PreemptPrevious&&d.Tier==HapticTier.Major;
        });
        Check("critical preempts minor while busy",()=>{
            var g=Gate();g.Record(Event(1,0));
            var d=g.Record(Event(2,1,HapticKind.CriticalDamage));
            return d.Outcome==HapticOutcome.Play&&d.PreemptPrevious&&d.RequireVisibleCriticalAlert;
        });
        Check("critical preempts major while busy",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.Collision));
            var d=g.Record(Event(2,1,HapticKind.CriticalDamage));
            return d.Outcome==HapticOutcome.Play&&d.PreemptPrevious;
        });
        Check("same-major cannot preempt busy major",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.Collision));
            return Suppressed(g.Record(Event(2,1,HapticKind.PartDetached)),HapticOutcome.SuppressedBusy);
        });
        Check("minor busy exact 79ms",()=>{
            var g=Gate();g.Record(Event(1,0));return Suppressed(g.Record(Event(2,79)),HapticOutcome.SuppressedBusy);
        });
        Check("minor at end 80ms blocked cooldown",()=>{
            var g=Gate();g.Record(Event(1,0));
            return Suppressed(g.Record(Event(2,80)),HapticOutcome.SuppressedCooldown);
        });
        Check("minor at 179ms blocked cooldown",()=>{
            var g=Gate();g.Record(Event(1,0));
            return Suppressed(g.Record(Event(2,179)),HapticOutcome.SuppressedCooldown);
        });
        Check("minor at 180ms allowed",()=>{
            var g=Gate();g.Record(Event(1,0));return g.Record(Event(2,180)).Outcome==HapticOutcome.Play;
        });
        Check("major at 160ms cooldown blocked",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.Collision));
            return Suppressed(g.Record(Event(2,160,HapticKind.PartDetached)),HapticOutcome.SuppressedCooldown);
        });
        Check("major at 180ms allowed",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.Collision));
            return g.Record(Event(2,180,HapticKind.PartDetached)).Outcome==HapticOutcome.Play;
        });
        Check("critical at 300ms immediately allowed despite cooldown",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            return g.Record(Event(2,300,HapticKind.CriticalDamage)).Outcome==HapticOutcome.Play;
        });
        Check("major preempt updates active until",()=>{
            var g=Gate();g.Record(Event(1,0));g.Record(Event(2,10,HapticKind.Collision));
            return g.ActiveUntilMs==170;
        });
        Check("critical preempt updates active until",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.Collision));g.Record(Event(2,10,HapticKind.CriticalDamage));
            return g.ActiveUntilMs==310;
        });
        Check("disabled while active signals stop motors",()=>{
            var g=Gate();g.Record(Event(1,0));return g.SetEnabled(false)&&!g.Enabled&&g.ActiveUntilMs==0;
        });
        Check("disabled when idle does not signal stop",()=>!Gate().SetEnabled(false));
        Check("repeated disabling no duplicate stop",()=>{
            var g=Gate();g.Record(Event(1,0));return g.SetEnabled(false)&&!g.SetEnabled(false);
        });
        Check("enable from off signals no stop",()=>!Gate(false).SetEnabled(true));
        Check("mode toggle preserves played cooldown",()=>{
            var g=Gate();g.Record(Event(1,0));g.SetEnabled(false);g.SetEnabled(true);
            return Suppressed(g.Record(Event(2,1)),HapticOutcome.SuppressedCooldown);
        });
        Check("disabled critical still visible after active preempt",()=>{
            var g=Gate();g.Record(Event(1,0));g.SetEnabled(false);
            return Suppressed(g.Record(Event(2,1,HapticKind.CriticalDamage)),HapticOutcome.SuppressedDisabled,true);
        });
        Check("no haptic feedback means visual flag for critical",()=>{
            var g=Gate(false);return g.Record(Event(1,0,HapticKind.CriticalDamage)).RequireVisibleCriticalAlert;
        });
        Check("advance exact current accepted",()=>{
            var g=Gate();g.AdvanceTo(50);g.AdvanceTo(50);return g.NowMs==50;
        });
        Check("advance backwards rejected",()=>{
            var g=Gate();g.AdvanceTo(50);return Throws<ArgumentOutOfRangeException>(()=>g.AdvanceTo(49));
        });
        Check("no stale timestamp can enter gate",()=>{
            var g=Gate();g.Record(Event(1,100));return Throws<ArgumentException>(()=>g.Record(Event(2,99)));
        });
        Check("new run clears active pulse",()=>{
            var g=Gate();g.Record(Event(1,0));g.StartNewRun(8);
            return g.ActiveUntilMs==0&&g.LastPlayedMs==-1&&g.NowMs==0;
        });
        Check("new run retains enabled configuration",()=>{
            var g=Gate(false);g.StartNewRun(8);return !g.Enabled&&g.RunId==8;
        });
        Check("new run resets sequence",()=>{
            var g=Gate();g.Record(Event(100,0));g.StartNewRun(8);
            return g.Record(Event(1,0,run:8)).Outcome==HapticOutcome.Play;
        });
        Check("new run ID must increase",()=>Throws<ArgumentOutOfRangeException>(()=>Gate().StartNewRun(7)));
        Check("new run ID cannot decrease",()=>Throws<ArgumentOutOfRangeException>(()=>Gate().StartNewRun(6)));
        Check("new run at maximum id then no extra id",()=>{
            var g=Gate();g.StartNewRun(int.MaxValue);
            return Throws<ArgumentOutOfRangeException>(()=>g.StartNewRun(int.MaxValue));
        });
        Check("old run event ignored",()=>{
            var g=Gate();var d=g.Record(Event(1,10,run:6));
            return d.Outcome==HapticOutcome.IgnoredOldRun&&g.NowMs==0;
        });
        Check("old run input no counter mutation",()=>{
            var g=Gate();g.Record(Event(999,100,run:6));
            return g.Record(Event(1,0)).Outcome==HapticOutcome.Play;
        });
        Check("future run rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,0,run:8))));
        Check("zero sequence rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(0,0))));
        Check("negative sequence rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(-1,0))));
        Check("repeated sequence rejected",()=>{
            var g=Gate();g.Record(Event(1,0));return Throws<ArgumentException>(()=>g.Record(Event(1,100)));
        });
        Check("replayed suppressed busy cue rejected",()=>{
            var g=Gate();g.Record(Event(1,0));g.Record(Event(2,10));
            return Throws<ArgumentException>(()=>g.Record(Event(2,190)));
        });
        Check("descending sequence rejected",()=>{
            var g=Gate();g.Record(Event(5,0));return Throws<ArgumentException>(()=>g.Record(Event(4,100)));
        });
        Check("negative time rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,-1))));
        Check("negative strength rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,0,strength:-1))));
        Check("strength >100 rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,0,strength:101))));
        Check("invalid kind rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,0,(HapticKind)99))));
        Check("invalid side rejected",()=>Throws<ArgumentException>(()=>Gate().Record(Event(1,0,side:(HapticSide)99))));
        Check("null cue rejected",()=>Throws<ArgumentNullException>(()=>Gate().Record(null)));
        Check("invalid cue doesn't consume sequence",()=>{
            var g=Gate();try{g.Record(Event(1,10,strength:101));}catch(ArgumentException){}
            return g.Record(Event(1,10)).Outcome==HapticOutcome.Play;
        });
        Check("invalid cue doesn't change current clock",()=>{
            var g=Gate();try{g.Record(Event(1,10,strength:101));}catch(ArgumentException){}
            return g.NowMs==0&&g.ActiveUntilMs==0;
        });
        Check("negative run ID rejected",()=>Throws<ArgumentOutOfRangeException>(()=>new HapticGate(-1)));
        Check("invalid critical cap rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(critical:101)));
        Check("invalid major cap rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(major:-1)));
        Check("invalid minor cap rejected",()=>Throws<ArgumentOutOfRangeException>(()=>Gate(minor:101)));
        Check("zero cap is permitted",()=>Gate(minor:0).Record(Event(1,0)).Outcome==HapticOutcome.SuppressedZero);
        Check("at long.MaxValue no duration overflow",()=>{
            var g=Gate();var d=g.Record(Event(1,long.MaxValue,HapticKind.CriticalDamage));
            return d.Outcome==HapticOutcome.Play&&g.ActiveUntilMs==long.MaxValue;
        });
        Check("near long.MaxValue retains busy semantics",()=>{
            var g=Gate();g.Record(Event(1,long.MaxValue-2,HapticKind.CriticalDamage));
            return Suppressed(g.Record(Event(2,long.MaxValue-1,HapticKind.CriticalDamage)),
                HapticOutcome.SuppressedBusy,true);
        });
        Check("near long.MaxValue cooldown arithmetic safe",()=>{
            var g=Gate();g.Record(Event(1,long.MaxValue-100,HapticKind.WeaponImpact));
            return Suppressed(g.Record(Event(2,long.MaxValue)),HapticOutcome.SuppressedCooldown);
        });
        Check("1000 pickup events never overdrive motors",()=>{
            var g=Gate();int played=0;
            for(int i=1;i<=1000;i++)if(g.Record(Event(i,i-1,HapticKind.Pickup)).Outcome==HapticOutcome.Play)played++;
            return played<=6;
        });
        Check("1000 mixed minor events critical can preempt anytime",()=>{
            var g=Gate();for(int i=1;i<=1000;i++)g.Record(Event(i,i-1));
            return g.Record(Event(1001,1000,HapticKind.CriticalDamage)).Outcome==HapticOutcome.Play;
        });
        Check("suppressed major does not lose semantic kind",()=>{
            var g=Gate(false);var d=g.Record(Event(1,0,HapticKind.PartDetached));
            return d.Kind==HapticKind.PartDetached&&d.Tier==HapticTier.Major;
        });
        Check("critical visual obligation despite busy channel",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            return g.Record(Event(2,1,HapticKind.CriticalDamage)).RequireVisibleCriticalAlert;
        });
        Check("W16 stale critical from prior run never requests a false visual alert",()=>{
            var g=Gate();var d=g.Record(Event(1,10,HapticKind.CriticalDamage,run:6));
            return d.Outcome==HapticOutcome.IgnoredOldRun&&!d.RequireVisibleCriticalAlert;
        });
        Check("W16 stale critical after reset never requests a false visual alert",()=>{
            var g=Gate();g.StartNewRun(8);
            var d=g.Record(Event(900,100,HapticKind.CriticalDamage,run:7));
            return d.Outcome==HapticOutcome.IgnoredOldRun&&!d.RequireVisibleCriticalAlert;
        });
        Check("W16 current critical still requires visual if haptics disabled",()=>{
            var g=Gate(false);var d=g.Record(Event(1,10,HapticKind.CriticalDamage));
            return d.Outcome==HapticOutcome.SuppressedDisabled&&d.RequireVisibleCriticalAlert;
        });

        // W17 RED: source-only new-run boundary must explicitly signal motor STOP.
        Check("W17 StartNewRun exposes motor-stop bool result contract",()=>
            typeof(HapticGate).GetMethod("StartNewRun").ReturnType==typeof(bool));
        Check("W17 active minor pulse demands motor-stop on new run",()=>{
            var g=Gate();g.Record(Event(1,0));var wasActive=g.ActiveUntilMs>g.NowMs;
            var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return wasActive&&ret is bool stop&&stop;
        });
        Check("W17 active critical pulse demands motor-stop on new run",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&stop;
        });
        Check("W17 no previous impulse does not request motor stop",()=>{
            var g=Gate();var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&!stop;
        });
        Check("W17 expired previous impulse does not request stop",()=>{
            var g=Gate();g.Record(Event(1,0));g.AdvanceTo(80);
            var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&!stop;
        });
        Check("W17 zero-strength suppressed pulse does not request stop",()=>{
            var g=Gate();g.Record(Event(1,0,strength:0));
            var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&!stop;
        });
        Check("W17 disabled gate does not request stop after new run",()=>{
            var g=Gate(false);var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&!stop;
        });
        Check("W17 after explicit motor stop old run is cleanly reset",()=>{
            var g=Gate();g.Record(Event(1,0));var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&stop&&g.ActiveUntilMs==0&&g.LastPlayedMs==-1&&g.NowMs==0&&
                   g.RunId==8&&g.Record(Event(1,0,run:8)).Outcome==HapticOutcome.Play;
        });
        Check("W17 active major pulse stop signal must be emitted at boundary",()=>{
            var g=Gate();g.Record(Event(1,10,HapticKind.PartDetached));g.AdvanceTo(150);
            var ret=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            return ret is bool stop&&stop;
        });
        Check("W17 previous physical pulse cannot re-trigger stop after boundary",()=>{
            var g=Gate();g.Record(Event(1,0));
            var first=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{8});
            var second=typeof(HapticGate).GetMethod("StartNewRun").Invoke(g,new object[]{9});
            return first is bool s1&&s1&&second is bool s2&&!s2;
        });
        Check("W17 invalid run ID leaves prior active motor boundary intact",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            bool rejected=Throws<ArgumentOutOfRangeException>(()=>g.StartNewRun(7));
            return rejected&&g.ActiveUntilMs==300&&g.RunId==7;
        });
        Check("W17 disconnect via SetEnabled false remains explicit motor stop",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            return g.SetEnabled(false)&&g.ActiveUntilMs==0;
        });

        // W18 RED: per-run pause must suppress hardware intents without muting preferences.
        Check("W18 pause API exists and returns motor-stop bool",()=>{
            var m=typeof(HapticGate).GetMethod("SetPaused");
            return m!=null&&m.ReturnType==typeof(bool);
        });
        Check("W18 initial pause property is false",()=>{
            var p=typeof(HapticGate).GetProperty("Paused");
            return p!=null&&p.GetValue(Gate()) is bool x&&!x;
        });
        Check("W18 pause active minor requests motor stop",()=>{
            var g=Gate();g.Record(Event(1,0));
            var m=typeof(HapticGate).GetMethod("SetPaused");
            return m!=null&&m.Invoke(g,new object[]{true}) is bool s&&s&&g.ActiveUntilMs==0;
        });
        Check("W18 pause active critical requests motor stop",()=>{
            var g=Gate();g.Record(Event(1,0,HapticKind.CriticalDamage));
            var m=typeof(HapticGate).GetMethod("SetPaused");
            return m!=null&&m.Invoke(g,new object[]{true}) is bool s&&s;
        });
        Check("W18 pause while idle never requests physical stop",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            return m!=null&&m.Invoke(g,new object[]{true}) is bool s&&!s;
        });
        Check("W18 repeated pause does not request double stop",()=>{
            var g=Gate();g.Record(Event(1,0));var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            var a=m.Invoke(g,new object[]{true});var b=m.Invoke(g,new object[]{true});
            return a is bool s1&&s1&&b is bool s2&&!s2;
        });
        Check("W18 resume does not stop hardware again",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            return m.Invoke(g,new object[]{false}) is bool x&&!x;
        });
        Check("W18 pause doesn't change selected enabled preference",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            return m!=null&&m.Invoke(g,new object[]{true}) is bool&&g.Enabled;
        });
        Check("W18 paused event returns typed SuppressedPaused",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            var d=g.Record(Event(1,0));
            return Enum.TryParse<HapticOutcome>("SuppressedPaused",out var v)&&
                   d.Outcome==v&&d.LeftPercent==0&&d.RightPercent==0&&d.DurationMs==0;
        });
        Check("W18 paused critical still requires static visual alert",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            var d=g.Record(Event(1,0,HapticKind.CriticalDamage));
            return Enum.TryParse<HapticOutcome>("SuppressedPaused",out var v)&&
                   d.Outcome==v&&d.RequireVisibleCriticalAlert;
        });
        Check("W18 paused events consume sequence and cannot replay",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});g.Record(Event(1,0));
            return Throws<ArgumentException>(()=>g.Record(Event(1,1)));
        });
        Check("W18 pause retains pre-pause cooldown after resume",()=>{
            var g=Gate();g.Record(Event(1,0));var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            g.AdvanceTo(1);m.Invoke(g,new object[]{false});
            return g.Record(Event(2,1)).Outcome==HapticOutcome.SuppressedCooldown;
        });
        Check("W18 paused event never starts new pulse after prior stopped",()=>{
            var g=Gate();g.Record(Event(1,0));var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});g.Record(Event(2,10,HapticKind.CriticalDamage));
            return g.ActiveUntilMs<=g.NowMs&&g.LastPlayedMs==0;
        });
        Check("W18 post pause resume allows future new haptic pulse",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});g.Record(Event(1,0));
            m.Invoke(g,new object[]{false});
            return g.Record(Event(2,0)).Outcome==HapticOutcome.Play;
        });
        Check("W18 new run clears transient pause without modifying enabled",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            var p=typeof(HapticGate).GetProperty("Paused");
            if(m==null||p==null)return false;
            m.Invoke(g,new object[]{true});g.StartNewRun(8);
            return p.GetValue(g) is bool paused&&!paused&&g.Enabled&&
                   g.Record(Event(1,0,run:8)).Outcome==HapticOutcome.Play;
        });
        Check("W18 disabling while paused does not request duplicate motor stop",()=>{
            var g=Gate();g.Record(Event(1,0));var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});return !g.SetEnabled(false)&&!g.Enabled;
        });
        Check("W18 invalid source cue still rejected while paused",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            return Throws<ArgumentException>(()=>g.Record(Event(1,-1)));
        });
        Check("W18 stale old-run critical ignored while paused",()=>{
            var g=Gate();var m=typeof(HapticGate).GetMethod("SetPaused");
            if(m==null)return false;
            m.Invoke(g,new object[]{true});
            var d=g.Record(Event(1,10,HapticKind.CriticalDamage,run:6));
            return d.Outcome==HapticOutcome.IgnoredOldRun&&!d.RequireVisibleCriticalAlert;
        });
        Console.WriteLine("RESULT "+pass+" PASS, "+fail+" FAIL");
        if(fail!=0)Environment.Exit(1);
    }
}
