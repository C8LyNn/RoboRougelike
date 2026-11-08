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
        Assert.That(part.GetComponent<Renderer>().enabled,Is.False,"The supplied wedge model replaces the temporary shell.");
        Assert.That(part.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True,"The authored model must actually render.");
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

    [UnityTest] public IEnumerator RotatedWedgeCanBeDamagedRepairedAndReturnedToInventory()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);
        Assert.That(run.GaragePurchase(R3PartKind.Armor,default,R3WheelStyle.Offroad,R3ArmorShape.Wedge),Is.True);
        var owned=model.Inventory.Last();string id=owned.Id;var cell=new Vector3Int(2,2,2);
        var garage=run.Garage;garage.ChooseInventory(id);garage.RotateSelected(1,1);garage.Select(cell);
        Assert.That(garage.ClickTarget(),Is.True,garage.PlacementIssue);yield return null;
        var mounted=model.At(cell);Assert.That(mounted.RotationQuarterTurns.y,Is.EqualTo(1));
        Assert.That(R3MountingGrid.PortOnFace(mounted,R3MountingGrid.Rotate(Vector3Int.back,mounted.RotationQuarterTurns)),Is.True);
        Assert.That(R3MountingGrid.PortOnFace(mounted,R3MountingGrid.Rotate(Vector3Int.right,mounted.RotationQuarterTurns)),Is.False);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var part=assembly.Parts.Single(p=>p.Id==id);
        part.Damageable.ApplyDamage(part.Damageable.MaxHp*.4f,part.transform.position,Vector3.up);assembly.Capture();
        Assert.That(model.At(cell).Health,Is.LessThan(1));Assert.That(run.GarageRepair(cell),Is.True);
        Assert.That(model.At(cell).Health,Is.EqualTo(1));Assert.That(run.GarageRemove(cell),Is.True);
        Assert.That(model.Inventory.Single(p=>p.Id==id).ArmorShape,Is.EqualTo(R3ArmorShape.Wedge));
    }

    [UnityTest] public IEnumerator AdditionalShapesAreDistinctPhysicalOwnedParts()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);yield return null;
        var model=run.VehicleModel;model.Award(1000);var cell=new Vector3Int(2,2,2);
        foreach(var shape in new[]{R3ArmorShape.HalfBlock,R3ArmorShape.Pyramid,R3ArmorShape.RoundEdge,R3ArmorShape.HalfWedge,R3ArmorShape.Dome})
        {
            Assert.That(R41PartGeometry.ArmorShapeAvailable(shape),Is.True,shape.ToString());
            Assert.That(run.GaragePurchase(R3PartKind.Armor,default,R3WheelStyle.Offroad,shape),Is.True);
            var item=model.Inventory.Last();string id=item.Id;
            Assert.That(item.ContactMask,Is.EqualTo(R41PartGeometry.FullArmorContacts(shape)));
            Assert.That(R3MountingGrid.PortOnFace(item,Vector3Int.down),Is.True);
            Assert.That(R3MountingGrid.PortOnFace(item,Vector3Int.right),Is.False);
            run.Garage.ChooseInventory(id);run.Garage.Select(cell);
            Assert.That(run.Garage.ClickTarget(),Is.True,run.Garage.PlacementIssue);yield return null;
            var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==id);
            var collider=part.GetComponent<MeshCollider>();Assert.That(collider,Is.Not.Null);
            Assert.That(collider.convex,Is.True);Assert.That(part.GetComponent<BoxCollider>(),Is.Null);
            Assert.That(part.GetComponent<Renderer>().enabled,Is.False);
            Assert.That(part.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
            float expectedHeight=shape==R3ArmorShape.HalfBlock ? .25f : shape==R3ArmorShape.HalfWedge ? .245f : .52f;
            Assert.That(collider.bounds.size.y,Is.EqualTo(expectedHeight).Within(.02f));
            var data=JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(model.MakeCheckpoint(Time.time)));
            Assert.That(R3VehicleModel.TryRestoreCheckpoint(data,R3VehicleKind.Standard,model.Economy,Time.time,out var restored,out var error),Is.True,error);
            Assert.That(restored.Parts.Single(p=>p.Id==id).ArmorShape,Is.EqualTo(shape));
            Assert.That(run.GarageRemove(cell),Is.True);yield return null;
            Assert.That(model.Inventory.Single(p=>p.Id==id).ArmorShape,Is.EqualTo(shape));
        }
    }

    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
