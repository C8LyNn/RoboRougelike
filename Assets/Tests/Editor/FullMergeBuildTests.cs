using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4_1;

public sealed class FullMergeBuildTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        yield return null;yield return null;
    }
    [UnityTest] public IEnumerator DirectBuyCannotUseGunAsStructuralScaffoldingOrSpendOnRefusal()
    {
        var model=run.VehicleModel;var gun=model.Parts.Single(p=>p.Kind==R3PartKind.Gun);
        var cell=gun.Cell+Vector3Int.up;Assert.That(model.At(cell),Is.Null);
        int money=model.Scrap;var ids=model.Parts.Select(p=>p.Id).ToArray();
        Assert.That(model.Buy(cell,R3PartKind.Armor),Is.False,"Direct model API must respect the actual bound garage validator");
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(model.Parts.Select(p=>p.Id),Is.EqualTo(ids));
        yield return null;
    }
    [UnityTest] public IEnumerator DirectMountCannotMoveInventoryOntoGunSupport()
    {
        var model=run.VehicleModel;var stored=new R3PartState(default,R3PartKind.Wheel);
        Assert.That(model.ReceiveSalvage(new[]{stored}),Is.True);
        var cell=model.Parts.Single(p=>p.Kind==R3PartKind.Gun).Cell+Vector3Int.up;
        int money=model.Scrap;var ids=model.Parts.Select(p=>p.Id).ToArray();
        Assert.That(model.Mount(stored.Id,cell),Is.False);
        Assert.That(model.Inventory.Single().Id,Is.EqualTo(stored.Id));Assert.That(model.Scrap,Is.EqualTo(money));
        Assert.That(model.Parts.Select(p=>p.Id),Is.EqualTo(ids));yield return null;
    }
    [UnityTest] public IEnumerator ForeignPhysicalObstacleRejectsGridValidPlacementWithoutSpending()
    {
        var cell=new Vector3Int(1,2,1);var model=run.VehicleModel;
        Assert.That(model.PlacementError(cell,R3PartKind.Armor),Is.Null,"Canonical logical precondition for physical overlap test");
        var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="FM270 external physical blocker";
        obstacle.transform.position=run.Player.transform.TransformPoint((Vector3)cell*.55f);
        obstacle.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
        int money=model.Scrap;var ids=model.Parts.Select(p=>p.Id).ToArray();
        Assert.That(run.GarageBuy(cell,R3PartKind.Armor),Is.False,"Actual colliders must be consulted before canonical spend");
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(model.Parts.Select(p=>p.Id),Is.EqualTo(ids));
        Object.Destroy(obstacle);yield return null;
    }
    [UnityTest] public IEnumerator WheelHitCannotAuthorizeMountingStructuralParts()
    {
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0);
        var ray=new Ray(wheel.transform.TransformPoint(new Vector3(-4,0,0)),wheel.transform.TransformDirection(Vector3.right));
        Physics.SyncTransforms();var hits=Physics.RaycastAll(ray,4,~0,QueryTriggerInteraction.Collide)
            .Where(h=>h.collider.GetComponentInParent<R3MountedPart>()!=null).OrderBy(h=>h.distance).ToArray();
        Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].collider.GetComponentInParent<R3MountedPart>().Kind,Is.EqualTo(R3PartKind.Wheel));
        Assert.That(run.Garage.Point(ray),Is.False,"Wheel hub/tire is not a structural socket owner");
        Assert.That(run.Garage.HasTarget,Is.False);yield return null;
    }
    [UnityTest] public IEnumerator RealArmorFaceRemainsClickableAfterNonQuarterVehicleYaw()
    {
        var root=run.Player.transform;root.rotation=Quaternion.Euler(0,37,0);
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==new Vector3Int(2,1,2));
        var ray=new Ray(armor.transform.TransformPoint(Vector3.right*4),armor.transform.TransformDirection(Vector3.left));
        Physics.SyncTransforms();Assert.That(run.Garage.Point(ray),Is.True);
        Assert.That(run.Garage.Cell,Is.EqualTo(armor.Cell+Vector3Int.right));yield return null;
    }
    [UnityTest] public IEnumerator RotatedWideOwnedPartUsesActualGeometryAndKeepsOneIdentity()
    {
        run.Player.transform.rotation=Quaternion.Euler(0,37,0);
        var model=run.VehicleModel;var part=new R3PartState(default,R3PartKind.BonusModule)
            {UpgradeId="fm270-wide",UpgradeEffect=R1UpgradeEffect.Damage,UpgradeAmount=.25f};
        Assert.That(model.ReceiveSalvage(new[]{part}),Is.True);int money=model.Scrap;
        Assert.That(run.GarageMount(part.Id,new Vector3Int(1,3,0),new Vector3Int(1,0,0)),Is.True);
        yield return null;var mounted=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==part.Id);
        Assert.That(model.Inventory,Is.Empty);Assert.That(model.Parts.Count(p=>p.Id==part.Id),Is.EqualTo(1));
        Assert.That(mounted.GetComponentsInChildren<R3MountedPart>().Length,Is.EqualTo(1));
        Assert.That(model.Scrap,Is.EqualTo(money));Assert.That(Quaternion.Angle(run.Player.transform.rotation,Quaternion.Euler(0,37,0)),Is.LessThan(.01f));
    }
    [UnityTest] public IEnumerator StructuralFaceRemainsClickableAfterSuccessfulGarageRebuild()
    {
        Assert.That(run.GarageBuy(new Vector3Int(1,2,1),R3PartKind.Armor),Is.True);yield return null;yield return null;
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Cell==new Vector3Int(2,1,2));
        var ray=new Ray(armor.transform.TransformPoint(Vector3.right*4),armor.transform.TransformDirection(Vector3.left));
        Physics.SyncTransforms();Assert.That(run.Garage.Point(ray),Is.True,"A successful edit must rebuild authorized hit surfaces too");
        Assert.That(run.Garage.Cell,Is.EqualTo(armor.Cell+Vector3Int.right));
    }
    [UnityTest] public IEnumerator ActualWheelFitsFreeAxleBesideStructuralArmor()
    {
        var wheel=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Wheel);
        Debug.Log("[Full270 wheel geometry] "+string.Join(",",wheel.GetComponentsInChildren<Collider>().Select(c=>c.GetType().Name+":"+c.transform.localScale)));
        Assert.That(run.GarageBuy(new Vector3Int(3,0,3),R3PartKind.Wheel),Is.True,run.VehicleModel.PlacementError(new Vector3Int(3,0,3),R3PartKind.Wheel));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}
