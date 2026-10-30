using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01KitGallery001;
using UnityEngine;

internal class OriginalEditorSourceTests
{
 static int checks=0;
 static KitCatalog FromFile()=>JsonUtility.FromJson<KitCatalog>(
    File.ReadAllText("Assets/B01KitGallery001/Data/KitGallery.json"));
 static KitCatalog Clone(KitCatalog input)=>JsonUtility.FromJson<KitCatalog>(
    JsonSerializer.Serialize(input,new JsonSerializerOptions{IncludeFields=true}));
 static bool IsRejected(Action action)
 { try {action();return false;} catch(InvalidOperationException){return true;} }
 static void Check(string name,Func<bool> result)
 {
   if (!result()) throw new Exception("RED "+name);
   checks++;Console.WriteLine("PASS "+name);
 }
 public static int Main()
 {
   try {
     var d=FromFile();
     Check("actual source parses 10 offers",()=>KitGalleryRules.Validate(d)==10);
     Check("source pinned",()=>d.source_sha==KitGalleryRules.SourceSha);
     Check("all kit identifiers unique",()=>d.offers.Select(x=>x.id).Distinct().Count()==10);
     Check("exact EK08 235 scrap",()=>d.offers[7].price_scrap==235);
     Check("exact EK08 5 items",()=>d.offers[7].pieces==5);
     Check("case-insensitive id search",()=>KitGalleryRules.Matches(d.offers[7],"eK08"));
     Check("case-insensitive role search",()=>KitGalleryRules.Matches(d.offers[7],"HEAVY-breach"));
     Check("no accidental search result",()=>!KitGalleryRules.Matches(d.offers[7],"surprise"));
     Check("TSV includes original weapons",()=>KitGalleryRules.ToTsv(d.offers[7]).Contains("RocketLauncher"));
     Check("real Unity-style TextAsset loader",()=>UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(KitGalleryWindow.AssetPath)!=null);
     Check("reject SHA tamper",()=>{var x=Clone(d);x.source_sha="wrong";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject live-game flag",()=>{var x=Clone(d);x.reference_only=false;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject kit price tamper",()=>{var x=Clone(d);x.offers[7].price_scrap++;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject piece-count tamper",()=>{var x=Clone(d);x.offers[7].pieces++;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject three gun types",()=>{var x=Clone(d);x.offers[0].parts=new[]{x.offers[7].parts[0],x.offers[7].parts[1],x.offers[6].parts[0]};x.offers[0].price_scrap=x.offers[0].parts.Sum(p=>p.unit_scrap*p.count);x.offers[0].pieces=3;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject core part",()=>{var x=Clone(d);x.offers[0].parts[0].kind="Core";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject duplicate part entry",()=>{var x=Clone(d);x.offers[0].parts[1]=x.offers[0].parts[0];return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject invalid qty",()=>{var x=Clone(d);x.offers[0].parts[0].count=-1;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject wrong kit identity",()=>{var x=Clone(d);x.offers[0].id="EK99";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject weapon on non-gun",()=>{var x=Clone(d);x.offers[0].parts[0].weapon="Shotgun";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("comparison EK01 to EK08 125 scrap",()=>KitComparisonRules.Compare(d.offers[0],d.offers[7]).deltaScrap==125);
     Check("comparison physical items delta is zero",()=>KitComparisonRules.Compare(d.offers[0],d.offers[7]).deltaPieces==0);
     Check("reverse comparison negative 125",()=>KitComparisonRules.Compare(d.offers[7],d.offers[0]).deltaScrap==-125);
     Check("all 90 ordered comparisons",()=>{
        int valid=0;
        for(int i=0;i<10;i++)for(int j=0;j<10;j++)if(i!=j)
            if(KitComparisonRules.Compare(d.offers[i],d.offers[j]).parts.Length>0)valid++;
        return valid==90;
     });
     Check("left-right delta price anti-symmetry",()=>{
        for(int i=0;i<10;i++)for(int j=0;j<10;j++)if(i!=j)
            if(KitComparisonRules.Compare(d.offers[i],d.offers[j]).deltaScrap !=
              -KitComparisonRules.Compare(d.offers[j],d.offers[i]).deltaScrap)return false;
        return true;
     });
     Check("part list sorted for reproducible TSV",()=>{
        var rows=KitComparisonRules.Compare(d.offers[0],d.offers[7]).parts;
        return rows.Select(x=>x.key).SequenceEqual(rows.Select(x=>x.key).OrderBy(x=>x,StringComparer.Ordinal));
     });
     Check("comparison TSV includes all source fields",()=>{
        var csv=KitComparisonRules.ToTsv(KitComparisonRules.Compare(d.offers[0],d.offers[7]));
        return csv.Contains("Gun:RocketLauncher") && csv.Contains("CrawlerTrack");
     });
     Check("no compare same kit",()=>IsRejected(()=>KitComparisonRules.Compare(d.offers[0],d.offers[0])));
     Check("no compare null side",()=>IsRejected(()=>KitComparisonRules.Compare(null,d.offers[0])));
     Check("zero or negative count rejected",()=>{
        var x=Clone(d);x.offers[0].parts[0].count=0;
        return IsRejected(()=>KitComparisonRules.Compare(x.offers[0],x.offers[7]));
     });
     Check("tampered source total rejected",()=>{
        var x=Clone(d);x.offers[0].price_scrap++;
        return IsRejected(()=>KitComparisonRules.Compare(x.offers[0],x.offers[7]));
     });
     Check("duplicate parts refused in side-by-side",()=>{
        var x=Clone(d);x.offers[0].parts[1]=x.offers[0].parts[0];
        return IsRejected(()=>KitComparisonRules.Compare(x.offers[0],x.offers[7]));
     });
     Check("invalid weapon on nongun refused",()=>{
        var x=Clone(d);x.offers[0].parts[0].weapon="Other";
        return IsRejected(()=>KitComparisonRules.Compare(x.offers[0],x.offers[7]));
     });
     Check("reference price conflict refused",()=>{
        var x=Clone(d);x.offers[7].parts[2].unit_scrap++;
        x.offers[7].price_scrap++;
        return IsRejected(()=>KitComparisonRules.Compare(x.offers[0],x.offers[7]));
     });
     Check("null missing comparison TSV rejected",()=>IsRejected(()=>KitComparisonRules.ToTsv(null)));
     Check("reject unknown valid-count part on unchanged source pin",()=>{var x=Clone(d);x.offers[0].parts[0].kind="UnlistedDrive";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject coherently forged line price and offer total",()=>{var x=Clone(d);x.offers[0].parts[0].unit_scrap+=3;x.offers[0].price_scrap+=6;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject coherently forged nonempty kit title",()=>{var x=Clone(d);x.offers[0].title="Inny zestaw";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject unknown positive-cost gun kind on pinned catalog",()=>{var x=Clone(d);x.offers[7].parts[0].weapon="UnlistedCannon";return IsRejected(()=>KitGalleryRules.Validate(x));});
     Check("reject coherent quantity, count and scrap recalculation",()=>{var x=Clone(d);x.offers[0].parts[0].count++;x.offers[0].pieces++;x.offers[0].price_scrap+=15;return IsRejected(()=>KitGalleryRules.Validate(x));});
     Console.WriteLine("PASS_TOTAL "+checks+"/40");
     return 0;
   } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
