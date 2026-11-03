#if B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using RoboRougelike.R3;
namespace RoboRougelike.R1
{
    // Offscreen, explicitly requested QA; does not read input or touch save data.
    public sealed class W27GarageRenderQA:MonoBehaviour
    {
        [Serializable] sealed class Result{public string status="RUNNING",error="",method="real Player garage camera to RenderTexture; API setup, no human input";public int redWireframes;public bool ghostPresent;}
        readonly Result result=new Result();string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-w27-garage-render-qa");if(i<0||i+1>=args.Length||!Application.isBatchMode)return;var go=new GameObject("W27 garage render QA");DontDestroyOnLoad(go);go.AddComponent<W27GarageRenderQA>().directory=args[i+1];}
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;StartCoroutine(Run());}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {var action=Exercise();bool ok=true;while(true){bool next=false;object value=null;try{next=action.MoveNext();if(next)value=action.Current;}catch(Exception e){ok=false;result.error=e.ToString();}if(!next||!ok)break;yield return value;}result.status=ok?"PASS":"FAIL";File.WriteAllText(Path.Combine(directory,"garage-render.json"),JsonUtility.ToJson(result,true));Application.Quit(ok?0:1);}
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+45;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run?.Progress!=null,"game bootstrap");run.SelectVehicle(R3VehicleKind.Light);Require(run.OpenSandboxGarage(),"open garage");
            run.Garage.SelectedPart=R3PartKind.Wheel;run.Garage.Select(new Vector3Int(1,1,1));result.ghostPresent=run.Garage.Preview!=null;
            foreach(var line in run.Player.GetComponentsInChildren<LineRenderer>())if(line.name=="W27 occupied cell")result.redWireframes++;
            Require(result.ghostPresent&&result.redWireframes==20,"missing actual overlay geometry");
            yield return null;yield return null;
            var camera=Camera.main;Require(camera!=null,"missing main camera");
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);rt.Create();camera.rect=new Rect(0,0,1,1);camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(directory,"garage-render.png"),image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
