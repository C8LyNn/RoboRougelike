using System;
using System.IO;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEditor;
using UnityEngine;

public static class R4MovementArtCatalogBuilder
{
    public static void LinkCrawlerTravel()
    {
        const string destination="Assets/Resources/R4Art/MovementCatalog.asset";
        const string source="Assets/Art/R4Intake/Movement/R4M_CrawlerTrack/R4M_CrawlerTrack.fbx";
        var catalog=AssetDatabase.LoadAssetAtPath<R4MovementArtCatalog>(destination);
        var clip=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().SingleOrDefault(c=>c.name=="Travel");
        if(catalog==null||clip==null)throw new InvalidOperationException("Missing movement catalog or crawler Travel clip.");
        if(catalog.CrawlerTravel!=null&&catalog.CrawlerTravel!=clip)throw new InvalidOperationException("Refusing to replace a different crawler clip.");
        catalog.CrawlerTravel=clip;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("R4_CRAWLER_TRAVEL_LINKED duration="+clip.length);
    }
    public static void Create()
    {
        const string destination="Assets/Resources/R4Art/MovementCatalog.asset";
        if(File.Exists(destination)||AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination)!=null)
            throw new InvalidOperationException("Catalog already exists; refusing to overwrite it.");
        var kinds=new[]{R3PartKind.Wheel,R3PartKind.CrawlerTrack,R3PartKind.HoverEngine,R3PartKind.OmniDrive,
            R3PartKind.Suspension,R3PartKind.Booster,R3PartKind.JumpDrive,R3PartKind.Blink,R3PartKind.Gyroscope,
            R3PartKind.MagneticGrapple,R3PartKind.IndependentSteering};
        var catalog=ScriptableObject.CreateInstance<R4MovementArtCatalog>();
        catalog.Models=new R4MovementArtEntry[kinds.Length];
        for(int i=0;i<kinds.Length;i++)
        {
            string name="R4M_"+kinds[i];
            var model=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/R4Intake/Movement/{name}/{name}.fbx");
            if(model==null)throw new InvalidOperationException("Missing imported model: "+name);
            catalog.Models[i]=new R4MovementArtEntry{Kind=kinds[i],Model=model};
        }
        Directory.CreateDirectory("Assets/Resources/R4Art");
        AssetDatabase.CreateAsset(catalog,destination);AssetDatabase.SaveAssets();
        Debug.Log("R4_MOVEMENT_ART_CATALOG_CREATED models="+catalog.Models.Length);
    }
}
