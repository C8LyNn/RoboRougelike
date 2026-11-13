using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R1;
using RoboRougelike.R6;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogPartFusionTests
{
    [Test]
    public void WreckCanBeFusedAndResultHasAveragePercentageHealth()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        model.Inventory[0].Health=.5f;model.Inventory[1].Health=0;model.Inventory[2].Health=1;
        var ids=model.Inventory.Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(ids),Is.True);
        Assert.That(model.Inventory.Single().Health,Is.EqualTo(.5f).Within(.001));
        Assert.That(model.Inventory.Single().Tier,Is.EqualTo(2));
    }
    [Test]
    public void FusionPreviewShowsResultHealthIncludingWrecks()
    {
        var method=typeof(R1RunHUD).GetMethod("FusionPreviewLabel",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That(method,Is.Not.Null,"The garage must preview the resulting tier and condition before consuming items.");
        var items=new[]{new R3PartState(default,R3PartKind.Armor,.5f),new R3PartState(default,R3PartKind.Armor,0),new R3PartState(default,R3PartKind.Armor,1)};
        var text=(string)method.Invoke(null,new object[]{1,items});
        Assert.That(text,Does.Contain("T2"));Assert.That(text,Does.Contain("50%"));
    }
    [Test]
    public void ThreeIdenticalOwnedPartsCanBecomeOneHigherTier()
    {
        var method=typeof(R3VehicleModel).GetMethod("FuseInventory",new[]{typeof(IEnumerable<string>)});
        Assert.That(method,Is.Not.Null,"The garage needs a 3:1 physical inventory fusion operation.");
    }
    [Test]
    public void NineArmorPiecesBecomeOneTierThreeWithoutFreeRepairOrDuplicatedIds()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<9;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var originals=model.Inventory.Select(p=>p.Id).ToArray();
        model.Inventory[0].Health=.1f;model.Inventory[1].Health=.4f;model.Inventory[2].Health=1f;
        var first=model.Inventory.Take(3).Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(first),Is.True);
        var firstTier2=model.Inventory.Single(p=>p.Tier==2);
        Assert.That(firstTier2.Health,Is.EqualTo(.5f).Within(.001));
        Assert.That(originals.Contains(firstTier2.Id),Is.False);
        for(int i=0;i<2;i++)Assert.That(model.FuseInventory(model.Inventory.Where(p=>p.Tier==1).Take(3).Select(p=>p.Id).ToArray()),Is.True);
        var tier2=model.Inventory.Select(p=>p.Id).ToArray();Assert.That(tier2.Length,Is.EqualTo(3));
        Assert.That(model.FuseInventory(tier2),Is.True);
        var tier3=model.Inventory.Single();Assert.That(tier3.Tier,Is.EqualTo(3));
        Assert.That(tier3.Health,Is.EqualTo(5f/6f).Within(.001));
        Assert.That(originals.Concat(tier2).Contains(tier3.Id),Is.False);
    }
    [Test]
    public void InvalidFusionNeverConsumesParts()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Armor),Is.True);
        var before=model.Inventory.Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(new[]{before[0],before[0],before[1]}),Is.False);
        Assert.That(model.FuseInventory(new[]{before[0],before[1],before[3]}),Is.False);
        Assert.That(model.FuseInventory(new[]{before[0],before[1],"missing"}),Is.False);
        model.Inventory[0].Tier=2;
        Assert.That(model.FuseInventory(new[]{before[0],before[1],before[2]}),Is.False,"Different tiers cannot be mixed.");
        Assert.That(model.Inventory.Select(p=>p.Id),Is.EqualTo(before));
    }
    [Test]
    public void LegacyTierZeroLoadsAsTierOneAndNewTierSurvivesCheckpoint()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Wheel),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
        var data=model.MakeCheckpoint(0);data.mounted[0].Tier=0;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Light,model.Economy,0,out var loaded,out var error),Is.True,error);
        Assert.That(loaded.Parts[0].Tier,Is.EqualTo(1));Assert.That(loaded.Inventory.Single().Tier,Is.EqualTo(2));
        string path=Path.Combine(Path.GetTempPath(),"rrl-tier-"+Guid.NewGuid().ToString("N")+".json");
        try{R1RunCheckpointStorage.Write(path,new R1RunCheckpoint{version=2,model=data});Assert.That(R1RunCheckpointStorage.Read(path).version,Is.EqualTo(2));}
        finally{if(File.Exists(path))File.Delete(path);}
    }
}

public sealed class BacklogPartFusionPhysicsTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator TierTwoWheelHasPhysicalDurabilityMassAndMoreTraction()
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Parts.First(p=>p.Kind==R3PartKind.Wheel).Tier=2;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,.7f,0),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var drive=vehicle.GetComponent<R6VehicleDrive>();
        var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Tier==2);
        Assert.That(wheel.Damageable.MaxHp,Is.EqualTo(160).Within(.001));
        Assert.That(R6VehicleDrive.PartMass(wheel.Damageable),Is.EqualTo(13.8f).Within(.001));
        float deadline=Time.realtimeSinceStartup+8;
        while(drive.GroundedWheels<4&&Time.realtimeSinceStartup<deadline){yield return new WaitForFixedUpdate();drive.RefreshMass();}
        Assert.That(drive.GroundedWheels,Is.EqualTo(4));
        drive.RefreshMass();float upgraded=drive.AvailableForce;
        wheel.Tier=1;drive.RefreshMass();Assert.That(drive.AvailableForce,Is.LessThan(upgraded));
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(floor);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

public sealed class BacklogPartFusionGarageTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run?.Progress,Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),Is.True);
    }
    [UnityTest] public IEnumerator GarageFusionProducesOwnedTierTwoWheelThatCanBeMounted()
    {
        for(int i=0;i<3;i++)Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);
        var ids=run.VehicleModel.Inventory.Select(p=>p.Id).ToArray();
        Assert.That(run.GarageFuse(ids),Is.True);
        var result=run.VehicleModel.Inventory.Single();Assert.That(result.Tier,Is.EqualTo(2));
        Assert.That(run.Garage.SelectedInventoryId,Is.EqualTo(result.Id));
        var cells=run.VehicleModel.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct();
        var cell=cells.First(c=>run.VehicleModel.OwnedPlacementError(result.Id,c,default)==null);
        Assert.That(run.GarageMount(result.Id,cell),Is.True);
        var actual=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==result.Id);
        Assert.That(actual.Tier,Is.EqualTo(2));Assert.That(actual.Damageable.MaxHp,Is.EqualTo(160).Within(.001));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
