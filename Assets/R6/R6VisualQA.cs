#if R6_VISUAL_QA
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using RoboRougelike.R1;
using RoboRougelike.R3;
namespace RoboRougelike.R6
{
    // Separate QA executable only. Uses game APIs; never claims native mouse/keyboard validation.
    public sealed class R6VisualQA : MonoBehaviour
    {
        int stage;float next;
        string output;
        R3VehicleModel original;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-r6-visual-output");
            if(at<0||at+1>=args.Length)return;
            Application.runInBackground=true;
            SceneManager.sceneLoaded+=(scene,mode)=>new GameObject("R6 visual QA fixture").AddComponent<R6VisualQA>().Initialize(args[at+1]);
        }
        void Initialize(string directory){output=directory;Directory.CreateDirectory(output);next=Time.unscaledTime+2;}
        void Capture(string name)
        {
            if(!Environment.GetCommandLineArgs().Contains("-r6-qa-no-capture"))ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
            Debug.Log("[R6 VISUAL QA] "+name);next=Time.unscaledTime+1.2f;
        }
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Update()
        {
            if(stage==15&&Time.unscaledTime>=next)
            {
                Debug.Log("[R6 VISUAL QA] Empty scene: drives="+FindObjectsByType<R6VehicleDrive>(FindObjectsSortMode.None).Length+", cameras="+FindObjectsByType<Camera>(FindObjectsSortMode.None).Length);
                enabled=false;Application.Quit(0);return;
            }
            var run=R1RunController.Instance;if(run?.Progress==null||run.Transitioning||Time.unscaledTime<next)return;
            try
            {
                switch(stage)
                {
                    case 0:run.SelectVehicle(R3VehicleKind.Light);Capture("qa-menu");break;
                    case 1:Check(run.OpenSandboxGarage(),"Open workshop");next=Time.unscaledTime+1;break;
                    case 2:
                        Check(run.GarageBuy(new Vector3Int(1,1,0),R3PartKind.Armor),"Mount armor");original=run.VehicleModel;
                        run.Garage.SetTool(RoboRougelike.R4_1.R41GarageTool.Inspect);run.Garage.Select(new Vector3Int(1,1,0));
                        next=Time.unscaledTime+1;break;
                    case 3:Capture("qa-garage");break;
                    case 4:Check(run.EnterTestRange(),"Enter range");next=Time.unscaledTime+1;break;
                    case 5:Capture("qa-range");break;
                    case 6:
                        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel);
                        wheel.Damageable.ApplyDamage(10000,wheel.transform.position,Vector3.left);next=Time.unscaledTime+.5f;break;
                    case 7:
                        Check(run.Player.GetComponent<R6VehicleDrive>().Wheels==3,"Wheel telemetry after loss");Capture("qa-range-damaged");break;
                    case 8:Check(run.ResetTestRange(),"Reset range");next=Time.unscaledTime+1;break;
                    case 9:
                        Check(run.Player.GetComponent<R6VehicleDrive>().Wheels==4,"Reset restores wheels");Capture("qa-range-reset");break;
                    case 10:run.TogglePause();next=Time.unscaledTime+.4f;break;
                    case 11:Capture("qa-range-pause");break;
                    case 12:Check(run.ReturnFromTestRange(),"Return workshop");next=Time.unscaledTime+1;break;
                    case 13:
                        Check(ReferenceEquals(run.VehicleModel,original)&&original.Parts.Count==21&&original.Scrap==595,"Workshop state preserved");Capture("qa-garage-return");break;
                    case 14:
                        File.WriteAllText(Path.Combine(output,"visual-qa-result.json"),"{\"assertionsPassed\":true,\"method\":\"standalone API fixture, not native input\",\"parts\":21,\"scrap\":595,\"lostWheelCount\":3,\"resetWheelCount\":4,\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"graphics\":\""+SystemInfo.graphicsDeviceType+"\"}");
                        if(Environment.GetCommandLineArgs().Contains("-r6-qa-clean-scene"))
                        {
                            DontDestroyOnLoad(gameObject);
                            foreach(var root in run.gameObject.scene.GetRootGameObjects())Destroy(root);
                            next=Time.unscaledTime+3;stage=15;return;
                        }
                        Debug.Log("[R6 VISUAL QA] PASSED");enabled=false;Application.Quit(0);return;
                }
                stage++;
            }
            catch(Exception error){Debug.LogException(error);enabled=false;Application.Quit(2);}
        }
    }
}
#endif
