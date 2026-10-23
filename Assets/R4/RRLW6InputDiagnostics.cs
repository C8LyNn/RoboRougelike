#if MOVSERIES_VISUAL_QA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;

namespace RoboRougelike.R4
{
    // Optional observer only: never queues input, invokes controls or changes resources.
    public sealed class RRLW6InputDiagnostics : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frame;public string state;public bool pressed,held,escape,controls,available;
            public int ammo;public float reloadReadyAt;
        }
        [Serializable] public sealed class Result
        {
            public string method="QA-only native input observer; no input or gameplay mutations";
            public int tEvents,tPressFrames,tHeldFrames,escapePressFrames,guiTEvents,guiEscapeEvents,lastAmmo=-1;
            public float lastReloadReadyAt;
            public List<Sample> samples=new List<Sample>();
        }
        string path;Result result=new Result();float saveAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-w6-input-diagnostics-output");
            if(i<0||i+1>=args.Length)return;
            Directory.CreateDirectory(args[i+1]);
            var obj=new GameObject("W6 input diagnostics observer");DontDestroyOnLoad(obj);
            var observer=obj.AddComponent<RRLW6InputDiagnostics>();observer.path=Path.Combine(args[i+1],"input-diagnostics.json");
            InputSystem.onEvent+=observer.OnEvent;
        }
        void OnEvent(InputEventPtr evt,InputDevice device)
        {
            if(device is Keyboard keyboard&&evt.IsA<StateEvent>()&&keyboard.tKey.ReadValueFromEvent(evt)>0)result.tEvents++;
        }
        void LateUpdate()
        {
            var activeGun=R1RunController.Instance?.Player?.GetComponentInChildren<RRLShotgunBinding>();
            result.lastAmmo=activeGun?.State.Ammo??-1;result.lastReloadReadyAt=activeGun?.State.ReloadReadyAt??0;
            var keyboard=Keyboard.current;bool pressed=keyboard?.tKey.wasPressedThisFrame==true;
            bool held=keyboard?.tKey.isPressed==true,escape=keyboard?.escapeKey.wasPressedThisFrame==true;
            if(pressed)result.tPressFrames++;if(held)result.tHeldFrames++;if(escape)result.escapePressFrames++;
            if((pressed||held||escape)&&result.samples.Count<256)
            {
                var run=R1RunController.Instance;var gun=run?.Player?.GetComponentInChildren<RRLShotgunBinding>();
                result.samples.Add(new Sample{frame=Time.frameCount,state=run?.Progress?.State.ToString(),pressed=pressed,held=held,escape=escape,
                    controls=run?.Player?.GetComponent<RRLWeaponControls>()?.isActiveAndEnabled==true,available=gun?.Available==true,
                    ammo=gun?.State.Ammo??-1,reloadReadyAt=gun?.State.ReloadReadyAt??0});
            }
            if(Time.unscaledTime>=saveAt){saveAt=Time.unscaledTime+.25f;Save();}
        }
        void OnGUI()
        {
            var evt=Event.current;if(evt==null||evt.rawType!=EventType.KeyDown)return;
            if(evt.keyCode==KeyCode.T||evt.character=='t'||evt.character=='T')result.guiTEvents++;
            if(evt.keyCode==KeyCode.Escape)result.guiEscapeEvents++;
        }
        void Save(){if(path!=null)File.WriteAllText(path,JsonUtility.ToJson(result,true));}
        void OnDestroy(){InputSystem.onEvent-=OnEvent;Save();}
        void OnApplicationQuit()=>Save();
    }
}
#endif
