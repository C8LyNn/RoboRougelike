#!/usr/bin/env python3
"""Independent B01-61 W02 receiver of B01-32's *actual original* two-file C# emitter.
Compiles UNMODIFIED producer WheelWakeProfile + B0132WheelWakeEmitter in dotnet8
against an intentionally limited Unity API surrogate. NOT native Unity/ParticleSystem.
"""
import argparse
from pathlib import Path
import subprocess
import sys
import tempfile

PRODUCER_REF="005e177a6742634add0e3e434ca34647d590f2fd"
SOURCES={
 "Assets/B0132P17WheelWake/Runtime/WheelWakeProfile.cs":"45949c3c550f048ba8bccfe1071a29edf45d2807",
 "Assets/B0132P17WheelWake/Runtime/B0132WheelWakeEmitter.cs":"debb9f0fb7803102cfdaa78babb5de0e9a47fba8",
}
PACKAGE_ID="B01-61-W02-WHEELWAKE-RECEIVER-R1"
PROJECT='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>false</TreatWarningsAsErrors></PropertyGroup></Project>\n'
SHIM=r'''
using System;
using System.Linq;
using System.Collections.Generic;
namespace UnityEngine {
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute {}
    public class Object {
        public bool Destroyed;
        public static void Destroy(Object obj){obj.Destroyed=true;}
    }
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    }
    public struct Quaternion {
        public float X,Y,Z;
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{X=x,Y=y,Z=z};
    }
    public struct Color {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color clear=>new(0,0,0,0);
    }
    public class Shader:Object {
        public string Name="";
        public static Shader Find(string name)=>new Shader{Name=name};
    }
    public class Material:Object {
        public Shader Shader;
        public Material(Shader s){Shader=s;}
    }
    public class Component:Object {
        public GameObject gameObject=null!;
        public Transform transform=>gameObject.transform;
    }
    public class MonoBehaviour:Component {
        public bool isActiveAndEnabled=>gameObject.activeSelf;
    }
    public sealed class Transform {
        public readonly GameObject gameObject;
        public readonly List<Transform> children=new();
        public Transform? parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Transform(GameObject parent){gameObject=parent;}
        public void SetParent(Transform root,bool worldPositionStays){
            if(parent!=null)parent.children.Remove(this);
            parent=root;root.children.Add(this);
        }
    }
    public sealed class GameObject:Object {
        public string name;
        public bool activeSelf=true;
        public readonly Transform transform;
        public readonly Dictionary<Type,Component> components=new();
        public GameObject(string name){this.name=name;transform=new Transform(this);}
        public T AddComponent<T>() where T:Component,new() {
            T value=new(){gameObject=this};
            components[typeof(T)]=value;
            if(value is ParticleSystem && !components.ContainsKey(typeof(ParticleSystemRenderer)))
                components[typeof(ParticleSystemRenderer)]=new ParticleSystemRenderer{gameObject=this};
            return value;
        }
        public T? GetComponent<T>() where T:Component {
            return components.TryGetValue(typeof(T),out var val)?(T)val:null;
        }
        public T[] GetComponentsInChildren<T>() where T:Component {
            List<T> output=new();
            void Scan(Transform node) {
                foreach(Component c in node.gameObject.components.Values)if(c is T t)output.Add(t);
                foreach(var ch in node.children)Scan(ch);
            }
            Scan(transform);return output.ToArray();
        }
        public void SetActive(bool active){
            if(activeSelf==active)return;
            activeSelf=active;
            if(!active)foreach(var b in components.Values.OfType<MonoBehaviour>())
                b.GetType().GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(b,null);
        }
    }
    public enum ParticleSystemSimulationSpace {World,Local}
    public enum ParticleSystemShapeType {Cone}
    public enum ParticleSystemStopBehavior {StopEmitting,StopEmittingAndClear}
    public sealed class ParticleSystemRenderer:Component {
        public Material? sharedMaterial;
    }
    public sealed class ParticleSystem:Component {
        public sealed class MainModule {
            public bool playOnAwake,loop;
            public float duration,startLifetime,startSpeed,startSize;
            public int maxParticles;
            public ParticleSystemSimulationSpace simulationSpace;
            public Color startColor;
        }
        public sealed class EmissionModule {public bool enabled;public float rateOverTime;}
        public sealed class ShapeModule {public bool enabled;public ParticleSystemShapeType shapeType;public float radius,angle;}
        public readonly MainModule main=new();
        public readonly EmissionModule emission=new();
        public readonly ShapeModule shape=new();
        public bool isPlaying;
        public int aliveParticles,stopCount,clearCount,playCount;
        public void Play(bool withChildren) {
            isPlaying=true;
            playCount++;
            aliveParticles=Math.Max(1,aliveParticles);
        }
        public void Stop(bool withChildren,ParticleSystemStopBehavior how) {
            isPlaying=false;stopCount++;
            if(how==ParticleSystemStopBehavior.StopEmittingAndClear){aliveParticles=0;clearCount++;}
        }
    }
}
'''
TEST=r'''
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RoboRougelike.B0132P17;
public static class IndependentB0161WheelWakeReceiver {
    static int checks,negativeScenarios,frames;
    static void Check(bool value,string why) {
        checks++;
        if(!value)throw new Exception("WHEELWAKE_ORACLE_FAIL:"+why);
    }
    static bool Near(float left,float right)=>Math.Abs(left-right)<.0002f;
    static void CheckEmit(B0132WheelWakeEmitter emitter,GameObject root,
           WheelWakeMode mode,float speed,float slip,bool ground,bool enabled) {
        emitter.SetVisualInput(mode,speed,slip,ground,enabled);
        var expected=WheelWakeProfile.Evaluate(mode,speed,slip,ground,enabled);
        var sys=root.GetComponentsInChildren<ParticleSystem>();
        Check(sys.Length==2,"actual source must generate exactly two separate wheel emitters");
        Check(emitter.SystemCount==2,"emitter count");
        Check(emitter.IsEmitting==expected.Visible,"actual emitter boolean state");
        Check(Near(emitter.LastSample.RatePerSecond,expected.RatePerSecond),"last source evaluation");
        Check(sys[0].gameObject.name=="left contact wake" &&
              sys[1].gameObject.name=="right contact wake","both authored wheel names");
        Check(Near(sys[0].transform.localPosition.x,-.8f) &&
              Near(sys[1].transform.localPosition.x,.8f),"two side offsets");
        Check(Near(sys[0].transform.localRotation.Y,180f) &&
              Near(sys[1].transform.localRotation.Y,180f),"rear orientation on both");
        foreach(var item in sys){
            Check(item.main.maxParticles==128,"bounded emitter lifetime capacity");
            Check(item.main.simulationSpace==ParticleSystemSimulationSpace.World,"world-space trail");
            Check(item.main.playOnAwake==false,"no auto-play on construction");
            if(expected.Visible){
                Check(item.isPlaying,"both emitters must start");
                Check(item.aliveParticles>0,"running source creates observable surrogate particles");
                Check(Near(item.emission.rateOverTime,expected.RatePerSecond*.5f),
                   "source must distribute rate equally over TWO wheels");
                Check(Near(item.main.startLifetime,expected.ParticleLifetime),"lifetime from policy");
                Check(Near(item.main.startSpeed,expected.ParticleSpeed),"speed from policy");
                Check(Near(item.main.startSize,expected.ParticleSize),"size from policy");
                Check(Near(item.main.startColor.a,expected.Alpha),"alpha from policy");
            }else{
                Check(!item.isPlaying,"invalid/off/contact lost must stop emitter");
                Check(item.aliveParticles==0,"invalid/off/contact lost must CLEAR existing particles");
                Check(Near(item.emission.rateOverTime,0),"stopped emitter rate must be zero");
            }
        }
    }
    public static int Main(){
      try{
        var root=new GameObject("independent original wake receiver");
        var emitter=root.AddComponent<B0132WheelWakeEmitter>();
        // Unity calls Awake automatically; use reflection ONLY for lifecycle binding,
        // not to substitute the original emitter implementation.
        typeof(B0132WheelWakeEmitter).GetMethod("Awake",
           BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(emitter,null);
        var initial=root.GetComponentsInChildren<ParticleSystem>();
        Check(initial.Length==2,"original Awake must create exactly two systems");
        foreach(var sys in initial) {
            Check(!sys.isPlaying&&sys.aliveParticles==0,"initial source state must be truly cleared");
            Check(sys.clearCount>=1,"init source must invoke StopEmittingAndClear");
            Check(sys.gameObject.GetComponent<ParticleSystemRenderer>()?.sharedMaterial!=null,
                  "renderer receives owned material");
        }
        CheckEmit(emitter,root,WheelWakeMode.Cruise,9,.35f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,11,.8f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Brake,8,.9f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Brake,8,.9f,true,false);
        CheckEmit(emitter,root,WheelWakeMode.Drift,11,.8f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,11,.8f,false,true);
        CheckEmit(emitter,root,WheelWakeMode.Off,9,.5f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Cruise,float.NaN,.5f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Brake,float.PositiveInfinity,.5f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,10,float.NaN,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,10,-.001f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,10,1.001f,true,true);
        CheckEmit(emitter,root,(WheelWakeMode)99,10,.5f,true,true);
        CheckEmit(emitter,root,WheelWakeMode.Drift,0,.5f,true,true);
        // Important missing from producer's three *unrun* Unity cases:
        // each of the two independent wheels must stop and immediately clear
        // old particles across many *different* input sequences.
        var rng=new Random(610217);
        for(int i=0;i<600;i++){
            var mode=(WheelWakeMode)rng.Next(-1,5);
            float speed=rng.Next(0,25);
            float slip=(float)(rng.Next(-1,12)*.1);
            bool contact=rng.Next(7)!=0,vfx=rng.Next(7)!=0;
            if(i%43==0)speed=float.NaN;
            if(i%47==0)slip=float.PositiveInfinity;
            CheckEmit(emitter,root,mode,speed,slip,contact,vfx);
            frames++;
        }
        CheckEmit(emitter,root,WheelWakeMode.Cruise,11,.4f,true,true);
        root.SetActive(false);  // Surrogate dispatches original OnDisable.
        Check(!emitter.IsEmitting,"disabled component reports no live emission");
        foreach(var sys in root.GetComponentsInChildren<ParticleSystem>())
            Check(!sys.isPlaying&&sys.aliveParticles==0,"Unity OnDisable must clear both wheels");
        emitter.SetVisualInput(WheelWakeMode.Cruise,11,.4f,true,true);
        Check(!emitter.IsEmitting&&emitter.LastSample.RatePerSecond==0,
            "stale input must not restart effects while disabled");
        root.SetActive(true);
        Check(!emitter.IsEmitting,"reactivated component cannot resurrect stale emission");
        CheckEmit(emitter,root,WheelWakeMode.Drift,11,.9f,true,true);
        // Own material must be freed by original OnDestroy (also clears particles).
        typeof(B0132WheelWakeEmitter).GetMethod("OnDestroy",
            BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(emitter,null);
        foreach(var sys in root.GetComponentsInChildren<ParticleSystem>())
            Check(!sys.isPlaying&&sys.aliveParticles==0,"OnDestroy clears particles");
        Check(!emitter.IsEmitting,"destroyed source cannot retain live state");
        Console.WriteLine("ORIGINAL_WHEELWAKE_EMITTER=PASS | CHECKS="+checks+
          " | INDEPENDENT_RANDOM_TRANSITIONS="+frames+
          " | DUAL_WHEEL_RATE_SPLIT=PASS | OFF_CONTACT_DISABLE_DESTROY_CLEAR=PASS");
        return 0;
      }catch(Exception e){
        Console.Error.WriteLine(e);
        return 2;
      }
    }
}
'''
def process(args):
    p=subprocess.run(args,capture_output=True,text=True)
    print("COMMAND="+" ".join(args))
    print("STDOUT="+p.stdout[-700:])
    if p.stderr:print("STDERR="+p.stderr[-1400:])
    print("EXIT="+str(p.returncode))
    return p

def git(root,*args):
    result=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    if result.returncode:raise RuntimeError("GIT_FAILURE "+repr(args)+": "+result.stderr.decode(errors="replace")[:300])
    return result.stdout

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    options=parser.parse_args()
    root=Path(options.repo).resolve()
    actual_ref=git(root,"rev-parse",PRODUCER_REF+"^{commit}").decode().strip()
    if actual_ref!=PRODUCER_REF:raise RuntimeError("PRODUCER_SHA_MISSING")
    original={}
    for src,blob in SOURCES.items():
        actual=git(root,"rev-parse",PRODUCER_REF+":"+src).decode().strip()
        if actual!=blob:raise RuntimeError(f"ORIGINAL_SOURCE_BLOB_DRIFT {src}: {actual} != {blob}")
        original[Path(src).name]=git(root,"show",PRODUCER_REF+":"+src)
        print(f"ORIGINAL_PRODUCER_GIT_SOURCE_VERIFIED={src} | {blob}")
    emitter_name="B0132WheelWakeEmitter.cs"
    for label in ("ORIGINAL","NEGATIVE_NO_CLEAR","NEGATIVE_DOUBLE_RATE"):
        with tempfile.TemporaryDirectory(prefix="b0161_w02_wake_") as directory:
            temp=Path(directory)
            (temp/"Receiver.csproj").write_text(PROJECT,encoding="utf8")
            (temp/"UnitySurrogate.cs").write_text(SHIM,encoding="utf8")
            (temp/"IndependentTests.cs").write_text(TEST,encoding="utf8")
            for name,contents in original.items():
                if name==emitter_name and label!="ORIGINAL":
                    decoded=contents.decode()
                    if label=="NEGATIVE_NO_CLEAR":
                        before="system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);"
                        idx=decoded.rfind(before)
                        if idx<0:raise RuntimeError("NEGATIVE_MUTATION_ANCHOR_MISSING")
                        decoded=decoded[:idx]+decoded[idx:].replace(before,
                           "system.Stop(true, ParticleSystemStopBehavior.StopEmitting);",1)
                    else:
                        before="emission.rateOverTime = value.RatePerSecond * .5f;"
                        if decoded.count(before)!=1:raise RuntimeError("RATE_ANCHOR_CHANGED")
                        decoded=decoded.replace(before,"emission.rateOverTime = value.RatePerSecond;",1)
                    contents=decoded.encode()
                (temp/name).write_bytes(contents)
            p=process(["dotnet","run","--project",str(temp/"Receiver.csproj"),
                       "--configuration","Release","--no-launch-profile"])
            output=p.stdout+p.stderr
            if label=="ORIGINAL":
                if p.returncode!=0 or "ORIGINAL_WHEELWAKE_EMITTER=PASS" not in output:
                    raise RuntimeError("ORIGINAL_EMITTER_NOT_GREEN")
            else:
                if p.returncode!=2 or "WHEELWAKE_ORACLE_FAIL:" not in output:
                    raise RuntimeError("MUTANT_NOT_BEHAVIORALLY_REJECTED:"+label)
            print(f"B01_61_RECEIVER_{label}={'ACTUAL_ORIGINAL_CSHARP_PASS' if label=='ORIGINAL' else 'EXPECTED_CSHARP_BEHAVIOR_FAILURE_REJECTED'}")
    print("B01_61_W02_PACKAGE="+PACKAGE_ID+" | ORIGINAL_PRODUCER_CSHARP=2/2_FILES_EXECUTED | INDEPENDENT_MUTATIONS=2/2_REJECTED | UNITY_NATIVE=NOT_RUN | ARTIFACT_OWNER=UNCHANGED | GAME_INTEGRATED=NO")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as exc:
        print("B0161_W02_RECEIVER_ERROR="+repr(exc),file=sys.stderr)
        sys.exit(2)
