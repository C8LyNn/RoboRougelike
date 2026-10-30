// W25 B01-62 independent ORIGINAL #773 exact source case-sensitive owner path audit.
// Deliberately proves a POSIX-only bypass: OrdinalIgnoreCase canonical comparison.
#nullable enable
using System;
using System.IO;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore004;
using B01.DetachedSaveContract001;

internal static class Original773CaseQA
{
    const long Run=62562;
    static readonly Guid Slot=Guid.Parse("62562625-0000-4000-8000-000000000773");
    private sealed class Gate:ICanonicalMutationGate{public bool IsQuiescent()=>true;}
    private sealed class Factory:ICanonicalRigFactory
    {
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
            => throw new InvalidOperationException("No synthetic Unity-world stage needed");
    }
    private sealed class Port:ICanonicalCommitPort
    {
        public bool Read(long run,out long generation,out string hash)
        {generation=0;hash="";return false;}
        public bool Commit(long run,long prior,string before,long next,string after)=>false;
    }
    private static bool CaseAliasOriginal()
    {
        string root=Path.Combine(Path.GetTempPath(),"b0162-w25-case-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var owner)||
                owner==null)return false;
            var gateway=new CanonicalWorldGateway(new Gate(),new Factory(),new Port(),owner);
            try
            {
                var expectedParent=Path.Combine(root,"b0162_canonical_single_authority_v3");
                var aliasParent=Path.Combine(root,"B0162_canonical_single_authority_v3");
                var dirName="run_"+Run+"_slot_"+Slot.ToString("N");
                string alias=Path.Combine(aliasParent,dirName);
                string expected=Path.Combine(expectedParent,dirName);
                Directory.CreateDirectory(aliasParent);
                var status=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    alias,Run,Slot,owner,out var raw);
                if(OperatingSystem.IsLinux())
                {
                    // Source security should have rejected a DIFFERENT physical path
                    // even when differing only by case. Original #773 actually accepts.
                    bool bug=status==SlotStatus.Accepted&&raw!=null&&
                        Directory.Exists(alias)&&!Directory.Exists(expected)&&
                        File.Exists(Path.Combine(alias,"slot.canonical-v3"));
                    raw?.Dispose();
                    if(!bug)return false;
                    if(!gateway.TryCloseAfterPhysicalRetirement(()=>true))return false;
                    if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var later)||
                        later==null)return false;
                    var nextGateway=new CanonicalWorldGateway(new Gate(),new Factory(),
                        new Port(),later);
                    try
                    {
                        var reopen=CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                            expected,Run,Slot,later,out var cold);
                        cold?.Dispose();
                        bool lost=reopen==SlotStatus.WrongOwner&&cold==null;
                        if(lost)Console.WriteLine(
                            "W25_ORIGINAL_773_LINUX_CASE_ALIAS_WRITES_NONCANONICAL_SAVE=REPRODUCED");
                        return lost;
                    }
                    finally{nextGateway.TryCloseAfterPhysicalRetirement(()=>true);}
                }
                else
                {
                    // On Windows this is usually same physical directory, not the
                    // separate-POSIX-path exploit. Avoid false Windows RED attribution.
                    bool physicallySame=Directory.Exists(expected);
                    raw?.Dispose();
                    if(status!=SlotStatus.Accepted||!physicallySame)return false;
                    Console.WriteLine("W25_ORIGINAL_773_WINDOWS_PATH_ALIAS_SAME_DIRECTORY=PASS");
                    return true;
                }
            }
            finally{gateway.TryCloseAfterPhysicalRetirement(()=>true);}
        }
        finally{Directory.Delete(root,true);}
    }
    public static int Main()
    {
        bool ok=false;
        try{ok=CaseAliasOriginal();}
        catch(Exception e){Console.WriteLine("FAIL "+e);}
        Console.WriteLine("W25_ORIGINAL_773_CASE_PATH_NEGATIVE "+
            (ok?"1 PASS 0 FAIL":"0 PASS 1 FAIL"));
        Console.WriteLine("W25_ORIGINAL_773_PRODUCT_G0=HOLD_PENDING_VERSIONED_CASE_SENSITIVE_PATH_FIX");
        Console.WriteLine("W25_NATIVE_UNITY_G1=NOT_RUN");
        return ok?0:1;
    }
}
