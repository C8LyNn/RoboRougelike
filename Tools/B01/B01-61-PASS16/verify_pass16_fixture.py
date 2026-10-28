#!/usr/bin/env python3
"""PASS16 source-compatibility for new authored NUnit transition fixture.
This only executes Python original-code static guards: no C# compile or Unity run.
"""
import json,pathlib,subprocess,sys

HERE=pathlib.Path(__file__).resolve().parent
def git(root,*args):
    return subprocess.check_output(["git","-C",str(root),*args],text=True).strip()
def source(root,ref,path,sha):
    actual=git(root,"rev-parse",f"{ref}:{path}")
    if len(sha)!=40 or actual!=sha:
        raise ValueError(f"SOURCE_PIN_MISMATCH path={path} actual={actual} expected={sha}")
    return git(root,"show",f"{ref}:{path}")
def contains_all(s,anchors):
    return all(x in s for x in anchors)

def main(root):
    m=json.loads((HERE/"manifest.json").read_text(encoding="utf8"))
    f=m["authored_native_fixture"]
    path=pathlib.Path(root)/f["path"]
    code=path.read_text(encoding="utf8")
    observed=git(root,"hash-object",str(path))
    if observed!=f["git_blob"]:raise ValueError(f"FIXTURE_SHA_MISMATCH {observed}")
    checks=[]
    def ck(name,condition):
        print(("PASS" if condition else "FAIL")+" | "+name)
        checks.append(bool(condition))
    ck("two authored NEW Unity cases, no claims of native run",
       code.count("[UnityTest]")==2 and code.count("[Test]")==0
       and f["actual_author_execution"]=="NOT_RUN")
    for case in f["expected_native_cases"]:
        ck("source-authored "+case,case+"()" in code)
    ck("original scene + intentional batch isolation",
       'Application.isBatchMode' in code and 'Assets/scene 1.unity' in code)
    ck("bounded real scene transition loops",
       code.count("Time.realtimeSinceStartup+30f")>=2)
    ck("real virtual LPM input and release/repress",
       "InputSystem.AddDevice<Mouse>()" in code and
       "InputState.Change(virtualMouse.leftButton,1f);" in code and
       "InputState.Change(virtualMouse.leftButton,0f);" in code)
    ck("original pause transition + held guard assertions",
       code.count("run.TogglePause();")>=2 and
       'Value(controller,"waitForPrimaryRelease")' in code and
       'Value(controller,"fireHeld")' in code)
    ck("camera moved physically with source target identity",
       "run.Player.transform.position=moved" in code and
       "Physics.SyncTransforms()" in code and
       "Is.SameAs(run.Player.transform)" in code)
    ck("camera responsive but not synthetic FPS evidence",
       "ReceiverRig.enabled,Is.False" in code and
       "Vector3.Distance(camera.transform.position,previous)" in code)
    ck("safe input removal and Unity teardown",
       "InputSystem.RemoveDevice(virtualMouse)" in code and
       "[UnityTearDown]" in code and "new ExitPlayMode()" in code)

    game=m["game_sha"]
    sources={k:source(root,game,k,v) for k,v in m["source_fixture_pins"].items()}
    ctrl=sources["Assets/Pack/P12_PipelineSafeTankController.cs"]
    run=sources["Assets/R1/R1RunController.cs"]
    cam=sources["Assets/Pack/P12_PipelineSafeCamera.cs"]
    latches=[
      "waitForPrimaryRelease = waitForSecondaryRelease = true;",
      "fireHeld = secondaryFireHeld = false;",
      "if(!primaryPressed)waitForPrimaryRelease=false;",
      "fireHeld=primaryPressed&&!waitForPrimaryRelease;",
      "secondaryFireHeld=secondaryPressed&&!waitForSecondaryRelease;",
    ]
    ck("original P12 input state / pause release latch API",contains_all(ctrl,latches))
    ck("original P12 real single group trigger disabling on pause",
       "if(modularAssembly!=null)modularAssembly.SetGroupTriggers(false,false);" in ctrl)
    ck("original real R1 pause propagates controller input inactive",
       contains_all(run,[
         "Paused = !Paused;",
         "Player.Controller.SetGameplayInputEnabled(active);",
         "SetSimulation(!Paused && (Progress.State == R1RunState.Combat || Progress.State == R1RunState.TestDrive));"
       ]))
    ck("original camera follows live target with one rig step",
       contains_all(cam,[
         "public Transform TargetRoot=>targetRoot;",
         "public Camera001Rig ReceiverRig=>rig;",
         "rig.enabled=false;",
         "rig.StepCamera(Time.deltaTime);",
         "public float SmoothedZoomDistance=>renderedDistance;"
       ]))
    bad_ctrl=ctrl.replace(latches[0],"/* MUTATED_NO_RELEASE_LATCH */")
    ck("negative mutation catches removed input release guard",not contains_all(bad_ctrl,latches))
    bad_cam=cam.replace("rig.StepCamera(Time.deltaTime);","/* MUTATED_NO_CAMERA_STEP */")
    ck("negative mutation catches removed camera step",
       not contains_all(bad_cam,["rig.StepCamera(Time.deltaTime);"]))
    print(f"PASS16_SOURCE_FIXTURE_STATIC={sum(checks)}/{len(checks)} | GAME_SHA={game} | AUTHORED_UNITY_TESTS=2 | UNITY_NATIVE=NOT_RUN")
    return int(not all(checks))
if __name__=="__main__":
    try:sys.exit(main(pathlib.Path(sys.argv[1] if len(sys.argv)>1 else ".").resolve()))
    except Exception as exc:
        print("PASS16_SOURCE_FIXTURE_ERROR="+repr(exc),file=sys.stderr)
        sys.exit(2)
