using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.R4_1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WheelVariantsTests
{
    [UnitySetUp]public IEnumerator Setup(){EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();while(R1RunController.Instance?.Progress==null)yield return null;}
    [UnityTest]public IEnumerator RealGarageSwitchesWheelAppearanceAndPersistsIt()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var state=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel);var id=state.Id;var cell=state.Cell;
        var before=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var collider=before.transform.Find("Koło").GetComponent<Collider>();Assert.That(collider.enabled,Is.True);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(wheel.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Slick_Rotor"),Is.True);
        Assert.That(wheel.transform.Find("Koło").GetComponent<Collider>().enabled,Is.True);
        var checkpoint=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(run.VehicleModel.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(checkpoint,R3VehicleKind.Standard,run.VehicleModel.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That(R3VehicleModel.SameFusionType(state,new R3PartState(cell,R3PartKind.Wheel)),Is.False);
        Assert.That(run.GarageToggleWheelStyle(cell),Is.True);yield return null;
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
    }
    [UnityTest]public IEnumerator BothWheelAppearancesCanBePurchasedDirectlyAndSurviveCheckpoint()
    {
        var run=R1RunController.Instance;Assert.That(run.PrepareUnifiedRun(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);int startingScrap=model.Scrap;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Offroad),Is.True);
        var offroad=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);
        var slick=model.Inventory.Last();
        Assert.That(offroad.WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
        Assert.That(slick.WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
        Assert.That(R3VehicleModel.SameFusionType(offroad,slick),Is.False);
        Assert.That(model.Scrap,Is.EqualTo(startingScrap-2*model.Price(R3PartKind.Wheel)));
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,(R3WheelStyle)99),Is.False);
        Assert.That(run.GaragePurchase(R3PartKind.Armor,default,R3WheelStyle.Slick),Is.False);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Inventory.Single(p=>p.Id==offroad.Id).WheelStyle,Is.EqualTo(R3WheelStyle.Offroad));
        Assert.That(restored.Inventory.Single(p=>p.Id==slick.Id).WheelStyle,Is.EqualTo(R3WheelStyle.Slick));
    }
    [UnityTest]public IEnumerator ChangingWheelStackUpdatesGhostAndRepeatedMountKeepsSameAppearance()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);
        var support=model.Inventory.Last();var garage=run.Garage;garage.ChooseInventory(support.Id);
        garage.Select(new Vector3Int(2,2,2));Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);var offroad=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);var slick=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.Slick),Is.True);var nextSlick=model.Inventory.Last();
        garage.ChooseInventory(offroad.Id);garage.Select(Vector3Int.zero);
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Offroad_Rotor"),Is.True);
        garage.ChooseInventory(slick.Id);garage.Select(Vector3Int.zero);
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_WheelM_Side_Slick_Rotor"),Is.True);
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().ToArray();
        var target=candidates.FirstOrDefault(c=>model.OwnedPlacementError(slick.Id,c,Vector3Int.zero)==null);
        Assert.That(model.OwnedPlacementError(slick.Id,target,Vector3Int.zero),Is.Null,"Expected a valid real wheel mount; " +
            string.Join(", ",candidates.Select(c=>model.OwnedPlacementError(slick.Id,c,Vector3Int.zero)).GroupBy(e=>e).Select(g=>$"{g.Key}: {g.Count()}"))+
            $" wheels={model.Parts.Count(p=>p.Kind==R3PartKind.Wheel)}");
        garage.Select(target);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);
        Assert.That(garage.SelectedInventoryId,Is.EqualTo(nextSlick.Id));
    }
    [UnityTest]public IEnumerator LargeWheelUsesItsRealSizeContactAndOwnedStyle()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var garage=run.Garage;
        foreach(var cell in new[]{new Vector3Int(2,2,2),new Vector3Int(2,3,2)})
        {
            Assert.That(run.GaragePurchase(R3PartKind.Armor),Is.True);var support=model.Inventory.Last();
            garage.ChooseInventory(support.Id);garage.Select(cell);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        }
        Assert.That(run.GaragePurchase(R3PartKind.Wheel),Is.True);var medium=model.Inventory.Last();
        Assert.That(run.GaragePurchase(R3PartKind.Wheel,default,R3WheelStyle.LargeOffroad),Is.True);var large=model.Inventory.Last();
        Assert.That(R3VehicleModel.SameFusionType(medium,large),Is.False);
        var candidates=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().ToArray();
        var target=candidates.FirstOrDefault(c=>model.OwnedPlacementError(large.Id,c,Vector3Int.zero)==null);
        Assert.That(model.OwnedPlacementError(large.Id,target,Vector3Int.zero),Is.Null,"Expected room for a large wheel.");
        Assert.That(model.OwnedPlacementError(medium.Id,target,Vector3Int.zero),Is.Null);
        var body=run.Player.GetComponent<Rigidbody>();
        var tireCenter=run.Player.transform.TransformPoint((Vector3)target*.55f+Vector3.down*.26f);
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=tireCenter+run.Player.transform.forward*.48f;
        blocker.transform.localScale=Vector3.one*.06f;Physics.SyncTransforms();
        Assert.That(model.OwnedPlacementError(medium.Id,target,Vector3Int.zero),Is.Null,"The medium tire clears the obstacle.");
        Assert.That(model.OwnedPlacementError(large.Id,target,Vector3Int.zero),Is.Not.Null,"The real larger tire must not intersect the obstacle.");
        Object.Destroy(blocker);yield return null;Physics.SyncTransforms();
        garage.ChooseInventory(large.Id);garage.Select(target);Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==large.Id);
        var tire=part.transform.Find("Koło").GetComponent<Collider>();
        Assert.That(tire.bounds.extents.y,Is.EqualTo(.56f).Within(.02f));
        var rotor=part.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="R4M_WheelL_Side_Offroad_Rotor");
        Assert.That(Vector3.Distance(rotor.position,tire.bounds.center),Is.LessThan(.01f));
        Assert.That(R6VehicleDrive.PartMass(part.Damageable),Is.EqualTo(18).Within(.01f));
        Assert.That(run.GarageToggleWheelStyle(target),Is.False,"Changing geometry must require a validated remount.");
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.localScale=new Vector3(.4f,.2f,.4f);
        floor.transform.position=new Vector3(tire.bounds.center.x,tire.bounds.min.y-.1f,tire.bounds.center.z);Physics.SyncTransforms();
        var contacts=new R6WheelContacts();contacts.Refresh(run.Player.GetComponent<Rigidbody>(),new[]{part.Damageable});
        Assert.That(contacts.Count,Is.EqualTo(1));Assert.That(contacts.Traction,Is.EqualTo(1.25f).Within(.01f));
        Object.Destroy(floor);
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==large.Id).WheelStyle,Is.EqualTo(R3WheelStyle.LargeOffroad));
    }
    [UnityTest]public IEnumerator LargeWheelsDriveActualRigidVehicleOnGround()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var palette=(R3VisualPalette)typeof(R1RunController).GetField("palette",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        var design=new R3VehicleDesign{Name="Large wheel drive QA",Speed=7,TurnSpeed=90,CoreHp=100};
        design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
        for(int z=1;z<=3;z++)design.Parts.Add(new R3PartState(new Vector3Int(0,0,z),R3PartKind.Armor));
        foreach(int x in new[]{-1,1})foreach(int z in new[]{0,3})
            design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),R3PartKind.Wheel){WheelStyle=R3WheelStyle.LargeOffroad});
        var model=new R3VehicleModel(design);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position=new Vector3(1000,-.1f,1000);floor.transform.localScale=new Vector3(30,.2f,30);
        var vehicle=R3VehicleAssembly.Build(model,run.Progress.Stats,palette,new Vector3(1000,.82f,1000),Quaternion.identity);
        var tank=vehicle.GetComponent<P12PipelineSafeTankController>();tank.enabled=false;
        var drive=vehicle.GetComponent<R6VehicleDrive>();Assert.That(drive,Is.Not.Null);Time.timeScale=1;
        try
        {
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            float start=vehicle.transform.position.z;int grounded=0;
            for(int i=0;i<80;i++){drive.Step(1,0,7,4,90);grounded=Mathf.Max(grounded,drive.GroundedWheels);yield return new WaitForFixedUpdate();}
            Assert.That(grounded,Is.GreaterThanOrEqualTo(2),"Real large tires must register ground support.");
            Assert.That(vehicle.transform.position.z-start,Is.GreaterThan(.5f),"The actual R6 body must travel forward.");
        }
        finally{Object.Destroy(vehicle.gameObject);Object.Destroy(floor);}
    }
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
