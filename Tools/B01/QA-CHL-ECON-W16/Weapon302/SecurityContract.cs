// B01-22 expectation tests for future CODEX-owner remediation.
// REAL ORIGINAL immutable R3VehicleModel / R4WeaponDefinition are compiled;
// Unity and mounting APIs here are explicit test doubles. NOT a Unity test.
// This executable returns non-zero on insecure current #302 / #303 source.
// Never silently treat EXPECTED_RED as "fixed" or gameplay PASS.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

static class SecurityContract
{
    static readonly List<object> Cases=new();
    static int red,green;
    static void Check(bool value,string message)
    {
        if(!value)throw new InvalidOperationException(message);
    }
    static R3VehicleModel New() => new(new R3VehicleDesign());
    static R3PartState Invalid(float hp=1f) =>
        new(Vector3Int.zero,R3PartKind.Gun,hp){Weapon=(R4WeaponKind)1234567};
    static void Execute(string id,Action test)
    {
        try{test();green++;Console.WriteLine("CONTRACT_GREEN "+id);Cases.Add(new{id,status="PASS"});}
        catch(Exception error){red++;Console.WriteLine("CONTRACT_RED "+id+" "+error.Message);
            Cases.Add(new{id,status="FAIL",reason=error.GetType().Name+": "+error.Message});}
    }
    static int Main(string[] args)
    {
        Execute("E01_REJECT_INVALID_ENUM_BEFORE_SALVAGE_COMMIT",()=>{
            var m=New();var invalid=Invalid();var wallet=m.Scrap;
            bool accepted=m.ReceiveSalvage(new[]{invalid});
            Check(!accepted && m.Inventory.Count==0 && m.Scrap==wallet,
                $"EXPECTED no ingress, ACTUAL accepted={accepted}; inventory={m.Inventory.Count}; scrap={m.Scrap-wallet:+#;-#;0}");
        });
        Execute("E02_PREEXISTING_CORRUPT_REPAIR_IS_FAIL_CLOSED_ATOMIC",()=>{
            var m=New();var valid=new R3PartState(Vector3Int.zero,R3PartKind.Gun,.5f){Weapon=R4WeaponKind.Basic};
            Check(m.ReceiveSalvage(new[]{valid}),"valid precondition failed");
            m.Award(100);var stored=m.Inventory.Single();stored.Weapon=(R4WeaponKind)1234567;
            var wallet=m.Scrap;var hp=stored.Health;bool threw=false;bool accepted=false;
            try{accepted=m.RepairInventory(stored.Id);}catch(ArgumentOutOfRangeException){threw=true;}
            Check(!threw && !accepted && m.Scrap==wallet && m.Inventory.Single().Id==stored.Id &&
                  m.Inventory.Single().Health==hp,
                  $"EXPECTED false/unchanged, ACTUAL threw={threw}, accepted={accepted}, walletDelta={m.Scrap-wallet}, hp={stored.Health}");
        });
        Execute("E03_PREEXISTING_CORRUPT_DISMANTLE_NEVER_LOSES_ITEM",()=>{
            var m=New();var valid=new R3PartState(Vector3Int.zero,R3PartKind.Gun){Weapon=R4WeaponKind.Basic};
            Check(m.ReceiveSalvage(new[]{valid}),"valid precondition failed");
            var stored=m.Inventory.Single();stored.Weapon=(R4WeaponKind)1234567;
            string id=stored.Id;var wallet=m.Scrap;bool threw=false;bool accepted=false;
            try{accepted=m.Dismantle(id);}catch(ArgumentOutOfRangeException){threw=true;}
            bool retained=m.Inventory.Any(p=>p.Id==id);
            Check(!threw&&!accepted && retained && m.Scrap==wallet,
                $"EXPECTED false+GUID retained+no debit, ACTUAL threw={threw}, accepted={accepted}, itemRetained={retained}, scrapDelta={m.Scrap-wallet}");
        });
        var outPath=args.Length>0?args[0]:"artifacts/chl-econ/weapon302-security-contract.json";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath,JsonSerializer.Serialize(new {
            contract="b01-22-w34-r3-inventory-invalid-enum-atomicity",
            source="Codex PR302 @ d44fa09c227dffca1c1f8832383bbd80430bba90, Git identical at PR303 c89744d5ab43f4a795abe62527d94439be573693",
            native_unity="NOT_RUN",tested_csharp_original=true,
            tests_total=red+green,security_contract_pass=green,security_contract_fail=red,cases=Cases
        },new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"SECURITY_CONTRACT_RESULT {green} GREEN / {red} RED / {green+red} TOTAL");
        return red==0?0:1;
    }
}
