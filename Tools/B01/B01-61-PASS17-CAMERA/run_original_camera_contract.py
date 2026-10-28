#!/usr/bin/env python3
"""B01-61 PASS17 CAMERA: compile + execute untouched source P12 camera methods,
using disposable .NET8 stubs. No Unity Test Runner or actual rendered camera.
"""
import argparse
from pathlib import Path
import re
import subprocess
import sys
import tempfile

SOURCE_REF="be2c4b6ac63e04eff5b046e50641ec8d1da4837f"
SOURCE_PATH="Assets/Pack/P12_PipelineSafeCamera.cs"
SOURCE_BLOB="8519938f5734487d0350634cb6d81679a6434d62"
METHODS=[
 "void EnsureRig()",
 "public void SetGameplayInputEnabled(bool active)",
 "public void Configure(Transform root,Transform cameraPivot)",
 "void Update()",
 "void LateUpdate()",
 "void ApplyCursor()"
]
def cmd(args,cwd=None):
    p=subprocess.run(args,cwd=cwd,capture_output=True,text=True)
    if p.returncode:raise RuntimeError(f"COMMAND_FAIL {args}: {p.stderr[:500]}")
    return p.stdout.strip()
def original_method(src,signature):
    found=re.search(r"\b"+re.escape(signature)+r"\s*\{",src)
    if not found:raise RuntimeError("ORIGINAL_METHOD_NOT_FOUND:"+signature)
    beginning=src.index("{",found.start())
    depth=0
    for n in range(beginning,len(src)):
        if src[n]=="{":depth+=1
        elif src[n]=="}":
            depth-=1
            if depth==0:return src[found.start():n+1]
    raise RuntimeError("UNBALANCED_SOURCE:"+signature)

PROJECT='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>\n'
HOST=r'''
using System;
using UnityEngine;
using B01.CAMERA001;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnityEngine {
    public class MonoBehaviour {
        public readonly GameObject gameObject = new();
        private readonly Camera camera=new();
        public T? GetComponent<T>() where T:class {
            if(typeof(T)==typeof(Camera))return (T)(object)camera;
            if(typeof(T)==typeof(Camera001Rig))return (T?)(object?)gameObject.rig;
            return null;
        }
    }
    public class GameObject {
        public Camera001Rig? rig;
        public T AddComponent<T>() where T:class,new() {
            if(typeof(T)!=typeof(Camera001Rig))throw new Exception("Wrong original component");
            rig=new Camera001Rig();return (T)(object)rig;
        }
    }
    public sealed class Camera {}
    public sealed class Collider {}
    public struct Bounds {}
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public float magnitude=>MathF.Sqrt(x*x+y*y+z*z);
    }
    public struct Vector2 {
        public float x,y;
        public Vector2(float a,float b){x=a;y=b;}
        public static Vector2 zero=>new(0,0);
    }
    public class Rigidbody {public Vector3 linearVelocity;}
    public class Transform {
        public Vector3 eulerAngles;
        public Rigidbody? body;
        public T? GetComponent<T>() where T:class =>
            typeof(T)==typeof(Rigidbody)?(T?)(object?)body:null;
    }
    public static class Application {public static bool isBatchMode=true;}
    public enum CursorLockMode {None,Locked}
    public static class Cursor {
        public static CursorLockMode lockState=CursorLockMode.None;
        public static bool visible=true;
    }
    public enum KeyCode {Escape}
    public static class Input {
        public static float dx,dy,scroll;
        public static bool esc;
        public static float GetAxis(string axis)=>axis=="Mouse X"?dx/8:axis=="Mouse Y"?dy/8:0;
        public static bool GetKeyDown(KeyCode code)=>code==KeyCode.Escape&&esc;
        public static Vector2 mouseScrollDelta=>new(0,scroll/120);
    }
    public static class Time {public static float deltaTime=1f/60;}
    public static class Mathf {
        public const float Infinity=float.PositiveInfinity;
        public static float Max(float a,float b)=>MathF.Max(a,b);
        public static float Clamp(float value,float min,float max)=>MathF.Max(min,MathF.Min(max,value));
        // Unity's documented GameMath Mathf.SmoothDamp iterative approximation.
        // This is an intentionally small surrogate, not Unity native physics.
        public static float SmoothDamp(float current,float target,ref float velocity,float smoothTime,float maxSpeed,float dt) {
            smoothTime=Max(.0001f,smoothTime);
            float omega=2f/smoothTime;
            float x=omega*dt;
            float exp=1f/(1f+x+.48f*x*x+.235f*x*x*x);
            float change=current-target;
            float originalTarget=target;
            float maxChange=maxSpeed*smoothTime;
            change=Clamp(change,-maxChange,maxChange);
            target=current-change;
            float temp=(velocity+omega*change)*dt;
            velocity=(velocity-omega*temp)*exp;
            float output=target+(change+temp)*exp;
            if((originalTarget-current>0)==(output>originalTarget)) {
                output=originalTarget;
                velocity=(output-originalTarget)/dt;
            }
            return output;
        }
    }
}
namespace UnityEngine.InputSystem {
    public sealed class ButtonControl {public bool wasPressedThisFrame;}
    public sealed class Vector2Control {
        public Vector2 value;
        public Vector2 ReadValue()=>value;
    }
    public sealed class Keyboard {
        public static Keyboard? current=new();
        public ButtonControl escapeKey=new();
    }
    public sealed class Mouse {
        public static Mouse? current=new();
        public Vector2Control delta=new(),scroll=new();
    }
}
namespace B01.CAMERA001 {
    public sealed class Camera001Rig {
        public bool enabled=true;
        public Func<Collider,bool>? WorldColliderFilter;
        public bool RequireFullHullFraming,ReceiverResponsiveMotion;
        public Func<Transform,Bounds>? ReceiverHullBounds;
        public float ReceiverHorizontalDamping,ReceiverVerticalDamping,ReceiverFramingDamping;
        public float ReceiverZoomMaximum,ReceiverFollowSpeed;
        public int steps,resets,configures;
        public float zoom,yaw,pitch,lastDt;
        public Transform? root;
        public void Configure(Transform t,Camera? c){configures++;root=t;}
        public void ResetSmoothing(){resets++;}
        public void SetOrbit(float a,float b,float c){yaw=a;pitch=b;zoom=c;}
        public void StepCamera(float dt){steps++;lastDt=dt;}
    }
}
namespace RoboRougelike.R1 {
    public sealed class R1RunController {
        public static R1RunController? Instance;
    }
}
public sealed partial class P12PipelineSafeCamera : MonoBehaviour {
    Transform? targetRoot,pivot;
    float distance=7.8f,minDistance=3,maxDistance=45,mouseSensitivity=.12f,minPitch=-5,maxPitch=58,zoomSpeed=.015f;
    float horizontalFollowSeconds=.06f,verticalFollowSeconds=.12f,framingFollowSeconds=.22f;
    float zoomFollowSeconds=.10f;
    float yaw,pitch=18;
    float renderedDistance,zoomVelocity;
    bool zoomInitialized;
    bool cursorLocked=true,gameplayInputEnabled=true;
    Camera001Rig? rig;
    public Transform? TargetRoot=>targetRoot;
    public Camera001Rig Rig=>rig!;
    public float Desired=>distance;
    public float Smoothed=>renderedDistance;
    public bool CursorLocked=>cursorLocked;
    public bool Enabled=>gameplayInputEnabled;
    static bool IsCameraWorld(Collider c)=>true;
    static Bounds StableVehicleBounds(Transform t)=>new();
    public void SimulateUpdate()=>Update();
    public void SimulateLate()=>LateUpdate();
@@ORIGINAL_METHODS@@
}
public static class Program {
    static int asserts;
    static void Is(bool c,string name){
        asserts++;
        if(!c)throw new InvalidOperationException("CAMERA_CONTRACT_RED: "+name);
    }
    static bool Finite(float a)=>!float.IsInfinity(a)&&!float.IsNaN(a);
    static void Mouse(float dx,float dy,float scroll,bool escape=false){
        Input.dx=dx;Input.dy=dy;Input.scroll=scroll;Input.esc=escape;
        var m=UnityEngine.InputSystem.Mouse.current!;
        m.delta.value=new Vector2(dx,dy);
        m.scroll.value=new Vector2(0,scroll);
        UnityEngine.InputSystem.Keyboard.current!.escapeKey.wasPressedThisFrame=escape;
    }
    public static int Main() {
      try {
        var camera=new P12PipelineSafeCamera();
        var target=new Transform{body=new Rigidbody{linearVelocity=new Vector3(3,0,4)}};
        var pivot=new Transform();
        camera.Configure(target,pivot);
        Is(camera.TargetRoot==target,"Configure must own requested player target");
        Is(camera.Rig.configures==1,"one rig Configure");
        Is(camera.Rig.enabled==false,"facade must disable independent rig LateUpdate");
        Is(camera.Rig.ReceiverResponsiveMotion,"preserve responsive follow capability");
        Is(camera.Rig.ReceiverZoomMaximum==45,"max zoom bound transferred to rig");
        Time.deltaTime=1f/60;Mouse(0,0,0);
        camera.SimulateLate();
        Is(camera.Rig.steps==1,"facade must step rig exactly once per frame");
        Is(MathF.Abs(camera.Smoothed-7.8f)<.00001f,"initial zoom initialized");
        Is(camera.Rig.ReceiverFollowSpeed==5,"speed comes from actual target Rigidbody");

        Mouse(0,0,-30000);camera.SimulateUpdate();
        Is(camera.Desired==45f,"large scroll out is bounded at 45m");
        camera.SimulateLate();
        Is(camera.Smoothed>7.8f&&camera.Smoothed<45f,"camera zoom must smooth, not teleport");
        int before=camera.Rig.steps;
        for(int frame=0;frame<160;frame++) {
            Time.deltaTime=frame%3==0?1f/120:frame%3==1?1f/60:1f/30;
            Mouse(0,0,0);camera.SimulateUpdate();camera.SimulateLate();
            Is(Finite(camera.Smoothed)&&camera.Smoothed<=45&&camera.Smoothed>=7.8f,"finite increasing smooth zoom");
        }
        Is(camera.Rig.steps==before+160,"single-step per actual LateUpdate");
        Is(MathF.Abs(camera.Smoothed-45)<.025f,"zoom converges within 160 frames");

        Mouse(0,0,30000);camera.SimulateUpdate();
        Is(camera.Desired==3f,"large scroll inward is bounded at 3m");
        for(int frame=0;frame<160;frame++) {
            Time.deltaTime=1f/60;Mouse(0,0,0);camera.SimulateUpdate();camera.SimulateLate();
        }
        Is(MathF.Abs(camera.Smoothed-3)<.025f,"inward zoom converges");
        Mouse(10,200,0);camera.SimulateUpdate();camera.SimulateLate();
        Is(camera.Rig.pitch==-5,"negative pitch is clamped");
        Mouse(10,-10000,0);camera.SimulateUpdate();camera.SimulateLate();
        Is(camera.Rig.pitch==58,"positive pitch is clamped");
        Is(camera.Rig.yaw>1,"mouse orbit is direct not disabled");

        var stopped=camera.Rig.steps;
        var oldDesired=camera.Desired;
        camera.SetGameplayInputEnabled(false);
        Mouse(1,1,3000);camera.SimulateUpdate();camera.SimulateLate();
        Is(camera.Rig.steps==stopped,"paused camera cannot step");
        Is(camera.Desired==oldDesired,"paused zoom must reject input");
        Is(!camera.Enabled,"paused facade disables gameplay input");

        camera.SetGameplayInputEnabled(true);
        Is(camera.Rig.resets==1,"only one ResetSmoothing after paused->resumed");
        Mouse(0,0,0);camera.SimulateLate();
        Is(camera.Rig.steps==stopped+1,"resume reactivates single camera stepping");
        Is(camera.TargetRoot==target,"pause/resume cannot replace target root");
        camera.SetGameplayInputEnabled(true);
        Is(camera.Rig.resets==1,"repeated enable must not reset smoothing");

        // No player target + pivot means no accidental step even while enabled.
        camera.Configure(new Transform(),null!);
        stopped=camera.Rig.steps;
        camera.SimulateLate();
        Is(camera.Rig.steps==stopped,"missing camera pivot must block LateUpdate");
        camera.Configure(target,pivot);
        camera.Rig.steps=0;

        var random=new Random(610017);
        float referenceDesired=camera.Desired;
        for(int frame=0;frame<2400;frame++) {
            Time.deltaTime=frame%4==0?1f/144:frame%4==1?1f/60:frame%4==2?1f/30:1f/24;
            float scroll=(float)(random.Next(-5,6)*120);
            float dx=(float)(random.Next(-8,9));
            float dy=(float)(random.Next(-8,9));
            Mouse(dx,dy,scroll);
            referenceDesired=MathF.Max(3,MathF.Min(45,referenceDesired-scroll*.015f));
            camera.SimulateUpdate();camera.SimulateLate();
            Is(camera.Desired==referenceDesired,"input scroll clamp drift at seeded frame "+frame);
            Is(Finite(camera.Smoothed),"nonfinite filtered zoom at seeded frame "+frame);
            Is(camera.Rig.zoom==camera.Smoothed,"only filtered zoom can reach orbit at "+frame);
            Is(camera.Rig.steps==frame+1,"actual one rig step at "+frame);
            Is(camera.Rig.pitch>=-5&&camera.Rig.pitch<=58,"pitch escapes clamp");
        }
        // The physical camera viewport movement/render is deliberately NOT modeled.
        Console.WriteLine("ORIGINAL_CAMERA_CSHARP=PASS | ASSERTIONS="+asserts+
          " | SEEDED_FRAMES=2400 | INPUT_SCROLL_CLAMP=PASS | SMOOTH_ZOOM=PASS | PAUSE_RESUME=PASS | SINGLE_RIG_STEP=PASS");
        return 0;
      }catch(Exception e){Console.Error.WriteLine(e);return 2;}
    }
}
'''
def run(args):
    p=subprocess.run(args,capture_output=True,text=True)
    print("CMD="+ " ".join(args))
    if p.stdout:print(p.stdout[-1900:])
    if p.stderr:print("STDERR="+p.stderr[-2500:])
    print("EXIT="+str(p.returncode))
    return p.returncode

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    args=parser.parse_args()
    root=str(Path(args.repo).resolve())
    assert cmd(["git","-C",root,"rev-parse",SOURCE_REF+"^{commit}"])==SOURCE_REF
    sha=cmd(["git","-C",root,"rev-parse",SOURCE_REF+":"+SOURCE_PATH])
    if sha!=SOURCE_BLOB:raise RuntimeError("ORIGINAL_CAMERA_SOURCE_CHANGED: "+sha)
    src=cmd(["git","-C",root,"show",SOURCE_REF+":"+SOURCE_PATH])
    extracted={name:original_method(src,name) for name in METHODS}
    exact="\n".join(extracted.values())
    for required in ("rig.enabled=false;","rig.StepCamera(Time.deltaTime);",
        "distance=Mathf.Clamp(distance-scroll*zoomSpeed,minDistance,maxDistance);",
        "if(active&&!gameplayInputEnabled&&rig!=null)rig.ResetSmoothing();",
        "Mathf.SmoothDamp(renderedDistance,distance,ref zoomVelocity"):
        if required not in exact:raise RuntimeError("REQUIRED_ORIGINAL_CAMERA_BEHAVIOR_NOT_PRESENT:"+required)
    print("CAMERA_ORIGINAL_SOURCE_SHA="+SOURCE_REF+" | BLOB="+SOURCE_BLOB+" | UNMODIFIED_METHODS="+str(len(METHODS)))
    for label,define in (("LEGACY",False),("INPUT_SYSTEM",True)):
        for mutation in (False,True):
            with tempfile.TemporaryDirectory(prefix="b0161_cam_") as temp:
                project=Path(temp)
                (project/"Contract.csproj").write_text(PROJECT,encoding="utf8")
                methods=exact
                if mutation:
                    methods=methods.replace("distance=Mathf.Clamp(distance-scroll*zoomSpeed,minDistance,maxDistance);",
                       "distance=distance-scroll*zoomSpeed;")
                    if methods==exact:raise RuntimeError("MUTATION_DID_NOT_APPLY")
                (project/"Program.cs").write_text(HOST.replace("@@ORIGINAL_METHODS@@",methods),encoding="utf8")
                command=["dotnet","run","--project",str(project/"Contract.csproj"),
                         "--configuration","Release","--no-launch-profile"]
                if define:command.append("-p:DefineConstants=ENABLE_INPUT_SYSTEM")
                exit_code=run(command)
                if mutation and exit_code==0:raise RuntimeError("DID_NOT_REJECT_BROKEN_ZOOM_CLAMP:"+label)
                if not mutation and exit_code!=0:raise RuntimeError("REAL_ORIGINAL_CAMERA_TEST_FAILED:"+label)
                print("EXECUTION_"+label+"_"+("BROKEN_ZOOM_NEGATIVE_REJECTED" if mutation else "SOURCE_GREEN")+"=PASS")
    print("PACKAGE=B01-61-P17-CAMERA-STATE-R1 | ORIGINAL_CSHARP_BRANCHES=2/2_PASS | SYNTHETIC_MUTATIONS=2/2_REJECTED | UNITY_NATIVE=NOT_RUN | CANONICAL_GAME_CHANGED=NO")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as exc:
        print("CAMERA_HARNESS_FAIL="+repr(exc),file=sys.stderr)
        sys.exit(2)
