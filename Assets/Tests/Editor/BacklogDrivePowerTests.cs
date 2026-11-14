using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using RoboRougelike.R6;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogDrivePowerTests
{
    [Test] public void VehicleMassContinuesToAffectSpeedAndSteeringBeyondOldThresholds()
    {
        Assert.That(R3VehicleAssembly.SpeedFor(2000,4),Is.LessThan(R3VehicleAssembly.SpeedFor(1000,4)));
        Assert.That(R3VehicleAssembly.TurnFor(2000),Is.LessThan(R3VehicleAssembly.TurnFor(1000)));
        Assert.That(R3VehicleAssembly.SpeedFor(50,4),Is.GreaterThan(14));
        Assert.That(R3VehicleAssembly.TurnFor(50),Is.GreaterThan(130));
    }
    [Test] public void MotorCopiesContinueAddingPowerWithoutAnArbitraryFiveUnitCap()
    {
        Assert.That(R6VehicleDrive.MotorMultiplier(5),Is.EqualTo(2).Within(.001));
        Assert.That(R6VehicleDrive.MotorMultiplier(6),Is.EqualTo(2.2f).Within(.001));
        Assert.That(R6VehicleDrive.MotorMultiplier(12),Is.EqualTo(3.4f).Within(.001));
        Assert.That(R6VehicleDrive.MotorMultiplier(float.NaN),Is.EqualTo(1));
        Assert.That(R6VehicleDrive.MotorMultiplier(float.PositiveInfinity),Is.EqualTo(1));
    }
    [Test]
    public void DrivePowerIsAPhysicalPassivePartWithEnergyReservation()
    {
        Assert.That(Enum.TryParse("DrivePower",out R3PartKind kind),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(500);float maximum=model.Energy.Maximum;
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,kind,false)==null);
        Assert.That(model.Buy(cell,kind),Is.True);
        model.RefreshEnergyLayout();
        Assert.That(model.Energy.Maximum,Is.EqualTo(maximum-25));
        Assert.That(model.At(cell).Kind,Is.EqualTo(kind));
    }
}

public sealed class BacklogDrivePowerIntegrationTests
{
    [UnitySetUp] public IEnumerator Setup()
    {EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}

    [UnityTest] public IEnumerator SixthMountedMotorIncreasesTheRealWheelForce()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(5000);
        model.Parts.RemoveAll(p=>p.Kind==R3PartKind.DrivePower);model.RefreshEnergyLayout();
        for(int i=0;i<6;i++)
        {
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.DrivePower,false)==null);
            Assert.That(model.Buy(cell,R3PartKind.DrivePower),Is.True);
        }
        var paletteObject=new GameObject("motor QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,0),Quaternion.identity);
        var drive=vehicle.GetComponent<R6VehicleDrive>();var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        float deadline=Time.realtimeSinceStartup+8;
        while(drive.GroundedWheels==0&&Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();drive.RefreshMass();}
        Assert.That(drive.GroundedWheels,Is.GreaterThan(0));drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(6));float six=drive.AvailableForce;
        assembly.TuneChassis();float sixTurn=vehicle.Controller.EffectiveTurnSpeed;float sixMass=vehicle.GetComponent<Rigidbody>().mass;
        var upgraded=assembly.Parts.First(p=>p.Kind==R3PartKind.DrivePower);upgraded.Tier=2;assembly.TuneChassis();
        float upgradedMass=vehicle.GetComponent<Rigidbody>().mass;
        float expected=R3VehicleAssembly.TurnFor(upgradedMass)/R3VehicleAssembly.TurnFor(sixMass)*(1f+.12f*7)/(1f+.12f*6);
        Assert.That(vehicle.Controller.EffectiveTurnSpeed/sixTurn,Is.EqualTo(expected).Within(.001),
            "Motor steering must continue beyond five units while accounting for the tier's mass.");
        vehicle.Controller.ConfigureChassis(7,400,upgradedMass);
        Assert.That(vehicle.Controller.EffectiveTurnSpeed,Is.EqualTo(400),"The controller must not silently clamp the configured turning target to 130.");
        drive.Step(0,0,7,4,vehicle.Controller.EffectiveTurnSpeed);
        Assert.That(vehicle.GetComponent<Rigidbody>().maxAngularVelocity,Is.GreaterThanOrEqualTo(400*Mathf.Deg2Rad),
            "The rigidbody must allow the steering target instead of silently enforcing 3 radians per second.");
        vehicle.Controller.SetRunModifiers(1,1,float.MaxValue);
        Assert.That(float.IsInfinity(vehicle.Controller.EffectiveTurnSpeed),Is.False,"Large finite bonuses must not create an infinite target.");
        Assert.That(float.IsInfinity(vehicle.Controller.EffectiveForwardSpeed),Is.False);
        vehicle.Controller.SetRunModifiers(1,1,1);vehicle.Controller.ConfigureChassis(20,400,upgradedMass);
        Assert.That(vehicle.Controller.EffectiveForwardSpeed,Is.EqualTo(20));
        vehicle.Controller.ConfigureChassis(.5f,400,upgradedMass);
        Assert.That(vehicle.Controller.EffectiveForwardSpeed,Is.EqualTo(.5f));
        vehicle.Controller.SetRunModifiers(1,1,1);
        upgraded.Tier=1;assembly.TuneChassis();
        assembly.Parts.First(p=>p.Kind==R3PartKind.DrivePower).gameObject.SetActive(false);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(5));
        Assert.That(six,Is.GreaterThan(drive.AvailableForce));
        Assert.That(six/drive.AvailableForce,Is.EqualTo(2.2f/2f).Within(.001));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }

    [UnityTest] public IEnumerator RealWheelsKeepPassiveMotorPowerUntilDestruction()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        // Isolate one motor below the power cap so its contribution is measurable.
        model.Parts.RemoveAll(p=>p.Kind==R3PartKind.DrivePower);model.RefreshEnergyLayout();
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
            .First(c=>model.PlacementError(c,R3PartKind.DrivePower,false)==null);
        Assert.That(model.Buy(cell,R3PartKind.DrivePower),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,0),Quaternion.identity);
        var drive=vehicle.GetComponent<R6VehicleDrive>();var assembly=vehicle.GetComponent<R3VehicleAssembly>();
        float deadline=Time.realtimeSinceStartup+8;
        while(drive.GroundedWheels==0&&Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();drive.RefreshMass();}
        Assert.That(drive.GroundedWheels,Is.GreaterThan(0),"Motor force requires real wheel-ground contact.");
        var motor=assembly.Parts.Single(p=>p.Kind==R3PartKind.DrivePower);
        drive.RefreshMass();float fullForce=drive.AvailableForce,fullTurn=vehicle.Controller.EffectiveTurnSpeed;
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(1).Within(.001f));
        var body=vehicle.GetComponent<Rigidbody>();float fullMass=body.mass;
        motor.gameObject.SetActive(false);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.Zero,"An inactive motor cannot drive the wheels.");
        Assert.That(drive.AvailableForce,Is.LessThan(fullForce));
        Assert.That(body.mass,Is.EqualTo(fullMass-R6VehicleDrive.PartMass(motor.Damageable)).Within(.01f));
        motor.gameObject.SetActive(true);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(1).Within(.001f));
        Assert.That(body.mass,Is.EqualTo(fullMass).Within(.01f));
        var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Operational);int wheels=drive.Wheels;
        wheel.gameObject.SetActive(false);drive.RefreshMass();
        Assert.That(drive.Wheels,Is.EqualTo(wheels-1),"An inactive wheel cannot count toward support.");
        Assert.That(body.mass,Is.EqualTo(fullMass-R6VehicleDrive.PartMass(wheel.Damageable)).Within(.01f));
        wheel.gameObject.SetActive(true);drive.RefreshMass();
        Assert.That(drive.Wheels,Is.EqualTo(wheels));Assert.That(body.mass,Is.EqualTo(fullMass).Within(.01f));
        motor.Damageable.RestoreHealthRatio(.5f);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(1f).Within(.001f),"A passive module retains its full effect until destroyed.");
        Assert.That(drive.AvailableForce,Is.EqualTo(fullForce).Within(.001f));
        Assert.That(vehicle.Controller.EffectiveTurnSpeed,Is.EqualTo(fullTurn).Within(.001f));
        motor.Damageable.RestoreHealthRatio(.01f);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(1f).Within(.001f));
        motor.Damageable.ApplyDamage(100000,motor.transform.position,Vector3.up);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.Zero);
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTest] public IEnumerator HeavyChassisQueriesDoNotAllocatePerPartOnEveryRefresh()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Heavy));
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,0),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();vehicle.GetComponent<Rigidbody>().isKinematic=true;
        for(int i=0;i<3;i++)assembly.TuneChassis();
        using(var recorder=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame",16))
        {
            Assert.That(recorder.Valid,Is.True);yield return null;yield return null;
            long idle=recorder.LastValue;
            for(int i=0;i<50;i++)assembly.TuneChassis();
            yield return null;long sample=recorder.LastValue;
            var control=new byte[1024*1024];control[0]=1;yield return null;long controlBytes=recorder.LastValue;GC.KeepAlive(control);
            Debug.Log($"[CHASSIS_ALLOC] calls=50 parts={model.Parts.Count} idle={idle} sample={sample} control={controlBytes}");
            Assert.That(controlBytes,Is.GreaterThanOrEqualTo(1024*1024));
            Assert.That(sample-idle,Is.LessThan(20000),"Chassis refresh should not allocate in proportion to vehicle size.");
        }
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTest] public IEnumerator ManyNonTerrainCollidersCannotHideRealWheelGround()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,R3VehicleModel.WheelRadius(R3WheelStyle.Offroad),0),Quaternion.identity);
        var drive=vehicle.GetComponent<R6VehicleDrive>();vehicle.GetComponent<Rigidbody>().isKinematic=true;
        Physics.SyncTransforms();drive.RefreshMass();int grounded=drive.GroundedWheels;
        Assert.That(grounded,Is.EqualTo(4),"The real vehicle must first have wheel contact with the floor.");
        var clutter=new GameObject("Dense non-terrain physics objects");clutter.AddComponent<Rigidbody>().isKinematic=true;
        for(int i=0;i<128;i++)
        {
            var layer=new GameObject("Non-terrain collider "+i);layer.transform.SetParent(clutter.transform,false);
            layer.transform.localPosition=new Vector3(0,.02f+i*.0015f,0);
            layer.AddComponent<BoxCollider>().size=new Vector3(100,.0003f,100);
        }
        Physics.SyncTransforms();
        var wheel=vehicle.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel);
        var probe=new RaycastHit[64];
        Assert.That(Physics.RaycastNonAlloc(wheel.transform.position,Vector3.down,probe,1f,~0,QueryTriggerInteraction.Ignore),Is.EqualTo(probe.Length),
            "The fixture must actually fill the former 64-hit buffer.");
        drive.RefreshMass();Assert.That(drive.GroundedWheels,Is.EqualTo(grounded),"Unrelated bodies must not make valid ground disappear.");
        floor.transform.position=Vector3.down*5;Physics.SyncTransforms();drive.RefreshMass();
        Assert.That(drive.GroundedWheels,Is.Zero,"Non-terrain colliders must not become imaginary ground.");
        UnityEngine.Object.Destroy(clutter);UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(floor);
        UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
