using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Diagnostic only: it drives the real player with R6 forces on the existing
// range ramp. It does not modify the camera package or replace the vehicle.
public sealed class CameraRamp18DiagnosticTests
{
    R1RunController run;

    [UnityTest] public IEnumerator RealLightVehicleOnRangeRampRecordsCameraAndAimCollision()=>DriveRamp(R3VehicleKind.Light);
    [UnityTest] public IEnumerator RealStandardVehicleOnRangeRampRecordsCameraAndAimCollision()=>DriveRamp(R3VehicleKind.Standard);
    [UnityTest] public IEnumerator RealHeavyVehicleOnRangeRampRecordsCameraAndAimCollision()=>DriveRamp(R3VehicleKind.Heavy);
    [UnityTest] public IEnumerator HeavyApproachesRangeRampFromFlatGround()=>ApproachRamp(false,false);
    [UnityTest] public IEnumerator HeavyWithPowerModuleApproachesRangeRampFromFlatGround()=>ApproachRamp(true,false);
    [UnityTest] public IEnumerator HeavyWithPowerModuleAndSteeringApproachesRangeRampFromFlatGround()=>ApproachRamp(true,true);

    static Vector3Int AvailableAttachment(R3VehicleModel model,string id)
    {
        var directions=new[]{Vector3Int.up,Vector3Int.right,Vector3Int.left,Vector3Int.forward,Vector3Int.back};
        foreach(var part in model.Parts)
            foreach(var direction in directions)
            {
                var cell=part.Cell+direction;
                if(model.OwnedPlacementError(id,cell,Vector3Int.zero)==null)return cell;
            }
        Assert.Fail("No valid adjacent cell for the power module.");
        return default;
    }

    IEnumerator ApproachRamp(bool addPower,bool correctSteering)
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+30;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy)&&run.OpenSandboxGarage(),Is.True);
        if(addPower)
        {
            Assert.That(run.GaragePurchase(R3PartKind.DrivePower),Is.True);
            var module=run.VehicleModel.Inventory.Last();
            Assert.That(run.GarageMount(module.Id,AvailableAttachment(run.VehicleModel,module.Id),Vector3Int.zero),Is.True);
        }
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var range=run.GetComponent<R6TestRange>();
        var ramp=range.Layout.GetComponentsInChildren<BoxCollider>().First(c=>c.name.StartsWith("Rampa terenu • poligon")&&c.transform.position.x>0);
        var vehicle=run.Player;var body=vehicle.GetComponent<Rigidbody>();var drive=vehicle.GetComponent<R6VehicleDrive>();
        vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        // Initial placement on the real flat approach only. All subsequent motion
        // is driven by the same R6 force solver and physical wheel contacts.
        body.interpolation=RigidbodyInterpolation.None;
        body.position=new Vector3(ramp.transform.position.x,.7f,20);body.rotation=Quaternion.Euler(0,180,0);
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;Physics.SyncTransforms();body.WakeUp();
        body.interpolation=RigidbodyInterpolation.Interpolate;
        for(int i=0;i<30;i++){drive.Step(0,0,10,4,90);yield return MOVSeriesDeviceTests.FixedStep();}
        var start=body.position;float maxHeight=start.y;int grounded=0;bool reachedTop=false;
        for(int i=0;i<600;i++)
        {
            float steering=0;
            if(correctSteering)
            {
                var toward=new Vector3(ramp.transform.position.x-body.position.x,0,3-body.position.z);
                float desiredYaw=Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;
                steering=Mathf.Clamp(Mathf.DeltaAngle(body.rotation.eulerAngles.y,desiredYaw)/45f,-1,1);
            }
            drive.Step(1,steering,10,4,90);yield return MOVSeriesDeviceTests.FixedStep();
            if(drive.GroundedWheels>0)grounded++;
            maxHeight=Mathf.Max(maxHeight,body.position.y);
            if(body.position.z<4.5f&&body.position.y>2f)reachedTop=true;
            if(i%50==0)Debug.Log($"[RAMP-APPROACH] power={addPower} steering={correctSteering} fixed={i} pos={body.position.ToString("F2")} velocity={body.linearVelocity.ToString("F2")} contacts={drive.GroundedWheels}/{drive.Wheels} force={drive.AvailableForce:F0} powerUnits={drive.ActivePowerUnits:F2}");
        }
        float progress=start.z-body.position.z;
        Debug.Log($"[RAMP-APPROACH SUMMARY] power={addPower} steering={correctSteering} start={start.ToString("F2")} end={body.position.ToString("F2")} progress={progress:F2} maxHeight={maxHeight:F2} groundedFrames={grounded}/600 reachedTop={reachedTop}");
        Assert.That(grounded,Is.GreaterThan(0),"The actual wheels must contact the terrain.");
        Assert.That(progress,Is.GreaterThan(.5f),"The actual drive must move from the flat approach.");
        // This diagnostic deliberately records the crest result instead of
        // turning a short sample into an assertion about all driving skills.
    }

    IEnumerator DriveRamp(R3VehicleKind kind)
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+30;
        while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
        run=R1RunController.Instance;Assert.That(run,Is.Not.Null);
        Assert.That(run.SelectVehicle(kind)&&run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False,"The actual test range must load.");
        var range=run.GetComponent<R6TestRange>();Assert.That(range?.Layout,Is.Not.Null);
        var ramp=range.Layout.GetComponentsInChildren<BoxCollider>()
            .FirstOrDefault(c=>c.name.StartsWith("Rampa terenu • poligon"));
        Assert.That(ramp,Is.Not.Null,"Use the real, solid range ramp.");
        Assert.That(ramp.enabled&&!ramp.isTrigger,Is.True);
        Assert.That(Vector3.Angle(ramp.transform.up,Vector3.up),Is.EqualTo(18).Within(.1f));

        var vehicle=run.Player;var body=vehicle.GetComponent<Rigidbody>();
        var drive=vehicle.GetComponent<R6VehicleDrive>();
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        Assert.That(body,Is.Not.Null);Assert.That(drive,Is.Not.Null);Assert.That(follow,Is.Not.Null);
        Assert.That(body.isKinematic,Is.False);Assert.That(body.useGravity,Is.True);
        Assert.That(follow.TargetRoot,Is.EqualTo(vehicle.transform));

        // Initial setup only: the rest of the trial uses the actual R6 force
        // solver, contacts, Rigidbody and the enabled LateUpdate camera adapter.
        vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;
        var up=ramp.transform.up;var uphill=-ramp.transform.forward;
        var surface=ramp.transform.TransformPoint(new Vector3(0,.5f,.27f));
        var pose=Quaternion.LookRotation(uphill,up);
        body.interpolation=RigidbodyInterpolation.None;
        body.transform.SetPositionAndRotation(surface+up*.65f,pose);
        body.position=surface+up*.65f;body.rotation=pose;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        Physics.SyncTransforms();body.WakeUp();body.interpolation=RigidbodyInterpolation.Interpolate;
        // A low viewing angle with the camera looking downhill puts terrain
        // between the subject and the desired zoom position. This is an
        // explicit fixture input, not a claim about the player's exact input.
        SetFollowField(follow,"yaw",0f);SetFollowField(follow,"pitch",-5f);
        SetFollowField(follow,"distance",20f);SetFollowField(follow,"renderedDistance",20f);
        SetFollowField(follow,"zoomInitialized",true);follow.ReceiverRig.ResetSmoothing();

        // EnterPlayMode under the EditMode runner can ignore WaitForFixedUpdate;
        // wait for actual fixed-time progress before counting a sample.
        for(int i=0;i<15;i++){drive.Step(0,0,10,4,90);yield return MOVSeriesDeviceTests.FixedStep();}
        var start=body.position;int grounded=0,aimOverlaps=0,worldClips=0,cameraOverlaps=0;
        float maxDistance=0,maxDesiredGap=0,firstFixedTime=Time.fixedTime;var rig=follow.ReceiverRig;
        Debug.Log($"[RAMP-DRIVE SETUP] kind={kind} massKg={body.mass:F2} gravity={Physics.gravity.ToString("F3")} uphill={uphill.ToString("F3")} start={start.ToString("F3")} installedWheels={drive.Wheels} powerUnits={drive.ActivePowerUnits:F2}");
        for(int i=0;i<100;i++)
        {
            drive.Step(1,0,10,4,90);yield return MOVSeriesDeviceTests.FixedStep();
            if(drive.GroundedWheels>0)grounded++;
            if(rig.WorldOverlapAtAim)aimOverlaps++;
            if(rig.WorldClipped)worldClips++;
            if(rig.WorldOverlapAtCamera)cameraOverlaps++;
            var camera=follow.transform;var aim=rig.LastSolution.AimWorld;
            float targetDistance=Vector3.Distance(camera.position,body.position);
            float desiredGap=Vector3.Distance(camera.position,rig.LastSolution.DesiredCameraWorld);
            maxDistance=Mathf.Max(maxDistance,targetDistance);maxDesiredGap=Mathf.Max(maxDesiredGap,desiredGap);
            // AvailableForce is a traction-dependent capacity, not proof of the
            // actual applied force: braking, speed limits and other adapters can
            // change it. Record it alongside observed motion and gravity.
            if(i%5==0)Debug.Log($"[RAMP-DRIVE] kind={kind} fixed={i} progress={Vector3.Dot(body.position-start,uphill):F3} uphillSpeed={Vector3.Dot(body.linearVelocity,uphill):F3} forwardSpeed={drive.ForwardSpeed:F3} availableForceN={drive.AvailableForce:F1} gravityAlongRampN={Vector3.Dot(Physics.gravity*body.mass,uphill):F1} groundNormal={drive.GroundNormal.ToString("F3")} grounded={drive.GroundedWheels}/{drive.Wheels} powerUnits={drive.ActivePowerUnits:F2}");
            if(i%5==0)Debug.Log($"[CAMERA-RAMP18] fixed={i} t={Time.fixedTime:F2} body={body.position.ToString("F3")} velocity={body.linearVelocity.ToString("F3")} bodyRotation={body.rotation.eulerAngles.ToString("F2")} contacts={drive.GroundedWheels} camera={camera.position.ToString("F3")} cameraRotation={camera.rotation.eulerAngles.ToString("F2")} aim={aim.ToString("F3")} cameraToBody={targetDistance:F3} cameraToAim={Vector3.Distance(camera.position,aim):F3} desiredGap={desiredGap:F3} overlapAim={rig.WorldOverlapAtAim} clipped={rig.WorldClipped} overlapCamera={rig.WorldOverlapAtCamera}");
        }
        float progress=Vector3.Dot(body.position-start,uphill);
        Debug.Log($"[CAMERA-RAMP18 SUMMARY] kind={kind} progress={progress:F3} groundedFrames={grounded}/100 overlapAimFrames={aimOverlaps} clippedFrames={worldClips} overlapCameraFrames={cameraOverlaps} maxCameraToBody={maxDistance:F3} maxDesiredGap={maxDesiredGap:F3}");
        Assert.That(Time.fixedTime-firstFixedTime,Is.GreaterThanOrEqualTo(99*Time.fixedDeltaTime),"Samples must represent real physics steps.");
        Assert.That(grounded,Is.GreaterThan(0),"The real wheels must contact the ramp during this diagnostic.");
        Assert.That(progress,Is.GreaterThan(.05f),"R6 forces must actually move the vehicle uphill; otherwise the camera result is inconclusive.");
        Assert.That(cameraOverlaps,Is.Zero,"The actual camera volume must not penetrate the world during the sampled traverse.");
    }

    static void SetFollowField(P12PipelineSafeCamera camera,string name,object value)
    {
        var field=typeof(P12PipelineSafeCamera).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That(field,Is.Not.Null,$"Camera diagnostic field {name} is unavailable.");field.SetValue(camera,value);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}
