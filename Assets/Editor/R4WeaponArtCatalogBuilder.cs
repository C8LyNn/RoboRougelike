using System;
using System.IO;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using UnityEditor;
using UnityEngine;

public static class R4WeaponArtCatalogBuilder
{
    public static void Create()
    {
        const string destination="Assets/Resources/R4Art/WeaponCatalog.asset";
        if(File.Exists(destination)||AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination)!=null)
            throw new InvalidOperationException("Catalog already exists; refusing to overwrite it.");
        var kinds=new[]{R4WeaponKind.Basic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun,R4WeaponKind.Railgun,
            R4WeaponKind.BeamCutter,R4WeaponKind.RocketLauncher,R4WeaponKind.RotarySaw,R4WeaponKind.EmpEmitter};
        var catalog=ScriptableObject.CreateInstance<R4WeaponArtCatalog>();
        catalog.Models=new R4WeaponArtEntry[kinds.Length];
        for(int i=0;i<kinds.Length;i++)
        {
            string name="R4W_"+kinds[i];
            var model=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/R4Intake/Weapons/{name}/{name}.fbx");
            if(model==null)throw new InvalidOperationException("Missing imported model: "+name);
            catalog.Models[i]=new R4WeaponArtEntry{Kind=kinds[i],Model=model};
        }
        Directory.CreateDirectory("Assets/Resources/R4Art");
        AssetDatabase.CreateAsset(catalog,destination);AssetDatabase.SaveAssets();
        Debug.Log("R4_WEAPON_ART_CATALOG_CREATED models="+catalog.Models.Length);
    }
}
