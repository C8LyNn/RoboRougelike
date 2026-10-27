#if MOV009_VISUAL_QA
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.MOV009Integration
{
    // Compile-time isolated standalone fixture; APIs and engine captures, not native input.
    public sealed class MOV009VisualQA : MonoBehaviour
    {
        int stage;float next;string output;R3VehicleModel original;
        float fullY,raisedY;int damagedSupports;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-mov009-qa-output");
            if(at<0||at+1>=args.Length)return;Application.runInBackground=true;
            SceneManager.sceneLoaded+=(s,m)=>new GameObject("MOV009 QA fixture").AddComponent<MOV009VisualQA>().Initialize(args[at+1]);
        }
        void Initialize(string directory){output=directory;Directory.CreateDirectory(output);next=Time.unscaledTime+2;}
        void Capture(string name)
        {
            if(!Environment.GetCommandLineArgs().Contains("-mov009-no-capture"))ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
            Debug.Log("[MOV009 QA] "+name);next=Time.unscaledTime+1;
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Update()
        {
            var run=R1RunController.Instance;if(run?.Progress==null||run.Transitioning||Time.unscaledTime<next)return;
            try
            {
                switch(stage)
                {
                    case 0:run.SelectVehicle(R3VehicleKind.Light);Check(run.OpenSandboxGarage(),"Open garage");next=Time.unscaledTime+1;break;
                    case 1:
                        foreach(int x in new[]{-1,1})foreach(int z in new[]{-3,3})Check(run.GarageBuy(new Vector3Int(x,0,z),R3PartKind.Suspension),"Buy actual suspension part");
                        original=run.VehicleModel;run.Garage.SelectedPart=R3PartKind.Suspension;Capture("qa-garage");break;
                    case 2:Check(run.EnterTestRange(),"Enter range");next=Time.unscaledTime+2;break;
                    case 3:
                        Check(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports==4,"Four working physical supports");
                        fullY=run.Player.transform.position.y;Capture("qa-full-support");break;
                    case 4:Check(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.2f),"Set ride height");next=Time.unscaledTime+2.5f;break;
                    case 5:
                        raisedY=run.Player.transform.position.y;Check(raisedY>fullY+.12f,"Ride height moved by springs");Capture("qa-raised-height");break;
                    case 6:
                        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Suspension);
                        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);next=Time.unscaledTime+1.5f;break;
                    case 7:
                        damagedSupports=run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports;Check(damagedSupports==3,"Dead module cannot support");Capture("qa-one-support-lost");break;
                    case 8:Check(run.ResetTestRange(),"Reset range");next=Time.unscaledTime+1.5f;break;
                    case 9:
                        Check(run.Player.GetComponent<MOV009VehicleAdapter>().OperationalSupports==4,"Reset restores supports");run.TogglePause();Capture("qa-height-controls");break;
                    case 10:Check(run.ReturnFromTestRange(),"Return garage");next=Time.unscaledTime+1;break;
                    case 11:
                        Check(ReferenceEquals(run.VehicleModel,original)&&original.Parts.Count==24&&original.Scrap==500&&original.Inventory.Count==0&&original.SuspensionHeightOffset==0,"Original model/resources preserved");Capture("qa-garage-return");break;
                    case 12:
                        File.WriteAllText(Path.Combine(output,"visual-qa-result.json"),JsonUtility.ToJson(new Result{assertionsPassed=true,method="standalone API fixture and engine screenshots; not native input",fullHeight=fullY,raisedHeight=raisedY,damagedSupports=damagedSupports,parts=24,scrap=500,graphics=SystemInfo.graphicsDeviceType.ToString()},true));
                        enabled=false;Application.Quit(0);return;
                }
                stage++;
            }
            catch(Exception error){Debug.LogException(error);enabled=false;Application.Quit(2);}
        }
        [Serializable] sealed class Result
        {
            public bool assertionsPassed;public string method,graphics;public float fullHeight,raisedHeight;public int damagedSupports,parts,scrap;
        }
    }
}
#endif
