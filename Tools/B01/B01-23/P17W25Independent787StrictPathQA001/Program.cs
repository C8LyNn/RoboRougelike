#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore005;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
internal static class Original787IndependentQA
{
    const long Run=251787;
    static readonly Guid Slot=Guid.Parse("25178725-1787-4000-8000-000000000787");
    static int pass,fail;
    static void Check(string name,Func<bool> test)
    {
        try {if(test()){pass++;Console.WriteLine("PASS "+name);}
             else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static bool Temp(Func<string,bool> test)
    {
        string root=Path.Combine(Path.GetTempPath(),"w25-q787-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return test(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}}
    }
    static string Canonical(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3",
        "run_"+Run.ToString(CultureInfo.InvariantCulture)+"_slot_"+Slot.ToString("N"));
    static bool Open(string root,out CanonicalWorldSlotLease? lease)
        =>CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out lease)&&lease!=null;
    static byte[] Actual556()
    {
        var items=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<items.Length;i++)
        {
            bool detached=i==0;
            long owner=FrozenPartCatalog.Owner(i);
            items[i]=new PersistedPart(FrozenPartCatalog.Id(i),owner,
                detached?0:owner,FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,detached,!detached,
                new Vector3d(i==0?-2:3,1,0),0,0,0,1,
                detached?new Vector3d(1,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,1,items));
    }
    static bool WrongCaseRejected()
    {
        return Temp(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var owner)||owner==null)return false;
            try
            {
                string path=Canonical(root);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string wrongLeaf=Path.Combine(Path.GetDirectoryName(path)!,
                    "RUN_"+Run+"_slot_"+Slot.ToString("N"));
                string wrongBase=Path.Combine(root,"B0162_CANONICAL_SINGLE_AUTHORITY_V3",
                    "run_"+Run+"_slot_"+Slot.ToString("N"));
                bool one=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    wrongLeaf,Run,Slot,owner,out var a)==SlotStatus.Invalid&&a==null&&
                    !Directory.Exists(wrongLeaf);
                bool two=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    wrongBase,Run,Slot,owner,out var b)==SlotStatus.Invalid&&b==null&&
                    !Directory.Exists(wrongBase);
                Console.WriteLine("W25_787_NONCANONICAL_CASE_ALIAS_REFUSED="+(one&&two));
                return one&&two;
            }
            finally{owner.TryCancelUnbound();}
        });
    }
    static bool GenuineOneStoreAndColdRecovery()
    {
        return Temp(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var owner)||owner==null)return false;
            string path=Canonical(root);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var state=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                path,Run,Slot,owner,out var store);
            if(state!=SlotStatus.Accepted||store==null){Console.WriteLine("R3 first TryCreateNew="+state);return false;}
            byte[] bytes=Actual556();
            var commit=store.TryCommit(0,bytes);
            var read=store.TryRead(out long generation,out byte[] payload);
            if(commit!=SlotStatus.Accepted||read!=SlotStatus.Accepted||generation!=1||!payload.AsSpan().SequenceEqual(bytes))
            {Console.WriteLine("R3 commit="+commit+" read="+read+" gen="+generation+" equal="+payload.AsSpan().SequenceEqual(bytes));return false;}
            bool noSecond=CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                path,Run,Slot,owner,out var duplicate)==SlotStatus.Busy&&duplicate==null;
            store.Dispose();
            bool afterDispose=CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                path,Run,Slot,owner,out duplicate)==SlotStatus.Busy&&duplicate==null;
            if(!owner.TryCancelUnbound()){Console.WriteLine("R3 original owner cancelled=false");return false;}
            if(!Open(root,out var next)||next==null)return false;
            try
            {
                var newState=CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                    path,Run,Slot,next,out var cold);
                if(newState!=SlotStatus.Accepted||cold==null){Console.WriteLine("R3 cold TryOpenExisting="+newState);return false;}
                bool correct=cold.TryRead(out long old,out byte[] recovered)==SlotStatus.Accepted&&
                    old==1&&recovered.AsSpan().SequenceEqual(bytes)&&
                    ColdRestore.TryStage(recovered,out ColdRestore? parsed)&&parsed!=null&&
                    parsed.ContainsConsumedId(0,2310001)&&
                    !parsed.ContainsConsumedId(1,2310002);
                cold.Dispose();
                Console.WriteLine("R3 cold read correct="+correct+" same-store="+noSecond+" after-Dispose="+afterDispose);
                return correct&&noSecond&&afterDispose&&
                    !File.Exists(Path.Combine(path,"slot.lease"));
            }
            finally{next.TryCancelUnbound();}
        });
    }
    static bool ForeignRootNeverMutates()
    {
        return Temp(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var owner)||owner==null)return false;
            try
            {
                string foreign=Path.Combine(p,"foreign-sibling");
                return CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    foreign,Run,Slot,owner,out var store)==SlotStatus.Invalid&&store==null&&
                    !Directory.Exists(foreign);
            }
            finally{owner.TryCancelUnbound();}
        });
    }
    static int Child(string root)
    {
        var psi=new ProcessStartInfo("dotnet"){UseShellExecute=false};
        psi.ArgumentList.Add(typeof(Original787IndependentQA).Assembly.Location);
        psi.ArgumentList.Add("--probe");psi.ArgumentList.Add(root);
        using var proc=Process.Start(psi);if(proc==null)return -1;
        if(!proc.WaitForExit(30000)){proc.Kill(true);return -2;}
        return proc.ExitCode;
    }
    static bool ActualCrossProcessOwner()
    {
        return Temp(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var owner)||owner==null)return false;
            bool refused=Child(root)==7;
            bool released=owner.TryCancelUnbound();
            return refused&&released&&Child(root)==0;
        });
    }
    public static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--probe")
        {
            bool granted=CanonicalWorldSlotLease.TryAcquire(args[1],Run,Slot,out var x)&&x!=null;
            if(!granted)return 7;
            return x!.TryCancelUnbound()?0:9;
        }
        Check("EXACT original R3 denies uppercase run leaf and base without files",WrongCaseRejected);
        Check("EXACT original R3 one full frozen 556 envelope, one store and cold real ID",GenuineOneStoreAndColdRecovery);
        Check("EXACT original R3 denies physical foreign sibling path",ForeignRootNeverMutates);
        Check("EXACT original R3 one physical OS .owner excludes real child CLR",ActualCrossProcessOwner);
        Console.WriteLine("W25_787_INDEPENDENT_ORIGINAL_SOURCE_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("PRODUCT_QA_SOURCE="+(fail==0?"GREEN_TESTED_SCOPE":"RED")+
            "; NATIVE_UNITY_G1=NOT_RUN; PRIVATE_510_RIGS=NOT_IMPORTED");
        return fail==0?0:1;
    }
}
