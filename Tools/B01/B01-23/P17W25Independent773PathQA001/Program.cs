#nullable enable
using System;
using System.IO;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore004;
internal static class W25Independent773
{
    static readonly long Run=251773;
    static readonly Guid Slot=Guid.Parse("25177325-1773-4000-8000-000000000773");
    static int pass,fail;
    static void Check(string label,Func<bool> f)
    {try{if(f()){pass++;Console.WriteLine("PASS "+label);}
         else{fail++;Console.WriteLine("FAIL "+label);}}
     catch(Exception e){fail++;Console.WriteLine("FAIL "+label+" "+e);}}
    static bool Root(Func<string,bool> f)
    {
        string root=Path.Combine(Path.GetTempPath(),"w25-q773-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return f(root);}finally{try{Directory.Delete(root,true);}catch(IOException){}}
    }
    static string Exact(string r)=>Path.Combine(r,"b0162_canonical_single_authority_v3",
        "run_"+Run+"_slot_"+Slot.ToString("N"));
    static bool Open(string r,out CanonicalWorldSlotLease? lease)
        =>CanonicalWorldSlotLease.TryAcquire(r,Run,Slot,out lease)&&lease!=null;
    static bool CaseOnlyPhysicalAliasAdmitted()
    {
        return Root(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var lease)||lease==null)return false;
            try
            {
                string canonical=Exact(root);
                string different=Path.Combine(Path.GetDirectoryName(canonical)!,
                    "RUN_"+Run+"_slot_"+Slot.ToString("N"));
                Directory.CreateDirectory(Path.GetDirectoryName(different)!);
                var outcome=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    different,Run,Slot,lease,out var store);
                bool admitted=outcome==SlotStatus.Accepted&&store!=null&&
                    Directory.Exists(different)&&
                    !string.Equals(different,canonical,StringComparison.Ordinal);
                store?.Dispose();
                Console.WriteLine("W25_773_NONCANONICAL_CASE_ALIAS_ADMITTED="+admitted+
                    " (TRUE is release P0 RED; witness PASS is NOT security PASS)");
                return admitted;
            }
            finally{lease.TryCancelUnbound();}
        });
    }
    static bool ValidPath()
    {
        return Root(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var lease)||lease==null)return false;
            try
            {
                string canon=Exact(root);Directory.CreateDirectory(Path.GetDirectoryName(canon)!);
                var result=CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    canon,Run,Slot,lease,out var store);
                if(result!=SlotStatus.Accepted||store==null)return false;
                var second=CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                    canon,Run,Slot,lease,out var duplicate);
                store.Dispose();
                var afterDispose=CanonicalSingleAuthorityStoreV3R2.TryOpenExisting(
                    canon,Run,Slot,lease,out duplicate);
                return second==SlotStatus.Busy&&afterDispose==SlotStatus.Busy;
            }
            finally{lease.TryCancelUnbound();}
        });
    }
    static bool ForeignSibling()
    {
        return Root(p=>{
            string root=Path.Combine(p,"trusted");Directory.CreateDirectory(root);
            if(!Open(root,out var lease)||lease==null)return false;
            try
            {
                string sibling=Path.Combine(p,"foreign");
                return CanonicalSingleAuthorityStoreV3R2.TryCreateNew(
                    sibling,Run,Slot,lease,out var store)==SlotStatus.Invalid&&
                    store==null&&!Directory.Exists(sibling);
            }
            finally{lease.TryCancelUnbound();}
        });
    }
    static int Main()
    {
        Check("Original #773 correct canonical Save + one Store per owner",ValidPath);
        Check("Original #773 foreign sibling path rejected",ForeignSibling);
        Check("Original #773 case-only path P0 source RED witness",CaseOnlyPhysicalAliasAdmitted);
        Console.WriteLine("B01_W25_773_INDEPENDENT "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("CASE_ALIAS_WITNESS_PASS=PRODUCT_RELEASE_RED; UNITY_G1=NOT_RUN");
        return fail==0?0:1;
    }
}
