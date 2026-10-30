using System;
using B01.RecoveryHud;
internal static class QuoteTests
{
    private static int passed, failed;
    private static RecoveryRead Read(int price,int? quote)
    {
        return new RecoveryRead {
            RunId="run-W25",SlotId="slot-W25",Epoch=101,
            VerifiedOwnerRead=true,SingleLiveOwner=true,
            PhysicalStateVerified=true,DurableCommitCertain=true,
            CoreAlive=true,NextChallengeEligible=true,NormalConstructionFunctional=true,
            OriginalPartPriceScrap=price,HostQuotedPaidScrap=quote
        };
    }
    private static RecoveryNotice Notice(RecoveryRead s) =>
        RecoveryNoticeBuilder.Build(s,"run-W25","slot-W25",101);
    private static void Test(string name,Action check)
    {
        try {check();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+" "+ex);}
    }
    private static void Expect(int price,int? quote,RecoveryNoticeKind kind)
    {
        var s=Read(price,quote);var snapshot=Notice(s);
        if(snapshot.Kind!=kind)throw new Exception("price "+price+" quote "+quote+" expected "+kind+" got "+snapshot.Kind);
        if(kind==RecoveryNoticeKind.Unknown && snapshot.MayShowOwnerProvidedOffer)
            throw new Exception("unverified offer");
        if(kind==RecoveryNoticeKind.Ready && quote.HasValue &&
            !snapshot.Detail.Contains(quote.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)+" Scrap",StringComparison.Ordinal))
            throw new Exception("owner quote missing");
    }
    private static void Valid(int p,int q)=>Expect(p,q,RecoveryNoticeKind.Ready);
    private static void Invalid(int p,int q)=>Expect(p,q,RecoveryNoticeKind.Unknown);
    public static int Main()
    {
        Test("SOURCE RED 1 Scrap rebuild priced 1 denied in R2",()=>Invalid(1,1));
        Test("1 Scrap rebuild 0 permitted, zero PAID is not automatically free",()=>Valid(1,0));
        Test("5 Scrap quote3 permitted",()=>Valid(5,3));
        Test("15 floor9 permitted while owner rounds undecided",()=>Valid(15,9));
        Test("15 halfup10 permitted while owner rounds undecided",()=>Valid(15,10));
        Test("30 floor19 permitted",()=>Valid(30,19));
        Test("30 halfup20 permitted",()=>Valid(30,20));
        Test("100 quote65 permitted",()=>Valid(100,65));
        Test("100 quote64 rejected as mismatched 65 percent",()=>Invalid(100,64));
        Test("100 quote66 rejected as mismatched 65 percent",()=>Invalid(100,66));
        Test("101 floor65 permitted",()=>Valid(101,65));
        Test("101 halfup66 permitted",()=>Valid(101,66));
        Test("101 quote67 rejected",()=>Invalid(101,67));
        Test("0 original price with 0 quote rejected",()=>Invalid(0,0));
        Test("negative original price rejected",()=>Invalid(-1,0));
        Test("negative paid quote rejected",()=>Invalid(1,-1));
        Test("same price for premium part rejected",()=>Invalid(500,500));
        Test("greater-than-buy price rejected",()=>Invalid(500,501));
        Test("max price 64-bit safe floor",()=>Valid(int.MaxValue,(int)((long)int.MaxValue*65/100)));
        Test("max price 64-bit safe halfup",()=>Valid(int.MaxValue,(int)(((long)int.MaxValue*65+50)/100)));
        Test("max price quote0 rejected",()=>Invalid(int.MaxValue,0));
        Test("host may omit a quote when no paid order is requested",()=>Expect(100,null,RecoveryNoticeKind.Ready));
        Test("unanchored quote denied regardless of affordability",()=>{var s=Read(100,65);s.OriginalPartPriceScrap=null;if(Notice(s).Kind!=RecoveryNoticeKind.Unknown)throw new Exception("unanchored quote accepted");});
        Test("invalid quote blocks host-provided loaner display",()=>{var s=Read(1,1);s.NormalConstructionFunctional=false;s.LoanerOfferAuthorizedByHost=true;var n=Notice(s);if(n.Kind!=RecoveryNoticeKind.Unknown||n.MayShowOwnerProvidedOffer)throw new Exception("invalid quote displayed loaner");});
        Test("uncertain Save has higher priority than invalid quote",()=>{var s=Read(1,1);s.DurableCommitCertain=false;if(Notice(s).Kind!=RecoveryNoticeKind.SaveUncertain)throw new Exception("Save precedence lost");});
        Test("valid quote does not validate host authority",()=>{var s=Read(100,65);s.SingleLiveOwner=false;if(Notice(s).Kind!=RecoveryNoticeKind.Unknown)throw new Exception("owner bypass");});
        Test("valid quote does not bypass expected epoch",()=>{var s=Read(100,65);s.Epoch++;if(Notice(s).Kind!=RecoveryNoticeKind.Unknown)throw new Exception("stale epoch");});
        Test("invalid quote leaves source snapshot immutable",()=>{var s=Read(1,1);var b=s.HostQuotedPaidScrap;Notice(s);if(s.HostQuotedPaidScrap!=b)throw new Exception("HUD mutated quote");});
        // 100 distinct purchase values exercise both still-unresolved owner
        // rounding postures; the view must not select a policy.
        for(int price=1;price<=100;price++)
        {
            var p=price;long n=(long)p*65L;int floor=(int)(n/100),halfUp=(int)((n+50)/100);
            Test("generated "+p+" floor 65pct and strict discount",()=>
                Expect(p,floor,RecoveryNoticeKind.Ready));
            Test("generated "+p+" halfup accepted ONLY if strictly cheaper",()=>
                Expect(p,halfUp,halfUp<p?RecoveryNoticeKind.Ready:RecoveryNoticeKind.Unknown));
            Test("generated "+p+" no equal-price fake discount",()=>
                Expect(p,p,RecoveryNoticeKind.Unknown));
            Test("generated "+p+" no over-price fake discount",()=>
                Expect(p,p+1,RecoveryNoticeKind.Unknown));
        }
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}
