// B01-40 W25 independent ORIGINAL SOURCE runtime negative (not producer Program).
// Executed with #773 exact Git bytes of Store + frozen original #729/#556/#510.
#nullable enable
using System;
using System.IO;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore004;
internal static class B0140IndependentCaseAlias
{
    private const long Run=62462;
    private static readonly Guid Id=Guid.Parse("62462624-0000-4000-8000-000000000749");
    private static void Need(bool ok,string reason)
    {if(!ok)throw new InvalidOperationException(reason);}
    public static int Main()
    {
        string parent=Path.Combine(Path.GetTempPath(),"b0140-w25-case-"+Guid.NewGuid().ToString("N"));
        string root=Path.Combine(parent,"trusted");
        Directory.CreateDirectory(root);
        CanonicalWorldSlotLease? owner=null;
        try
        {
            Need(CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out owner)&&owner!=null,
                "BASELINE_V3_PHYSICAL_OWNER_ACQUIRE_FAILED");
            string expected=Path.Combine(root,"b0162_canonical_single_authority_v3",
                "run_"+Run+"_slot_"+Id.ToString("N"));
            string sibling=Path.Combine(parent,"foreign");
            var denied=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(sibling,Run,Id,owner!,out var foreign);
            Need(denied==SlotStatus.Invalid&&foreign==null&&!Directory.Exists(sibling),
                "BASELINE_DIFFERENT_SIBLING_PATH_NOT_DENIED");
            Console.WriteLine("B0140_W25_BASELINE_FOREIGN_SIBLING_DENIED=PASS");
            if(OperatingSystem.IsLinux())
            {
                // Linux has case-sensitive distinct filesystem entries; Windows would not.
                string uppercase=Path.Combine(root,"b0162_canonical_single_authority_v3",
                    "RUN_"+Run+"_slot_"+Id.ToString("N"));
                Need(!string.Equals(expected,uppercase,StringComparison.Ordinal),
                    "CASE_ALIAS_NOT_DISTINCT_STRING");
                Need(!Directory.Exists(uppercase),"CASE_ALIAS_PREEXISTS");
                var status=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    uppercase,Run,Id,owner!,out var unexpected);
                bool rejected=status==SlotStatus.Invalid&&unexpected==null&&!Directory.Exists(uppercase);
                if(!rejected)
                {
                    Console.Error.WriteLine("B0140_W25_P0_RAW_STORE_CASE_ALIAS_RED: status="+status+
                        "; physical_alias_created="+Directory.Exists(uppercase)+
                        "; original_owner="+owner!.Identity);
                    return 2;
                }
                Console.WriteLine("B0140_W25_RAW_STORE_CASE_ALIAS_DENIED=PASS");
            }
            else
            {
                Console.WriteLine("B0140_W25_CASE_ALIAS=NOT_APPLICABLE_WINDOWS_CASE_INSENSITIVE_FILESYSTEM");
            }
            Console.WriteLine("B0140_W25_SELECTED773_ONE_CANONICAL_PHYSICAL_PATH=PASS");
            return 0;
        }
        catch(Exception e)
        {Console.Error.WriteLine("B0140_W25_RUNTIME_QA_ERROR "+e);return 3;}
        finally
        {
            owner?.TryCancelUnbound();
            // ONLY this fresh temporary test-owned root, not any user installation.
            try{Directory.Delete(parent,true);}catch(Exception ex)
            {Console.Error.WriteLine("B0140_W25_SCRATCH_CLEANUP_HOLD "+ex.Message);}
        }
    }
}
