using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.MAPGEN001;
using UnityEngine;

// QA-only minimal host: no material/shader overrides or scene material references.
public sealed class MapgenR341MaterialsQA : MonoBehaviour
{
    [Serializable] public sealed class Row
    {
        public string recipe,status="NOT_RUN",shader,materialName,pipelineTag,screenshot;
        public int seed=7,requestedWidth,width,height,renderers,uniqueMaterials;
        public bool generated,lifecycleRan,resourceMatches,shaderSupported,oldRootsRemoved,clearRemoved,sameMaterial;
        public List<int> materialCounts=new List<int>();
    }
    [Serializable] public sealed class Result
    {
        public string source="5be31039411f89f17654b1c332f8692883fa03cc",target="4c9236a4cdc27814ea2e7679872ec4a524150cc8",status="Running",resourceName,graphics,pipeline;
        public bool resourceLoaded,resourceSupported,materialOverride=false;
        public int baselineMaterials,afterClearMaterials,afterDestroyMaterials;
        public List<Row> rows=new List<Row>();public List<string> errors=new List<string>();
    }
    const string MaterialName="MAPGEN001_Generated_URP_Blocks_Runtime";
    Result result=new Result();string output;MapGenBuilder builder;
    int Count()=>Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.name==MaterialName);
    void Save()=>File.WriteAllText(Path.Combine(output,"material-results.json"),JsonUtility.ToJson(result,true));
    void Check(bool ok,string message){if(!ok)result.errors.Add(message);}
    MapResult Generate(MapRecipe recipe,bool regenerate=false)
    {
        try{return regenerate?builder.Regenerate(recipe,7,MapGenConfig.Default,MapVehicleProfile.Light):builder.Generate(recipe,7,MapGenConfig.Default,MapVehicleProfile.Light);}
        catch(Exception e){result.errors.Add(recipe+": "+e);return null;}
    }
    IEnumerator Start()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-mapgen-material-output");output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"R341MaterialsQA");Directory.CreateDirectory(output);
        Application.runInBackground=true;Application.logMessageReceived+=OnLog;
        result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
        result.pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().FullName??"builtin";
        var resource=Resources.Load<Shader>("MAPGEN001_BlocksURP");result.resourceLoaded=resource!=null;result.resourceSupported=resource!=null&&resource.isSupported;result.resourceName=resource?.name??"none";
        result.baselineMaterials=Count();var host=new GameObject("Original R341 builder only");builder=host.AddComponent<MapGenBuilder>();
        foreach(int width in new[]{1280,1920})foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        {
            Screen.SetResolution(width,width==1280?720:1080,false);yield return new WaitForSecondsRealtime(.3f);
            var row=new Row{recipe=recipe.ToString(),requestedWidth=width,width=Screen.width,height=Screen.height};result.rows.Add(row);int errorsBefore=result.errors.Count;
            var map=Generate(recipe);if(map==null){row.status="FAIL_GENERATE";Save();continue;}
            row.generated=true;yield return null;
            var renderers=map.GeneratedRoot.GetComponentsInChildren<Renderer>();var material=renderers.FirstOrDefault()?.sharedMaterial;
            row.renderers=renderers.Length;row.uniqueMaterials=renderers.Select(r=>r.sharedMaterial).Distinct().Count();row.shader=material?.shader?.name??"none";row.materialName=material?.name??"none";
            row.resourceMatches=material!=null&&material.shader==resource;row.shaderSupported=material!=null&&material.shader.isSupported;row.pipelineTag=material?.GetTag("RenderPipeline",false)??"none";
            row.oldRootsRemoved=row.clearRemoved=row.sameMaterial=true;
            Check(row.renderers>0&&row.uniqueMaterials==1&&row.resourceMatches&&row.shaderSupported&&row.pipelineTag=="UniversalPipeline",recipe+": retained material validation failed");
            row.materialCounts.Add(Count());
            for(int cycle=0;cycle<3;cycle++)
            {
                var old=map.GeneratedRoot;map=Generate(recipe,true);if(map==null)break;
                row.oldRootsRemoved&=!old.activeSelf;yield return null;row.oldRootsRemoved&=old==null;
                row.sameMaterial&=map.GeneratedRoot.GetComponentInChildren<Renderer>().sharedMaterial==material;
                row.materialCounts.Add(Count());builder.Clear();yield return null;
                row.clearRemoved&=host.transform.childCount==0&&builder.Current==null;row.materialCounts.Add(Count());
                map=Generate(recipe);if(map==null)break;
            }
            row.lifecycleRan=row.materialCounts.Count==7;
            Check(row.lifecycleRan&&row.oldRootsRemoved&&row.clearRemoved&&row.sameMaterial&&row.materialCounts.All(n=>n==result.baselineMaterials+1),recipe+": regenerate/clear/cache lifetime failed");
            if(map!=null)
            {
                var camera=Camera.main;var center=map.Bounds.center;float extent=Mathf.Max(map.Bounds.size.x,map.Bounds.size.z);
                camera.transform.position=center+new Vector3(extent*.45f,extent*.9f,-extent*.7f);camera.transform.LookAt(center);
                yield return new WaitForEndOfFrame();row.screenshot=$"{recipe}-{width}-original-resource.png";ScreenCapture.CaptureScreenshot(Path.Combine(output,row.screenshot));yield return new WaitForSecondsRealtime(.4f);
            }
            builder.Clear();yield return null;row.clearRemoved&=host.transform.childCount==0;
            row.status=result.errors.Count==errorsBefore&&row.clearRemoved?"PASS":"FAIL";Save();
        }
        builder.Clear();yield return null;result.afterClearMaterials=Count();Destroy(host);yield return null;yield return null;result.afterDestroyMaterials=Count();
        bool anyGenerated=result.rows.Any(r=>r.generated);
        if(anyGenerated)Check(result.afterClearMaterials==result.baselineMaterials+1&&result.afterDestroyMaterials==result.baselineMaterials,"Material count after Clear/owner destruction invalid");
        result.status=result.errors.Count==0&&result.rows.Count==4&&result.rows.All(r=>r.status=="PASS")?"PASS":"FAIL";Save();Application.Quit(result.status=="PASS"?0:2);
    }
    void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){result.errors.Add(message);Save();}}
    void OnDestroy(){Application.logMessageReceived-=OnLog;}
}
