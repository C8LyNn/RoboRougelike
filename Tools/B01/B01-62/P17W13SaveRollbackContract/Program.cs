// B01-62 independent .NET 8 ledger persistence-contract validation.
// This compiles UNCHANGED ORIGINAL W06 DebrisImpulseCore.cs. NOT Unity/PhysX/native SaveLoad.
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using B01.DebrisRepulse001;

internal static class Program {
    private static int checks;
    private static void Check(bool ok,string why) {
        if(!ok)throw new Exception("FAIL: "+why);
        checks++;
    }
    private static void ReadAndRehydrate(string path,int expectConsumed) {
        byte[] original=File.ReadAllBytes(path);
        using JsonDocument doc=JsonDocument.Parse(original);
        JsonElement root=doc.RootElement;
        JsonElement payload=root.GetProperty("payload");
        var checksum=Convert.ToHexString(SHA256.HashData(
            Encoding.ASCII.GetBytes(payload.GetRawText()+"\n"))).ToLowerInvariant();
        Check(checksum==root.GetProperty("checksum_sha256").GetString(),"real SHA256:"+path);
        Check(payload.GetProperty("selected_sha").GetString()=="d94400d95d13db7f27e1533df05c9292d5380431","selected producer");
        Check(payload.GetProperty("transaction_active").GetBoolean()==false,"quiet source");
        Check(payload.GetProperty("phase").GetString()=="idle","no live commit");
        JsonElement parts=payload.GetProperty("parts");
        Check(parts.GetArrayLength()==2,"two original rig IDs");
        int consumed=0;
        foreach(JsonElement part in parts.EnumerateArray()) {
            long id=part.GetProperty("id").GetInt64();
            Check(id==2310001 || id==2310002,"original catalog ID");
            bool detached=part.GetProperty("detached").GetBoolean();
            bool expected=part.GetProperty("consumed").GetBoolean();
            Check(detached==expected,"ID consumed iff detached");
            // A NEW ledger restored exclusively from the previously validated snapshot,
            // not an existing in-memory instance, preserving real W06 consumed-ID semantics.
            var restored=new DebrisImpulseLedger();
            Vector3d zero=new Vector3d(0,0,0);
            if(expected) {
                Check(restored.TryDetach(id,zero,zero,zero,1,0,0,out Vector3d impulse),"restore consumed W06 ID");
                Check(impulse.MagnitudeSquared==0,"restore must NOT apply detach force");
                Check(restored.Contains(id),"ID cannot be applied twice after restore");
                Check(!restored.TryDetach(id,zero,zero,zero,1,0,0,out _),"duplicate detach denied");
                Check(restored.RearmAfterReattach(id),"one rearm allowed");
                Check(!restored.RearmAfterReattach(id),"duplicate rearm denied");
                Check(restored.TryDetach(id,zero,zero,zero,1,0,0,out _),"original W06 ID reusable after rearm");
                consumed++;
            } else {
                Check(restored.Count==0 && !restored.Contains(id),"attached must not consume original ID");
                Check(!restored.RearmAfterReattach(id),"attached rearm rejected");
            }
        }
        Check(consumed==expectConsumed,"expected consumed count");
        Console.WriteLine("ORIGINAL_W06_LEDGER_REHYDRATED fixture="+Path.GetFileName(path)+
            " consumed="+consumed+" SOURCE_ONLY");
    }
    public static int Main(string[] args) {
        if(args.Length!=3)throw new ArgumentException("attached detached both files");
        ReadAndRehydrate(args[0],0);
        ReadAndRehydrate(args[1],1);
        ReadAndRehydrate(args[2],2);
        Console.WriteLine("ORIGINAL_W06_LEDGER_ROUNDTRIP_ASSERTIONS_PASS="+checks);
        Console.WriteLine("GAME_SAVE_ADAPTER=NOT_IMPLEMENTED NATIVE_UNITY=NOT_RUN");
        return 0;
    }
}
