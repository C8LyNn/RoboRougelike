using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01KitReadiness001;

internal static class OriginalReadinessSourceTests
{
    private static int passes;
    private const string Catalog = "Research/B01-22/P17/EQUIPMENT-KITS-001/catalog.json";
    private const string Scenarios = "Research/B01-22/P17/KITREADINESS001/scenarios/";
    private static void Check(string title, Func<bool> assertion)
    {
        if (!assertion()) throw new Exception("RED " + title);
        passes++; Console.WriteLine("PASS " + title);
    }
    private static bool Rejected(Action test)
    {
        try { test(); return false; }
        catch (ArgumentException) { return true; }
    }
    private static KitRecipe[] OriginalCatalog()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Catalog));
        var root = json.RootElement;
        if (root.GetProperty("version").GetString() != "1.0.3" ||
            root.GetProperty("provenance").GetProperty("codex_sha").GetString() !=
            "203a0805251ed28293888e43f2d49f0df63767c6")
            throw new Exception("CATALOG_SOURCE_NOT_ACCEPTED");
        var partPrices = root.GetProperty("part_prices");
        var weaponPrices = root.GetProperty("weapon_prices");
        return root.GetProperty("kits").EnumerateArray().Select(kit =>
            new KitRecipe {
                Id = kit.GetProperty("id").GetString(),
                Parts = kit.GetProperty("parts").EnumerateArray().Select(p => {
                    string kind = p.GetProperty("kind").GetString();
                    string weapon = p.TryGetProperty("weapon", out var w) ? w.GetString() : "";
                    int price = kind == "Gun" ? weaponPrices.GetProperty(weapon).GetInt32() :
                        partPrices.GetProperty(kind).GetInt32();
                    return new PartNeed {Kind=kind, Weapon=weapon,
                        Quantity=p.GetProperty("count").GetInt32(), UnitScrap=price};
                }).ToArray()
            }).ToArray();
    }
    private static KitRecipe[] Clone(KitRecipe[] source) =>
        source.Select(k => new KitRecipe {Id=k.Id,
            Parts=k.Parts.Select(p => new PartNeed {Kind=p.Kind, Weapon=p.Weapon,
                Quantity=p.Quantity, UnitScrap=p.UnitScrap}).ToArray()}).ToArray();
    private static OwnedPart[] Stock(params (string kind,string weapon,int count)[] parts) =>
        parts.Select(p=>new OwnedPart {Kind=p.kind, Weapon=p.weapon, Quantity=p.count}).ToArray();
    private static ReadinessPlan Plan(KitRecipe[] k, OwnedPart[] stock, params string[] ids) =>
        KitReadinessPlanner.Preview(KitReadinessPlanner.SourceSha,k,stock,ids);
    private static OwnedPart[] FromScenario(string file, out string[] ids)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Scenarios+file));
        var root=doc.RootElement;
        ids=root.GetProperty("requests").EnumerateArray().Select(x=>x.GetString()).ToArray();
        return root.GetProperty("owned_parts").EnumerateArray().Select(x=>new OwnedPart {
            Kind=x.GetProperty("kind").GetString(),
            Weapon=x.TryGetProperty("weapon",out var w)?w.GetString():"",
            Quantity=x.GetProperty("count").GetInt32()
        }).ToArray();
    }
    private static int Left(ReadinessPlan plan,string kind,string weapon="")
        => plan.RemainingVirtualStock.Single(x=>x.Kind==kind&&x.Weapon==weapon).Quantity;
    private static void Scenario(string file,KitRecipe[] source,string output)
    {
        string[] ids;
        var pool=FromScenario(file,out ids);
        var result=Plan(source,pool,ids);
        var serializer=new JsonSerializerOptions { WriteIndented=true, IncludeFields=true };
        string json=JsonSerializer.Serialize(new {
            schema="b01-kitreadiness-preview-v1",
            source_sha=KitReadinessPlanner.SourceSha,
            scenario=file,
            reference_only=true,
            modifies_game=false,
            result
        },serializer);
        File.WriteAllText(Path.Combine(output,file.Replace(".json","-preview.json")),json.Replace("\r\n","\n")+"\n");
        Console.WriteLine("SCENARIO "+file+" ready="+result.ReadyCount+" blocked="+result.BlockedCount);
    }
    public static int Main(string[] args)
    {
        try
        {
            var c=OriginalCatalog();
            Check("actual immutable #411 catalog has 10 kits",()=>c.Length==10);
            Check("all EK IDs ordered",()=>c.Select((x,i)=>x.Id=="EK"+(i+1).ToString("D2")).All(x=>x));
            Check("original source sha pinned",()=>KitReadinessPlanner.SourceSha.Length==40);
            Check("EK01 exactly 5 physical pieces",()=>c[0].Parts.Sum(x=>x.Quantity)==5);
            Check("EK08 has original 235 reference scrap",()=>c[7].Parts.Sum(x=>x.Quantity*x.UnitScrap)==235);
            var empty=Plan(c,Array.Empty<OwnedPart>(),"EK01");
            Check("empty stock blocks EK01",()=>!empty.Requests[0].Ready);
            Check("empty stock has five missing pieces",()=>empty.Requests[0].MissingPieces==5);
            Check("empty EK01 shortfall has correct reference scrap 110",()=>empty.Requests[0].ReferenceMissingScrap==110);
            Check("empty stock no virtual mutations",()=>empty.RemainingVirtualStock.Length==0);
            var enough=Stock(("Wheel","",2),("Suspension","",2),("Gyroscope","",1));
            var originalQuantities=enough.Select(x=>x.Quantity).ToArray();
            var a=Plan(c,enough,"EK01","EK01");
            Check("first EK01 reserves only private virtual stock",()=>a.Requests[0].Ready);
            Check("second EK01 blocked by depleted virtual stock",()=>!a.Requests[1].Ready);
            Check("second EK01 has full five-piece shortage",()=>a.Requests[1].MissingPieces==5);
            Check("one ready and one blocked",()=>a.ReadyCount==1&&a.BlockedCount==1);
            Check("input inventory remains entirely unchanged",()=>enough.Select(x=>x.Quantity).SequenceEqual(originalQuantities));
            Check("all three used parts have zero virtual remainder",()=>Left(a,"Wheel")==0&&Left(a,"Suspension")==0&&Left(a,"Gyroscope")==0);
            var reorder=Plan(c,enough,"EK01");
            Check("independent second plan not affected by prior virtual selection",()=>reorder.ReadyCount==1);
            var low=Stock(("Wheel","",2),("Suspension","",2),("Gyroscope","",0));
            var blocked=Plan(c,low,"EK01","EK01");
            Check("short gyro blocks both requests",()=>blocked.BlockedCount==2);
            Check("failed reservation does not drain wheels/suspension",()=>Left(blocked,"Wheel")==2&&Left(blocked,"Suspension")==2);
            Check("short gyro means one piece and 30 scrap missing",()=>blocked.Requests[0].MissingPieces==1&&blocked.Requests[0].ReferenceMissingScrap==30);
            var bundle=Stock(("Wheel","",2),("Suspension","",2),("Gyroscope","",1),
              ("CrawlerTrack","",2),("Gun","Heavy",1),("Gun","RocketLauncher",1),
              ("DirectionalShield","",1));
            var ordered=Plan(c,bundle,"EK01","EK08","EK02");
            Check("ordered first EK01 wins one gyro",()=>ordered.Requests[0].Ready);
            Check("later EK08 blocked only by gyro",()=>ordered.Requests[1].MissingPieces==1&&ordered.Requests[1].ReferenceMissingScrap==30);
            Check("failed EK08 atomic rollback leaves tracks for EK02",()=>ordered.Requests[2].Ready);
            Check("two successful requests and one blocked",()=>ordered.ReadyCount==2&&ordered.BlockedCount==1);
            var reversed=Plan(c,bundle,"EK08","EK01","EK02");
            Check("reversed first EK08 wins gyro",()=>reversed.Requests[0].Ready);
            Check("reversed EK01 blocked by gyro",()=>reversed.Requests[1].ReferenceMissingScrap==30);
            Check("reversed EK02 also blocked after tracks reserved",()=>!reversed.Requests[2].Ready);
            Check("zero requests are valid with unchanged stock",()=>Plan(c,enough).RemainingVirtualStock.Sum(x=>x.Quantity)==5);
            Check("reject wrong published producer source",()=>Rejected(()=>KitReadinessPlanner.Preview("bad",c,enough,new[]{"EK01"})));
            Check("reject null source catalog",()=>Rejected(()=>Plan(null,enough,"EK01")));
            Check("reject incomplete catalog",()=>Rejected(()=>Plan(c.Take(9).ToArray(),enough,"EK01")));
            Check("reject unknown kit",()=>Rejected(()=>Plan(c,enough,"EK99")));
            Check("reject wrong-case ID",()=>Rejected(()=>Plan(c,enough,"ek01")));
            Check("reject null request",()=>Rejected(()=>Plan(c,enough,new string[]{null})));
            Check("reject duplicate stock entries",()=>Rejected(()=>Plan(c,Stock(("Wheel","",1),("Wheel","",1)),"EK01")));
            Check("reject negative stock",()=>Rejected(()=>Plan(c,Stock(("Wheel","",-1)),"EK01")));
            Check("reject excessive stock",()=>Rejected(()=>Plan(c,Stock(("Wheel","",1000001)),"EK01")));
            Check("reject noncatalog physical stock type",()=>Rejected(()=>Plan(c,Stock(("Unknown","",1)),"EK01")));
            Check("reject weapon on non-Gun",()=>Rejected(()=>Plan(c,Stock(("Wheel","Shotgun",1)),"EK01")));
            Check("reject missing Gun weapon subtype",()=>Rejected(()=>Plan(c,Stock(("Gun","",1)),"EK01")));
            Check("reject too many kit requests",()=>Rejected(()=>Plan(c,enough,Enumerable.Repeat("EK01",101).ToArray())));
            Check("reject duplicate physical BOM line",()=>{
                var x=Clone(c);x[0].Parts[1]=x[0].Parts[0];return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("reject Core injection",()=>{
                var x=Clone(c);x[0].Parts[0].Kind="Core";return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("reject zero part quantity",()=>{
                var x=Clone(c);x[0].Parts[0].Quantity=0;return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("reject negative reference price",()=>{
                var x=Clone(c);x[0].Parts[0].UnitScrap=-1;return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("reject illegal 3 weapon families",()=>{
                var x=Clone(c);x[0].Parts=new[]{
                    new PartNeed {Kind="Gun",Weapon="Shotgun",Quantity=1,UnitScrap=45},
                    new PartNeed {Kind="Gun",Weapon="Basic",Quantity=1,UnitScrap=30},
                    new PartNeed {Kind="Gun",Weapon="Heavy",Quantity=1,UnitScrap=55}};
                return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("reject injected catalog ID",()=>{
                var x=Clone(c);x[3].Id="EK14";return Rejected(()=>Plan(x,enough,"EK01"));
            });
            Check("REJECT coerced unit scrap with old source SHA",()=>{
                var x=Clone(c);x[0].Parts[0].UnitScrap+=7;
                return Rejected(()=>Plan(x,Array.Empty<OwnedPart>(),"EK01"));
            });
            Check("REJECT coherent but inflated part quantity with old SHA",()=>{
                var x=Clone(c);x[0].Parts[0].Quantity+=1;
                return Rejected(()=>Plan(x,Array.Empty<OwnedPart>(),"EK01"));
            });
            Check("REJECT substituted valid physical part under copied SHA",()=>{
                var x=Clone(c);x[0].Parts[0].Kind="Booster";x[0].Parts[0].UnitScrap=35;
                return Rejected(()=>Plan(x,Array.Empty<OwnedPart>(),"EK01"));
            });
            Check("REJECT changed approved BOM ordering without new source version",()=>{
                var x=Clone(c);var temp=x[0].Parts[0];x[0].Parts[0]=x[0].Parts[1];x[0].Parts[1]=temp;
                return Rejected(()=>Plan(x,Array.Empty<OwnedPart>(),"EK01"));
            });
            string[] requests;
            var first=FromScenario("starter-cache.json",out requests);
            var sp=Plan(c,first,requests);
            Check("authored starter cache ready then blocked",()=>sp.ReadyCount==1&&sp.BlockedCount==1);
            var second=FromScenario("siege-cache.json",out requests);
            var siege=Plan(c,second,requests);
            Check("authored siege cache EK08 ready then EK02 blocked",()=>siege.Requests[0].Ready&&!siege.Requests[1].Ready);
            var third=FromScenario("shared-gyro.json",out requests);
            var shared=Plan(c,third,requests);
            Check("authored shared gyro 2 ready 1 blocked",()=>shared.ReadyCount==2&&shared.BlockedCount==1);
            if(args.Length==2&&args[0]=="--out")
            {
                Directory.CreateDirectory(args[1]);
                foreach(string file in new[]{"starter-cache.json","siege-cache.json","shared-gyro.json"})
                    Scenario(file,c,args[1]);
            }
            Console.WriteLine("PASS_TOTAL "+passes+"/53");
            return passes==53?0:2;
        }
        catch(Exception e) {Console.Error.WriteLine(e.ToString());return 2;}
    }
}
