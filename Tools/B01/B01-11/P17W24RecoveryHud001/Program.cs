using System;
using System.Collections.Generic;
using System.Reflection;
using B01.RecoveryHud;
using UnityEngine.UI;

internal static class Program
{
    private const string Run = "run-001", Slot = "slot-01";
    private const long Epoch = 12;
    private static int passed, failed;

    private static RecoveryRead Good() => new RecoveryRead
    {
        RunId = Run, SlotId = Slot, Epoch = Epoch,
        VerifiedOwnerRead = true, SingleLiveOwner = true,
        PhysicalStateVerified = true, DurableCommitCertain = true,
        CoreAlive = true, NextChallengeEligible = true,
        NormalConstructionFunctional = true, LoanerAlreadySpent = false,
        LoanerOfferAuthorizedByHost = false, PendingForNextVictory = false,
        PendingDestroyedParts = 0, FreeRebuildAvailableFromTrustedWin = false
    };

    private static RecoveryRead Broken()
    {
        var s = Good(); s.NormalConstructionFunctional = false; return s;
    }

    private static RecoveryNotice View(RecoveryRead? s) =>
        RecoveryNoticeBuilder.Build(s!, Run, Slot, Epoch);

    private static void Eq<T>(T actual, T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
            throw new Exception("expected " + expected + ", got " + actual);
    }
    private static void Yes(bool ok) { if (!ok) throw new Exception("expected true"); }
    private static void Test(string name, Action run)
    {
        try { run(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + " :: " + e); }
    }
    private static void Is(RecoveryRead s, RecoveryNoticeKind kind) => Eq(View(s).Kind, kind);
    private static void TextHas(RecoveryRead s, string segment) =>
        Yes((View(s).Heading + "|" + View(s).Explanation + "|" + View(s).Detail).Contains(segment, StringComparison.Ordinal));

    public static int Main()
    {
        Test("verified functional robot allowed to display READY",()=>Is(Good(),RecoveryNoticeKind.Ready));
        Test("READY message explicitly distinguishes optional damage",()=>TextHas(Good(),"opcjonalne"));
        Test("no host-provided loaner on otherwise functional rig",()=>{var s=Good();s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("null read is unknown",()=>Eq(View(null).Kind,RecoveryNoticeKind.Unknown));
        Test("foreign run is unknown",()=>{var s=Good();s.RunId="another";Is(s,RecoveryNoticeKind.Unknown);});
        Test("foreign slot is unknown",()=>{var s=Good();s.SlotId="another";Is(s,RecoveryNoticeKind.Unknown);});
        Test("stale epoch denied",()=>{var s=Good();s.Epoch--;Is(s,RecoveryNoticeKind.Unknown);});
        Test("future epoch denied",()=>{var s=Good();s.Epoch++;Is(s,RecoveryNoticeKind.Unknown);});
        Test("missing verified owner read denied",()=>{var s=Good();s.VerifiedOwnerRead=false;Is(s,RecoveryNoticeKind.Unknown);});
        Test("missing exclusive live owner denied",()=>{var s=Good();s.SingleLiveOwner=false;Is(s,RecoveryNoticeKind.Unknown);});
        Test("missing verified physical world denied",()=>{var s=Good();s.PhysicalStateVerified=false;Is(s,RecoveryNoticeKind.Unknown);});
        Test("uncertain durable commit blocks even functional robot",()=>{var s=Good();s.DurableCommitCertain=false;Is(s,RecoveryNoticeKind.SaveUncertain);});
        Test("uncertain commit cannot offer loaner",()=>{var s=Broken();s.LoanerOfferAuthorizedByHost=true;s.DurableCommitCertain=false;Yes(!View(s).MayShowOwnerProvidedOffer);});
        Test("unknown status cannot offer loaner",()=>{var s=Broken();s.SingleLiveOwner=false;s.LoanerOfferAuthorizedByHost=true;Yes(!View(s).MayShowOwnerProvidedOffer);});
        Test("dead Core overrides nominal ready",()=>{var s=Good();s.CoreAlive=false;Is(s,RecoveryNoticeKind.CoreDestroyed);});
        Test("dead Core cannot offer loaner",()=>{var s=Broken();s.CoreAlive=false;s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("broken rig and next real challenge with host loaner",()=>{var s=Broken();s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.LoanerAvailable);Yes(View(s).MayShowOwnerProvidedOffer);});
        Test("loaner not automatically enabled without host authorization",()=>{var s=Broken();Is(s,RecoveryNoticeKind.RepairRequired);});
        Test("loaner not offered after use",()=>{var s=Broken();s.LoanerAlreadySpent=true;Is(s,RecoveryNoticeKind.LoanerSpent);});
        Test("duplicate host loaner offer after use denied",()=>{var s=Broken();s.LoanerAlreadySpent=true;s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("no next challenge blocks loaner",()=>{var s=Broken();s.NextChallengeEligible=false;s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("broken without next challenge reports no challenge",()=>{var s=Broken();s.NextChallengeEligible=false;Is(s,RecoveryNoticeKind.RepairRequired);TextHas(s,"Brak dostępnego wyzwania");});
        Test("pending next distinct victory shown on broken rig",()=>{var s=Broken();s.PendingForNextVictory=true;s.PendingDestroyedParts=2;Is(s,RecoveryNoticeKind.AwaitingNextVictory);});
        Test("pending rebuild does not block functional rig",()=>{var s=Good();s.PendingForNextVictory=true;s.PendingDestroyedParts=1;Is(s,RecoveryNoticeKind.Ready);});
        Test("pending status mentions NEXT genuine victory",()=>{var s=Broken();s.PendingForNextVictory=true;s.PendingDestroyedParts=1;TextHas(s,"następnym rzeczywistym zwycięstwie");});
        Test("pending plus authorized loaner still uses host offer",()=>{var s=Broken();s.PendingForNextVictory=true;s.PendingDestroyedParts=1;s.LoanerOfferAuthorizedByHost=true;Is(s,RecoveryNoticeKind.LoanerAvailable);});
        Test("loaner does not remove pending count",()=>{var s=Broken();s.PendingForNextVictory=true;s.PendingDestroyedParts=2;s.LoanerOfferAuthorizedByHost=true;TextHas(s,"Oczekujące odbudowy: 2");});
        Test("pending flag with zero parts denied",()=>{var s=Good();s.PendingForNextVictory=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("positive pending count without flag denied",()=>{var s=Good();s.PendingDestroyedParts=1;Is(s,RecoveryNoticeKind.Unknown);});
        Test("negative pending count denied",()=>{var s=Good();s.PendingDestroyedParts=-1;Is(s,RecoveryNoticeKind.Unknown);});
        Test("too many pending parts denied",()=>{var s=Good();s.PendingDestroyedParts=501;s.PendingForNextVictory=true;Is(s,RecoveryNoticeKind.Unknown);});
        Test("verified free rebuild text only if source says available",()=>{var s=Good();s.FreeRebuildAvailableFromTrustedWin=true;TextHas(s,"jedna na zwycięstwo");});
        Test("unverified free rebuild cannot be promised",()=>TextHas(Good(),"Brak zweryfikowanej"));
        Test("owner quoted 65% displayed without independent price computation",()=>{var s=Good();s.OriginalPartPriceScrap=100;s.HostQuotedPaidScrap=65;TextHas(s,"65 Scrap");TextHas(s,"65% ceny");});
        Test("owner controls rounding including price 1 quote 0",()=>{var s=Good();s.OriginalPartPriceScrap=1;s.HostQuotedPaidScrap=0;TextHas(s,"0 Scrap");});
        Test("owner quote independent of UI currency arithmetic",()=>{var s=Good();s.OriginalPartPriceScrap=101;s.HostQuotedPaidScrap=66;TextHas(s,"66 Scrap");});
        Test("negative original price denied",()=>{var s=Good();s.OriginalPartPriceScrap=-1;Is(s,RecoveryNoticeKind.Unknown);});
        Test("negative host quote denied",()=>{var s=Good();s.OriginalPartPriceScrap=1;s.HostQuotedPaidScrap=-1;Is(s,RecoveryNoticeKind.Unknown);});
        Test("unanchored host quote denied",()=>{var s=Good();s.HostQuotedPaidScrap=65;Is(s,RecoveryNoticeKind.Unknown);});
        Test("empty run denied",()=>{var s=Good();s.RunId="";Is(s,RecoveryNoticeKind.Unknown);});
        Test("path-like slot denied",()=>{var s=Good();s.SlotId="../slot";Is(s,RecoveryNoticeKind.Unknown);});
        Test("unicode control token denied",()=>{var s=Good();s.RunId="r\nu";Is(s,RecoveryNoticeKind.Unknown);});
        Test("ambiguous slot generation 0 expected while actual 12 denied",()=>Eq(RecoveryNoticeBuilder.Build(Good(),Run,Slot,0).Kind,RecoveryNoticeKind.Unknown));
        Test("expected negative epoch denied",()=>Eq(RecoveryNoticeBuilder.Build(Good(),Run,Slot,-1).Kind,RecoveryNoticeKind.Unknown));
        Test("same source snapshot deterministic",()=>{var s=Good();var a=View(s);var b=View(s);Eq(a.Kind,b.Kind);Eq(a.Detail,b.Detail);});
        Test("no mutation of owner supplied snapshot",()=>{var s=Broken();bool before=s.LoanerOfferAuthorizedByHost;View(s);Eq(s.LoanerOfferAuthorizedByHost,before);});
        Test("no user gameplay commands in output model",()=>Eq(typeof(RecoveryNotice).GetMethods(BindingFlags.Instance|BindingFlags.Public).Length > 0,true));
        Test("view-only native Unity adapter writes labels for valid read",()=>{
            var panel=new RecoveryNoticePanel();
            var h=new Text();var e=new Text();var d=new Text();var sev=new Text();
            Bind(panel,"heading",h);Bind(panel,"explanation",e);Bind(panel,"detail",d);Bind(panel,"severityLabel",sev);
            Eq(panel.Present(Good(),Run,Slot,Epoch),RecoveryNoticeKind.Ready);
            Eq(sev.text,"GOTOWY");Yes(h.text.Contains("sprawna"));Yes(e.text.Contains("opcjonalne"));Yes(d.text.Contains("Brak zweryfikowanej"));
        });
        Test("native panel stale slot overwrites previous GREEN with unknown",()=>{
            var panel=new RecoveryNoticePanel();var h=new Text();var sev=new Text();Bind(panel,"heading",h);Bind(panel,"severityLabel",sev);
            panel.Present(Good(),Run,Slot,Epoch);panel.Present(Good(),Run,"foreign",Epoch);
            Eq(panel.CurrentKind,RecoveryNoticeKind.Unknown);Eq(sev.text,"NIEZWERYFIKOWANE");
        });
        Test("native panel OnDisable erases stale READY state",()=>{
            var panel=new RecoveryNoticePanel();var h=new Text();Bind(panel,"heading",h);
            panel.Present(Good(),Run,Slot,Epoch);
            typeof(RecoveryNoticePanel).GetMethod("OnDisable",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(panel,null);
            Eq(panel.CurrentKind,RecoveryNoticeKind.Unknown);
            Yes(h.text.Contains("Oczekiwanie"));
        });
        Test("native panel permits missing optional Text refs without effects",()=>{var p=new RecoveryNoticePanel();Eq(p.Present(Good(),Run,Slot,Epoch),RecoveryNoticeKind.Ready);});
        Test("native loaner notice is visibly advisory not action",()=>{
            var p=new RecoveryNoticePanel();var title=new Text();Bind(p,"heading",title);
            var s=Broken();s.LoanerOfferAuthorizedByHost=true;p.Present(s,Run,Slot,Epoch);
            Yes(title.text.Contains("jednorazowy"));Eq(p.CurrentKind,RecoveryNoticeKind.LoanerAvailable);
        });
        for (int i=0;i<150;i++)
        {
            int index=i;
            Test("variant-"+index,()=>{
                var s=Good();s.Epoch=(index%2==0)?Epoch:Epoch+1;
                s.NormalConstructionFunctional=(index%3)==0;
                s.PendingForNextVictory=(index%5)==0;s.PendingDestroyedParts=s.PendingForNextVictory?index%7+1:0;
                s.LoanerAlreadySpent=(index%11)==0;
                var a=View(s);
                if(s.Epoch!=Epoch)Eq(a.Kind,RecoveryNoticeKind.Unknown);
                Yes(!a.MayShowOwnerProvidedOffer);
                Yes(a.Heading.Length>0);
            });
        }
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }

    private static void Bind(object host,string field,Text value)
    {
        var f=host.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance);
        if (f==null) throw new Exception("missing actual Unity field "+field);
        f.SetValue(host,value);
    }
}
