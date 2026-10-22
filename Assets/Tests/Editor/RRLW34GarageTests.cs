using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;

public class RRLW34GarageTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
    }
    string Receive(float hp=1,Vector3Int rotation=default,int ports=R3MountingGrid.AllFaces)
    {
        var p=new R3PartState(default,R3PartKind.BonusModule,hp){UpgradeId="garage-module",
            UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.25f,RotationQuarterTurns=rotation,ContactMask=ports};
        Assert.That(run.VehicleModel.ReceiveSalvage(new[]{p}),Is.True);return p.Id;
    }
    [UnityTest] public IEnumerator SixShortcutsRotateOnlyTheirAxisAndInverseUndoesWithoutSpending()
    {
        var garage=run.Garage;garage.ChooseInventory(Receive());garage.Select(new Vector3Int(1,2,0));
        int funds=run.VehicleModel.Scrap;
        var keys=new[]{KeyCode.Q,KeyCode.E,KeyCode.R,KeyCode.F,KeyCode.Z,KeyCode.X};
        var expected=new[]{new Vector3Int(0,3,0),Vector3Int.zero,new Vector3Int(1,0,0),Vector3Int.zero,new Vector3Int(0,0,3),Vector3Int.zero};
        for(int i=0;i<keys.Length;i++)
        {
            var input=new Event{type=EventType.KeyDown,keyCode=keys[i]};
            garage.HandleGuiPointer(input,new Vector2(640,360));
            Assert.That(input.type,Is.EqualTo(EventType.Used));Assert.That(garage.SelectedRotation,Is.EqualTo(expected[i]));
            Assert.That(Quaternion.Angle(garage.Preview.localRotation,R3MountingGrid.Rotation(expected[i])),Is.LessThan(.01));
        }
        garage.SetTool(R41GarageTool.Repair);
        garage.HandleGuiPointer(new Event{type=EventType.KeyDown,keyCode=KeyCode.R},new Vector2(640,360));
        Assert.That(garage.SelectedRotation,Is.EqualTo(Vector3Int.zero));Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator RotatedGreenFootprintPortsAndActualMountAgreeAfterVehicleYaw()
    {
        run.Player.transform.rotation=Quaternion.Euler(0,37,0);
        var garage=run.Garage;string id=Receive();garage.ChooseInventory(id);garage.RotateSelected(0,1);
        garage.Select(new Vector3Int(1,3,0));Assert.That(garage.PreviewValid,Is.True,garage.PlacementIssue);
        var ghost=garage.Preview;var position=ghost.position;var rotation=ghost.rotation;
        Assert.That(ghost.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(4),"Footprint mounting ports must be visible.");
        int funds=run.VehicleModel.Scrap;Assert.That(garage.ClickTarget(),Is.True);
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(Vector3.Distance(part.transform.position,position),Is.LessThan(.001));
        Assert.That(Quaternion.Angle(part.transform.rotation,rotation),Is.LessThan(.01));
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).RotationQuarterTurns,Is.EqualTo(new Vector3Int(1,0,0)));
        Assert.That(garage.PreviewValid,Is.False);yield return null;
    }
    [UnityTest] public IEnumerator ZeroHpBoundsAndMismatchedPortsNeverShowGreenOrSpend()
    {
        var garage=run.Garage;int funds=run.VehicleModel.Scrap;
        string dead=Receive(0);garage.ChooseInventory(dead);garage.Select(new Vector3Int(1,2,0));
        Assert.That(garage.PreviewValid,Is.False);Assert.That(garage.PlacementIssue,Does.Contain("odbudowy"));
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(run.GarageMount(dead,garage.Cell),Is.False);
        string blocked=Receive(ports:0);garage.ChooseInventory(blocked);garage.Select(new Vector3Int(1,2,0));
        Assert.That(garage.PreviewValid,Is.False);Assert.That(garage.ClickTarget(),Is.False);
        string healthy=Receive();garage.ChooseInventory(healthy);garage.Select(new Vector3Int(6,2,0));
        Assert.That(garage.PreviewValid,Is.False);Assert.That(garage.ClickTarget(),Is.False);
        garage.Select(new Vector3Int(0,1,0));Assert.That(garage.PreviewValid,Is.False);
        Assert.That(garage.ClickTarget(),Is.False);Assert.That(run.VehicleModel.Scrap,Is.EqualTo(funds));
        yield return null;
    }
    [UnityTest] public IEnumerator ControllerRemountPreservesStoredOrientationIdPortsAndHp()
    {
        var turns=new Vector3Int(0,1,0);string id=Receive(.4f,turns);
        Assert.That(run.GarageMount(id,new Vector3Int(1,2,0),turns),Is.True);
        Assert.That(run.GarageRemove(new Vector3Int(1,2,0)),Is.True);
        Assert.That(run.VehicleModel.Inventory.Single(p=>p.Id==id).RotationQuarterTurns,Is.EqualTo(turns));
        Assert.That(run.GarageMount(id,new Vector3Int(1,2,0)),Is.True);
        var part=run.VehicleModel.Parts.Single(p=>p.Id==id);
        Assert.That(part.RotationQuarterTurns,Is.EqualTo(turns));Assert.That(part.Health,Is.EqualTo(.4f).Within(.001));
        Assert.That(part.ContactMask,Is.EqualTo(R3MountingGrid.AllFaces));
        var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        Assert.That(mounted.Damageable.CurrentHp/mounted.Damageable.MaxHp,Is.EqualTo(.4f).Within(.001));yield return null;
    }
    [UnityTest] public IEnumerator SwitchingIdenticalShapesRefreshesTheirDifferentContactMarkers()
    {
        var garage=run.Garage;string open=Receive();string closed=Receive(ports:0);
        garage.ChooseInventory(open);garage.Select(new Vector3Int(1,2,0));
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Punkt mocowania ")),Is.EqualTo(16));
        garage.ChooseInventory(closed);garage.Select(new Vector3Int(1,2,0));
        Assert.That(garage.Preview.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Punkt mocowania ")),Is.Zero,
            "Switching equal shapes must display the selected item's own ports, not the preceding item's ports.");
        Assert.That(garage.PreviewValid,Is.False);yield return null;
    }

    [TestCase(1280,720)][TestCase(1920,1080)]
    public void BothRequiredResolutionsKeepPanelsOutsidePlacement(int width,int height)
    {
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*.1f,height*.5f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*.85f,height*.5f),width,height),Is.False);
        Assert.That(R3GarageView.InBuildArea(new Vector2(width*.5f,height*.5f),width,height),Is.True);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
