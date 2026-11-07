using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class R4WedgeArmorTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
    }

    [UnityTest] public IEnumerator PurchasedWedgeUsesRealSlopedColliderAndSurvivesCheckpoint()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        Assert.That(R41PartGeometry.WedgeAvailable,Is.True,"Source geometry and visual must be bundled.");
        Assert.That(run.GaragePurchase(R3PartKind.Armor,default,R3WheelStyle.Offroad,R3ArmorShape.Wedge),Is.True);
        var item=model.Inventory.Last();var id=item.Id;
        Assert.That(item.ContactMask,Is.EqualTo(R41PartGeometry.WedgeFullContacts));
        Assert.That(R3MountingGrid.PortOnFace(item,Vector3Int.right),Is.False);
        Assert.That(R3MountingGrid.PortOnFace(item,Vector3Int.down),Is.True);
        Assert.That(R3VehicleModel.SameFusionType(item,new R3PartState(default,R3PartKind.Armor)),Is.False);
        var garage=run.Garage;garage.ChooseInventory(id);garage.Select(new Vector3Int(2,2,2));
        Assert.That(garage.Preview.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled),Is.True);
        Assert.That(garage.Preview.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
        var collider=part.GetComponent<MeshCollider>();Assert.That(collider,Is.Not.Null);Assert.That(collider.convex,Is.True);
        Assert.That(part.GetComponent<BoxCollider>(),Is.Null,"The wedge must not retain a cube collider.");
        var t=part.transform;
        var highRay=new Ray(t.TransformPoint(new Vector3(0,2,-.38f)), -t.up);
        var lowRay=new Ray(t.TransformPoint(new Vector3(0,2,.38f)), -t.up);
        Assert.That(collider.Raycast(highRay,out var high,3),Is.True);
        Assert.That(collider.Raycast(lowRay,out var low,3),Is.True);
        Assert.That(high.point.y-low.point.y,Is.GreaterThan(.2f),"The physical collider must slope down toward its open side.");
        var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
        Assert.That(restored.Parts.Single(p=>p.Id==id).ArmorShape,Is.EqualTo(R3ArmorShape.Wedge));
        data.mounted.Single(p=>p.Id==id).ContactMask=R3MountingGrid.AllFaces;
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out _,out _),Is.False);
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
