using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
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
    [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
