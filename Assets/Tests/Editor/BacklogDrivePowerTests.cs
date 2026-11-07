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

    [UnityTest] public IEnumerator RealWheelsUseWorkingMotorPowerAndLoseItOnDamage()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
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
        motor.Damageable.RestoreHealthRatio(.5f);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.EqualTo(.5f).Within(.001f));
        Assert.That(drive.AvailableForce,Is.LessThan(fullForce));
        Assert.That(vehicle.Controller.EffectiveTurnSpeed,Is.LessThan(fullTurn));
        motor.Damageable.ApplyDamage(100000,motor.transform.position,Vector3.up);drive.RefreshMass();
        Assert.That(drive.ActivePowerUnits,Is.Zero);
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
