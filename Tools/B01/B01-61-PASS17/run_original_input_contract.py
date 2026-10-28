#!/usr/bin/env python3
"""B01-61 P17 -- execute *unchanged extracted original Codex C# methods*
through isolated .NET8 shims, in BOTH Unity input preprocessor branches.
This is NOT a Unity or physics test. No production source edits.
"""
import argparse
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile

GAME_SHA="682dc0c5c13615577334375ffb6c27be5cdebf4a"
SOURCE="Assets/Pack/P12_PipelineSafeTankController.cs"
SOURCE_BLOB="8beeedb4db40b7b33c8f685d7b6aab1247a4b2d7"
PACKAGE="B01-61-P17-INPUT-LATCH-R1"

def git(root,*args):
    p=subprocess.run(["git","-C",root,*args],capture_output=True,text=True)
    if p.returncode:raise RuntimeError("git "+repr(args)+": "+p.stderr[:400])
    return p.stdout.strip()

def method(full,header):
    match=re.search(r"\b"+re.escape(header)+r"\s*\{",full)
    if match is None:raise RuntimeError("MISSING_OR_CHANGED_ORIGINAL_METHOD:"+header)
    opening=full.index("{",match.start())
    n=0
    for ix in range(opening,len(full)):
        if full[ix]=="{":n+=1
        elif full[ix]=="}":
            n-=1
            if n==0:return full[match.start():ix+1]
    raise RuntimeError("UNBALANCED_ORIGINAL_CSHARP:"+header)

PROJECT=r'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'''

# All dependencies here are DISPOSABLE minimal Unity type/input shims; the
# methods injected below, however, are exact original raw C# method text.
HOST=r'''
using System;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.MOVSeries {
    public sealed class MOVSeriesVehicleAdapter {
        public void CancelActions() {}
    }
}
public enum KeyCode { W, UpArrow, S, DownArrow, D, RightArrow, A, LeftArrow }
public static class Input {
    public static bool primary,secondary,w,s,a,d;
    public static bool GetMouseButton(int button)=>button==0?primary:button==1?secondary:false;
    public static bool GetKey(KeyCode key)=>key switch {
        KeyCode.W or KeyCode.UpArrow => w,
        KeyCode.S or KeyCode.DownArrow => s,
        KeyCode.D or KeyCode.RightArrow => d,
        KeyCode.A or KeyCode.LeftArrow => a,
        _=>false
    };
}
namespace UnityEngine.InputSystem {
    public sealed class Button { public bool isPressed; }
    public sealed class Keyboard {
        public static Keyboard? current=new Keyboard();
        public Button wKey=new(),upArrowKey=new(),sKey=new(),downArrowKey=new();
        public Button aKey=new(),leftArrowKey=new(),dKey=new(),rightArrowKey=new();
    }
    public sealed class Mouse {
        public static Mouse? current=new Mouse();
        public Button leftButton=new(),rightButton=new();
    }
}

public sealed class FakeAssembly {
    public bool lastPrimary,lastSecondary;
    public int resetCount;
    public void SetGroupTriggers(bool a,bool b) {
        lastPrimary=a;lastSecondary=b;
        if(!a&&!b)resetCount++;
    }
}
public partial class P12PipelineSafeTankController {
    private bool gameplayInputEnabled=true,fireHeld,secondaryFireHeld;
    private bool waitForPrimaryRelease,waitForSecondaryRelease;
    private float moveInput,turnInput,currentSpeed;
    private bool AimFeedback;
    private readonly List<object> weaponReticles=new();
    private FakeAssembly? modularAssembly=new();
    private object? rb=new();
    public int PhysicsResetCalls;
    private void StopResidualPhysics(){PhysicsResetCalls++;}
    private T GetComponent<T>() where T:new()=>new T();
    public void Tick()=>Update();
    public bool PrimaryFire=>fireHeld;
    public bool SecondaryFire=>secondaryFireHeld;
    public bool WaitA=>waitForPrimaryRelease;
    public bool WaitB=>waitForSecondaryRelease;
    public float Move=>moveInput;
    public float Turn=>turnInput;
    public bool Enabled=>gameplayInputEnabled;
    public FakeAssembly Assembly=>modularAssembly!;
    public void SetFakeSpeed(float speed)=>currentSpeed=speed;
    // EXACT ORIGINAL METHOD INSERT A
@@ORIGINAL_SET@@
    // EXACT ORIGINAL METHOD INSERT B
@@ORIGINAL_UPDATE@@
}

public static class Program {
    static int assertions;
    static void Check(bool ok,string why){
        assertions++;
        if(!ok)throw new InvalidOperationException("ORIGINAL_CSHARP_CONTRACT_FAIL | "+why);
    }
    static void InputSet(bool a,bool b,bool w=false,bool d=false){
        Input.primary=a;Input.secondary=b;Input.w=w;Input.d=d;
        Input.s=false;Input.a=false;
        UnityEngine.InputSystem.Mouse.current!.leftButton.isPressed=a;
        UnityEngine.InputSystem.Mouse.current!.rightButton.isPressed=b;
        var kb=UnityEngine.InputSystem.Keyboard.current!;
        kb.wKey.isPressed=w;kb.dKey.isPressed=d;
        kb.sKey.isPressed=false;kb.aKey.isPressed=false;
        kb.upArrowKey.isPressed=false;kb.downArrowKey.isPressed=false;
        kb.leftArrowKey.isPressed=false;kb.rightArrowKey.isPressed=false;
    }
    sealed class Oracle {
        public bool enabled=true,heldA,heldB,waitA,waitB;
        public void SetEnabled(bool active){
            enabled=active;
            if(!active){heldA=false;heldB=false;waitA=true;waitB=true;}
        }
        public void Tick(bool a,bool b){
            heldA=heldB=false;
            if(!enabled)return;
            if(!a)waitA=false;
            if(!b)waitB=false;
            heldA=a&&!waitA;
            heldB=b&&!waitB;
        }
    }
    public static int Main(){
        try{
            var c=new P12PipelineSafeTankController();
            InputSet(true,true,true,true);c.Tick();
            Check(c.PrimaryFire&&c.SecondaryFire,"both firing groups must respond to fresh physical input");
            Check(c.Move==1f&&c.Turn==1f,"the actual movement input must still read correctly");
            c.SetFakeSpeed(20);c.SetGameplayInputEnabled(false);
            Check(!c.PrimaryFire&&!c.SecondaryFire,"disable clears actual player weapon inputs");
            Check(c.WaitA&&c.WaitB,"disable latches both groups");
            Check(!c.Assembly.lastPrimary&&!c.Assembly.lastSecondary,"disable clears the actual group trigger calls");
            Check(c.PhysicsResetCalls==1,"disable cancels fake physics through unchanged original hook");
            c.Tick();Check(!c.PrimaryFire&&!c.SecondaryFire,"disabled held inputs must not auto-fire");
            c.SetGameplayInputEnabled(true);c.Tick();
            Check(!c.PrimaryFire&&!c.SecondaryFire,"both buttons held across resume require physical release");
            InputSet(false,true);c.Tick();
            Check(!c.PrimaryFire&&!c.SecondaryFire&&!c.WaitA&&c.WaitB,"LPM release cannot unlatch held RMB");
            InputSet(true,true);c.Tick();
            Check(c.PrimaryFire&&!c.SecondaryFire,"repress LPM must not re-enable held RMB");
            InputSet(true,false);c.Tick();
            Check(c.PrimaryFire&&!c.SecondaryFire&&!c.WaitB,"RMB release unlatches only RMB");
            InputSet(true,true);c.Tick();
            Check(c.PrimaryFire&&c.SecondaryFire,"fresh RMB press enables both valid groups");
            InputSet(false,false);c.Tick();
            Check(!c.PrimaryFire&&!c.SecondaryFire,"both release causes no fire");
            c.SetGameplayInputEnabled(false);InputSet(false,false);c.Tick();
            c.SetGameplayInputEnabled(true);c.Tick();
            InputSet(true,true);c.Tick();
            Check(c.PrimaryFire&&c.SecondaryFire,"release while in pause, observed after resume, safely permits a new press");

            // Stateful deterministic independent reference oracle, repeatedly
            // tests both individually-latched groups and every enable/disable.
            var f=new P12PipelineSafeTankController();
            var oracle=new Oracle();
            var random=new Random(610017);
            for(int i=0;i<2400;i++){
                if(random.Next(9)==0){
                    var active=random.Next(2)==0;
                    f.SetGameplayInputEnabled(active);
                    oracle.SetEnabled(active);
                }
                bool a=random.Next(2)==0,b=random.Next(2)==0;
                bool w=random.Next(2)==0,d=random.Next(2)==0;
                InputSet(a,b,w,d);f.Tick();oracle.Tick(a,b);
                Check(f.PrimaryFire==oracle.heldA&&f.SecondaryFire==oracle.heldB,
                    "seeded frame "+i+" groups inconsistent with safety oracle");
                Check(f.WaitA==oracle.waitA&&f.WaitB==oracle.waitB,
                    "seeded frame "+i+" pending-input-release latch drift");
                if(f.Enabled)Check(f.Move==(w?1f:0f)&&f.Turn==(d?1f:0f),
                    "seeded frame "+i+" controls lost movement");
                else Check(f.Move==0&&f.Turn==0,"disabled frame motion unexpectedly accepted");
            }
            Console.WriteLine("ORIGINAL_CSHARP_INPUT_MATRIX=PASS | CASES="+assertions+
                " | SEEDED_FRAMES=2400 | FIRST_BUTTON=PASS | SECOND_BUTTON=PASS | DISABLE_RESUME=PASS");
            return 0;
        }catch(Exception exc){Console.Error.WriteLine(exc);return 2;}
    }
}
'''

def run_process(args,cwd=None):
    p=subprocess.run(args,cwd=cwd,capture_output=True,text=True)
    print("CMD="+ " ".join(str(x) for x in args))
    print(p.stdout[-1800:])
    if p.stderr:print("STDERR="+p.stderr[-1400:])
    print("EXIT="+str(p.returncode))
    return p

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--quick",action="store_true",help="run only both original branches, no injected mutation")
    options=ap.parse_args()
    root=str(pathlib.Path(options.repo).resolve())
    assert git(root,"rev-parse",GAME_SHA+"^{commit}")==GAME_SHA
    blob=git(root,"rev-parse",GAME_SHA+":"+SOURCE)
    if blob!=SOURCE_BLOB:raise RuntimeError(f"ORIGINAL_GAME_BLOB_DRIFT {blob} expected {SOURCE_BLOB}")
    original=git(root,"show",GAME_SHA+":"+SOURCE)
    set_method=method(original,"public void SetGameplayInputEnabled(bool active)")
    update_method=method(original,"private void Update()")
    for required in ("waitForPrimaryRelease = waitForSecondaryRelease = true;",
                     "if(modularAssembly!=null)modularAssembly.SetGroupTriggers(false,false);"):
        if required not in set_method:raise RuntimeError("ORIGINAL_SAFETY_INTERFACE_CHANGED:"+required)
    for required in ("#if ENABLE_INPUT_SYSTEM","if(!primaryPressed)waitForPrimaryRelease=false;",
                     "if(!secondaryPressed)waitForSecondaryRelease=false;",
                     "fireHeld=primaryPressed&&!waitForPrimaryRelease;",
                     "secondaryFireHeld=secondaryPressed&&!waitForSecondaryRelease;"):
        if required not in update_method:raise RuntimeError("ORIGINAL_INPUT_INTERFACE_CHANGED:"+required)
    print("ORIGINAL_GAME_SHA="+GAME_SHA+" | SOURCE_GIT_BLOB="+SOURCE_BLOB)
    print("EXTRACTED_UNMODIFIED_METHODS=2 | EXACT_ORIGINAL_SOURCE_ONLY=YES")
    passes=[]
    for label,define in (("LEGACY_INPUT",False),("UNITY_INPUT_SYSTEM",True)):
        for mutation in (False,True) if not options.quick else (False,):
            with tempfile.TemporaryDirectory(prefix="b0161_input_") as tmp:
                path=pathlib.Path(tmp)
                (path/"Contract.csproj").write_text(PROJECT,encoding="utf8")
                current=set_method
                if mutation:
                    current=current.replace("waitForPrimaryRelease = waitForSecondaryRelease = true;",
                       "waitForPrimaryRelease = waitForSecondaryRelease = false;")
                    if current==set_method:raise RuntimeError("MUTATION_DID_NOT_CHANGE_ORIGINAL")
                generated=HOST.replace("@@ORIGINAL_SET@@",current).replace("@@ORIGINAL_UPDATE@@",update_method)
                (path/"Program.cs").write_text(generated,encoding="utf8")
                args=["dotnet","run","--project",str(path/"Contract.csproj"),
                      "--configuration","Release","--no-launch-profile"]
                if define:args.append("-p:DefineConstants=ENABLE_INPUT_SYSTEM")
                outcome=run_process(args)
                if not mutation and outcome.returncode!=0:raise RuntimeError("ORIGINAL_CODE_TEST_RED_"+label)
                if mutation and outcome.returncode==0:raise RuntimeError("SAFETY_NEGATIVE_MUTATION_NOT_REJECTED_"+label)
                passes.append((label,mutation,outcome.returncode))
                print("EXECUTED_"+label+"_"+("INJECTED_BROKEN_GUARD_REJECTED" if mutation else "ORIGINAL_GREEN")+"=PASS")
    if len(passes)!=(2 if options.quick else 4):raise RuntimeError("NOT_ALL_CASES_EXECUTED")
    print("PASS17_PACKAGE="+PACKAGE+" | EXECUTED_ORIGINAL_INPUT_VARIANTS=2/2 | EXPECTED_MUTATION_REJECTIONS="+
          ("0_QUICK" if options.quick else "2/2")+" | UNITY_NATIVE=NOT_RUN | GAME_CHANGED=NO")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as error:
        print("B0161_PASS17_FAIL="+repr(error),file=sys.stderr)
        sys.exit(2)
