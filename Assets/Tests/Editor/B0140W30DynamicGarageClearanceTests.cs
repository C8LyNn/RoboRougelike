// B01-40 W30 independent receiver test on EXACT #877. Real Unity PlayMode and PhysX.
// This is NOT a new garage placement authority; the original live model gate is tested.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;

public sealed class B0140W30DynamicGarageClearanceTests
{
    [UnityTest]
    public IEnumerator RealGarage_ReevaluatesMovingOwnedCollider_RejectsWithoutSpending_ThenMounts()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline=Time.realtimeSinceStartup+20f;
        while(R1RunController.Instance?.Progress==null && Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run, Is.Not.Null, "real R1 game bootstrap required");
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);

        var model=run.VehicleModel;
        var original=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
        string id=original.Id;
        Assert.That(run.GarageRemove(original.Cell), Is.True);
        var stored=model.Inventory.Single(p=>p.Id==id);
        stored.Weapon=R4WeaponKind.RotarySaw;
        var exterior=new Vector3Int(0,1,4);
        Assert.That(model.OwnedPlacementError(id,exterior,default), Is.Null,
            "initial connected exterior saw position must be physically mountable");
        Assert.That(run.GarageMount(id,exterior,default), Is.True);

        // Capture the actual R3 saw-disc center while it is correctly installed.
        // Unlike #877's fixed-body test, this fixture then REMOVES the gun and
        // moves a separate owned Rigidbody collision into/out of its placement.
        var rig=run.Player.GetComponent<R3VehicleAssembly>();
        var saw=rig.Parts.Single(p=>p.Id==id);
        Assert.That(saw.Muzzle, Is.Not.Null, "actual WPN005 saw-disc endpoint");
        Vector3 discLocal=run.Player.transform.InverseTransformPoint(saw.Muzzle.position);
        Assert.That(run.GarageRemove(exterior), Is.True);
        model=run.VehicleModel;
        int wallet=model.Scrap;
        int mountedCount=model.Parts.Count;
        int inventoryCount=model.Inventory.Count;

        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name="B01-40 W30: owner Rigidbody moving into LIVE saw envelope";
        blocker.transform.SetParent(run.Player.transform,false);
        blocker.transform.localScale=Vector3.one*.12f;
        var physical=blocker.AddComponent<Rigidbody>();
        physical.isKinematic=true;
        physical.useGravity=false;
        var center=run.Player.transform.TransformPoint(discLocal);
        var safelyAway=center+run.Player.transform.right*12f;
        blocker.transform.position=safelyAway;
        Physics.SyncTransforms();
        Assert.That(model.OwnedPlacementError(id,exterior,default),Is.Null,
            "far-away physical collider must not incorrectly forbid connected saw mount");

        physical.MovePosition(center);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();
        string issue=model.OwnedPlacementError(id,exterior,default);
        Assert.That(issue,Is.Not.Null.And.Not.Empty,
            "moving owner-child Rigidbody at saw disc must invalidate the same preview gate");
        Assert.That(issue,Does.Contain("Fioletowa").Or.Contain("Fizyczny"),
            "rejection should name the actual weapon envelope or physical collision");
        Assert.That(run.GarageMount(id,exterior,default),Is.False,
            "real canonical Mount must reject the same live physical obstruction");
        Assert.That(model.Scrap,Is.EqualTo(wallet),"failed Mount must not debit Scrap");
        Assert.That(model.Parts.Count,Is.EqualTo(mountedCount),"failed Mount must not create parts");
        Assert.That(model.Inventory.Count,Is.EqualTo(inventoryCount),"failed Mount must not remove inventory");
        Assert.That(model.Inventory.Single(p=>p.Id==id).Id,Is.EqualTo(id),
            "failed Mount must preserve the exact original part ID");

        physical.MovePosition(safelyAway);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();
        Assert.That(model.OwnedPlacementError(id,exterior,default),Is.Null,
            "real physical obstruction removed: owner gate must recover");
        Assert.That(run.GarageMount(id,exterior,default),Is.True,
            "a real connected saw mount must recover without a competing validator");
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(wallet));
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Id==id).Cell,Is.EqualTo(exterior));
        Assert.That(run.VehicleModel.Parts.Count(p=>p.Id==id),Is.EqualTo(1),
            "no duplicate original part ID after blocked→allowed transition");
        if(blocker!=null)Object.Destroy(blocker);
        yield return new ExitPlayMode();
    }
}
