using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.BUILD001;
using UnityEngine;

// Observes unchanged producer demo and calls its existing Next-candidate action.
// No shader/material/geometry correction and no game integration.
public sealed class BuildR12DemoObserver : MonoBehaviour
{
    [Serializable] public sealed class Row
    {
        public int choice,requestedWidth,width,height;
        public string expected,actual,message,shader,pipelineTag,screenshot,logicStatus;
        public bool shaderSupported,errorShader;
    }
    [Serializable] public sealed class Result
    {
        public string source="489676414a3eb45fe70124098e3778a81d0aaff2",target="4c9236a4cdc27814ea2e7679872ec4a524150cc8",status="Running",method="Original BUILD demo controller in a copied QA scene; automatic API Next-candidate action, not human input";
        public List<Row> rows=new List<Row>();public List<string> errors=new List<string>();
    }
    Result result=new Result();string output;
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    void Awake()
    {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-build-qa-output");
        output=i>=0?args[i+1]:Path.Combine(Application.persistentDataPath,"BuildR12QA");Directory.CreateDirectory(output);
        Application.logMessageReceived+=OnLog;Application.runInBackground=true;
    }
    void Save()=>File.WriteAllText(Path.Combine(output,"demo-results.json"),JsonUtility.ToJson(result,true));
    void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){result.errors.Add(message);Save();}}
    IEnumerator Start()
    {
        yield return null;yield return new WaitForSecondsRealtime(.3f);
        var demo=FindFirstObjectByType<Build001Demo>();
        if(demo==null){result.errors.Add("Producer demo controller missing");result.status="FAIL";Save();Application.Quit(2);yield break;}
        var choiceField=typeof(Build001Demo).GetField("choice",Fields);
        var verdictField=typeof(Build001Demo).GetField("verdict",Fields);
        var messageField=typeof(Build001Demo).GetField("message",Fields);
        var next=typeof(Build001Demo).GetMethod("Cycle",Fields);
        foreach(int width in new[]{1280,1920})
        {
            Screen.SetResolution(width,width==1280?720:1080,false);yield return new WaitForSecondsRealtime(.4f);
            for(int index=0;index<5;index++)
            {
                var row=new Row{choice=(int)choiceField.GetValue(demo),requestedWidth=width,width=Screen.width,height=Screen.height};
                row.expected=(row.choice==1||row.choice==3?BuildPlacementResult.NoCompatibleSocket:BuildPlacementResult.Valid).ToString();
                row.actual=verdictField.GetValue(demo).ToString();row.message=messageField.GetValue(demo) as string;
                row.logicStatus=row.expected==row.actual?"PASS":"FAIL";
                var ghost=GameObject.Find("BUILD001_GHOST_PREVIEW");var material=ghost?.GetComponent<Renderer>()?.sharedMaterial;
                row.shader=material?.shader?.name??"none";row.shaderSupported=material?.shader?.isSupported??false;
                row.pipelineTag=material==null?"none":material.GetTag("RenderPipeline",false);
                row.errorShader=row.shader=="Hidden/InternalErrorShader"||!row.shaderSupported;
                row.screenshot=$"Demo-{width}-choice{row.choice}.png";result.rows.Add(row);Save();
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,row.screenshot));yield return new WaitForSecondsRealtime(.3f);
                try{next.Invoke(demo,null);}catch(Exception e){result.errors.Add("Producer Next action: "+e);}
                yield return null;
            }
        }
        result.status=result.errors.Count==0&&result.rows.All(r=>r.logicStatus=="PASS"&&!r.errorShader)?"PASS_OBSERVED":"FAIL_OBSERVED";Save();
        Application.Quit(result.status=="PASS_OBSERVED"?0:2);
    }
    void OnDestroy(){Application.logMessageReceived-=OnLog;}
}
