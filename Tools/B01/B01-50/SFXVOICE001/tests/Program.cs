using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.Performance.OptionalAudio;

internal static class Program
{
    private static int pass, fail;
    private static readonly List<string> errors = new List<string>();
    private static void True(bool v, string m) { if (!v) throw new Exception(m); }
    private static void Throws<T>(Action fn) where T : Exception
    {
        try { fn(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void Check(string label, Action action)
    {
        try { action(); pass++; Console.WriteLine("PASS " + label); }
        catch(Exception e) { fail++; errors.Add(label + ": " + e.Message); Console.Error.WriteLine("FAIL " + label + ": " + e); }
    }
    private static OptionalSfxVoiceBudget New(int max=2, double distance=40, double cooldown=0)
        => new OptionalSfxVoiceBudget(max, distance, cooldown);
    private static VoiceAllocation A(OptionalSfxVoiceBudget b, string key, double now=0,
        double duration=500, double distance=2, OptionalSfxTier tier=OptionalSfxTier.Ambient)
        => b.Request(key, tier, distance, now, duration);

    private static int Main(string[] args)
    {
        Check("reject-zero-and-excessive-capacity", () => {
            Throws<ArgumentOutOfRangeException>(()=>New(0));
            Throws<ArgumentOutOfRangeException>(()=>New(33));
        });
        Check("reject-bad-max-distance", () => {
            foreach (var n in new [] {0d, -1d, 1001d, double.NaN, double.PositiveInfinity})
                Throws<ArgumentOutOfRangeException>(()=>New(distance:n));
        });
        Check("reject-bad-cooldown", () => {
            foreach (var n in new [] {-1d, 10001d, double.NaN, double.PositiveInfinity})
                Throws<ArgumentOutOfRangeException>(()=>New(cooldown:n));
        });
        Check("reject-empty-and-overlong-key", () => {
            var b=New(); Throws<ArgumentException>(()=>A(b," "));
            Throws<ArgumentException>(()=>A(b,new string('a',81)));
            True(b.ActiveCount==0,"mutated on invalid key");
        });
        Check("reject-invalid-tier", () => {
            var b=New(); Throws<ArgumentOutOfRangeException>(()=>b.Request("x",(OptionalSfxTier)8,0,0,100));
        });
        Check("reject-invalid-request-distance", () => {
            var b=New(); foreach(var v in new[]{-1d,double.NaN,double.PositiveInfinity})
                Throws<ArgumentOutOfRangeException>(()=>A(b,"x",distance:v));
            True(b.ActiveCount==0,"modified");
        });
        Check("reject-invalid-request-duration", () => {
            var b=New(); foreach(var v in new[]{-1d,0d,60001d,double.NaN,double.NegativeInfinity})
                Throws<ArgumentOutOfRangeException>(()=>A(b,"x",duration:v));
        });
        Check("reject-invalid-monotonic-clock", () => {
            var b=New(); Throws<ArgumentOutOfRangeException>(()=>A(b,"a",-1));
            Throws<ArgumentOutOfRangeException>(()=>A(b,"a",double.PositiveInfinity));
            A(b,"a",10);
            Throws<ArgumentOutOfRangeException>(()=>A(b,"b",9));
            True(b.ActiveCount==1,"clock corrupted");
        });
        Check("first-voice-grant-and-token", () => {
            var b=New();var d=A(b,"a");True(d.Granted && d.Slot==0 && d.Token==1 && d.ReplacedToken==0,"first");
            True(b.TokenAt(0)==1 && b.Started==1,"slot");
        });
        Check("allocate-next-free-slot", () => {
            var b=New();A(b,"a");var d=A(b,"b");
            True(d.Slot==1 && b.ActiveCount==2 && d.Token==2,"second slot");
        });
        Check("reject-full-ambient", () => {
            var b=New(1);A(b,"a");
            var d=A(b,"b");True(!d.Granted && d.Outcome==VoiceOutcome.RejectedCapacity && d.Slot==-1,"capacity");
            True(b.RejectedCapacity==1 && b.ActiveCount==1,"stats");
        });
        Check("accented-preempts-farthest-ambient", () => {
            var b=New(3);var a=A(b,"near",distance:1);var far=A(b,"far",distance:30);A(b,"middle",distance:20);
            var high=A(b,"hi",distance:3,tier:OptionalSfxTier.Accented);
            True(high.Outcome==VoiceOutcome.Replaced && high.Slot==far.Slot &&
                high.ReplacedToken==far.Token && b.TokenAt(high.Slot)==high.Token,"steal");
            True(b.TokenAt(a.Slot)==a.Token && b.Replaced==1,"wrong victim");
        });
        Check("ambient-cannot-preempt-accented", () => {
            var b=New(1);A(b,"high",tier:OptionalSfxTier.Accented);
            True(A(b,"ambient").Outcome==VoiceOutcome.RejectedCapacity && b.Replaced==0,"stolen");
        });
        Check("same-tier-never-preempts", () => {
            var b=New(1);A(b,"one",distance:30);
            True(A(b,"closer",distance:0).Outcome==VoiceOutcome.RejectedCapacity,"same priority steal");
        });
        Check("accented-cannot-steal-only-accented", () => {
            var b=New(1);A(b,"one",tier:OptionalSfxTier.Accented);
            True(A(b,"two",tier:OptionalSfxTier.Accented).Outcome==VoiceOutcome.RejectedCapacity,"steal high");
        });
        Check("ties-pick-first-ambient-slot", () => {
            var b=New(2);A(b,"a",distance:10);A(b,"b",distance:10);
            var d=A(b,"hi",distance:3,tier:OptionalSfxTier.Accented);
            True(d.Slot==0,"tie victim not stable");
        });
        Check("cooldown-suppresses-same-key", () => {
            var b=New(4,cooldown:100);A(b,"spark",now:0);
            True(A(b,"spark",now:99).Outcome==VoiceOutcome.RejectedCooldown && b.RejectedCooldown==1,"spam");
        });
        Check("cooldown-allows-boundary", () => {
            var b=New(4,cooldown:100);A(b,"spark",now:0);
            True(A(b,"spark",now:100).Granted,"boundary");
        });
        Check("cooldown-persists-after-early-stop", () => {
            var b=New(cooldown:100);var first=A(b,"spark",0);
            True(b.Stop(first.Slot,first.Token),"stop");
            True(A(b,"spark",30).Outcome==VoiceOutcome.RejectedCooldown,"lost cooldown");
        });
        Check("cooldown-is-key-specific", () => {
            var b=New(4,cooldown:200);A(b,"spark",0);
            True(A(b,"clank",0).Granted,"unrelated key suppressed");
        });
        Check("zero-cooldown-allows-repeat-key", () => {
            var b=New();A(b,"same",0);True(A(b,"same",0).Granted,"repeated key");
        });
        Check("distance-cutoff-rejects-only-greater", () => {
            var b=New(2,distance:5);
            True(A(b,"edge",distance:5).Granted,"edge fail");
            True(A(b,"outside",distance:5.01).Outcome==VoiceOutcome.RejectedDistance,"far grant");
            True(b.RejectedDistance==1,"stat");
        });
        Check("expiration-at-end-of-voice", () => {
            var b=New(); A(b,"spark",now:0,duration:10);
            True(b.Advance(9)==0 && b.ActiveCount==1,"premature");
            True(b.Advance(10)==1 && b.Expired==1 && b.ActiveCount==0,"expired");
        });
        Check("expiry-frees-capacity-before-new-request", () => {
            var b=New(1);A(b,"a",0,10);
            True(A(b,"b",10,100).Granted,"capacity not reclaimed");
        });
        Check("stale-stop-cannot-kill-replacement", () => {
            var b=New(1);var old=A(b,"ambient");
            var newer=A(b,"accented",tier:OptionalSfxTier.Accented);
            True(!b.Stop(old.Slot,old.Token),"stale stop accepted");
            True(b.TokenAt(newer.Slot)==newer.Token && b.ActiveCount==1,"lost replacement");
        });
        Check("matched-stop-clears-voice-once", () => {
            var b=New(1);var d=A(b,"a");
            True(b.Stop(d.Slot,d.Token) && !b.Stop(d.Slot,d.Token),"double stop");
            True(b.Stopped==1 && b.ActiveCount==0,"stop counter");
        });
        Check("invalid-stop-is-harmless", () => {
            var b=New();var d=A(b,"a");
            True(!b.Stop(-1,d.Token) && !b.Stop(400,d.Token) && !b.Stop(d.Slot,-1),"bad stop");
            True(b.ActiveCount==1,"corrupted");
        });
        Check("tokenat-out-of-bounds-rejected", () => {
            var b=New();Throws<ArgumentOutOfRangeException>(()=>b.TokenAt(-1));
            Throws<ArgumentOutOfRangeException>(()=>b.TokenAt(2));
        });
        Check("clear-resets-slots-and-cooldown", () => {
            var b=New(cooldown:100);A(b,"same",0);A(b,"other",0);
            True(b.Clear()==2 && b.ActiveCount==0 && b.Cleared==2,"clear");
            True(A(b,"same",0).Granted,"cooldown not cleared");
        });
        Check("clear-does-not-reuse-old-tokens", () => {
            var b=New(1);var old=A(b,"a");b.Clear();var updated=A(b,"a");
            True(old.Token!=updated.Token && !b.Stop(0,old.Token),"token reused");
        });
        Check("stress-500-requests-never-exceed-capacity", () => {
            var b=New(8,distance:50);
            int grants=0;
            for(int i=0;i<500;i++)
            {
                var d=A(b,"burst"+i,0,1000,i%45);
                if(d.Granted) grants++;
                True(b.ActiveCount<=8,"over-allocated");
            }
            True(grants==8 && b.RejectedCapacity==492 && b.ActiveCount==8,"unexpected stress");
        });
        Check("stress-accented-preemptions-bounded", () => {
            var b=New(4);for(int i=0;i<4;i++) A(b,"amb"+i,0,2000,10+i);
            for(int i=0;i<20;i++) A(b,"high"+i,0,2000,2,OptionalSfxTier.Accented);
            True(b.Replaced==4 && b.RejectedCapacity==16 && b.ActiveCount==4,"over-steal");
        });
        Check("advance-rejects-clock-regression", () => {
            var b=New();b.Advance(100);
            Throws<ArgumentOutOfRangeException>(()=>b.Advance(99));
        });
        Check("max-voice-capacity-valid", () => {
            var b=New(32);for(int i=0;i<32;i++) True(A(b,"v"+i).Granted,"capacity 32");
            True(b.ActiveCount==32 && !A(b,"extra").Granted,"bound");
        });

        int total=pass+fail;
        var receipt=new { package="B01-50-SFXVOICE-001/R1", passed=pass, failed=fail, total, errors=errors.ToArray(),
            classification="ORIGINAL_CSHARP_OPTIONAL_AUDIO_VOICE_LOGIC_NOT_UNITY_SOUND_BENCHMARK",
            stress="500 optional voice requests limited to 8 simultaneous; simulated timestamps only",
            nativeUnity="NOT_RUN", measuredAudioLatency="NOT_MEASURED", fps="NOT_MEASURED" };
        if(args.Length==2 && args[0]=="--receipt")
        {
            if(File.Exists(args[1])) { Console.Error.WriteLine("REFUSE overwrite"); return 3; }
            File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        }
        Console.WriteLine("RESULT "+pass+"/"+total+" PASS; "+fail+" FAIL");
        return fail==0?0:1;
    }
}
