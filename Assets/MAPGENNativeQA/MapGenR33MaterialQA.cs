using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.MAPGEN001;
using UnityEngine;

// Standalone QA only: no material assignment, vehicle or gameplay integration.
public sealed class MapGenR33MaterialQA : MonoBehaviour
{
    [Serializable] public sealed class Case
    {
        public string recipe,status="NOT_RUN",shader,materialName,screenshot;
        public int seed=7,renderers,uniqueMaterials,materialCountAfterGenerate;
        public bool supportedShader,oldRootsRemoved=true,clearedRootsRemoved=true,sameMaterial=true;
        public List<int> materialCountsAfterRegenerate=new List<int>();
    }
    [Serializable] public sealed class Result
    {
        public string producer="7334f3b1d8cd3a976098faa4e683f4de3d9a592f",status="Running",pipeline;
        public bool materialOverride=false;
        public int baselineMaterials,afterClearMaterials,afterDestroyMaterials;
        public List<Case> cases=new List<Case>(); public List<string> errors=new List<string>();
    }
    const string OwnedName="MAPGEN001_Generated_URP_Blocks_Runtime";
    Result result=new Result(); string output; MapGenBuilder builder;
    int Count()=>Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.name==OwnedName);
    void Save()=>File.WriteAllText(Path.Combine(output,"material-results.json"),JsonUtility.ToJson(result,true));
    void Check(bool ok,string message){if(!ok)result.errors.Add(message);}
    MapResult Generate(MapRecipe recipe){try{return builder.Generate(recipe,7,MapGenConfig.Default,MapVehicleProfile.Light);}catch(Exception e){result.errors.Add(recipe+": "+e);return null;}}
    IEnumerator Start()
    {
        string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-mapgen-material-output");
        output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"R33-Material-QA");
        Directory.CreateDirectory(output);
        result.pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().FullName??"builtin";
        result.baselineMaterials=Count();
        var host=new GameObject("R33 source builder only"); builder=host.AddComponent<MapGenBuilder>();
        foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        {
            var row=new Case{recipe=recipe.ToString()}; result.cases.Add(row);
            int errorsBefore=result.errors.Count; var map=Generate(recipe);
            if(map==null){row.status="FAIL";Save();continue;}
            yield return null;
            var renderers=map.GeneratedRoot.GetComponentsInChildren<Renderer>();
            row.renderers=renderers.Length;row.uniqueMaterials=renderers.Select(r=>r.sharedMaterial).Distinct().Count();
            var material=renderers.FirstOrDefault()?.sharedMaterial;
            row.shader=material?.shader?.name??"none";row.materialName=material?.name??"none";
            row.supportedShader=material!=null&&material.shader!=null&&material.shader.isSupported&&row.shader.StartsWith("Universal Render Pipeline/");
            row.materialCountAfterGenerate=Count();
            Check(row.renderers>0&&row.uniqueMaterials==1&&row.supportedShader,recipe+": source renderer/material invalid");
            for(int cycle=0;cycle<3;cycle++)
            {
                var old=map.GeneratedRoot;map=Generate(recipe);if(map==null)break;
                row.oldRootsRemoved&=!old.activeSelf;
                yield return null;
                row.oldRootsRemoved&=old==null;
                row.sameMaterial&=map.GeneratedRoot.GetComponentInChildren<Renderer>().sharedMaterial==material;
                row.materialCountsAfterRegenerate.Add(Count());
                builder.Clear();yield return null;
                row.clearedRootsRemoved&=host.transform.childCount==0&&builder.Current==null;
                map=Generate(recipe);if(map==null)break;
            }
            Check(row.oldRootsRemoved&&row.clearedRootsRemoved&&row.sameMaterial&&row.materialCountsAfterRegenerate.All(n=>n==result.baselineMaterials+1),recipe+": regenerate/clear lifecycle failed");
            if(map!=null)
            {
                var camera=Camera.main;Vector3 center=map.Bounds.center;float extent=Mathf.Max(map.Bounds.size.x,map.Bounds.size.z);
                camera.transform.position=center+new Vector3(extent*.45f,extent*.9f,-extent*.7f);camera.transform.LookAt(center);
                yield return new WaitForEndOfFrame();
                row.screenshot=recipe+"-original-source-URP.png";
                ScreenCapture.CaptureScreenshot(Path.Combine(output,row.screenshot));
                yield return new WaitForSecondsRealtime(.5f);
            }
            builder.Clear();yield return null;
            row.clearedRootsRemoved&=host.transform.childCount==0;
            row.status=result.errors.Count==errorsBefore&&row.clearedRootsRemoved?"PASS":"FAIL";Save();
        }
        builder.Clear();yield return null;result.afterClearMaterials=Count();
        Destroy(host);yield return null;yield return null;
        result.afterDestroyMaterials=Count();
        Check(result.afterClearMaterials==result.baselineMaterials+1,"Unexpected builder cache count after Clear");
        Check(result.afterDestroyMaterials==result.baselineMaterials,"Owned material leaked after builder destroy");
        result.status=result.errors.Count==0&&result.cases.All(c=>c.status=="PASS")?"PASS":"FAIL";Save();
        Application.Quit(result.status=="PASS"?0:2);
    }
}
