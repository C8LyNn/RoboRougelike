// Executes the EXACT ORIGINAL C# of Codex PR #302 R3 model & R4 weapon definitions.
// Explicit Unity API and mounting stubs are linked only for unrelated signatures.
// Do not call this a native Unity gameplay test or a host settlement test.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

static class Program
{
    static readonly List<object> results=new();
    static int passed=0,failures=0,confirmedDefects=0;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static R3VehicleModel New() => new(new R3VehicleDesign());
    static R3PartState Part(R4WeaponKind weapon,float hp=1f)
      => new(Vector3Int.zero,R3PartKind.Gun,hp){Weapon=weapon};
    static void Test(string id,Action fn,bool defect=false)
    {
        try{fn();passed++;if(defect)confirmedDefects++;results.Add(new{id,result=defect?"DEFECT_REPRODUCED":"PASS"});
            Console.WriteLine((defect?"DEFECT_REPRODUCED ":"PASS ")+id);}
        catch(Exception ex){failures++;results.Add(new{id,result="FAIL",message=ex.ToString()});Console.WriteLine("FAIL "+id+" "+ex);}
    }
    static int Main(string[] args)
    {
        Test("R01_valid_basic_salvage_preserves_unique_physical_id_and_kind",()=>{
            var m=New();var p=Part(R4WeaponKind.Basic);
            Require(m.ReceiveSalvage(new[]{p}),"valid salvage rejected");
            Require(m.Inventory.Count==1&&m.Inventory[0].Id==p.Id&&m.Inventory[0].Weapon==p.Weapon,"physical state corrupted");
        });
        Test("R02_valid_automatic_salvage_and_paid_repair_inventory",()=>{
            var m=New();var p=Part(R4WeaponKind.Automatic,.5f);
            Require(m.ReceiveSalvage(new[]{p}),"valid salvage failed");m.Award(100);
            int price=m.RepairCost(m.Inventory.Single());
            int before=m.Scrap;
            Require(price>0 && m.RepairInventory(p.Id),"repair failed");
            Require(m.Inventory.Single().Health==1 && m.Scrap==before-price,"repair loss/debit mismatch");
        });
        Test("R03_valid_two_physical_automatic_parts_price_and_dismantle_count",()=>{
            var m=New();var a=Part(R4WeaponKind.Automatic),b=Part(R4WeaponKind.Automatic);
            Require(m.ReceiveSalvage(new[]{a,b}),"multiple same weapon failed");
            int before=m.Scrap;int expected=m.DismantleValue(a);
            Require(m.Dismantle(a.Id),"valid dismantle failed");
            Require(m.Scrap==before+expected && m.Inventory.Count==1&&m.Inventory[0].Id==b.Id,
                "dismantle gave duplicate payout, incorrect count, or wrong ID");
        });
        Test("R04_salvage_duplicate_ID_rejected_atomically",()=>{
            var m=New();var a=Part(R4WeaponKind.Basic);var b=Part(R4WeaponKind.Automatic);b.Id=a.Id;
            Require(!m.ReceiveSalvage(new[]{a,b}),"duplicate physical ID accepted");
            Require(m.Inventory.Count==0,"partial grant from invalid reward batch");
        });
        Test("R05_unrecognized_gun_type_salvage_injects_invalid_physical_reward",()=>{
            var m=New();var invalid=Part((R4WeaponKind)1234567);
            bool accepted=m.ReceiveSalvage(new[]{invalid});
            // EXPECTED SECURE policy would reject. A reproduced defect is this exact
            // original-source positive ACCEPTANCE, not a fabricated repaired result.
            Require(accepted && m.Inventory.Single().Id==invalid.Id,"expected invalid-type ingress no longer reproducible");
            Require(!Enum.IsDefined(typeof(R4WeaponKind),invalid.Weapon),"fixture accidentally valid");
        },defect:true);
        Test("R06_unrecognized_stored_gun_type_throws_during_inventory_repair_quote",()=>{
            var m=New();var invalid=Part((R4WeaponKind)1234567,.5f);
            Require(m.ReceiveSalvage(new[]{invalid}),"precondition salvage no longer accepts invalid weapon");
            m.Award(100);int funds=m.Scrap;
            bool threw=false;
            try{m.RepairInventory(invalid.Id);}catch(ArgumentOutOfRangeException){threw=true;}
            Require(threw,"invalid enum did not throw during repair quote");
            Require(m.Scrap==funds&&m.Inventory.Single().Id==invalid.Id,"money/item changed on quote exception");
        },defect:true);
        Test("R07_invalid_weapon_dismantle_removes_part_before_price_exception",()=>{
            var m=New();var invalid=Part((R4WeaponKind)1234567);
            Require(m.ReceiveSalvage(new[]{invalid}),"precondition: ingress must be currently permissive");
            int before=m.Scrap;
            bool threw=false;
            try{m.Dismantle(invalid.Id);}catch(ArgumentOutOfRangeException){threw=true;}
            Require(threw,"invalid enum did not throw in pricing");
            Require(m.Inventory.All(x=>x.Id!=invalid.Id),"expected REAL item-loss defect absent");
            Require(m.Scrap==before,"unexpected credit minting");
            // Authoritative inventory mutates before exception: not atomic.
        },defect:true);
        Test("R08_non_weapon_kind_dismantle_does_not_evaluate_invalid_weapon_enum",()=>{
            var m=New();var p=new R3PartState(Vector3Int.zero,R3PartKind.Wheel,1f){Weapon=(R4WeaponKind)1234567};
            Require(m.ReceiveSalvage(new[]{p}),"other item no receipt");
            Require(m.Dismantle(p.Id),"wheel dismantle failed");
            Require(m.Inventory.Count==0 && m.Scrap==3,"wheel price baseline expected 15, floor quarter=3");
        });
        Test("R09_real_WeaponDefinition_Get_is_total_only_for_declared_enum",()=>{
            int defined=0;
            foreach(R4WeaponKind k in Enum.GetValues(typeof(R4WeaponKind))){
                Require(R4WeaponDefinition.Get(k).Price>0,"declared type not priced");
                defined++;
            }
            Require(defined==9,"expected immutable 9 types");
            bool threw=false;try{R4WeaponDefinition.Get((R4WeaponKind)1234567);}catch(ArgumentOutOfRangeException){threw=true;}
            Require(threw,"actual definition Get unexpectedly accepts unknown kind");
        });
        string file=args.Length>0?args[0]:"artifacts/chl-econ/weapon302-result.json";
        var dir=System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(file));
        System.IO.Directory.CreateDirectory(dir!);
        var report=new{schema="b01-22-w30-weapon302-readonly-actual-source",
            input_commit="d44fa09c227dffca1c1f8832383bbd80430bba90",
            original_csharp_compiled=new[]{"R3VehicleModel.cs","R4WeaponDefinition.cs"},
            unity_api="explicit isolated signature doubles for unrelated types",
            compiled_unity_native="NOT_RUN",codex_integrated_game="NOT_RUN",
            total=passed+failures,tests_pass=passed,tests_fail=failures,confirmed_defect_reproductions=confirmedDefects,
            suspected_root="ReceiveSalvage accepts unknown R4WeaponKind; Dismantle removes inventory before Price throws",
            cases=results};
        System.IO.File.WriteAllText(file,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"ORIGINAL_CODEX_302_ECON_QA {passed}/{passed+failures} tests, " +
                          $"{confirmedDefects} DEFECT_REPRODUCED, no native Unity or save.");
        return failures==0 && confirmedDefects==3?0:1;
    }
}
