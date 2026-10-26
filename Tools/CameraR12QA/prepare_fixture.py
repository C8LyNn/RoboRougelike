"""Derive receiver R1.1 reproduction, with stricter independent final-pose probes.
Only stages source outside Assets while the original Unity suites are running.
"""
import pathlib
r=pathlib.Path(__file__).resolve().parents[2]
old=r.parent/'RoboRougelike-W9-Validation/Assets/CameraNativeQA/CameraR11NativeQA.cs'
s=old.read_text(encoding='utf-8-sig')
def replace(a,b):
 global s
 assert a in s,a[:90]
 s=s.replace(a,b)
replace('using UnityEngine;','using UnityEngine;\nusing UnityEngine.InputSystem;\nusing UnityEngine.InputSystem.LowLevel;')
replace('CameraR11NativeQA','CameraR12NativeQA')
replace('CAMERA R1.1','CAMERA R1.2')
replace('d48108f56d3fe0790ad715683e77b2de9219e4b1','48581e49a878eca376674551017532bf17fd35b2')
replace('CameraR11QA','CameraR12QA')
replace('public bool fitSaturated,damageDetached;','public bool fitSaturated,damageDetached;\n        public int finalOriginOwnHits,originInsideOwnSamples,speedCapViolations,worldSphereOverlapSamples,diagnosticOwnFlags,aimErrorViolations,zoomSteps,inputIdleShells,inputRightShells,inputLeftShells,inputLeftObservedFrames,inputRightObservedFrames;\n        public float maxContinuousSpeed,maxActualAimError,maxStepExcess,maxMuzzleAfterSettle;\n        public string inputMethod="InputSystem queued virtual Mouse/Keyboard consumed by unchanged P12 controller; not OS/human input";')
replace('Vector3 previous;Quaternion previousRotation;int sampleFrame;','Vector3 previous;Quaternion previousRotation;int sampleFrame;float stepDt;Mouse qaMouse;Keyboard qaKeyboard;\n    void Step(float dt){stepDt=dt;rig.StepCamera(dt);}\n    void QueueInput(bool left,bool right,bool forward){InputSystem.QueueStateEvent(qaMouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)}.WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));InputSystem.QueueStateEvent(qaKeyboard,forward?new KeyboardState(Key.W):new KeyboardState());}')
replace('Application.logMessageReceived+=OnLog;','Application.logMessageReceived+=OnLog;\n        qaMouse=InputSystem.AddDevice<Mouse>("Camera QA virtual mouse");qaKeyboard=InputSystem.AddDevice<Keyboard>("Camera QA virtual keyboard");')
replace('idealOwnBlocked,actualOwnBlocked,enemyOwnBlocked,enemyViewportX,enemyViewportY,clipped,pivotBlocked','idealOwnBlocked,actualOwnBlocked,enemyOwnBlocked,enemyViewportX,enemyViewportY,clipped,pivotBlocked,deltaTime,allowedStep,stepExcess,actualOriginOwnHit,insideOwn,worldSphereOverlap,ownDiagnostic,actualAimError,screenWidth,screenHeight')
# Keep explicit resets as in original harness; all ordinary updates use measured dt.
replace('rig.StepCamera(Time.deltaTime);','Step(Time.deltaTime);')
replace('rig.StepCamera(Time.fixedDeltaTime);','Step(Time.fixedDeltaTime);')
replace('row.phase="orbit360";float yaw=0;','row.phase="pitch18TransitionAndOrbit360";float yaw=0;')
replace('SetAimOrbit();rig.ResetSmoothing();rig.StepCamera(0);previous=view.transform.position;previousRotation=view.transform.rotation;\n            Vector3 aim=', '''row.phase="zoom3to18";
            foreach(float zoom in new[]{3f,18f,8f})for(int i=0;i<50;i++){yield return null;rig.SetOrbit(0,18,zoom);Step(Time.deltaTime);Measure(false);row.zoomSteps++;}
            yield return Capture("zoom");
            SetAimOrbit();rig.ResetSmoothing();rig.StepCamera(0);previous=view.transform.position;previousRotation=view.transform.rotation;
            Vector3 aim=''')
start=s.index('            if(kind=="Light")\n')
end=s.index('            row.status=',start)
s=s[:start]+'''            // Exercise actual unchanged Update/FixedUpdate/LateUpdate input consumers.
            // Virtual device events are queued before normal native InputSystem updates.
            actor.Controller.enabled=true;actor.Controller.SetGameplayInputEnabled(true);
            oldBodies=new HashSet<int>(FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Select(b=>b.GetInstanceID()));
            measuredBodies.Clear();row.phase="inputIdle";yield return InputPhase(false,false,false,.8f);
            row.inputIdleShells=row.newFastBodies;
            row.phase="inputRMB";yield return InputPhase(false,true,false,1.2f);
            row.inputRightShells=row.newFastBodies-row.inputIdleShells;
            body.isKinematic=false;actor.GetComponent<R6VehicleDrive>().RefreshParts();row.initialPosition=body.position;
            row.phase="inputLMBDrive";yield return InputPhase(true,false,true,5f);
            row.inputLeftShells=row.newFastBodies-row.inputIdleShells-row.inputRightShells;
            QueueInput(false,false,false);yield return null;actor.Controller.enabled=false;
            row.finalPosition=body.position;row.distanceMoved=Vector3.Distance(row.initialPosition,row.finalPosition);yield return Capture("driveFire");
''' +s[end:]
replace('row.status=row.ownReticleHits==0&&row.worldPenetrationSamples==0&&row.enemyBlockedSamples==0?','row.status=row.ownReticleHits==0&&row.finalOriginOwnHits==0&&row.originInsideOwnSamples==0&&row.speedCapViolations==0&&row.worldSphereOverlapSamples==0&&row.aimErrorViolations==0&&row.worldPenetrationSamples==0&&row.enemyBlockedSamples==0&&row.damageDetached&&row.inputIdleShells==0&&row.inputRightShells==0&&row.inputLeftShells>0&&row.maxMuzzleAfterSettle<=.1f?')
replace('Save();trace.Dispose();Application.Quit','Save();trace.Dispose();InputSystem.RemoveDevice(qaMouse);InputSystem.RemoveDevice(qaKeyboard);Application.Quit')
replace('    void SetAimOrbit()','''    IEnumerator InputPhase(bool left,bool right,bool forward,float seconds)
    {
        float start=Time.time;
        while(Time.time-start<seconds)
        {
            QueueInput(left,right,forward);yield return null;SetAimOrbit();Step(Time.deltaTime);Measure(true);
            if(Mouse.current!=null&&Mouse.current.leftButton.isPressed)row.inputLeftObservedFrames++;
            if(Mouse.current!=null&&Mouse.current.rightButton.isPressed)row.inputRightObservedFrames++;
            yield return new WaitForEndOfFrame();
            Vector3 aim=actor.Controller.ModuleAimPoint();
            foreach(var gun in assembly.Parts.Where(p=>p.Kind==R3PartKind.Gun&&p.Operational&&p.Muzzle!=null))
            {
                float error=Vector3.Angle(gun.Muzzle.forward,aim-gun.Muzzle.position);
                row.maxMuzzleAimError=Mathf.Max(row.maxMuzzleAimError,error);
                if(left&&Time.time-start>1.5f)row.maxMuzzleAfterSettle=Mathf.Max(row.maxMuzzleAfterSettle,error);
            }
            foreach(var rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))if(!oldBodies.Contains(rb.GetInstanceID())&&rb.linearVelocity.magnitude>10&&measuredBodies.Add(rb.GetInstanceID()))
            {row.newFastBodies++;row.shotComponents=string.Join(";",rb.GetComponents<Component>().Select(c=>c.GetType().Name));owned.Add(rb.gameObject);}
        }
    }
    void SetAimOrbit()''')
replace('        if(rig.WorldClipped)row.clipSamples++;','''        Ray originRay=new Ray(view.transform.position,view.transform.forward);
        var ownColliders=actor.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy).ToArray();
        bool originHit=ownColliders.Any(c=>c.Raycast(originRay,out RaycastHit h,200));
        bool inside=ownColliders.Any(c=>(c.ClosestPoint(view.transform.position)-view.transform.position).sqrMagnitude<.000001f);
        if(originHit)row.finalOriginOwnHits++;if(inside)row.originInsideOwnSamples++;
        float near=view.nearClipPlane,halfHeight=near*Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f),halfWidth=halfHeight*view.aspect;
        float radius=Mathf.Max(.26f,Mathf.Sqrt(near*near+halfHeight*halfHeight+halfWidth*halfWidth))+.14f;
        bool worldOverlap=Physics.OverlapSphere(view.transform.position,radius,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(actor.transform));
        if(worldOverlap)row.worldSphereOverlapSamples++;
        float excess=step-15*stepDt;row.maxStepExcess=Mathf.Max(row.maxStepExcess,excess);
        if(excess>.0001f)row.speedCapViolations++;
        if(stepDt>0)row.maxContinuousSpeed=Mathf.Max(row.maxContinuousSpeed,step/stepDt);
        float actualAimError=Vector3.Angle(view.transform.forward,rig.LastSolution.AimWorld-view.transform.position);
        row.maxActualAimError=Mathf.Max(row.maxActualAimError,actualAimError);if(actualAimError>.05f)row.aimErrorViolations++;
        if(rig.ActualOwnHullIntersectsCrosshair)row.diagnosticOwnFlags++;
        if(rig.WorldClipped)row.clipSamples++;''')
replace('rig.WorldOverlapAtAim.ToString()}','rig.WorldOverlapAtAim.ToString(),stepDt.ToString("R",System.Globalization.CultureInfo.InvariantCulture),(15*stepDt).ToString("R",System.Globalization.CultureInfo.InvariantCulture),excess.ToString("R",System.Globalization.CultureInfo.InvariantCulture),originHit.ToString(),inside.ToString(),worldOverlap.ToString(),rig.ActualOwnHullIntersectsCrosshair.ToString(),actualAimError.ToString("R",System.Globalization.CultureInfo.InvariantCulture),Screen.width.ToString(),Screen.height.ToString()}')
(r/'Tools/CameraR12QA/CameraR12NativeQA.cs').write_text(s,encoding='utf-8')
print('Staged receiver fixture; original producer source untouched.')
