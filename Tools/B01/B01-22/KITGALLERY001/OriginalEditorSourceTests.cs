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
     Console.WriteLine("PASS_TOTAL "+checks+"/20");
     return 0;
   } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
